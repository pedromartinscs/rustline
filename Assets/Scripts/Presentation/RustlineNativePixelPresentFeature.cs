using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>
    /// Experimental M1B presentation feature. The lightweight utility camera drives two
    /// explicit RenderGraph passes: optional logical penumbra resolve, then point-sampled
    /// presentation into the camera backbuffer. This removes the need for a separate
    /// physical presentation camera while keeping the expensive effect at logical size.
    /// </summary>
    public sealed class RustlineNativePixelPresentFeature : ScriptableRendererFeature
    {
        private const string PenumbraPassName = "Rustline Logical Penumbra";
        private const string PresentPassName = "Rustline Native Pixel Present";

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int SourceScaleBiasId = Shader.PropertyToID("_SourceScaleBias");

        private static Camera s_DriverCamera;
        private static RenderTexture s_WorldTarget;
        private static RenderTexture s_ResolvedTarget;
        private static Material s_PenumbraMaterial;
        private static Material s_PresentationMaterial;
        private static NativePixelViewport s_Viewport;
        private static bool s_PenumbraEnabled;

        private NativePixelPresentPass _pass;

        public static bool IsConfigured =>
            s_DriverCamera != null &&
            s_WorldTarget != null &&
            s_ResolvedTarget != null &&
            s_PenumbraMaterial != null &&
            s_PresentationMaterial != null;

        public static void Configure(
            Camera driverCamera,
            RenderTexture worldTarget,
            RenderTexture resolvedTarget,
            Material penumbraMaterial,
            Material presentationMaterial,
            NativePixelViewport viewport,
            bool penumbraEnabled)
        {
            s_DriverCamera = driverCamera;
            s_WorldTarget = worldTarget;
            s_ResolvedTarget = resolvedTarget;
            s_PenumbraMaterial = penumbraMaterial;
            s_PresentationMaterial = presentationMaterial;
            s_Viewport = viewport;
            s_PenumbraEnabled = penumbraEnabled;
        }

        public static void Clear(Camera driverCamera)
        {
            if (s_DriverCamera != driverCamera)
            {
                return;
            }

            s_DriverCamera = null;
            s_WorldTarget = null;
            s_ResolvedTarget = null;
            s_PenumbraMaterial = null;
            s_PresentationMaterial = null;
            s_Viewport = default;
            s_PenumbraEnabled = false;
        }

        public override void Create()
        {
            _pass?.Dispose();
            _pass = new NativePixelPresentPass
            {
                renderPassEvent = RenderPassEvent.AfterRendering
            };
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_pass == null || !IsConfigured || renderingData.cameraData.camera != s_DriverCamera)
            {
                return;
            }

            renderer.EnqueuePass(_pass);
        }

        protected override void Dispose(bool disposing)
        {
            _pass?.Dispose();
            _pass = null;
            base.Dispose(disposing);
        }

        private sealed class NativePixelPresentPass : ScriptableRenderPass
        {
            private RTHandle _worldHandle;
            private RTHandle _resolvedHandle;

            public void Dispose()
            {
                _worldHandle?.Release();
                _worldHandle = null;
                _resolvedHandle?.Release();
                _resolvedHandle = null;
            }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
            {
                if (!IsConfigured)
                {
                    return;
                }

                EnsureHandle(ref _worldHandle, s_WorldTarget, "Rustline World - Imported");
                EnsureHandle(ref _resolvedHandle, s_ResolvedTarget, "Rustline Penumbra - Imported");

                TextureHandle worldSource = renderGraph.ImportTexture(
                    _worldHandle,
                    CreateRenderTargetInfo(s_WorldTarget));
                TextureHandle selectedSource = worldSource;
                RenderTexture selectedTexture = s_WorldTarget;

                if (s_PenumbraEnabled)
                {
                    TextureHandle resolvedTarget = renderGraph.ImportTexture(
                        _resolvedHandle,
                        CreateRenderTargetInfo(s_ResolvedTarget));
                    RecordPenumbraPass(renderGraph, worldSource, resolvedTarget, s_WorldTarget);
                    selectedSource = resolvedTarget;
                    selectedTexture = s_ResolvedTarget;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle backBuffer = resourceData.backBufferColor;
                if (!backBuffer.IsValid())
                {
                    return;
                }

                RecordPresentationPass(renderGraph, selectedSource, backBuffer, selectedTexture);
                resourceData.SwitchActiveTexturesToBackbuffer();
            }

            private static void RecordPenumbraPass(
                RenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination,
                RenderTexture sourceTexture)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           PenumbraPassName,
                           out var passData))
                {
                    passData.source = source;
                    passData.destination = destination;
                    passData.sourceTexture = sourceTexture;
                    passData.material = s_PenumbraMaterial;
                    passData.viewport = new Rect(0f, 0f, s_Viewport.LogicalWidth, s_Viewport.LogicalHeight);

                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        ConfigureSourceSampling(data, context);
                        context.cmd.SetViewport(data.viewport);
                        context.cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, data.material, 0, 0);
                    });
                }
            }

            private static void RecordPresentationPass(
                RenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination,
                RenderTexture sourceTexture)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           PresentPassName,
                           out var passData))
                {
                    passData.source = source;
                    passData.destination = destination;
                    passData.sourceTexture = sourceTexture;
                    passData.material = s_PresentationMaterial;
                    passData.clearColor = (Color)RustlinePalette.DeepSpace;
                    passData.viewport = new Rect(
                        s_Viewport.OutputOffsetX,
                        s_Viewport.OutputOffsetY,
                        s_Viewport.OutputWidth,
                        s_Viewport.OutputHeight);

                    builder.UseTexture(source, AccessFlags.Read);
                    // This pass deterministically defines the entire physical target: clear
                    // once to canonical Deep Space, then overwrite only the centered integer
                    // output rectangle with the selected point-sampled logical image.
                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(false, true, data.clearColor);
                        ConfigureSourceSampling(data, context);
                        context.cmd.SetViewport(data.viewport);
                        context.cmd.DrawMesh(RenderingUtils.fullscreenMesh, Matrix4x4.identity, data.material, 0, 0);
                    });
                }
            }

            private static void ConfigureSourceSampling(PassData data, RasterGraphContext context)
            {
                bool flip = context.GetTextureUVOrigin(data.source) !=
                            context.GetTextureUVOrigin(data.destination);

                // Keep TextureHandle for RenderGraph dependency/orientation tracking only.
                // Binding the known persistent RenderTexture directly avoids the implicit
                // TextureHandle -> Texture conversion assertion in Unity 6 RenderGraph.
                data.material.SetTexture(MainTexId, data.sourceTexture);
                data.material.SetVector(
                    SourceScaleBiasId,
                    flip
                        ? new Vector4(1f, -1f, 0f, 1f)
                        : new Vector4(1f, 1f, 0f, 0f));
            }

            private static RenderTargetInfo CreateRenderTargetInfo(RenderTexture target)
            {
                return new RenderTargetInfo
                {
                    format = target.graphicsFormat,
                    width = target.width,
                    height = target.height,
                    bindMS = target.bindTextureMS,
                    msaaSamples = Mathf.Max(target.antiAliasing, 1),
                    volumeDepth = target.volumeDepth
                };
            }

            private static void EnsureHandle(
                ref RTHandle handle,
                RenderTexture target,
                string handleName)
            {
                if (handle != null && handle.rt == target)
                {
                    return;
                }

                handle?.Release();
                handle = target != null ? RTHandles.Alloc(target, handleName) : null;
            }

            private sealed class PassData
            {
                public TextureHandle source;
                public TextureHandle destination;
                public RenderTexture sourceTexture;
                public Material material;
                public Color clearColor;
                public Rect viewport;
            }
        }
    }
}
