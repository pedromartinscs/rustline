using System;
using Rustline.Gameplay.Player;
using Unity.Profiling;
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

    [Serializable]
    public struct LongwatchBackpedalAimPose
    {
        [SerializeField] private int angleDegrees;
        [SerializeField] private Sprite frame0;
        [SerializeField] private Sprite frame1;
        [SerializeField] private Sprite frame2;
        [SerializeField] private Sprite frame3;

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

    /// <summary>
    /// Owns the shared overlay renderer while the prototype Longwatch is in an
    /// authored aim-capable state. Body remains Animator-driven; its displayed
    /// frame selects the matching weapon frame at the nearest ten-degree pose.
    /// </summary>
    [DefaultExecutionOrder(100)]
    [DisallowMultipleComponent]
    public sealed class PlayerLongwatchAimPresenter2D : MonoBehaviour
    {
        private static readonly ProfilerMarker LongwatchMarker =
            new ProfilerMarker("Rustline.Presentation.Longwatch");

        [SerializeField] private PlayerAim2D playerAim;
        [SerializeField] private PlayerAnimator2D playerAnimator;
        [SerializeField] private PlayerUnarmedArmsPresenter2D unarmedPresenter;
        [SerializeField] private SpriteRenderer bodySpriteRenderer;
        [SerializeField] private SpriteRenderer armsWeaponSpriteRenderer;
        [SerializeField] private Sprite[] bodyIdleFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchIdleAimPose[] idleAimPoses = Array.Empty<LongwatchIdleAimPose>();
        [SerializeField] private Sprite[] bodyRunFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchRunAimPose[] runAimPoses = Array.Empty<LongwatchRunAimPose>();
        [SerializeField] private Sprite[] bodyBackpedalFrames = Array.Empty<Sprite>();
        [SerializeField] private LongwatchBackpedalAimPose[] backpedalAimPoses = Array.Empty<LongwatchBackpedalAimPose>();

        private LongwatchAimSelection _selection = LongwatchAimSelection.Default;
        private bool _hasValidAim;
        private bool _ownsRenderer;
        private Sprite _lastBodySprite;
        private int _lastDirectionIndex = -1;
        private bool _configurationValid;
        private bool _hasObservedAimRevision;
        private uint _lastAimRevision;

        public PlayerAim2D PlayerAim => playerAim;
        public PlayerAnimator2D PlayerAnimator => playerAnimator;
        public PlayerUnarmedArmsPresenter2D UnarmedPresenter => unarmedPresenter;
        public SpriteRenderer BodySpriteRenderer => bodySpriteRenderer;
        public SpriteRenderer ArmsWeaponSpriteRenderer => armsWeaponSpriteRenderer;
        public int BodyIdleFrameCount => bodyIdleFrames?.Length ?? 0;
        public int BodyRunFrameCount => bodyRunFrames?.Length ?? 0;
        public int IdleAimPoseCount => idleAimPoses?.Length ?? 0;
        public int RunAimPoseCount => runAimPoses?.Length ?? 0;
        public int BodyBackpedalFrameCount => bodyBackpedalFrames?.Length ?? 0;
        public int BackpedalAimPoseCount => backpedalAimPoses?.Length ?? 0;
        public bool OwnsRenderer => _ownsRenderer;
        public bool HasValidAim => playerAim != null && playerAim.HasValidAim;
        public LongwatchAimSelection Selection => _selection;
        public Vector2 ContinuousAimDirection => playerAim != null
            ? playerAim.ContinuousAimDirection
            : Vector2.right;
        public Vector3 AimOriginWorld => playerAim != null ? playerAim.AimOriginWorld : transform.position;

        private void LateUpdate()
        {
            using (LongwatchMarker.Auto())
            {
                if (!CanOwnRenderer())
                {
                    ReleaseRenderer();
                    return;
                }

                UpdateAimSelection();
                AcquireRenderer();

                int directionIndex = _selection.DirectionIndex;
                Sprite bodySprite = bodySpriteRenderer.sprite;
                if (bodySprite == _lastBodySprite && directionIndex == _lastDirectionIndex)
                {
                    return;
                }

                if (!TryResolveDisplayedBodyFrame(bodySprite, out PlayerAnimationState bodyState, out int bodyFrameIndex))
                {
                    return;
                }

                _lastBodySprite = bodySprite;
                _lastDirectionIndex = directionIndex;
                switch (bodyState)
                {
                    case PlayerAnimationState.Idle:
                        armsWeaponSpriteRenderer.sprite = idleAimPoses[directionIndex].GetFrame(bodyFrameIndex);
                        break;
                    case PlayerAnimationState.Run:
                        armsWeaponSpriteRenderer.sprite = runAimPoses[directionIndex].GetFrame(bodyFrameIndex);
                        break;
                    case PlayerAnimationState.Backpedal:
                        armsWeaponSpriteRenderer.sprite = backpedalAimPoses[directionIndex].GetFrame(bodyFrameIndex);
                        break;
                }
            }
        }

        private void OnEnable()
        {
            _configurationValid = ValidateConfiguration();
            _hasObservedAimRevision = false;
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
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

        public LongwatchBackpedalAimPose GetBackpedalAimPose(int index)
        {
            return backpedalAimPoses[index];
        }

        public Sprite GetBodyBackpedalFrame(int index)
        {
            return bodyBackpedalFrames[index];
        }

        private bool CanOwnRenderer()
        {
            if (!_configurationValid)
            {
                return false;
            }

            PlayerAnimationState? state = playerAnimator.CurrentState;
            return state == PlayerAnimationState.Idle || state == PlayerAnimationState.Run ||
                   state == PlayerAnimationState.Backpedal;
        }

        private bool TryResolveDisplayedBodyFrame(
            Sprite displayedBody,
            out PlayerAnimationState bodyState,
            out int frameIndex)
        {
            for (int index = 0; index < bodyIdleFrames.Length; index++)
            {
                if (displayedBody == bodyIdleFrames[index])
                {
                    bodyState = PlayerAnimationState.Idle;
                    frameIndex = index;
                    return true;
                }
            }

            for (int index = 0; index < bodyRunFrames.Length; index++)
            {
                if (displayedBody == bodyRunFrames[index])
                {
                    bodyState = PlayerAnimationState.Run;
                    frameIndex = index;
                    return true;
                }
            }

            for (int index = 0; index < bodyBackpedalFrames.Length; index++)
            {
                if (displayedBody == bodyBackpedalFrames[index])
                {
                    bodyState = PlayerAnimationState.Backpedal;
                    frameIndex = index;
                    return true;
                }
            }

            bodyState = PlayerAnimationState.Idle;
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
            return playerAim != null && playerAnimator != null && unarmedPresenter != null &&
                   bodySpriteRenderer != null && armsWeaponSpriteRenderer != null &&
                   bodyIdleFrames != null && bodyIdleFrames.Length == 2 &&
                   idleAimPoses != null && idleAimPoses.Length == 19 &&
                   bodyRunFrames != null && bodyRunFrames.Length == 6 &&
                   runAimPoses != null && runAimPoses.Length == 19 &&
                   bodyBackpedalFrames != null && bodyBackpedalFrames.Length == 4 &&
                   backpedalAimPoses != null && backpedalAimPoses.Length == 19;
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
            unarmedPresenter?.SetRendererOwnership(true);
            _lastBodySprite = null;
            _lastDirectionIndex = -1;
        }
    }
}
