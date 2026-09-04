using System;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Presentation
{
    [Serializable]
    public struct LongwatchIdleAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;

        public int AngleDegrees => angleDegrees;
        public Sprite Frame0 => frame0;
        public Sprite Frame1 => frame1;
        public Sprite GetFrame(int frameIndex) => frameIndex == 0 ? frame0 : frame1;
    }

    [Serializable]
    public struct LongwatchRunAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;
        [SerializeField] private Sprite frame2;
        [SerializeField] private Sprite frame3;
        [SerializeField] private Sprite frame4;
        [SerializeField] private Sprite frame5;

        public int AngleDegrees => angleDegrees;

        public Sprite GetFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return frame0;
                case 1: return frame1;
                case 2: return frame2;
                case 3: return frame3;
                case 4: return frame4;
                case 5: return frame5;
                default: throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }
        }
    }

    /// <summary>
    /// Owns the shared overlay renderer while the prototype Longwatch is in an
    /// authored aim-capable state. Body remains Animator-driven; its displayed
    /// frame selects the matching weapon frame at the nearest ten-degree pose.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerLongwatchAimPresenter2D : MonoBehaviour
    {
        public const float AimOriginOffsetSourcePixels = 38f;
        public const float SourcePixelsPerUnit = 16f;
        public const float AimOriginOffsetWorldUnits = AimOriginOffsetSourcePixels / SourcePixelsPerUnit;

        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAnimator2D playerAnimator;
        [SerializeField] private PlayerUnarmedArmsPresenter2D unarmedPresenter;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private NativePixelPresentation nativePixelPresentation;
        [SerializeField] private Sprite[] bodyIdleFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchIdleAimPose[] idleAimPoses = Array.Empty<LongwatchIdleAimPose>();
        [SerializeField] private Sprite[] bodyRunFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchRunAimPose[] runAimPoses = Array.Empty<LongwatchRunAimPose>();

        private LongwatchAimSelection _selection = LongwatchAimSelection.Default;
        private Vector2 _continuousAimDirection = Vector2.right;
        private bool _hasValidAim;
        private bool _ownsRenderer;
        private Sprite _lastBodySprite;
        private int _lastDirectionIndex = -1;

        public PlayerInputReader Input => input;
        public PlayerAnimator2D PlayerAnimator => playerAnimator;
        public PlayerUnarmedArmsPresenter2D UnarmedPresenter => unarmedPresenter;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public SpriteRenderer ArmsWeaponSpriteRenderer => armsWeaponSpriteRenderer;
        public NativePixelPresentation NativePixelPresentation => nativePixelPresentation;
        public int BodyIdleFrameCount => bodyIdleFrames?.Length ?? 0;
        public int BodyRunFrameCount => bodyRunFrames?.Length ?? 0;
        public int IdleAimPoseCount => idleAimPoses?.Length ?? 0;
        public int RunAimPoseCount => runAimPoses?.Length ?? 0;
        public bool OwnsRenderer => _ownsRenderer;
        public bool HasValidAim => _hasValidAim;
        public LongwatchAimSelection Selection => _selection;
        public Vector2 ContinuousAimDirection => _continuousAimDirection;
        public Vector3 AimOriginWorld => bodySpriteRenderer == null
            ? Vector3.up * AimOriginOffsetWorldUnits
            : bodySpriteRenderer.transform.position + Vector3.up * AimOriginOffsetWorldUnits;

        private void LateUpdate()
        {
            if (!CanOwnRenderer())
            {
                ReleaseRenderer();
                return;
            }

            UpdateAimSelection();
            AcquireRenderer();
            playerAnimator.SetFacingOverride(true, _selection.FlipX);

            if (!TryResolveDisplayedBodyFrame(out bool isRunFrame, out int bodyFrameIndex))
            {
                return;
            }

            int directionIndex = _selection.DirectionIndex;
            Sprite bodySprite = bodySpriteRenderer.sprite;
            if (bodySprite == _lastBodySprite && directionIndex == _lastDirectionIndex)
            {
                return;
            }

            _lastBodySprite = bodySprite;
            _lastDirectionIndex = directionIndex;
            armsWeaponSpriteRenderer.sprite = isRunFrame
                ? runAimPoses[directionIndex].GetFrame(bodyFrameIndex)
                : idleAimPoses[directionIndex].GetFrame(bodyFrameIndex);
        }

        private void OnDisable()
        {
            ReleaseRenderer();
        }

        public LongwatchIdleAimPose GetIdleAimPose(int index)
        {
            return idleAimPoses[index];
        }

        public LongwatchRunAimPose GetRunAimPose(int index)
        {
            return runAimPoses[index];
        }

        public Sprite GetBodyIdleFrame(int index)
        {
            return bodyIdleFrames[index];
        }

        public Sprite GetBodyRunFrame(int index)
        {
            return bodyRunFrames[index];
        }

        private bool CanOwnRenderer()
        {
            if (input == null || playerAnimator == null || unarmedPresenter == null ||
                bodySpriteRenderer == null || armsWeaponSpriteRenderer == null ||
                nativePixelPresentation == null || nativePixelPresentation.WorldCamera == null ||
                bodyIdleFrames == null || bodyIdleFrames.Length != 2 ||
                idleAimPoses == null || idleAimPoses.Length != 19 ||
                bodyRunFrames == null || bodyRunFrames.Length != 6 ||
                runAimPoses == null || runAimPoses.Length != 19)
            {
                return false;
            }

            PlayerAnimationState? state = playerAnimator.CurrentState;
            return state == PlayerAnimationState.Idle || state == PlayerAnimationState.Run;
        }

        private bool TryResolveDisplayedBodyFrame(out bool isRunFrame, out int frameIndex)
        {
            Sprite displayedBody = bodySpriteRenderer.sprite;
            for (int index = 0; index < bodyIdleFrames.Length; index++)
            {
                if (displayedBody == bodyIdleFrames[index])
                {
                    isRunFrame = false;
                    frameIndex = index;
                    return true;
                }
            }

            for (int index = 0; index < bodyRunFrames.Length; index++)
            {
                if (displayedBody == bodyRunFrames[index])
                {
                    isRunFrame = true;
                    frameIndex = index;
                    return true;
                }
            }

            isRunFrame = false;
            frameIndex = -1;
            return false;
        }

        private void UpdateAimSelection()
        {
            NativePixelViewport viewport = nativePixelPresentation.Viewport;
            Vector2 viewportPosition = NativePixelViewportMath.PhysicalToLogicalViewport(
                input.PointerScreenPosition,
                viewport);
            Camera worldCamera = nativePixelPresentation.WorldCamera;
            Vector3 aimOriginWorld = AimOriginWorld;
            float playerViewportDepth = worldCamera.WorldToViewportPoint(aimOriginWorld).z;
            Vector3 pointerWorld = worldCamera.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, playerViewportDepth));
            Vector2 aimVector = (Vector2)(pointerWorld - aimOriginWorld);
            if (LongwatchAimMath.TrySelect(aimVector, _hasValidAim, _selection, out LongwatchAimSelection next))
            {
                _selection = next;
                _continuousAimDirection = aimVector.normalized;
                _hasValidAim = true;
            }
        }

        private void AcquireRenderer()
        {
            if (_ownsRenderer)
            {
                return;
            }

            unarmedPresenter.SetRendererOwnership(false);
            _ownsRenderer = true;
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
        }

        private void ReleaseRenderer()
        {
            if (!_ownsRenderer)
            {
                return;
            }

            _ownsRenderer = false;
            playerAnimator?.SetFacingOverride(false, false);
            unarmedPresenter?.SetRendererOwnership(true);
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
        }
    }
}
