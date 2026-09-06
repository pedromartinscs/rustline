using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Supplies a deterministic shot offset to PixelCameraFollow2D. The follow component
    /// combines it with its independent base position before final pixel snapping.
    /// </summary>
    [DefaultExecutionOrder(150)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PixelCameraFollow2D))]
    public sealed class LongwatchCameraImpulse2D : MonoBehaviour
    {
        public const float ImpulseDistanceSourcePixels = 1f;
        public const float ImpulseDistanceWorldUnits = ImpulseDistanceSourcePixels / 16f;
        public const float RecoveryDuration = 0.1f;

        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private PixelCameraFollow2D cameraFollow;

        private Vector2 _shotDirection;
        private float _shotTime;
        private bool _isActive;

        public PlayerWeaponController2D WeaponController => weaponController;
        public PixelCameraFollow2D CameraFollow => cameraFollow;
        public Vector2 LastShotDirection => _shotDirection;
        public Vector2 CurrentOffset => cameraFollow != null ? cameraFollow.PresentationOffset : Vector2.zero;
        public bool IsActive => _isActive;
        public int ImpulseCount { get; private set; }

        private void Awake()
        {
            if (cameraFollow == null)
            {
                cameraFollow = GetComponent<PixelCameraFollow2D>();
            }
        }

        private void OnEnable()
        {
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

            ClearImpulse();
        }

        private void Update()
        {
            if (!_isActive || cameraFollow == null)
            {
                return;
            }

            float elapsed = Time.time - _shotTime;
            if (elapsed >= RecoveryDuration)
            {
                ClearImpulse();
                return;
            }

            cameraFollow.SetPresentationOffset(LongwatchRecoilPresenter2D.EvaluateOffset(
                _shotDirection,
                elapsed,
                RecoveryDuration,
                ImpulseDistanceWorldUnits));
        }

        private void OnShotResolved(WeaponShotResult2D result)
        {
            _shotDirection = result.Direction;
            _shotTime = Time.time;
            _isActive = true;
            ImpulseCount++;
            cameraFollow?.SetPresentationOffset(LongwatchRecoilPresenter2D.EvaluateOffset(
                _shotDirection,
                0f,
                RecoveryDuration,
                ImpulseDistanceWorldUnits));
        }

        private void ClearImpulse()
        {
            _isActive = false;
            cameraFollow?.SetPresentationOffset(Vector2.zero);
        }
    }
}
