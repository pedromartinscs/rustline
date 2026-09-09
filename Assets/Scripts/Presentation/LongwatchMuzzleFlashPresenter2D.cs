using System;
using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    public static class LongwatchMuzzleFlashMath
    {
        public const float SourcePixelsPerUnit = 16f;
        public const int VariantCount = 10;
        public const int VariantStride = 7;

        public static Vector2 ToLocalPosition(Vector2 offsetPixels, bool facingLeft)
        {
            float x = offsetPixels.x / SourcePixelsPerUnit;
            float y = offsetPixels.y / SourcePixelsPerUnit;
            return new Vector2(facingLeft ? -x : x, y);
        }

        public static float ToLocalAngle(int authoredAngleDegrees, bool facingLeft)
        {
            return facingLeft ? 180f - authoredAngleDegrees : authoredAngleDegrees;
        }

        public static int SelectVariant(int shotSequence)
        {
            int normalizedSequence = shotSequence % VariantCount;
            if (normalizedSequence < 0)
            {
                normalizedSequence += VariantCount;
            }

            return normalizedSequence * VariantStride % VariantCount;
        }

        public static bool TryResolve(
            LongwatchMuzzleMetadata2D metadata,
            in LongwatchRenderedPose2D pose,
            out Vector2 localPosition,
            out float localAngleDegrees)
        {
            if (metadata == null || !metadata.TryGetMuzzleOffset(
                    pose.State,
                    pose.DirectionIndex,
                    pose.FrameIndex,
                    out Vector2 offsetPixels))
            {
                localPosition = default;
                localAngleDegrees = 0f;
                return false;
            }

            localPosition = ToLocalPosition(offsetPixels, pose.FacingLeft);
            localAngleDegrees = ToLocalAngle(pose.AuthoredAngleDegrees, pose.FacingLeft);
            return true;
        }
    }

    /// <summary>
    /// Displays a persistent two-rendered-frame Longwatch muzzle flash after a
    /// successful shot. Pose resolution happens after weapon aim and recoil presentation.
    /// </summary>
    [DefaultExecutionOrder(120)]
    [DisallowMultipleComponent]
    public sealed class LongwatchMuzzleFlashPresenter2D : MonoBehaviour
    {
        public const int FramesPerVariant = 2;
        public const int RequiredSpriteCount =
            LongwatchMuzzleFlashMath.VariantCount * FramesPerVariant;

        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private PlayerLongwatchAimPresenter2D longwatchPresenter;
        [SerializeField] private LongwatchMuzzleMetadata2D metadata;
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Sprite[] frames = Array.Empty<Sprite>();

        private bool _pendingShot;
        private int _pendingVariant;
        private int _shotSequence;
        private int _activationFrame;
        private int _activeVariant = -1;
        private int _activeFrame = -1;

        public PlayerWeaponController2D WeaponController => weaponController;
        public PlayerLongwatchAimPresenter2D LongwatchPresenter => longwatchPresenter;
        public LongwatchMuzzleMetadata2D Metadata => metadata;
        public SpriteRenderer FlashRenderer => flashRenderer;
        public int SpriteCount => frames?.Length ?? 0;
        public bool IsVisible => flashRenderer != null && flashRenderer.enabled;
        public int ActiveVariant => _activeVariant;
        public int ActiveFrame => _activeFrame;
        public int ShotEventCount { get; private set; }
        public int ShownFlashCount { get; private set; }

        public Sprite GetSprite(int index)
        {
            return frames[index];
        }

        private void OnEnable()
        {
            _pendingShot = false;
            _shotSequence = 0;
            ShotEventCount = 0;
            ShownFlashCount = 0;
            Hide();
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

            _pendingShot = false;
            Hide();
        }

        private void LateUpdate()
        {
            if (_pendingShot)
            {
                _pendingShot = false;
                ShowPendingShot();
                return;
            }

            if (_activeVariant < 0)
            {
                return;
            }

            int renderedFrameAge = Time.frameCount - _activationFrame;
            if (renderedFrameAge == 1)
            {
                SetFrame(1);
            }
            else if (renderedFrameAge >= FramesPerVariant)
            {
                Hide();
            }
        }

        private void OnShotResolved(WeaponShotResult2D _result)
        {
            _pendingVariant = LongwatchMuzzleFlashMath.SelectVariant(_shotSequence);
            unchecked
            {
                _shotSequence++;
            }

            _pendingShot = true;
            ShotEventCount++;
        }

        private void ShowPendingShot()
        {
            if (flashRenderer == null || frames == null || frames.Length != RequiredSpriteCount ||
                longwatchPresenter == null ||
                !longwatchPresenter.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D pose) ||
                !LongwatchMuzzleFlashMath.TryResolve(
                    metadata, in pose, out Vector2 localPosition, out float localAngleDegrees))
            {
                Hide();
                return;
            }

            transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, localAngleDegrees);
            flashRenderer.flipX = false;
            _activeVariant = _pendingVariant;
            _activationFrame = Time.frameCount;
            SetFrame(0);
            flashRenderer.enabled = true;
            ShownFlashCount++;
        }

        private void SetFrame(int frameIndex)
        {
            _activeFrame = frameIndex;
            flashRenderer.sprite = frames[_activeVariant * FramesPerVariant + frameIndex];
        }

        private void Hide()
        {
            _activeVariant = -1;
            _activeFrame = -1;
            if (flashRenderer != null)
            {
                flashRenderer.enabled = false;
            }
        }
    }
}
