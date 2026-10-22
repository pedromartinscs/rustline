using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Sprite-only adapter. Never moves the actor, changes health, or commits an attack.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class SentryPresenter2D : MonoBehaviour
    {
        [SerializeField] private Sentry2D enemy;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Sprite[] frames;
        private CombatHealth2D _health;
        private float _elapsed;
        private float _deathElapsed;

        public SpriteRenderer BodyRenderer => bodyRenderer;
        public int FrameIndex { get; private set; } = -1;
        public int FrameCount => frames != null ? frames.Length : 0;
        public bool HasCompleteFrames
        {
            get
            {
                if (FrameCount != SentryAnimationFrames.Count) return false;
                foreach (Sprite frame in frames) if (frame == null) return false;
                return true;
            }
        }

        private void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<Sentry2D>();
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (enemy == null || !HasCompleteFrames)
            {
                Debug.LogError("Sentry presentation requires its actor and all sixteen authored frames.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            if (enemy == null) return;
            _health = enemy.GetComponent<CombatHealth2D>();
            _health.Died += OnDied;
            _health.HealthReset += OnHealthReset;
            OnHealthReset();
        }

        private void OnDisable()
        {
            if (_health == null) return;
            _health.Died -= OnDied;
            _health.HealthReset -= OnHealthReset;
        }

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;
            if (_health.IsDead) _deathElapsed += Time.deltaTime;
            ApplyFrame(SentryAnimationFrames.Select(enemy.State, enemy.CombatActive, enemy.IsMoving,
                enemy.HitPauseRemaining > 0f, enemy.StateTimeRemaining, _elapsed, _deathElapsed));
            bool facingLeft = enemy.FacingDirection < 0;
            if (bodyRenderer.flipX != facingLeft) bodyRenderer.flipX = facingLeft;
            SnapVisual();
        }

        private void OnDied(DamageResult2D result)
        {
            _deathElapsed = 0f;
            ApplyFrame(12);
        }

        private void OnHealthReset()
        {
            _elapsed = 0f;
            _deathElapsed = _health.IsDead ? SentryAnimationFrames.DeathFrameDuration * 3f : 0f;
            FrameIndex = -1;
            bodyRenderer.enabled = true;
            bodyRenderer.color = Color.white; // Preserve authored canonical colors; no interpolated tint/fade.
            bodyRenderer.flipX = enemy.FacingDirection < 0;
            ApplyFrame(_health.IsDead ? 15 : 0);
            SnapVisual();
        }

        private void ApplyFrame(int index)
        {
            if (FrameIndex == index || frames == null || frames.Length != SentryAnimationFrames.Count) return;
            FrameIndex = index;
            bodyRenderer.sprite = frames[index];
        }

        private void SnapVisual()
        {
            // Snap only the presentation child, preserving continuous kinematic movement/hitboxes.
            Vector3 position = enemy.transform.position;
            position.x = Mathf.Round(position.x * 16f) / 16f;
            position.y = Mathf.Round(position.y * 16f) / 16f;
            if (!transform.position.Equals(position)) transform.position = position;
        }
    }
}
