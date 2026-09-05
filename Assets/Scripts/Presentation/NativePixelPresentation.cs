using Unity.Profiling;
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
        public const int WorldTargetDepthBits = 16;
        public const int ResolvedTargetDepthBits = 0;

        private static readonly ProfilerMarker NativePixelUpdateMarker =
            new ProfilerMarker("Rustline.Presentation.NativePixelUpdate");

        private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
        private static readonly int LogicalSizeId = Shader.PropertyToID("_LogicalSize");
        private static readonly int PlayerPixelCenterId = Shader.PropertyToID("_PlayerPixelCenter");
        private static readonly int WorldPixelOriginId = Shader.PropertyToID("_WorldPixelOrigin");
        private static readonly int FullVisibleRadiusId = Shader.PropertyToID("_FullVisibleRadius");
        private static readonly int FullDarknessRadiusId = Shader.PropertyToID("_FullDarknessRadius");
        private static readonly int PenumbraEnabledId = Shader.PropertyToID("_PenumbraEnabled");
        private static readonly int DarknessLookupId = Shader.PropertyToID("_DarknessLookup");
        private static readonly int DeepSpaceColorId = Shader.PropertyToID("_DeepSpaceColor");
        private static readonly int SourceScaleBiasId = Shader.PropertyToID("_SourceScaleBias");

        [SerializeField] private Camera worldCamera;
        [SerializeField] private Camera processingCamera;
        [SerializeField] private Transform playerTarget;
        [SerializeField] private Shader penumbraShader;
        [SerializeField] private Shader presentationShader;
        [SerializeField] private bool penumbraEnabled = true;

        private NativePixelViewport _viewport;
        private RenderTexture _worldTarget;
        private RenderTexture _penumbraTarget;
        private Texture2D _darknessLookupTexture;
        private Material _penumbraMaterial;
        private Material _presentationMaterial;
        private bool _hasLogicalSize;
        private bool _hasPlayerPixelCenter;
        private bool _hasWorldPixelOrigin;
        private Vector4 _lastLogicalSize;
        private Vector4 _lastPlayerPixelCenter;
        private Vector4 _lastWorldPixelOrigin;
        private bool _hasPenumbraInputs;
        private Vector3 _lastPlayerWorldPosition;
        private Vector3 _lastWorldCameraPosition;
        private Quaternion _lastWorldCameraRotation;
        private float _lastWorldCameraOrthographicSize;
        private float _lastWorldCameraAspect;
        private Rect _lastWorldCameraRect;
        private int _lastPenumbraLogicalWidth;
        private int _lastPenumbraLogicalHeight;

        public NativePixelViewport Viewport => _viewport;
        public bool PenumbraEnabled => penumbraEnabled;
        public bool HasAllocatedTargets => _worldTarget != null && _penumbraTarget != null;
        public RenderTexture WorldTarget => _worldTarget;
        public RenderTexture ResolvedTarget => _penumbraTarget;
        public Texture2D DarknessLookupTexture => _darknessLookupTexture;
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

            _penumbraMaterial = CreateRuntimeMaterial(
                penumbraShader,
                "Rustline Palette Penumbra - Runtime");
            _presentationMaterial = CreateRuntimeMaterial(
                presentationShader,
                "Rustline Native Pixel Present - Runtime");

            _darknessLookupTexture = CreateDarknessLookupTexture();
            _penumbraMaterial.SetTexture(DarknessLookupId, _darknessLookupTexture);
            _penumbraMaterial.SetColor(
                DeepSpaceColorId,
                ((Color)RustlinePalette.DeepSpace).linear);
            _penumbraMaterial.SetVector(
                SourceScaleBiasId,
                SystemInfo.graphicsUVStartsAtTop
                    ? new Vector4(1f, -1f, 0f, 1f)
                    : new Vector4(1f, 1f, 0f, 0f));
            _presentationMaterial.SetVector(
                SourceScaleBiasId,
                new Vector4(1f, 1f, 0f, 0f));
            _penumbraMaterial.SetFloat(FullVisibleRadiusId, FullyVisibleRadiusPixels);
            _penumbraMaterial.SetFloat(FullDarknessRadiusId, FullDarknessRadiusPixels);
            _penumbraMaterial.SetFloat(PenumbraEnabledId, 1f);

            InvalidatePenumbraParameterCaches();
            RefreshViewportAndTargets();
            UpdatePenumbraParameters();
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
            DestroyRuntimeObject(ref _darknessLookupTexture);
            DestroyRuntimeObject(ref _penumbraMaterial);
            DestroyRuntimeObject(ref _presentationMaterial);
        }

        private void LateUpdate()
        {
            using (NativePixelUpdateMarker.Auto())
            {
                if (_viewport.PhysicalWidth != Screen.width || _viewport.PhysicalHeight != Screen.height)
                {
                    RefreshViewportAndTargets();
                }

                UpdatePenumbraParameters();
            }
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

            SetLogicalSizeIfChanged(new Vector4(
                nextViewport.LogicalWidth,
                nextViewport.LogicalHeight,
                0f,
                0f));

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

            // Unity 6.4 RenderGraph requires a camera output RenderTexture to carry a depth
            // format even though Renderer2D and current effects do not consume scene depth.
            // The resolved RenderGraph-only target remains safely depthless.
            _worldTarget = CreateTarget(
                width,
                height,
                WorldTargetDepthBits,
                "Rustline World - Logical");
            _penumbraTarget = CreateTarget(
                width,
                height,
                ResolvedTargetDepthBits,
                "Rustline Penumbra - Logical");
            _penumbraMaterial.SetTexture(MainTexId, _worldTarget);
            InvalidatePenumbraParameterCaches();
        }

        private void UpdatePenumbraParameters()
        {
            if (_penumbraMaterial == null || playerTarget == null || worldCamera == null)
            {
                return;
            }

            Vector3 playerWorldPosition = playerTarget.position;
            Transform cameraTransform = worldCamera.transform;
            Vector3 cameraPosition = cameraTransform.position;
            Quaternion cameraRotation = cameraTransform.rotation;
            float orthographicSize = worldCamera.orthographicSize;
            float cameraAspect = worldCamera.aspect;
            Rect cameraRect = worldCamera.rect;
            if (_hasPenumbraInputs &&
                _lastPlayerWorldPosition.Equals(playerWorldPosition) &&
                _lastWorldCameraPosition.Equals(cameraPosition) &&
                _lastWorldCameraRotation.Equals(cameraRotation) &&
                _lastWorldCameraOrthographicSize == orthographicSize &&
                _lastWorldCameraAspect == cameraAspect &&
                _lastWorldCameraRect.Equals(cameraRect) &&
                _lastPenumbraLogicalWidth == _viewport.LogicalWidth &&
                _lastPenumbraLogicalHeight == _viewport.LogicalHeight)
            {
                return;
            }

            _lastPlayerWorldPosition = playerWorldPosition;
            _lastWorldCameraPosition = cameraPosition;
            _lastWorldCameraRotation = cameraRotation;
            _lastWorldCameraOrthographicSize = orthographicSize;
            _lastWorldCameraAspect = cameraAspect;
            _lastWorldCameraRect = cameraRect;
            _lastPenumbraLogicalWidth = _viewport.LogicalWidth;
            _lastPenumbraLogicalHeight = _viewport.LogicalHeight;
            _hasPenumbraInputs = true;

            Vector3 playerViewport = worldCamera.WorldToViewportPoint(playerWorldPosition);
            float cameraPixelX = cameraPosition.x * PixelsPerUnit;
            float cameraPixelY = cameraPosition.y * PixelsPerUnit;
            int worldOriginX = Mathf.FloorToInt(cameraPixelX - _viewport.LogicalWidth * 0.5f);
            int worldOriginY = Mathf.FloorToInt(cameraPixelY - _viewport.LogicalHeight * 0.5f);

            Vector4 playerPixelCenter = new Vector4(
                playerViewport.x * _viewport.LogicalWidth,
                playerViewport.y * _viewport.LogicalHeight,
                0f,
                0f);
            if (!_hasPlayerPixelCenter || !playerPixelCenter.Equals(_lastPlayerPixelCenter))
            {
                _penumbraMaterial.SetVector(PlayerPixelCenterId, playerPixelCenter);
                _lastPlayerPixelCenter = playerPixelCenter;
                _hasPlayerPixelCenter = true;
            }

            Vector4 worldPixelOrigin = new Vector4(worldOriginX, worldOriginY, 0f, 0f);
            if (!_hasWorldPixelOrigin || !worldPixelOrigin.Equals(_lastWorldPixelOrigin))
            {
                _penumbraMaterial.SetVector(WorldPixelOriginId, worldPixelOrigin);
                _lastWorldPixelOrigin = worldPixelOrigin;
                _hasWorldPixelOrigin = true;
            }
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

        private void SetLogicalSizeIfChanged(Vector4 logicalSize)
        {
            if (_penumbraMaterial == null ||
                _hasLogicalSize && logicalSize.Equals(_lastLogicalSize))
            {
                return;
            }

            _penumbraMaterial.SetVector(LogicalSizeId, logicalSize);
            _lastLogicalSize = logicalSize;
            _hasLogicalSize = true;
        }

        private static Texture2D CreateDarknessLookupTexture()
        {
            // linear:false creates an sRGB texture, so stored Canonical Color32 values are
            // decoded back to linear RGB when the linear-project shader samples them.
            Texture2D texture = new Texture2D(
                RustlinePalette.DarknessLookupWidth,
                RustlinePalette.DarknessLookupHeight,
                TextureFormat.RGBA32,
                false,
                false)
            {
                name = "Rustline Canonical Darkness Lookup - Runtime",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixels32(RustlinePalette.CreateDarknessLookupPixels());
            texture.Apply(false, true);
            return texture;
        }

        private void InvalidatePenumbraParameterCaches()
        {
            _hasLogicalSize = false;
            _hasPlayerPixelCenter = false;
            _hasWorldPixelOrigin = false;
            _hasPenumbraInputs = false;
        }

        private static RenderTexture CreateTarget(
            int width,
            int height,
            int depthBits,
            string targetName)
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
