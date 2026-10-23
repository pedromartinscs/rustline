using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Sprite/target-warning adapter; gameplay owns launch, facing, movement and health.</summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(SpriteRenderer))]
    public sealed class BombardierPresenter2D : MonoBehaviour
    {
        public const float HitReactionDuration = Bombardier2D.DefaultHitPauseDuration;
        [SerializeField] private CombatHealth2D health;
        [SerializeField] private Bombardier2D enemy;
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private Sprite[] frames;
        [SerializeField] private Sprite[] dissolutionFrames;
        [SerializeField] private Material markerMaterial;
        private LineRenderer _targetMarker;
        private float _elapsed;
        private float _deathElapsed;
        private int _deathStartedFrame = -1;

        public CombatHealth2D Health => health;
        public SpriteRenderer BodyRenderer => bodyRenderer;
        public LineRenderer TargetMarker => _targetMarker;
        public int FrameIndex { get; private set; } = -1;
        public int FrameCount => frames != null ? frames.Length : 0;
        public bool HasCompleteFrames
        {
            get
            {
                if (FrameCount != BombardierAnimationFrames.Count) return false;
                foreach (Sprite frame in frames) if (frame == null) return false;
                if (dissolutionFrames == null || dissolutionFrames.Length != BombardierAnimationFrames.DissolutionCount) return false;
                foreach (Sprite frame in dissolutionFrames) if (frame == null) return false;
                return true;
            }
        }
        public bool IsHitReacting => health != null && health.IsAlive && enemy.HitPauseRemaining > 0f;
        public bool IsDeadVisual => health != null && health.IsDead;
        public int HitReactionCount { get; private set; }
        public int DeathPresentationCount { get; private set; }
        public int ResetPresentationCount { get; private set; }

        private void Awake()
        {
            if (enemy == null) enemy = GetComponentInParent<Bombardier2D>();
            if (health == null) health = GetComponentInParent<CombatHealth2D>();
            if (bodyRenderer == null) bodyRenderer = GetComponent<SpriteRenderer>();
            if (enemy == null || health == null || !HasCompleteFrames || markerMaterial == null)
            {
                Debug.LogError("Bombardier presentation requires actor, health, sixteen poses, three dissolution sprites and warning material.", this);
                enabled = false;
                return;
            }
            GameObject marker = new GameObject("Locked Target Marker");
            _targetMarker = marker.AddComponent<LineRenderer>();
            _targetMarker.sharedMaterial = markerMaterial;
            _targetMarker.useWorldSpace = false;
            _targetMarker.loop = true;
            _targetMarker.positionCount = 4;
            _targetMarker.widthMultiplier = 0.0625f;
            _targetMarker.sortingOrder = 31;
            Color color = RustlinePalette.GetColor(13);
            _targetMarker.startColor = color;
            _targetMarker.endColor = color;
            _targetMarker.SetPosition(0, new Vector3(-0.25f, 0f));
            _targetMarker.SetPosition(1, new Vector3(0f, 0.25f));
            _targetMarker.SetPosition(2, new Vector3(0.25f, 0f));
            _targetMarker.SetPosition(3, new Vector3(0f, -0.25f));
            _targetMarker.enabled = false;
        }

        private void OnEnable()
        {
            if (health == null || !HasCompleteFrames) return;
            health.Damaged += OnDamaged;
            health.Died += OnDied;
            health.HealthReset += OnHealthReset;
            RestorePose();
        }

        private void OnDisable()
        {
            if (health != null)
            {
                health.Damaged -= OnDamaged;
                health.Died -= OnDied;
                health.HealthReset -= OnHealthReset;
            }
            if (_targetMarker != null) _targetMarker.enabled = false;
        }

        private void OnDestroy()
        {
            if (_targetMarker != null) Destroy(_targetMarker.gameObject);
        }

        private void LateUpdate()
        {
            _elapsed += Time.deltaTime;
            if (health.IsDead && Time.frameCount != _deathStartedFrame) _deathElapsed += Time.deltaTime;
            ApplyFrame(BombardierAnimationFrames.Select(enemy.State, enemy.CombatActive, enemy.IsMoving,
                IsHitReacting, enemy.StateTimeRemaining, _elapsed, _deathElapsed));
            bool left = enemy.PatrolDirection < 0;
            if (bodyRenderer.flipX != left) bodyRenderer.flipX = left;
            bool warning = enemy.CombatActive && enemy.State == BombardierState.Windup;
            if (_targetMarker.enabled != warning) _targetMarker.enabled = warning;
            if (warning && !_targetMarker.transform.position.Equals((Vector3)enemy.LockedTargetPoint))
                _targetMarker.transform.position = enemy.LockedTargetPoint;
            SnapVisual();
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (!result.DidApply || health.IsDead) return;
            HitReactionCount++;
            ApplyFrame(11);
        }

        private void OnDied(DamageResult2D result)
        {
            DeathPresentationCount++;
            _deathElapsed = 0f;
            _deathStartedFrame = Time.frameCount;
            ApplyFrame(12);
            _targetMarker.enabled = false;
        }

        private void OnHealthReset()
        {
            ResetPresentationCount++;
            RestorePose();
        }

        private void RestorePose()
        {
            _elapsed = 0f;
            _deathStartedFrame = -1;
            _deathElapsed = health.IsDead ? BombardierAnimationFrames.DeathEnd : 0f;
            FrameIndex = -2;
            bodyRenderer.enabled = true;
            bodyRenderer.color = Color.white;
            bodyRenderer.flipX = enemy.PatrolDirection < 0;
            ApplyFrame(health.IsDead ? -1 : 0);
            if (_targetMarker != null) _targetMarker.enabled = false;
            SnapVisual();
        }

        private void ApplyFrame(int index)
        {
            if (FrameIndex == index) return;
            FrameIndex = index;
            bodyRenderer.enabled = index >= 0;
            if (index >= 0) bodyRenderer.sprite = index < BombardierAnimationFrames.Count ? frames[index] :
                dissolutionFrames[index - BombardierAnimationFrames.Count];
        }

        private void SnapVisual()
        {
            Vector3 position = enemy.transform.position;
            position.x = Mathf.Round(position.x * 16f) / 16f;
            position.y = Mathf.Round(position.y * 16f) / 16f;
            if (!transform.position.Equals(position)) transform.position = position;
        }
    }
}
