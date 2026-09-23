using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Rustline.Presentation
{
    /// <summary>Logical-pixel HUD renderer for the equipment authority's weapon selector.</summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class WeaponCarouselHud2D : MonoBehaviour
    {
        public const string RustlineHudLayerName = "RustlineHUD";
        public const int RustlineHudLayerIndex = 8;
        public const int ReusableViewCount = 2;

        private const int CardWidthPixels = 360;
        private const int CardHeightPixels = 175;

        [SerializeField] private PlayerWeaponEquipment2D equipment;
        [SerializeField] private Shader paletteFadeShader;
        [FormerlySerializedAs("selectedScale")]
        [SerializeField, Range(0.1f, 1f)] private float cardScale = 0.60f;
        [SerializeField, Min(0)] private int lowerLeftMarginPixels = 16;
        [SerializeField, Min(0)] private int visualGapPixels = 20;
        [SerializeField, Min(1)] private int penumbraThicknessPixels = 20;

        private NativePixelPresentation _presentation;
        private Camera _camera;
        private Transform _root;
        private RenderTexture _target;
        private readonly CardView[] _views = new CardView[ReusableViewCount];
        private bool _wasStepping;
        private bool _hudDirty;
        private int _lastLogicalWidth;
        private int _lastLogicalHeight;

        private sealed class CardView
        {
            internal SpriteRenderer Renderer;
            internal Material Material;
            internal int Slot;
        }

        private void Awake()
        {
            if (equipment == null)
            {
                equipment = GetComponent<PlayerWeaponEquipment2D>();
            }
        }

        private void OnEnable()
        {
            if (equipment != null)
            {
                equipment.StepStarted += OnStepStarted;
                equipment.EquipmentChanged += OnEquipmentChanged;
            }
        }

        private void OnDisable()
        {
            if (equipment != null)
            {
                equipment.StepStarted -= OnStepStarted;
                equipment.EquipmentChanged -= OnEquipmentChanged;
            }

            _presentation?.SetWeaponCarouselHudSource(null);
            Release();
        }

        private void LateUpdate()
        {
            EnsurePresentation();
            if (_presentation == null || equipment == null)
            {
                return;
            }

            NativePixelViewport viewport = _presentation.Viewport;
            if (viewport.LogicalWidth <= 0 || viewport.LogicalHeight <= 0)
            {
                return;
            }

            EnsureTarget(viewport);
            if (_camera == null)
            {
                return;
            }

            if (equipment.IsStepActive)
            {
                DrawStep(
                    equipment.StepProgress,
                    equipment.StepDirection,
                    equipment.SelectedSlot,
                    equipment.StepIncomingSlot);
                _wasStepping = true;
            }
            else if (_wasStepping)
            {
                DrawRest(equipment.SelectedSlot);
                _wasStepping = false;
            }

            // The target remains composited while its camera is disabled. Render only a
            // dirty resting frame or the live transition frames.
            _camera.enabled = _hudDirty || equipment.IsStepActive;
            _hudDirty = false;
        }

        private void OnStepStarted(
            int fromSlot,
            int incomingSlot,
            WeaponCarouselDirection2D direction)
        {
            if (_presentation != null && _views[0] != null)
            {
                DrawStep(0f, direction, fromSlot, incomingSlot);
            }
        }

        private void OnEquipmentChanged(WeaponLoadoutEntry2D _)
        {
            if (!equipment.IsStepActive && _views[0] != null)
            {
                DrawRest(equipment.SelectedSlot);
            }
        }

        private void EnsurePresentation()
        {
            if (_presentation == null)
            {
                _presentation = FindAnyObjectByType<NativePixelPresentation>();
            }
        }

        private void EnsureTarget(NativePixelViewport viewport)
        {
            if (_target != null &&
                _lastLogicalWidth == viewport.LogicalWidth &&
                _lastLogicalHeight == viewport.LogicalHeight)
            {
                return;
            }

            int hudLayer = ResolveHudLayer();
            if (hudLayer < 0)
            {
                return;
            }

            Release();
            _lastLogicalWidth = viewport.LogicalWidth;
            _lastLogicalHeight = viewport.LogicalHeight;

            _target = new RenderTexture(
                viewport.LogicalWidth,
                viewport.LogicalHeight,
                0,
                RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = "Rustline Weapon Carousel - Logical HUD",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                antiAliasing = 1,
                useMipMap = false,
                autoGenerateMips = false,
                anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            _target.Create();

            GameObject cameraObject = new GameObject("Weapon Carousel HUD Camera")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.cullingMask = 1 << hudLayer;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.targetTexture = _target;
            _camera.enabled = false;
            _camera.depth = _presentation.WorldCamera.depth + 0.25f;

            // Deterministic scene setup owns this serialized contract. Keep a runtime
            // exclusion as a defensive guard for independently authored scenes.
            _presentation.WorldCamera.cullingMask &= ~(1 << hudLayer);

            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            _camera.orthographicSize =
                viewport.LogicalHeight / (2f * NativePixelPresentation.PixelsPerUnit);
            _camera.transform.position = new Vector3(
                viewport.LogicalWidth / (2f * NativePixelPresentation.PixelsPerUnit),
                viewport.LogicalHeight / (2f * NativePixelPresentation.PixelsPerUnit),
                -10f);

            _root = new GameObject("Weapon Carousel HUD Views")
            {
                hideFlags = HideFlags.HideAndDontSave
            }.transform;

            CreateViews();
            _presentation.SetWeaponCarouselHudSource(_target);
            DrawRest(equipment.SelectedSlot);
        }

        private void CreateViews()
        {
            for (int index = 0; index < _views.Length; index++)
            {
                GameObject viewObject = new GameObject("Weapon Carousel Card " + index)
                {
                    layer = RustlineHudLayerIndex,
                    hideFlags = HideFlags.HideAndDontSave
                };
                viewObject.transform.SetParent(_root, false);

                SpriteRenderer renderer = viewObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = index;

                Material material = new Material(paletteFadeShader)
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                material.SetTexture("_DarknessLookup", _presentation.DarknessLookupTexture);
                material.SetFloat("_PixelsPerUnit", NativePixelPresentation.PixelsPerUnit);
                renderer.sharedMaterial = material;
                renderer.enabled = false;

                _views[index] = new CardView
                {
                    Renderer = renderer,
                    Material = material,
                    Slot = -1
                };
            }
        }

        private void DrawRest(int centerSlot)
        {
            _hudDirty = true;
            Rect restingBounds = CalculateRestingCardBounds(cardScale, lowerLeftMarginPixels);
            ConfigureSpatialPenumbra(restingBounds);

            SetCard(0, centerSlot, restingBounds.center);
            Hide(1);
        }

        private void DrawStep(
            float progress,
            WeaponCarouselDirection2D direction,
            int outgoingSlot,
            int incomingSlot)
        {
            _hudDirty = true;
            if (outgoingSlot < 0 || incomingSlot < 0)
            {
                HideAll();
                return;
            }

            Rect restingBounds = CalculateRestingCardBounds(cardScale, lowerLeftMarginPixels);
            ConfigureSpatialPenumbra(restingBounds);

            float stepDistance = CalculateStepDistance(cardScale, visualGapPixels);
            Vector2 centerYs = CalculateTransitionCenterYs(
                restingBounds.center.y,
                stepDistance,
                progress,
                direction);

            SetCard(
                0,
                outgoingSlot,
                new Vector2(restingBounds.center.x, centerYs.x));
            SetCard(
                1,
                incomingSlot,
                new Vector2(restingBounds.center.x, centerYs.y));
        }

        private void ConfigureSpatialPenumbra(Rect restingBounds)
        {
            float thickness = Mathf.Max(1f, penumbraThicknessPixels);
            for (int index = 0; index < _views.Length; index++)
            {
                Material material = _views[index]?.Material;
                if (material == null)
                {
                    continue;
                }

                material.SetFloat("_ApertureBottom", restingBounds.yMin);
                material.SetFloat("_ApertureTop", restingBounds.yMax);
                material.SetFloat("_PenumbraThickness", thickness);
            }
        }

        private void SetCard(int index, int slot, Vector2 logicalPosition)
        {
            CardView view = _views[index];
            if (view == null ||
                !equipment.TryGetEntry(slot, out WeaponLoadoutEntry2D entry) ||
                entry.CarouselSprite == null)
            {
                Hide(index);
                return;
            }

            Rect restingBounds = CalculateRestingCardBounds(cardScale, lowerLeftMarginPixels);
            Vector2 quantizedPosition = QuantizeAroundRestingCenter(
                logicalPosition,
                restingBounds.center);

            view.Slot = slot;
            view.Renderer.sprite = entry.CarouselSprite;
            view.Renderer.sortingOrder = index;
            view.Renderer.transform.localPosition = new Vector3(
                quantizedPosition.x / NativePixelPresentation.PixelsPerUnit,
                quantizedPosition.y / NativePixelPresentation.PixelsPerUnit,
                0f);
            view.Renderer.transform.localScale = Vector3.one * cardScale;
            view.Renderer.enabled = true;
        }

        public static int DedicatedHudCullingMask => 1 << RustlineHudLayerIndex;

        /// <summary>Resting full-card bounds in logical HUD pixels.</summary>
        public static Rect CalculateRestingCardBounds(
            float presentationScale,
            int bottomLeftMarginPixels)
        {
            float scale = Mathf.Max(0f, presentationScale);
            return new Rect(
                bottomLeftMarginPixels,
                bottomLeftMarginPixels,
                CardWidthPixels * scale,
                CardHeightPixels * scale);
        }

        /// <summary>
        /// Center separation for a transition. Card height plus the explicit gap makes
        /// rectangle overlap impossible by construction.
        /// </summary>
        public static float CalculateStepDistance(
            float presentationScale,
            int gapPixels)
        {
            return CardHeightPixels * Mathf.Max(0f, presentationScale) +
                Mathf.Max(0, gapPixels);
        }

        /// <summary>
        /// Returns (outgoingY, incomingY). Both cards travel the same signed distance,
        /// so their center separation remains constant for the full step.
        /// </summary>
        public static Vector2 CalculateTransitionCenterYs(
            float restingCenterY,
            float stepDistance,
            float progress,
            WeaponCarouselDirection2D direction)
        {
            float t = Mathf.Clamp01(progress);
            float outgoingSign =
                direction == WeaponCarouselDirection2D.Successor ? -1f : 1f;

            float outgoingY = Mathf.Lerp(
                restingCenterY,
                restingCenterY + outgoingSign * stepDistance,
                t);
            float incomingY = Mathf.Lerp(
                restingCenterY - outgoingSign * stepDistance,
                restingCenterY,
                t);
            return new Vector2(outgoingY, incomingY);
        }

        /// <summary>
        /// Spatial distance from the fully visible aperture: 0 inside/on it, 1 at an
        /// outer penumbra edge, and >1 beyond the visible penumbra.
        /// </summary>
        public static float CalculateSpatialPenumbraDistance(
            float logicalY,
            float apertureBottom,
            float apertureTop,
            float penumbraThickness)
        {
            if (logicalY >= apertureBottom && logicalY <= apertureTop)
            {
                return 0f;
            }

            float thickness = Mathf.Max(0.0001f, penumbraThickness);
            return logicalY > apertureTop
                ? (logicalY - apertureTop) / thickness
                : (apertureBottom - logicalY) / thickness;
        }

        private static Vector2 QuantizeAroundRestingCenter(
            Vector2 logicalPosition,
            Vector2 restingCenter)
        {
            return new Vector2(
                restingCenter.x + Mathf.Round(logicalPosition.x - restingCenter.x),
                restingCenter.y + Mathf.Round(logicalPosition.y - restingCenter.y));
        }

        private static int ResolveHudLayer()
        {
            int layer = LayerMask.NameToLayer(RustlineHudLayerName);
            if (layer == RustlineHudLayerIndex)
            {
                return layer;
            }

            Debug.LogError(
                $"{RustlineHudLayerName} must exist at layer {RustlineHudLayerIndex}; " +
                "weapon selector HUD was not created.");
            return -1;
        }

        private void Hide(int index)
        {
            if (_views[index] != null)
            {
                _views[index].Renderer.enabled = false;
            }
        }

        private void HideAll()
        {
            for (int index = 0; index < _views.Length; index++)
            {
                Hide(index);
            }
        }

        private void Release()
        {
            if (_root != null)
            {
                Destroy(_root.gameObject);
            }

            for (int index = 0; index < _views.Length; index++)
            {
                if (_views[index]?.Material != null)
                {
                    Destroy(_views[index].Material);
                }
                _views[index] = null;
            }

            if (_camera != null)
            {
                Destroy(_camera.gameObject);
            }

            if (_target != null)
            {
                _target.Release();
                Destroy(_target);
            }

            _root = null;
            _camera = null;
            _target = null;
        }
    }
}
