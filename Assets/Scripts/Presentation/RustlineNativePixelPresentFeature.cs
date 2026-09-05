using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>
    /// Native-pixel presentation feature. One lightweight utility camera drives two
    /// explicit RenderGraph passes: optional logical penumbra resolve, then point-sampled
    /// presentation into the camera backbuffer.
    /// </summary>
    public sealed class RustlineNativePixelPresentFeature : ScriptableRendererFeature
    {
        private const string PenumbraPassName = "Rustline Logical Penumbra";
        private const string PresentPassName = "Rustline Native Pixel Present";

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

                if (s_PenumbraEnabled)
                {
                    TextureHandle resolvedTarget = renderGraph.ImportTexture(
                        _resolvedHandle,
                        CreateRenderTargetInfo(s_ResolvedTarget));
                    RecordPenumbraPass(renderGraph, worldSource, resolvedTarget);
                    selectedSource = resolvedTarget;
                }

                UniversalResourceData resourceData = frameData.Get<UniversalResourceData>();
                TextureHandle backBuffer = resourceData.backBufferColor;
                if (!backBuffer.IsValid())
                {
                    return;
                }

                RecordPresentationPass(renderGraph, selectedSource, backBuffer);
                resourceData.SwitchActiveTexturesToBackbuffer();
            }

            private static void RecordPenumbraPass(
                RenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           PenumbraPassName,
                           out var passData))
                {
                    passData.material = s_PenumbraMaterial;
                    passData.viewport = new Rect(0f, 0f, s_Viewport.LogicalWidth, s_Viewport.LogicalHeight);

                    // The persistent texture is bound by NativePixelPresentation; this
                    // TextureHandle keeps RenderGraph dependency/lifetime tracking explicit.
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.SetViewport(data.viewport);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            0,
                            MeshTopology.Triangles,
                            3,
                            1);
                    });
                }
            }

            private static void RecordPresentationPass(
                RenderGraph renderGraph,
                TextureHandle source,
                TextureHandle destination)
            {
                using (var builder = renderGraph.AddRasterRenderPass<PassData>(
                           PresentPassName,
                           out var passData))
                {
                    passData.material = s_PresentationMaterial;
                    // Raster commands write linear values to the sRGB backbuffer. Supplying
                    // authored display-space #01020B directly would be encoded again.
                    passData.clearColor = ((Color)RustlinePalette.DeepSpace).linear;
                    passData.viewport = new Rect(
                        s_Viewport.OutputOffsetX,
                        s_Viewport.OutputOffsetY,
                        s_Viewport.OutputWidth,
                        s_Viewport.OutputHeight);

                    // Material state changes only with target/toggle state, while this handle
                    // remains the authoritative RenderGraph read dependency.
                    builder.UseTexture(source, AccessFlags.Read);
                    builder.SetRenderAttachment(destination, 0, AccessFlags.WriteAll);
                    builder.SetRenderFunc(static (PassData data, RasterGraphContext context) =>
                    {
                        context.cmd.ClearRenderTarget(false, true, data.clearColor);
                        context.cmd.SetViewport(data.viewport);
                        context.cmd.DrawProcedural(
                            Matrix4x4.identity,
                            data.material,
                            0,
                            MeshTopology.Triangles,
                            3,
                            1);
                    });
                }
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
                public Material material;
                public Color clearColor;
                public Rect viewport;
            }
        }
    }
}
