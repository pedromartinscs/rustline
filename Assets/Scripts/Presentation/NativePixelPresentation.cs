using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>
    /// MovementLab-only native pixel compositor. The gameplay camera renders the logical
    /// world target; a lightweight utility camera drives RenderGraph passes for optional
    /// logical penumbra resolve and final point-sampled physical presentation.
    ///
    /// The old physical presentation camera/quad remain serialized during Experiment 2
    /// for trivial rollback, but are disabled at runtime and do not participate in rendering.
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
        [SerializeField] private Camera presentationCamera;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private MeshRenderer processingRenderer;
        [SerializeField] private MeshRenderer presentationRenderer;
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
        public Camera PresentationCamera => presentationCamera;
        public Transform PlayerTarget => playerTarget;
        public MeshRenderer ProcessingRenderer => processingRenderer;
        public MeshRenderer PresentationRenderer => presentationRenderer;
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
            ConfigureUtilityRenderers();

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

            // Keep the old quad materials wired during the experiment so rollback remains
            // mechanical. The renderers themselves are disabled by ApplyPenumbraState().
            processingRenderer.sharedMaterial = _penumbraMaterial;
            presentationRenderer.sharedMaterial = _presentationMaterial;

            ConfigureLayerIsolation();
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

            if (presentationCamera != null)
            {
                presentationCamera.enabled = false;
            }

            if (processingRenderer != null)
            {
                processingRenderer.enabled = false;
                processingRenderer.sharedMaterial = null;
            }

            if (presentationRenderer != null)
            {
                presentationRenderer.enabled = false;
                presentationRenderer.sharedMaterial = null;
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
            penumbraEnabled = !penumbraEnabled;
            ApplyPenumbraState();
        }

        private void ApplyCanonicalCameraClearColors()
        {
            // Camera clear colors are presentation values here. Feeding the gamma-authored
            // canonical color through Color.linear made #01020B effectively collapse to black.
            Color deepSpace = (Color)RustlinePalette.DeepSpace;

            if (worldCamera != null)
            {
                worldCamera.backgroundColor = deepSpace;
            }

            if (processingCamera != null)
            {
                processingCamera.backgroundColor = deepSpace;
            }

            if (presentationCamera != null)
            {
                presentationCamera.backgroundColor = deepSpace;
            }
        }

        private void ConfigureUtilityRenderers()
        {
            // Experiment 2 needs only the processing/driver camera. Keep the disabled
            // presentation camera assigned to the utility renderer so rollback is trivial.
            SelectUtilityRenderer(processingCamera);
            SelectUtilityRenderer(presentationCamera);
        }

        private static void SelectUtilityRenderer(Camera camera)
        {
            if (camera == null)
            {
                return;
            }

            camera.GetUniversalAdditionalCameraData().SetRenderer(UtilityRendererIndex);
        }

        private void ConfigureLayerIsolation()
        {
            int processingMask = 1 << processingRenderer.gameObject.layer;
            int presentationMask = 1 << presentationRenderer.gameObject.layer;
            worldCamera.cullingMask &= ~(processingMask | presentationMask);

            // The utility camera is now only a RenderGraph driver. It intentionally culls
            // no scene geometry; both old fullscreen quads are disabled at runtime.
            processingCamera.cullingMask = 0;
            presentationCamera.cullingMask = presentationMask;
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

            // With no target texture this camera's URP backbuffer is the physical display.
            // It does not clear through the Camera path: the final RenderGraph pass owns one
            // deterministic Deep Space clear before drawing the centered output rectangle.
            processingCamera.targetTexture = null;
            processingCamera.clearFlags = CameraClearFlags.Nothing;
            processingCamera.orthographicSize = nextViewport.PhysicalHeight * 0.5f;

            // Retain deterministic fallback geometry during the reversible experiment.
            processingRenderer.transform.localPosition = Vector3.zero;
            processingRenderer.transform.localScale = new Vector3(
                nextViewport.LogicalWidth,
                nextViewport.LogicalHeight,
                1f);

            presentationCamera.orthographicSize = nextViewport.PhysicalHeight * 0.5f;
            presentationRenderer.transform.localPosition = new Vector3(
                nextViewport.OutputOffsetX + nextViewport.OutputWidth * 0.5f - nextViewport.PhysicalWidth * 0.5f,
                nextViewport.OutputOffsetY + nextViewport.OutputHeight * 0.5f - nextViewport.PhysicalHeight * 0.5f,
                0f);
            presentationRenderer.transform.localScale = new Vector3(
                nextViewport.OutputWidth,
                nextViewport.OutputHeight,
                1f);

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

            // Keep both target descriptors identical during the experiment so the measured
            // variable is the camera/presentation path. If Experiment 2 is accepted, the
            // resolved target can be reconsidered separately because it is no longer a
            // camera output and therefore no longer inherently needs a depth attachment.
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

            // The Processing Camera must remain enabled even when the penumbra is OFF: it
            // now drives the final RenderGraph presentation pass. The expensive logical
            // penumbra pass itself is skipped by the renderer feature when OFF.
            processingCamera.enabled = true;
            processingRenderer.enabled = false;
            presentationCamera.enabled = false;
            presentationRenderer.enabled = false;

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
