using UnityEngine;
using UnityEngine.Rendering;

namespace Rustline.Presentation
{
    /// <summary>
    /// MovementLab-only native pixel compositor. The world is rendered once at logical
    /// resolution, palette-darkened there, then point-presented into an integral screen rect.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public sealed class NativePixelPresentation : MonoBehaviour
    {
        public const int PixelsPerUnit = 16;
        public const int FullyVisibleRadiusPixels = 456;
        public const int PenumbraThicknessPixels = 64;
        public const int FullDarknessRadiusPixels = 520;

        private static readonly int LogicalSizeId = Shader.PropertyToID("_LogicalSize");
        private static readonly int PlayerPixelCenterId = Shader.PropertyToID("_PlayerPixelCenter");
        private static readonly int WorldPixelOriginId = Shader.PropertyToID("_WorldPixelOrigin");
        private static readonly int FullVisibleRadiusId = Shader.PropertyToID("_FullVisibleRadius");
        private static readonly int FullDarknessRadiusId = Shader.PropertyToID("_FullDarknessRadius");
        private static readonly int PenumbraEnabledId = Shader.PropertyToID("_PenumbraEnabled");
        private static readonly int PaletteId = Shader.PropertyToID("_Palette");
        private static readonly int DarknessLutId = Shader.PropertyToID("_DarknessLut");

        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera presentationCamera;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Shader penumbraShader;
        [SerializeField] private bool penumbraEnabled = true;

        private readonly Vector4[] _palette = new Vector4[RustlinePalette.ColorCount];
        private readonly Vector4[] _darknessLut =
            new Vector4[RustlinePalette.ColorCount * RustlinePalette.DarknessLevelCount];

        private NativePixelViewport _viewport;
        private RenderTexture _worldTarget;
        private RenderTexture _penumbraTarget;
        private Material _penumbraMaterial;
        private CommandBuffer _penumbraCommands;
        private bool _subscribed;

        public NativePixelViewport Viewport => _viewport;
        public bool PenumbraEnabled => penumbraEnabled;
        public bool HasAllocatedTargets => _worldTarget != null && _penumbraTarget != null;
        public RenderTexture WorldTarget => _worldTarget;
        public RenderTexture ResolvedTarget => _penumbraTarget;
        public Camera WorldCamera => worldCamera;
        public Camera PresentationCamera => presentationCamera;
        public Transform PlayerTarget => playerTarget;
        public Shader PenumbraShader => penumbraShader;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            RustlinePalette.CopyLinearShaderData(_palette, _darknessLut);
            _penumbraMaterial = new Material(penumbraShader)
            {
                name = "Rustline Palette Penumbra - Runtime",
                hideFlags = HideFlags.HideAndDontSave
            };
            _penumbraMaterial.SetVectorArray(PaletteId, _palette);
            _penumbraMaterial.SetVectorArray(DarknessLutId, _darknessLut);
            _penumbraMaterial.SetFloat(FullVisibleRadiusId, FullyVisibleRadiusPixels);
            _penumbraMaterial.SetFloat(FullDarknessRadiusId, FullDarknessRadiusPixels);

            _penumbraCommands = new CommandBuffer { name = "Rustline Logical Penumbra" };
            RenderPipelineManager.endCameraRendering += HandleEndCameraRendering;
            _subscribed = true;
            RefreshViewportAndTargets();
        }

        private void OnDisable()
        {
            if (_subscribed)
            {
                RenderPipelineManager.endCameraRendering -= HandleEndCameraRendering;
                _subscribed = false;
            }

            if (worldCamera != null && worldCamera.targetTexture == _worldTarget)
            {
                worldCamera.targetTexture = null;
            }

            ReleaseTarget(ref _worldTarget);
            ReleaseTarget(ref _penumbraTarget);

            if (_penumbraCommands != null)
            {
                _penumbraCommands.Release();
                _penumbraCommands = null;
            }

            if (_penumbraMaterial != null)
            {
                Destroy(_penumbraMaterial);
                _penumbraMaterial = null;
            }
        }

        private void Update()
        {
            if (_viewport.PhysicalWidth != Screen.width || _viewport.PhysicalHeight != Screen.height)
            {
                RefreshViewportAndTargets();
            }
        }

        private void OnGUI()
        {
            if (Event.current.type != EventType.Repaint || _penumbraTarget == null)
            {
                return;
            }

            Color previousColor = GUI.color;
            GUI.color = Color.white;
            GUI.DrawTexture(
                new Rect(
                    _viewport.OutputOffsetX,
                    _viewport.OutputOffsetY,
                    _viewport.OutputWidth,
                    _viewport.OutputHeight),
                _penumbraTarget,
                ScaleMode.StretchToFill,
                false);
            GUI.color = previousColor;
        }

        public void TogglePenumbra()
        {
            penumbraEnabled = !penumbraEnabled;
        }

        private void RefreshViewportAndTargets()
        {
            int screenWidth = Mathf.Max(Screen.width, 1);
            int screenHeight = Mathf.Max(Screen.height, 1);
            NativePixelViewport nextViewport = NativePixelViewportMath.Calculate(screenWidth, screenHeight);
            bool targetSizeChanged = _worldTarget == null ||
                                     _worldTarget.width != nextViewport.LogicalWidth ||
                                     _worldTarget.height != nextViewport.LogicalHeight;
            _viewport = nextViewport;

            if (targetSizeChanged)
            {
                RecreateTargets(nextViewport.LogicalWidth, nextViewport.LogicalHeight);
            }

            if (worldCamera != null)
            {
                worldCamera.targetTexture = _worldTarget;
                worldCamera.orthographicSize = nextViewport.LogicalHeight / (2f * PixelsPerUnit);
            }
        }

        private void RecreateTargets(int width, int height)
        {
            if (worldCamera != null && worldCamera.targetTexture == _worldTarget)
            {
                worldCamera.targetTexture = null;
            }

            ReleaseTarget(ref _worldTarget);
            ReleaseTarget(ref _penumbraTarget);
            // Unity 6 URP RenderGraph requires a depth attachment on a Camera output texture,
            // even though Rustline's Renderer2D depth/stencil feature remains disabled.
            _worldTarget = CreateTarget(width, height, 16, "Rustline World - Logical");
            _penumbraTarget = CreateTarget(width, height, 0, "Rustline Penumbra - Logical");
        }

        private static RenderTexture CreateTarget(int width, int height, int depthBits, string targetName)
        {
            RenderTexture target = new RenderTexture(
                width,
                height,
                depthBits,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = targetName,
                antiAliasing = 1,
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            target.Create();
            return target;
        }

        private void HandleEndCameraRendering(ScriptableRenderContext context, Camera renderedCamera)
        {
            if (renderedCamera != worldCamera || _worldTarget == null || _penumbraTarget == null ||
                _penumbraMaterial == null || playerTarget == null)
            {
                return;
            }

            Vector3 playerViewport = worldCamera.WorldToViewportPoint(playerTarget.position);
            float cameraPixelX = worldCamera.transform.position.x * PixelsPerUnit;
            float cameraPixelY = worldCamera.transform.position.y * PixelsPerUnit;
            int worldOriginX = Mathf.FloorToInt(cameraPixelX - _viewport.LogicalWidth * 0.5f);
            int worldOriginY = Mathf.FloorToInt(cameraPixelY - _viewport.LogicalHeight * 0.5f);

            _penumbraMaterial.SetVector(
                LogicalSizeId,
                new Vector4(_viewport.LogicalWidth, _viewport.LogicalHeight, 0f, 0f));
            _penumbraMaterial.SetVector(
                PlayerPixelCenterId,
                new Vector4(
                    playerViewport.x * _viewport.LogicalWidth,
                    playerViewport.y * _viewport.LogicalHeight,
                    0f,
                    0f));
            _penumbraMaterial.SetVector(
                WorldPixelOriginId,
                new Vector4(worldOriginX, worldOriginY, 0f, 0f));
            _penumbraMaterial.SetFloat(PenumbraEnabledId, penumbraEnabled ? 1f : 0f);

            _penumbraCommands.Clear();
            _penumbraCommands.Blit(_worldTarget, _penumbraTarget, _penumbraMaterial, 0);
            context.ExecuteCommandBuffer(_penumbraCommands);
        }

        private static void ReleaseTarget(ref RenderTexture target)
        {
            if (target == null)
            {
                return;
            }

            target.Release();
            Destroy(target);
            target = null;
        }
    }
}
