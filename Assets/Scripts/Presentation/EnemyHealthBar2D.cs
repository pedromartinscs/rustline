using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Read-only, native-pixel combat presentation; no Canvas or gameplay writes.</summary>
    [DefaultExecutionOrder(350)]
    [DisallowMultipleComponent]
    public sealed class EnemyHealthBar2D : MonoBehaviour
    {
        public const int FillPixels = 24;
        public const float HeadHeight = 52f / 16f;
        public const float InactivitySeconds = 2.5f;
        [SerializeField] private Material barMaterial;
        private static Sprite _pixel;
        private static int _users;
        private CombatHealth2D _health;
        private Sentry2D _sentry;
        private Bombardier2D _bombardier;
        private Transform _anchor;
        private SpriteRenderer _backing, _well, _fill;
        private float _visibleUntil = float.NegativeInfinity;
        private int _filledPixels = -1;
        private bool _registered;

        public bool IsVisible => _backing != null && _backing.enabled;
        public int FilledPixels => _filledPixels;
        public Vector3 AnchorPosition => _anchor != null ? _anchor.position : transform.position;

        private void Awake()
        {
            _health = GetComponent<CombatHealth2D>();
            _sentry = GetComponent<Sentry2D>();
            _bombardier = GetComponent<Bombardier2D>();
            if (_health == null || (_sentry == null && _bombardier == null) || barMaterial == null)
            {
                Debug.LogError("Enemy health bar requires an enemy, health and shared unlit material.", this);
                enabled = false;
                return;
            }
            if (_pixel == null)
            {
                // Reuse Unity's white texture: one shared sprite, no new texture allocation.
                _pixel = Sprite.Create(Texture2D.whiteTexture, new Rect(0,0,1,1), Vector2.zero,
                    16f, 0, SpriteMeshType.FullRect);
                _pixel.name = "Enemy Health Bar - Shared Pixel";
                _pixel.hideFlags = HideFlags.DontSave;
            }
            _users++;
            _registered = true;
            _anchor = new GameObject("Enemy Health Bar - Native Pixels").transform;
            // A sibling under the static encounter root avoids inheriting fractional physics
            // movement between fixed/render updates. This component owns lifetime/visibility.
            _anchor.SetParent(transform.parent, false);
            _backing = Part("Backing", -13, 0, 26, 5, 0, 25);
            _well = Part("Steel Well", -12, 1, 24, 3, 3, 26);
            _fill = Part("Health", -12, 2, 24, 2, 14, 27);
            RefreshFill();
        }

        private SpriteRenderer Part(string name, int x, int y, int width, int height, int color, int order)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_anchor, false);
            go.transform.localPosition = new Vector3(x/16f, y/16f, 0);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = _pixel;
            renderer.sharedMaterial = barMaterial;
            renderer.drawMode = SpriteDrawMode.Sliced;
            renderer.size = new Vector2(width/16f, height/16f);
            renderer.sortingOrder = order;
            renderer.color = RustlinePalette.GetColor(color);
            renderer.enabled = false;
            return renderer;
        }

        private void OnEnable()
        {
            if (_health == null || _backing == null) return;
            _health.Damaged += OnDamaged;
            _health.Died += OnDied;
            _health.HealthReset += OnReset;
            OnReset();
        }

        private void OnDisable()
        {
            if (_health != null)
            {
                _health.Damaged -= OnDamaged;
                _health.Died -= OnDied;
                _health.HealthReset -= OnReset;
            }
            Hide();
        }

        private void LateUpdate()
        {
            if (_backing == null) return;
            bool engaged = _sentry != null
                ? _sentry.isActiveAndEnabled && _sentry.CombatActive && _sentry.State != SentryState.Patrol && _sentry.State != SentryState.Dead
                : _bombardier.isActiveAndEnabled && _bombardier.CombatActive &&
                    (_bombardier.State == BombardierState.Windup || _bombardier.State == BombardierState.Recover);
            if (engaged && _health.IsAlive) _visibleUntil = Time.time + InactivitySeconds;
            bool visible = _health.IsAlive && Time.time < _visibleUntil;
            SetVisible(visible);
            if (!visible) return;
            // Snap after camera/animation updates. Never follow animated sprite bounds.
            Vector3 position = transform.position + Vector3.up * HeadHeight;
            position.x = Mathf.Round(position.x * 16f) / 16f;
            position.y = Mathf.Round(position.y * 16f) / 16f;
            if (_anchor.position != position) _anchor.position = position;
        }

        private void RefreshFill()
        {
            int pixels = _health.IsAlive
                ? Mathf.Clamp(Mathf.CeilToInt(FillPixels * (_health.CurrentHealth / (float)_health.MaximumHealth)), 1, FillPixels)
                : 0;
            if (_filledPixels == pixels) return;
            _filledPixels = pixels;
            _fill.size = new Vector2(Mathf.Max(1, pixels)/16f, 2f/16f);
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (!result.DidApply) return;
            RefreshFill();
            if (_health.IsAlive) _visibleUntil = Time.time + InactivitySeconds;
        }

        private void OnDied(DamageResult2D result) => Hide();
        private void OnReset() { Hide(); RefreshFill(); }
        private void Hide() { _visibleUntil = float.NegativeInfinity; SetVisible(false); }
        private void SetVisible(bool visible)
        {
            if (_backing == null || _backing.enabled == visible) return;
            _backing.enabled = _well.enabled = _fill.enabled = visible;
        }

        private void OnDestroy()
        {
            if (_anchor != null) Destroy(_anchor.gameObject);
            if (_registered && --_users == 0) { Destroy(_pixel); _pixel = null; }
        }
    }
}
