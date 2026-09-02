using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Presentation
{
    [RequireComponent(typeof(PlayerMotor2D))]
    public sealed class PlayerJumpPresentation2D : MonoBehaviour
    {
        public const float FullAnchorDuration = 0.1f;
        public const float CatchUpDuration = 0.16f;
        public const float TotalTakeoffDuration = FullAnchorDuration + CatchUpDuration;

        [SerializeField] private Transform visual;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private PlayerJumpDustFx2D jumpDustPrefab;

        private PlayerMotor2D _motor;
        private Vector3 _baselineLocalPosition;
        private Vector3 _takeoffWorldPosition;
        private float _takeoffAnchorWorldY;
        private float _takeoffStartTime;
        private bool _takeoffActive;

        public Transform Visual => visual;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public PlayerJumpDustFx2D JumpDustPrefab => jumpDustPrefab;
        public Vector3 BaselineLocalPosition => _baselineLocalPosition;
        public Vector3 TakeoffWorldPosition => _takeoffWorldPosition;
        public float TakeoffAnchorWorldY => _takeoffAnchorWorldY;
        public bool TakeoffActive => _takeoffActive;
        public float TakeoffElapsed => _takeoffActive ? Mathf.Max(0f, Time.time - _takeoffStartTime) : 0f;

        private void Awake()
        {
            _motor = GetComponent<PlayerMotor2D>();
            if (visual != null)
            {
                _baselineLocalPosition = visual.localPosition;
            }
        }

        private void OnEnable()
        {
            if (_motor == null)
            {
                _motor = GetComponent<PlayerMotor2D>();
            }

            _motor.Jumped += OnJumped;
        }

        private void OnDisable()
        {
            if (_motor != null)
            {
                _motor.Jumped -= OnJumped;
            }

            RestoreBaseline();
        }

        private void Update()
        {
            if (!_takeoffActive || visual == null)
            {
                return;
            }

            float elapsed = TakeoffElapsed;
            if (elapsed >= TotalTakeoffDuration)
            {
                RestoreBaseline();
                return;
            }

            Vector3 normalWorldPosition = transform.TransformPoint(_baselineLocalPosition);
            normalWorldPosition.y = CalculateDisplayWorldY(
                _takeoffAnchorWorldY,
                normalWorldPosition.y,
                elapsed);
            visual.position = normalWorldPosition;
        }

        public static float EaseOutCubic(float normalizedTime)
        {
            float clamped = Mathf.Clamp01(normalizedTime);
            float inverse = 1f - clamped;
            return 1f - inverse * inverse * inverse;
        }

        public static float CalculateDisplayWorldY(float anchorWorldY, float normalTargetWorldY, float elapsed)
        {
            if (elapsed < FullAnchorDuration)
            {
                return anchorWorldY;
            }

            float progress = (elapsed - FullAnchorDuration) / CatchUpDuration;
            return Mathf.Lerp(anchorWorldY, normalTargetWorldY, EaseOutCubic(progress));
        }

        private void OnJumped(bool jumpedWhileGrounded)
        {
            if (visual == null)
            {
                return;
            }

            RestoreBaseline();
            _takeoffWorldPosition = visual.position;
            _takeoffAnchorWorldY = _takeoffWorldPosition.y;
            _takeoffStartTime = Time.time;
            _takeoffActive = true;

            if (jumpedWhileGrounded && jumpDustPrefab != null)
            {
                PlayerJumpDustFx2D dust = Instantiate(jumpDustPrefab, _takeoffWorldPosition, Quaternion.identity);
                dust.Initialize(bodySpriteRenderer != null && bodySpriteRenderer.flipX);
            }
        }

        private void RestoreBaseline()
        {
            if (visual != null)
            {
                visual.localPosition = _baselineLocalPosition;
            }

            _takeoffActive = false;
        }
    }
}
