using System;
using System.Collections.Generic;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Presentation
{
    [Serializable]
    public struct Latch9IdleAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;

        public Latch9IdleAimPose(int angleDegrees, Sprite frame0, Sprite frame1)
        {
            this.angleDegrees = angleDegrees;
            this.frame0 = frame0;
            this.frame1 = frame1;
        }

        public int AngleDegrees => angleDegrees;
        public Sprite Frame0 => frame0;
        public Sprite Frame1 => frame1;

        public Sprite GetFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return frame0;
                case 1: return frame1;
                default: throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }
        }
    }

    [Serializable]
    public struct Latch9RunAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;
        [SerializeField] private Sprite frame2;
        [SerializeField] private Sprite frame3;
        [SerializeField] private Sprite frame4;
        [SerializeField] private Sprite frame5;

        public Latch9RunAimPose(
            int angleDegrees, Sprite frame0, Sprite frame1, Sprite frame2,
            Sprite frame3, Sprite frame4, Sprite frame5)
        {
            this.angleDegrees = angleDegrees;
            this.frame0 = frame0;
            this.frame1 = frame1;
            this.frame2 = frame2;
            this.frame3 = frame3;
            this.frame4 = frame4;
            this.frame5 = frame5;
        }

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

    [Serializable]
    public struct Latch9BackpedalAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;
        [SerializeField] private Sprite frame2;
        [SerializeField] private Sprite frame3;

        public Latch9BackpedalAimPose(
            int angleDegrees, Sprite frame0, Sprite frame1, Sprite frame2, Sprite frame3)
        {
            this.angleDegrees = angleDegrees;
            this.frame0 = frame0;
            this.frame1 = frame1;
            this.frame2 = frame2;
            this.frame3 = frame3;
        }

        public int AngleDegrees => angleDegrees;

        public Sprite GetFrame(int frameIndex)
        {
            switch (frameIndex)
            {
                case 0: return frame0;
                case 1: return frame1;
                case 2: return frame2;
                case 3: return frame3;
                default: throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }
        }
    }

    [Serializable]
    public struct Latch9CrouchAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;
        [SerializeField] private Sprite frame2;
        [SerializeField] private Sprite frame3;
        [SerializeField] private Sprite frame4;
        [SerializeField] private Sprite frame5;

        public Latch9CrouchAimPose(
            int angleDegrees, Sprite frame0, Sprite frame1, Sprite frame2,
            Sprite frame3, Sprite frame4, Sprite frame5)
        {
            this.angleDegrees = angleDegrees;
            this.frame0 = frame0;
            this.frame1 = frame1;
            this.frame2 = frame2;
            this.frame3 = frame3;
            this.frame4 = frame4;
            this.frame5 = frame5;
        }

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

    [Serializable]
    public struct Latch9FallAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;

        public Latch9FallAimPose(int angleDegrees, Sprite frame0)
        {
            this.angleDegrees = angleDegrees;
            this.frame0 = frame0;
        }

        public int AngleDegrees => angleDegrees;
        public Sprite Frame0 => frame0;

        public Sprite GetFrame(int frameIndex)
        {
            if (frameIndex != 0)
            {
                throw new ArgumentOutOfRangeException(nameof(frameIndex));
            }

            return frame0;
        }
    }

    /// <summary>
    /// Owns the shared overlay renderer for authored Latch-9 aim/carry states.
    /// Aim-capable states also expose the exact rendered state/direction/frame tuple
    /// used by muzzle presentation. Jump/Land carry never expose a muzzle pose.
    /// </summary>
    [DefaultExecutionOrder(105)]
    [DisallowMultipleComponent]
    public sealed class PlayerLatch9AimPresenter2D : MonoBehaviour
    {
        [SerializeField] private PlayerAim2D playerAim;
        [SerializeField] private PlayerAnimator2D playerAnimator;
        [SerializeField] private PlayerUnarmedArmsPresenter2D unarmedPresenter;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private Sprite[] bodyIdleFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9IdleAimPose[] idleAimPoses = Array.Empty<Latch9IdleAimPose>();
        [SerializeField] private Sprite[] bodyRunFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9RunAimPose[] runAimPoses = Array.Empty<Latch9RunAimPose>();
        [SerializeField] private Sprite[] bodyBackpedalFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9BackpedalAimPose[] backpedalAimPoses = Array.Empty<Latch9BackpedalAimPose>();
        [SerializeField] private Sprite[] bodyCrouchFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9CrouchAimPose[] crouchAimPoses = Array.Empty<Latch9CrouchAimPose>();
        [SerializeField] private Sprite[] bodyFallFrames = Array.Empty<Sprite>();
        [SerializeField] private Latch9FallAimPose[] fallAimPoses = Array.Empty<Latch9FallAimPose>();
        [SerializeField] private Sprite[] bodyJumpFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] jumpCarryFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] bodyLandFrames = Array.Empty<Sprite>();
        [SerializeField] private Sprite[] landCarryFrames = Array.Empty<Sprite>();

        private LongwatchAimSelection _selection = LongwatchAimSelection.Default;
        private bool _hasValidAim;
        private bool _ownsRenderer;
        private bool _configurationValid;
        private bool _hasObservedAimRevision;
        private uint _lastAimRevision;
        private Sprite _lastBodySprite;
        private int _lastDirectionIndex = -1;
        private bool _lastFacingLeft;
        private bool _hasRenderedPose;
        private Latch9RenderedPose2D _renderedPose;

        public PlayerAim2D PlayerAim => playerAim;
        public PlayerAnimator2D PlayerAnimator => playerAnimator;
        public PlayerUnarmedArmsPresenter2D UnarmedPresenter => unarmedPresenter;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public SpriteRenderer ArmsWeaponSpriteRenderer => armsWeaponSpriteRenderer;
        public bool OwnsRenderer => _ownsRenderer;
        public int IdleAimPoseCount => idleAimPoses?.Length ?? 0;
        public int RunAimPoseCount => runAimPoses?.Length ?? 0;
        public int BackpedalAimPoseCount => backpedalAimPoses?.Length ?? 0;
        public int CrouchAimPoseCount => crouchAimPoses?.Length ?? 0;
        public int FallAimPoseCount => fallAimPoses?.Length ?? 0;
        public int JumpCarryFrameCount => jumpCarryFrames?.Length ?? 0;
        public int LandCarryFrameCount => landCarryFrames?.Length ?? 0;
        public LongwatchAimSelection Selection => _selection;

        public void Configure(
            PlayerAim2D aim,
            PlayerAnimator2D animator,
            PlayerUnarmedArmsPresenter2D unarmed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            IReadOnlyList<Sprite> idleBodyFrames,
            IReadOnlyList<Latch9IdleAimPose> idlePoses,
            IReadOnlyList<Sprite> runBodyFrames,
            IReadOnlyList<Latch9RunAimPose> runPoses,
            IReadOnlyList<Sprite> backpedalBodyFrames,
            IReadOnlyList<Latch9BackpedalAimPose> backpedalPoses,
            IReadOnlyList<Sprite> crouchBodyFrames,
            IReadOnlyList<Latch9CrouchAimPose> crouchPoses,
            IReadOnlyList<Sprite> fallBodyFrames,
            IReadOnlyList<Latch9FallAimPose> fallPoses,
            IReadOnlyList<Sprite> jumpBodyFrames,
            IReadOnlyList<Sprite> jumpCarry,
            IReadOnlyList<Sprite> landBodyFrames,
            IReadOnlyList<Sprite> landCarry)
        {
            ReleaseRenderer();

            playerAim = aim;
            playerAnimator = animator;
            unarmedPresenter = unarmed;
            bodySpriteRenderer = bodyRenderer;
            armsWeaponSpriteRenderer = armsRenderer;
            bodyIdleFrames = Copy(idleBodyFrames);
            idleAimPoses = Copy(idlePoses);
            bodyRunFrames = Copy(runBodyFrames);
            runAimPoses = Copy(runPoses);
            bodyBackpedalFrames = Copy(backpedalBodyFrames);
            backpedalAimPoses = Copy(backpedalPoses);
            bodyCrouchFrames = Copy(crouchBodyFrames);
            crouchAimPoses = Copy(crouchPoses);
            bodyFallFrames = Copy(fallBodyFrames);
            fallAimPoses = Copy(fallPoses);
            bodyJumpFrames = Copy(jumpBodyFrames);
            jumpCarryFrames = Copy(jumpCarry);
            bodyLandFrames = Copy(landBodyFrames);
            landCarryFrames = Copy(landCarry);

            _configurationValid = ValidateConfiguration();
            ResetCachedState();
            if (!_configurationValid)
            {
                Debug.LogError(
                    "Latch-9 presenter received an incomplete Idle/Run/Backpedal/Crouch/Jump/Fall/Land configuration.",
                    this);
            }
        }

        public bool TryGetCurrentRenderedPose(out Latch9RenderedPose2D pose)
        {
            if (_ownsRenderer && _hasRenderedPose)
            {
                pose = _renderedPose;
                return true;
            }

            pose = default;
            return false;
        }

        public Sprite GetBodyIdleFrame(int index) => bodyIdleFrames[index];
        public Sprite GetBodyRunFrame(int index) => bodyRunFrames[index];
        public Sprite GetBodyBackpedalFrame(int index) => bodyBackpedalFrames[index];
        public Sprite GetBodyCrouchFrame(int index) => bodyCrouchFrames[index];
        public Sprite GetBodyFallFrame(int index) => bodyFallFrames[index];

        private void OnEnable()
        {
            _configurationValid = ValidateConfiguration();
            ResetCachedState();
        }

        private void OnDisable()
        {
            ReleaseRenderer();
        }

        private void LateUpdate()
        {
            if (!CanOwnRenderer())
            {
                ReleaseRenderer();
                return;
            }

            UpdateAimSelection();
            int directionIndex = _selection.DirectionIndex;
            PlayerAnimationState? state = playerAnimator.CurrentState;
            if (!state.HasValue)
            {
                ReleaseRenderer();
                return;
            }

            Sprite bodySprite = bodySpriteRenderer.sprite;
            Sprite nextSprite;
            int frameIndex;
            int authoredAngleDegrees = 0;
            Latch9MuzzleState2D muzzleState = Latch9MuzzleState2D.Idle;
            bool exposesMuzzlePose = true;

            switch (state.Value)
            {
                case PlayerAnimationState.Idle:
                    if (directionIndex < 0 || directionIndex >= idleAimPoses.Length ||
                        !TryResolveFrame(bodyIdleFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = idleAimPoses[directionIndex].GetFrame(frameIndex);
                    authoredAngleDegrees = idleAimPoses[directionIndex].AngleDegrees;
                    muzzleState = Latch9MuzzleState2D.Idle;
                    break;

                case PlayerAnimationState.Run:
                    if (directionIndex < 0 || directionIndex >= runAimPoses.Length ||
                        !TryResolveFrame(bodyRunFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = runAimPoses[directionIndex].GetFrame(frameIndex);
                    authoredAngleDegrees = runAimPoses[directionIndex].AngleDegrees;
                    muzzleState = Latch9MuzzleState2D.Run;
                    break;

                case PlayerAnimationState.Backpedal:
                    if (directionIndex < 0 || directionIndex >= backpedalAimPoses.Length ||
                        !TryResolveFrame(bodyBackpedalFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = backpedalAimPoses[directionIndex].GetFrame(frameIndex);
                    authoredAngleDegrees = backpedalAimPoses[directionIndex].AngleDegrees;
                    muzzleState = Latch9MuzzleState2D.Backpedal;
                    break;

                case PlayerAnimationState.CrouchIdle:
                case PlayerAnimationState.CrouchMove:
                    if (directionIndex < 0 || directionIndex >= crouchAimPoses.Length ||
                        !TryResolveFrame(bodyCrouchFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = crouchAimPoses[directionIndex].GetFrame(frameIndex);
                    authoredAngleDegrees = crouchAimPoses[directionIndex].AngleDegrees;
                    muzzleState = Latch9MuzzleState2D.Crouch;
                    break;

                case PlayerAnimationState.Jump:
                    if (!TryResolveFrame(bodyJumpFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = jumpCarryFrames[frameIndex];
                    exposesMuzzlePose = false;
                    break;

                case PlayerAnimationState.Fall:
                    if (directionIndex < 0 || directionIndex >= fallAimPoses.Length ||
                        !TryResolveFrame(bodyFallFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = fallAimPoses[directionIndex].GetFrame(frameIndex);
                    authoredAngleDegrees = fallAimPoses[directionIndex].AngleDegrees;
                    muzzleState = Latch9MuzzleState2D.Fall;
                    break;

                case PlayerAnimationState.Land:
                    if (!TryResolveFrame(bodyLandFrames, bodySprite, out frameIndex))
                    {
                        ReleaseRenderer();
                        return;
                    }
                    nextSprite = landCarryFrames[frameIndex];
                    exposesMuzzlePose = false;
                    break;

                default:
                    ReleaseRenderer();
                    return;
            }

            AcquireRenderer();
            bool facingLeft = armsWeaponSpriteRenderer.flipX;
            if (exposesMuzzlePose)
            {
                _renderedPose = new Latch9RenderedPose2D(
                    muzzleState,
                    directionIndex,
                    authoredAngleDegrees,
                    frameIndex,
                    facingLeft);
                _hasRenderedPose = true;
            }
            else
            {
                _hasRenderedPose = false;
            }

            if (bodySprite == _lastBodySprite && directionIndex == _lastDirectionIndex &&
                facingLeft == _lastFacingLeft)
            {
                return;
            }

            armsWeaponSpriteRenderer.sprite = nextSprite;
            _lastBodySprite = bodySprite;
            _lastDirectionIndex = directionIndex;
            _lastFacingLeft = facingLeft;
        }

        private bool CanOwnRenderer()
        {
            if (!_configurationValid)
            {
                return false;
            }

            PlayerAnimationState? state = playerAnimator.CurrentState;
            return state.HasValue &&
                (state.Value == PlayerAnimationState.Idle ||
                 state.Value == PlayerAnimationState.Run ||
                 state.Value == PlayerAnimationState.Backpedal ||
                 state.Value == PlayerAnimationState.CrouchIdle ||
                 state.Value == PlayerAnimationState.CrouchMove ||
                 state.Value == PlayerAnimationState.Jump ||
                 state.Value == PlayerAnimationState.Fall ||
                 state.Value == PlayerAnimationState.Land);
        }

        private static bool TryResolveFrame(Sprite[] bodyFrames, Sprite displayedBody, out int frameIndex)
        {
            for (int index = 0; index < bodyFrames.Length; index++)
            {
                if (displayedBody == bodyFrames[index])
                {
                    frameIndex = index;
                    return true;
                }
            }

            frameIndex = -1;
            return false;
        }

        private void UpdateAimSelection()
        {
            if (!playerAim.HasValidAim ||
                _hasObservedAimRevision && playerAim.AimRevision == _lastAimRevision)
            {
                return;
            }

            _lastAimRevision = playerAim.AimRevision;
            _hasObservedAimRevision = true;
            if (LongwatchAimMath.TrySelect(
                    playerAim.ContinuousAimDirection,
                    playerAim.FacingLeft,
                    _hasValidAim,
                    _selection,
                    out LongwatchAimSelection next))
            {
                _selection = next;
                _hasValidAim = true;
            }
        }

        private bool ValidateConfiguration()
        {
            if (playerAim == null || playerAnimator == null || unarmedPresenter == null ||
                bodySpriteRenderer == null || armsWeaponSpriteRenderer == null ||
                bodyIdleFrames == null || bodyIdleFrames.Length != 2 ||
                idleAimPoses == null || idleAimPoses.Length != 19 ||
                bodyRunFrames == null || bodyRunFrames.Length != 6 ||
                runAimPoses == null || runAimPoses.Length != 19 ||
                bodyBackpedalFrames == null || bodyBackpedalFrames.Length != 4 ||
                backpedalAimPoses == null || backpedalAimPoses.Length != 19 ||
                bodyCrouchFrames == null || bodyCrouchFrames.Length != 6 ||
                crouchAimPoses == null || crouchAimPoses.Length != 19 ||
                bodyFallFrames == null || bodyFallFrames.Length != 1 ||
                fallAimPoses == null || fallAimPoses.Length != 19 ||
                bodyJumpFrames == null || bodyJumpFrames.Length != 3 ||
                jumpCarryFrames == null || jumpCarryFrames.Length != 3 ||
                bodyLandFrames == null || bodyLandFrames.Length != 2 ||
                landCarryFrames == null || landCarryFrames.Length != 2)
            {
                return false;
            }

            for (int directionIndex = 0; directionIndex < 19; directionIndex++)
            {
                if (idleAimPoses[directionIndex].Frame0 == null ||
                    idleAimPoses[directionIndex].Frame1 == null ||
                    fallAimPoses[directionIndex].Frame0 == null)
                {
                    return false;
                }

                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    if (runAimPoses[directionIndex].GetFrame(frameIndex) == null ||
                        crouchAimPoses[directionIndex].GetFrame(frameIndex) == null)
                    {
                        return false;
                    }
                }

                for (int frameIndex = 0; frameIndex < 4; frameIndex++)
                {
                    if (backpedalAimPoses[directionIndex].GetFrame(frameIndex) == null)
                    {
                        return false;
                    }
                }
            }

            for (int frameIndex = 0; frameIndex < jumpCarryFrames.Length; frameIndex++)
            {
                if (jumpCarryFrames[frameIndex] == null)
                {
                    return false;
                }
            }

            for (int frameIndex = 0; frameIndex < landCarryFrames.Length; frameIndex++)
            {
                if (landCarryFrames[frameIndex] == null)
                {
                    return false;
                }
            }

            return true;
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
            _hasRenderedPose = false;
            if (!_ownsRenderer)
            {
                return;
            }

            _ownsRenderer = false;
            unarmedPresenter?.SetRendererOwnership(true);
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
        }

        private void ResetCachedState()
        {
            _selection = LongwatchAimSelection.Default;
            _hasValidAim = false;
            _hasObservedAimRevision = false;
            _lastAimRevision = 0;
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
            _lastFacingLeft = false;
            _hasRenderedPose = false;
        }

        private static T[] Copy<T>(IReadOnlyList<T> source)
        {
            if (source == null)
            {
                return Array.Empty<T>();
            }

            T[] copy = new T[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }
}
