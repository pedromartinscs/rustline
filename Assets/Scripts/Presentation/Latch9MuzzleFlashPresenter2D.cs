using System;
using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    public enum Latch9MuzzleFlashProfile2D
    {
        Conventional = 0,
        Bouncing = 1,
    }

    public static class Latch9MuzzleFlashMath
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
            Latch9MuzzleMetadata2D metadata,
            in Latch9RenderedPose2D pose,
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
    /// Displays the Latch-9 two-rendered-frame muzzle flash immediately when a shot is launched.
    /// Conventional and Bouncing use independent authored flash banks.
    /// </summary>
    [DefaultExecutionOrder(125)]
    [DisallowMultipleComponent]
    public sealed class Latch9MuzzleFlashPresenter2D : MonoBehaviour
    {
        public const int FramesPerVariant = 2;
        public const int RequiredSpriteCount =
            Latch9MuzzleFlashMath.VariantCount * FramesPerVariant;
        private const string DefaultMetadataResourcePath = "Generated/Latch9MuzzleMetadata";

        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private PlayerLatch9AimPresenter2D latchPresenter;
        [SerializeField] private Latch9MuzzleMetadata2D metadata;
        [SerializeField] private SpriteRenderer flashRenderer;
        [SerializeField] private Sprite[] conventionalFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] bouncingFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9MuzzleFlashProfile2D profile =
            Latch9MuzzleFlashProfile2D.Conventional;

        private bool _pendingShot;
        private int _pendingVariant;
        private Latch9MuzzleFlashProfile2D _pendingProfile;
        private int _shotSequence;
        private int _activationFrame;
        private int _activeVariant = -1;
        private int _activeFrame = -1;
        private Latch9MuzzleFlashProfile2D _activeProfile;

        public PlayerWeaponController2D WeaponController => weaponController;
        public PlayerLatch9AimPresenter2D LatchPresenter => latchPresenter;
        public Latch9MuzzleMetadata2D Metadata => ResolveMetadata();
        public SpriteRenderer FlashRenderer => flashRenderer;
        public int ConventionalSpriteCount => conventionalFrames?.Length ?? 0;
        public int BouncingSpriteCount => bouncingFrames?.Length ?? 0;
        public Latch9MuzzleFlashProfile2D Profile => profile;
        public bool IsVisible => flashRenderer != null && flashRenderer.enabled;
        public int ActiveVariant => _activeVariant;
        public int ActiveFrame => _activeFrame;
        public Latch9MuzzleFlashProfile2D ActiveProfile => _activeProfile;
        public int ShotEventCount { get; private set; }
        public int ShownFlashCount { get; private set; }

        public void SetProfile(Latch9MuzzleFlashProfile2D nextProfile)
        {
            profile = nextProfile;
        }

        public Sprite GetSprite(Latch9MuzzleFlashProfile2D requestedProfile, int index)
        {
            Sprite[] frames = GetFrames(requestedProfile);
            if (frames == null || index < 0 || index >= frames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index));
            }

            return frames[index];
        }

        private void OnEnable()
        {
            _pendingShot = false;
            _shotSequence = 0;
            ShotEventCount = 0;
            ShownFlashCount = 0;
            Hide();
            ResolveMetadata();
            if (weaponController != null)
            {
                weaponController.ShotFired += OnShotFired;
            }
        }

        private void OnDisable()
        {
            if (weaponController != null)
            {
                weaponController.ShotFired -= OnShotFired;
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

            if (latchPresenter == null || !latchPresenter.TryGetCurrentRenderedPose(out _))
            {
                Hide();
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

        private void OnShotFired(WeaponShotFired2D result)
        {
            _pendingVariant = Latch9MuzzleFlashMath.SelectVariant(_shotSequence);
            _pendingProfile = result.ShotMode == WeaponShotMode2D.Bouncing
                ? Latch9MuzzleFlashProfile2D.Bouncing
                : Latch9MuzzleFlashProfile2D.Conventional;
            unchecked
            {
                _shotSequence++;
            }

            _pendingShot = true;
            ShotEventCount++;
        }

        private void ShowPendingShot()
        {
            Sprite[] frames = GetFrames(_pendingProfile);
            Latch9MuzzleMetadata2D resolvedMetadata = ResolveMetadata();
            if (flashRenderer == null || frames == null || frames.Length != RequiredSpriteCount ||
                latchPresenter == null ||
                !latchPresenter.TryGetCurrentRenderedPose(out Latch9RenderedPose2D pose) ||
                !Latch9MuzzleFlashMath.TryResolve(
                    resolvedMetadata, in pose, out Vector2 localPosition, out float localAngleDegrees))
            {
                Hide();
                return;
            }

            transform.localPosition = new Vector3(localPosition.x, localPosition.y, 0f);
            transform.localRotation = Quaternion.Euler(0f, 0f, localAngleDegrees);
            flashRenderer.flipX = false;
            _activeVariant = _pendingVariant;
            _activeProfile = _pendingProfile;
            _activationFrame = Time.frameCount;
            SetFrame(0);
            flashRenderer.enabled = true;
            ShownFlashCount++;
        }

        private Latch9MuzzleMetadata2D ResolveMetadata()
        {
            if (metadata == null)
            {
                metadata = Resources.Load<Latch9MuzzleMetadata2D>(DefaultMetadataResourcePath);
            }

            return metadata;
        }

        private void SetFrame(int frameIndex)
        {
            Sprite[] frames = GetFrames(_activeProfile);
            _activeFrame = frameIndex;
            flashRenderer.sprite = frames[_activeVariant * FramesPerVariant + frameIndex];
        }

        private Sprite[] GetFrames(Latch9MuzzleFlashProfile2D requestedProfile)
        {
            return requestedProfile == Latch9MuzzleFlashProfile2D.Bouncing
                ? bouncingFrames
                : conventionalFrames;
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
