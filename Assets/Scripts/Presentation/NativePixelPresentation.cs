using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>
    /// Native-pixel compositor for MovementLab. The gameplay camera renders the logical
    /// world target; one lightweight utility camera drives RenderGraph passes for optional
    /// logical penumbra resolve and final point-sampled physical presentation.
    /// </summary>
    [DefaultExecutionOrder(1000)]
    [DisallowMultipleComponent]
    public sealed class NativePixelPresentation : MonoBehaviour
    {
        public const int PixelsPerUnit = 16;
        public const int FullyVisibleRadiusPixels = 456;
        public const int PenumbraThicknessPixels = 64;
        public const int FullDarknessRadiusPixels = 520;
        public const int UtilityRendererIndex = 1;

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int LogicalSizeId = Shader.PropertyToID("_LogicalSize");
        private static readonly int PlayerPixelCenterId = Shader.PropertyToID("_PlayerPixelCenter");
        private static readonly int WorldPixelOriginId = Shader.PropertyToID("_WorldPixelOrigin");
        private static readonly int FullVisibleRadiusId = Shader.PropertyToID("_FullVisibleRadius");
        private static readonly int FullDarknessRadiusId = Shader.PropertyToID("_FullDarknessRadius");
        private static readonly int PenumbraEnabledId = Shader.PropertyToID("_PenumbraEnabled");
        private static readonly int PaletteId = Shader.PropertyToID("_Palette");
        private static readonly int DarknessLutId = Shader.PropertyToID("_DarknessLut");

        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera processingCamera;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Shader penumbraShader;
        [SerializeField] private Shader presentationShader;
        [SerializeField] private bool penumbraEnabled = true;

        private readonly Vector4[] _palette = new Vector4[RustlinePalette.ColorCount];
        private readonly Vector4[] _darknessLut =
            new Vector4[RustlinePalette.ColorCount * RustlinePalette.DarknessLevelCount];

        private NativePixelViewport _viewport;
        private RenderTexture _worldTarget;
        private RenderTexture _penumbraTarget;
        private Material _penumbraMaterial;
        private Material _presentationMaterial;

        public NativePixelViewport Viewport => _viewport;
        public bool PenumbraEnabled => penumbraEnabled;
        public bool HasAllocatedTargets => _worldTarget != null && _penumbraTarget != null;
        public RenderTexture WorldTarget => _worldTarget;
        public RenderTexture ResolvedTarget => _penumbraTarget;
        public Camera WorldCamera => worldCamera;
        public Camera ProcessingCamera => processingCamera;
        public Transform PlayerTarget => playerTarget;
        public Shader PenumbraShader => penumbraShader;
        public Shader PresentationShader => presentationShader;
        public Texture PresentedSource => _presentationMaterial != null
            ? _presentationMaterial.GetTexture(MainTexId)
            : null;

        private void OnEnable()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            ApplyCanonicalCameraClearColors();
            ConfigureDriverCamera();

            RustlinePalette.CopyLinearShaderData(_palette, _darknessLut);
            _penumbraMaterial = CreateRuntimeMaterial(
                penumbraShader,
                "Rustline Palette Penumbra - Runtime");
            _presentationMaterial = CreateRuntimeMaterial(
                presentationShader,
                "Rustline Native Pixel Present - Runtime");

            _penumbraMaterial.SetVectorArray(PaletteId, _palette);
            _penumbraMaterial.SetVectorArray(DarknessLutId, _darknessLut);
            _penumbraMaterial.SetFloat(FullVisibleRadiusId, FullyVisibleRadiusPixels);
            _penumbraMaterial.SetFloat(FullDarknessRadiusId, FullDarknessRadiusPixels);
            _penumbraMaterial.SetFloat(PenumbraEnabledId, 1f);

            RefreshViewportAndTargets();
            UpdatePenumbraParameters();
            ApplyPenumbraState();
        }

        private void OnDisable()
        {
            RustlineNativePixelPresentFeature.Clear(processingCamera);

            if (worldCamera != null && worldCamera.targetTexture == _worldTarget)
            {
                worldCamera.targetTexture = null;
            }

            if (processingCamera != null)
            {
                processingCamera.targetTexture = null;
                processingCamera.enabled = false;
            }

            ReleaseTarget(ref _worldTarget);
            ReleaseTarget(ref _penumbraTarget);
            DestroyRuntimeObject(ref _penumbraMaterial);
            DestroyRuntimeObject(ref _presentationMaterial);
        }

        private void LateUpdate()
        {
            if (_viewport.PhysicalWidth != Screen.width || _viewport.PhysicalHeight != Screen.height)
            {
                RefreshViewportAndTargets();
            }

            UpdatePenumbraParameters();
        }

        public void TogglePenumbra()
        {
            SetPenumbraEnabled(!penumbraEnabled);
        }

        public void SetPenumbraEnabled(bool enabled)
        {
            if (penumbraEnabled == enabled)
            {
                return;
            }

            penumbraEnabled = enabled;
            ApplyPenumbraState();
        }

        private void ApplyCanonicalCameraClearColors()
        {
            // Camera clear colors are authored display values here. Feeding #01020B through
            // Color.linear before Camera.backgroundColor made the camera surround too dark.
            Color deepSpace = (Color)RustlinePalette.DeepSpace;

            if (worldCamera != null)
            {
                worldCamera.backgroundColor = deepSpace;
            }

            if (processingCamera != null)
            {
                processingCamera.backgroundColor = deepSpace;
            }
        }

        private void ConfigureDriverCamera()
        {
            processingCamera.GetUniversalAdditionalCameraData().SetRenderer(UtilityRendererIndex);

            // The utility camera only drives the custom RenderGraph feature. It intentionally
            // performs no scene-object rendering or culling work.
            processingCamera.cullingMask = 0;
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

            worldCamera.targetTexture = _worldTarget;
            worldCamera.orthographicSize = nextViewport.LogicalHeight / (2f * PixelsPerUnit);

            // With no target texture this camera's URP target is the physical display. The
            // final RenderGraph pass owns the Deep Space clear and centered integer output.
            processingCamera.targetTexture = null;
            processingCamera.clearFlags = CameraClearFlags.Nothing;
            processingCamera.orthographicSize = nextViewport.PhysicalHeight * 0.5f;

            ApplyPenumbraState();
        }

        private void RecreateTargets(int width, int height)
        {
            if (worldCamera.targetTexture == _worldTarget)
            {
                worldCamera.targetTexture = null;
            }

            ReleaseTarget(ref _worldTarget);
            ReleaseTarget(ref _penumbraTarget);

            // Keep descriptors unchanged while Experiment 2 is being measured so camera
            // elimination remains the isolated performance variable. The resolved target is
            // no longer a camera output and can be made depthless in a later focused change.
            _worldTarget = CreateTarget(width, height, "Rustline World - Logical");
            _penumbraTarget = CreateTarget(width, height, "Rustline Penumbra - Logical");
            _penumbraMaterial.SetTexture(MainTexId, _worldTarget);
        }

        private void UpdatePenumbraParameters()
        {
            if (_penumbraMaterial == null || playerTarget == null || worldCamera == null)
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
        }

        private void ApplyPenumbraState()
        {
            if (_presentationMaterial == null)
            {
                return;
            }

            bool usePenumbra = penumbraEnabled && _penumbraTarget != null;

            // The driver camera remains active in both modes. Penumbra OFF skips only the
            // logical effect pass and presents the raw world target directly.
            processingCamera.enabled = true;
            _presentationMaterial.SetTexture(
                MainTexId,
                usePenumbra ? _penumbraTarget : _worldTarget);

            RustlineNativePixelPresentFeature.Configure(
                processingCamera,
                _worldTarget,
                _penumbraTarget,
                _penumbraMaterial,
                _presentationMaterial,
                _viewport,
                usePenumbra);
        }

        private static Material CreateRuntimeMaterial(Shader shader, string materialName)
        {
            return new Material(shader)
            {
                name = materialName,
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        private static RenderTexture CreateTarget(int width, int height, string targetName)
        {
            RenderTexture target = new RenderTexture(
                width,
                height,
                16,
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

        private static void DestroyRuntimeObject<T>(ref T target) where T : Object
        {
            if (target == null)
            {
                return;
            }

            Destroy(target);
            target = null;
        }
    }
}
