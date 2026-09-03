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

    /// <summary>
    /// Owns the shared overlay renderer only while the prototype Longwatch is Idle.
    /// Body remains Animator-driven; the displayed Body Idle frame selects the matching
    /// authored weapon frame at the nearest ten-degree aim pose.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerLongwatchIdleAimPresenter2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerAnimator2D playerAnimator;
        [SerializeField] private PlayerUnarmedArmsPresenter2D unarmedPresenter;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private NativePixelPresentation nativePixelPresentation;
        [SerializeField] private Sprite[] bodyIdleFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchIdleAimPose[] aimPoses = Array.Empty<LongwatchIdleAimPose>();

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
        public int AimPoseCount => aimPoses?.Length ?? 0;
        public bool OwnsRenderer => _ownsRenderer;
        public bool HasValidAim => _hasValidAim;
        public LongwatchAimSelection Selection => _selection;
        public Vector2 ContinuousAimDirection => _continuousAimDirection;

        private void LateUpdate()
        {
            if (!CanPresentIdle(out int bodyFrameIndex))
            {
                ReleaseRenderer();
                return;
            }

            UpdateAimSelection();
            AcquireRenderer();
            playerAnimator.SetFacingOverride(true, _selection.FlipX);

            int directionIndex = _selection.DirectionIndex;
            Sprite bodySprite = bodySpriteRenderer.sprite;
            if (bodySprite == _lastBodySprite && directionIndex == _lastDirectionIndex)
            {
                return;
            }

            _lastBodySprite = bodySprite;
            _lastDirectionIndex = directionIndex;
            armsWeaponSpriteRenderer.sprite = aimPoses[directionIndex].GetFrame(bodyFrameIndex);
        }

        private void OnDisable()
        {
            ReleaseRenderer();
        }

        public LongwatchIdleAimPose GetAimPose(int index)
        {
            return aimPoses[index];
        }

        public Sprite GetBodyIdleFrame(int index)
        {
            return bodyIdleFrames[index];
        }

        private bool CanPresentIdle(out int bodyFrameIndex)
        {
            bodyFrameIndex = -1;
            if (input == null || playerAnimator == null || unarmedPresenter == null ||
                bodySpriteRenderer == null || armsWeaponSpriteRenderer == null ||
                nativePixelPresentation == null || nativePixelPresentation.WorldCamera == null ||
                bodyIdleFrames == null || bodyIdleFrames.Length != 2 ||
                aimPoses == null || aimPoses.Length != 19 ||
                playerAnimator.CurrentState != PlayerAnimationState.Idle)
            {
                return false;
            }

            Sprite displayedBody = bodySpriteRenderer.sprite;
            if (displayedBody == bodyIdleFrames[0])
            {
                bodyFrameIndex = 0;
                return true;
            }

            if (displayedBody == bodyIdleFrames[1])
            {
                bodyFrameIndex = 1;
                return true;
            }

            return false;
        }

        private void UpdateAimSelection()
        {
            NativePixelViewport viewport = nativePixelPresentation.Viewport;
            Vector2 viewportPosition = NativePixelViewportMath.PhysicalToLogicalViewport(
                input.PointerScreenPosition,
                viewport);
            Camera worldCamera = nativePixelPresentation.WorldCamera;
            Vector3 bodyPivotWorld = bodySpriteRenderer.transform.position;
            float playerViewportDepth = worldCamera.WorldToViewportPoint(bodyPivotWorld).z;
            Vector3 pointerWorld = worldCamera.ViewportToWorldPoint(
                new Vector3(viewportPosition.x, viewportPosition.y, playerViewportDepth));
            Vector2 aimVector = (Vector2)(pointerWorld - bodyPivotWorld);
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
