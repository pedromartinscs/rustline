using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Rustline.Presentation
{
    /// <summary>Logical-pixel HUD renderer for the equipment authority's carousel steps.</summary>
    [DefaultExecutionOrder(900)]
    [DisallowMultipleComponent]
    public sealed class WeaponCarouselHud2D : MonoBehaviour
    {
        private const int HudLayer = 5; // Project's named UI layer.
        private const int CardWidthPixels = 360;
        private const int CardHeightPixels = 175;
        [SerializeField] private PlayerWeaponEquipment2D equipment;
        [SerializeField] private Shader paletteFadeShader;
        [SerializeField, Range(0.1f, 1f)] private float selectedScale = 0.60f;
        [SerializeField, Range(0.1f, 1f)] private float neighborScale = 0.48f;
        [SerializeField, Min(0)] private int lowerLeftMarginPixels = 16;
        [SerializeField, Min(1)] private int neighborVerticalSeparationPixels = 86;

        private NativePixelPresentation _presentation;
        private Camera _camera;
        private Transform _root;
        private RenderTexture _target;
        private readonly CardView[] _views = new CardView[4];
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
                DrawStep(equipment.StepProgress, equipment.StepDirection, equipment.SelectedSlot);
                _wasStepping = true;
            }
            else if (_wasStepping)
            {
                DrawRest(equipment.SelectedSlot);
                _wasStepping = false;
            }

            // The target remains composited while its camera is disabled. Enable only
            // for a dirty frame or a live transition; URP renders it later this frame.
            _camera.enabled = _hudDirty || equipment.IsStepActive;
            _hudDirty = false;
        }

        private void OnStepStarted(int fromSlot, int _, WeaponCarouselDirection2D direction)
        {
            if (_presentation != null && _views[0] != null)
            {
                DrawStep(0f, direction, fromSlot);
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
            if (_target != null && _lastLogicalWidth == viewport.LogicalWidth &&
                _lastLogicalHeight == viewport.LogicalHeight)
            {
                return;
            }
            Release();
            _lastLogicalWidth = viewport.LogicalWidth;
            _lastLogicalHeight = viewport.LogicalHeight;
            _target = new RenderTexture(viewport.LogicalWidth, viewport.LogicalHeight, 0, RenderTextureFormat.ARGB32,
                RenderTextureReadWrite.sRGB)
            {
                name = "Rustline Weapon Carousel - Logical HUD",
                filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp, antiAliasing = 1,
                useMipMap = false, autoGenerateMips = false, anisoLevel = 0,
                hideFlags = HideFlags.HideAndDontSave
            };
            _target.Create();
            GameObject cameraObject = new GameObject("Weapon Carousel HUD Camera") { hideFlags = HideFlags.HideAndDontSave };
            _camera = cameraObject.AddComponent<Camera>();
            _camera.orthographic = true;
            _camera.clearFlags = CameraClearFlags.SolidColor;
            _camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            _camera.cullingMask = 1 << HudLayer;
            _camera.allowHDR = false;
            _camera.allowMSAA = false;
            _camera.targetTexture = _target;
            _camera.enabled = false;
            _camera.depth = _presentation.WorldCamera.depth + 0.25f;
            cameraObject.AddComponent<UniversalAdditionalCameraData>();
            _camera.orthographicSize = viewport.LogicalHeight / (2f * NativePixelPresentation.PixelsPerUnit);
            _camera.transform.position = new Vector3(
                viewport.LogicalWidth / (2f * NativePixelPresentation.PixelsPerUnit),
                viewport.LogicalHeight / (2f * NativePixelPresentation.PixelsPerUnit), -10f);
            _root = new GameObject("Weapon Carousel HUD Views") { hideFlags = HideFlags.HideAndDontSave }.transform;
            CreateViews();
            _presentation.SetWeaponCarouselHudSource(_target);
            DrawRest(equipment.SelectedSlot);
        }

        private void CreateViews()
        {
            for (int index = 0; index < _views.Length; index++)
            {
                GameObject viewObject = new GameObject("Weapon Carousel Card " + index) { layer = HudLayer, hideFlags = HideFlags.HideAndDontSave };
                viewObject.transform.SetParent(_root, false);
                SpriteRenderer renderer = viewObject.AddComponent<SpriteRenderer>();
                renderer.sortingOrder = index;
                Material material = new Material(paletteFadeShader) { hideFlags = HideFlags.HideAndDontSave };
                material.SetTexture("_DarknessLookup", _presentation.DarknessLookupTexture);
                renderer.sharedMaterial = material;
                renderer.enabled = false;
                _views[index] = new CardView { Renderer = renderer, Material = material, Slot = -1 };
            }
        }

        private void DrawRest(int centerSlot)
        {
            _hudDirty = true;
            if (!TryGetNeighbors(centerSlot, out int upper, out int lower))
            {
                HideAll();
                return;
            }
            if (equipment.AvailableSlots.Count == 1)
            {
                SetCard(1, centerSlot, NeighborPosition(0), selectedScale, 1f, 1);
                Hide(0); Hide(2); Hide(3);
                return;
            }
            if (equipment.AvailableSlots.Count == 2)
            {
                SetCard(0, upper, NeighborPosition(+1), neighborScale, 1f, 0);
                SetCard(1, centerSlot, NeighborPosition(0), selectedScale, 1f, 1);
                Hide(2); Hide(3);
                return;
            }
            SetCard(0, upper, NeighborPosition(+1), neighborScale, 1f, 0);
            SetCard(1, centerSlot, NeighborPosition(0), selectedScale, 1f, 1);
            SetCard(2, lower, NeighborPosition(-1), neighborScale, 1f, 2);
            Hide(3);
        }

        private void DrawStep(float progress, WeaponCarouselDirection2D direction, int centerSlot)
        {
            _hudDirty = true;
            if (!TryGetNeighbors(centerSlot, out int upper, out int lower))
            {
                HideAll();
                return;
            }
            if (equipment.AvailableSlots.Count < 3)
            {
                // A two-entry ring has no distinct outgoing/incoming neighbors; keep the
                // one other card as the moving counterpart instead of duplicating it.
                int other = upper;
                SetCard(0, centerSlot, Lerp(NeighborPosition(0), NeighborPosition(-1), progress), Lerp(selectedScale, neighborScale, progress), 1f, 0);
                SetCard(1, other, Lerp(NeighborPosition(+1), NeighborPosition(0), progress), Lerp(neighborScale, selectedScale, progress), 1f, 1);
                Hide(2); Hide(3);
                return;
            }
            if (direction == WeaponCarouselDirection2D.Successor)
            {
                WeaponCarouselTransitionSlots2D slots = WeaponCarouselTransition2D.GetSlots(equipment.AvailableSlots, centerSlot, direction);
                SetCard(0, slots.OutgoingNeighbor, NeighborPosition(-1), neighborScale, 1f - progress, 0);
                SetCard(1, slots.MovingCenter, Lerp(NeighborPosition(0), NeighborPosition(-1), progress), Lerp(selectedScale, neighborScale, progress), 1f, 1);
                SetCard(2, slots.MovingNeighbor, Lerp(NeighborPosition(+1), NeighborPosition(0), progress), Lerp(neighborScale, selectedScale, progress), 1f, 2);
                SetCard(3, slots.IncomingNeighbor, NeighborPosition(+1), neighborScale, progress, 3);
            }
            else
            {
                WeaponCarouselTransitionSlots2D slots = WeaponCarouselTransition2D.GetSlots(equipment.AvailableSlots, centerSlot, direction);
                SetCard(0, slots.OutgoingNeighbor, NeighborPosition(+1), neighborScale, 1f - progress, 0);
                SetCard(1, slots.MovingCenter, Lerp(NeighborPosition(0), NeighborPosition(+1), progress), Lerp(selectedScale, neighborScale, progress), 1f, 1);
                SetCard(2, slots.MovingNeighbor, Lerp(NeighborPosition(-1), NeighborPosition(0), progress), Lerp(neighborScale, selectedScale, progress), 1f, 2);
                SetCard(3, slots.IncomingNeighbor, NeighborPosition(-1), neighborScale, progress, 3);
            }
        }

        private bool TryGetNeighbors(int centerSlot, out int upper, out int lower)
        {
            upper = lower = -1;
            if (centerSlot < 0 || equipment.AvailableSlots.Count == 0)
            {
                return false;
            }
            upper = WeaponSelectionNavigation2D.GetAdjacentSlot(equipment.AvailableSlots, centerSlot, WeaponCarouselDirection2D.Successor);
            lower = WeaponSelectionNavigation2D.GetAdjacentSlot(equipment.AvailableSlots, centerSlot, WeaponCarouselDirection2D.Predecessor);
            return true;
        }

        private void SetCard(int index, int slot, Vector2 logicalPosition, float scale, float fade, int sortingOrder)
        {
            CardView view = _views[index];
            if (!equipment.TryGetEntry(slot, out WeaponLoadoutEntry2D entry) || entry.CarouselSprite == null || fade <= 0f)
            {
                Hide(index);
                return;
            }
            view.Slot = slot;
            view.Renderer.sprite = entry.CarouselSprite;
            view.Renderer.sortingOrder = sortingOrder;
            view.Renderer.transform.localPosition = new Vector3(
                Mathf.Round(logicalPosition.x) / NativePixelPresentation.PixelsPerUnit,
                Mathf.Round(logicalPosition.y) / NativePixelPresentation.PixelsPerUnit,
                0f);
            view.Renderer.transform.localScale = Vector3.one * scale;
            view.Material.SetFloat("_Fade", fade);
            view.Renderer.enabled = true;
        }

        private Vector2 NeighborPosition(int level)
        {
            return CalculateCardBounds(
                level,
                selectedScale,
                neighborScale,
                lowerLeftMarginPixels,
                neighborVerticalSeparationPixels).center;
        }

        /// <summary>Pure logical-pixel layout used by runtime and focused EditMode tests.</summary>
        public static Rect CalculateCardBounds(
            int level,
            float selectedCardScale,
            float neighborCardScale,
            int bottomLeftMarginPixels,
            int neighborSeparationPixels)
        {
            float scale = level == 0 ? selectedCardScale : neighborCardScale;
            float width = CardWidthPixels * scale;
            float height = CardHeightPixels * scale;
            float lowerNeighborCenterY = bottomLeftMarginPixels +
                CardHeightPixels * neighborCardScale * 0.5f;
            // level -1 is the lower neighbor. Starting it at its half-height above
            // the configured margin preserves the full card rather than clipping it.
            float centerY = lowerNeighborCenterY + (level + 1) * neighborSeparationPixels;
            return new Rect(bottomLeftMarginPixels, centerY - height * 0.5f, width, height);
        }
        private static Vector2 Lerp(Vector2 from, Vector2 to, float t) => Vector2.Lerp(from, to, t);
        private static float Lerp(float from, float to, float t) => Mathf.Lerp(from, to, t);
        private void Hide(int index) { if (_views[index] != null) _views[index].Renderer.enabled = false; }
        private void HideAll() { for (int index = 0; index < _views.Length; index++) Hide(index); }
        private void Release()
        {
            if (_root != null) Destroy(_root.gameObject);
            for (int index = 0; index < _views.Length; index++)
            {
                if (_views[index]?.Material != null) Destroy(_views[index].Material);
                _views[index] = null;
            }
            if (_camera != null) Destroy(_camera.gameObject);
            if (_target != null) { _target.Release(); Destroy(_target); }
            _root = null; _camera = null; _target = null;
        }
    }
}
