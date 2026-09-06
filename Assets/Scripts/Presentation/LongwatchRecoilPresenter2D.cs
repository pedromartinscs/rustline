using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Applies a short presentation-only offset to the authored Longwatch overlay.
    /// It never touches the player root, Body Animator, aim, or weapon gameplay state.
    /// </summary>
    [DefaultExecutionOrder(110)]
    [DisallowMultipleComponent]
    public sealed class LongwatchRecoilPresenter2D : MonoBehaviour
    {
        public const float RecoilDistanceSourcePixels = 1.5f;
        public const float RecoilDistanceWorldUnits = RecoilDistanceSourcePixels / 16f;
        public const float RecoveryDuration = 0.1f;

        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private PlayerLongwatchAimPresenter2D longwatchPresenter;
        [SerializeField] private Transform armsWeaponTransform;

        private Vector3 _baselineLocalPosition;
        private Vector2 _shotDirection;
        private float _shotTime;
        private bool _isRecoiling;

        public PlayerWeaponController2D WeaponController => weaponController;
        public PlayerLongwatchAimPresenter2D LongwatchPresenter => longwatchPresenter;
        public Transform ArmsWeaponTransform => armsWeaponTransform;
        public Vector3 BaselineLocalPosition => _baselineLocalPosition;
        public Vector2 CurrentOffset => armsWeaponTransform != null
            ? (Vector2)(armsWeaponTransform.localPosition - _baselineLocalPosition)
            : Vector2.zero;
        public Vector2 LastShotDirection => _shotDirection;
        public bool IsRecoiling => _isRecoiling;
        public int ImpulseCount { get; private set; }

        private void OnEnable()
        {
            if (armsWeaponTransform != null)
            {
                _baselineLocalPosition = armsWeaponTransform.localPosition;
            }

            if (weaponController != null)
            {
                weaponController.ShotResolved += OnShotResolved;
            }
        }

        private void OnDisable()
        {
            if (weaponController != null)
            {
                weaponController.ShotResolved -= OnShotResolved;
            }

            RestoreBaseline();
        }

        private void LateUpdate()
        {
            if (!_isRecoiling || armsWeaponTransform == null)
            {
                return;
            }

            if (longwatchPresenter == null || !longwatchPresenter.OwnsRenderer)
            {
                RestoreBaseline();
                return;
            }

            float elapsed = Time.time - _shotTime;
            if (elapsed >= RecoveryDuration)
            {
                RestoreBaseline();
                return;
            }

            armsWeaponTransform.localPosition = _baselineLocalPosition +
                (Vector3)EvaluateOffset(_shotDirection, elapsed, RecoveryDuration, RecoilDistanceWorldUnits);
        }

        public static Vector2 EvaluateOffset(
            Vector2 shotDirection,
            float elapsed,
            float duration,
            float distance)
        {
            if (duration <= 0f || distance <= 0f || elapsed >= duration)
            {
                return Vector2.zero;
            }

            float remaining = 1f - Mathf.Clamp01(elapsed / duration);
            return -shotDirection.normalized * (distance * remaining * remaining);
        }

        private void OnShotResolved(WeaponShotResult2D result)
        {
            _shotDirection = result.Direction;
            _shotTime = Time.time;
            _isRecoiling = true;
            ImpulseCount++;
            if (armsWeaponTransform != null)
            {
                armsWeaponTransform.localPosition = _baselineLocalPosition +
                    (Vector3)EvaluateOffset(_shotDirection, 0f, RecoveryDuration, RecoilDistanceWorldUnits);
            }
        }

        private void RestoreBaseline()
        {
            _isRecoiling = false;
            if (armsWeaponTransform != null)
            {
                armsWeaponTransform.localPosition = _baselineLocalPosition;
            }
        }
    }
}
