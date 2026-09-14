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

    /// <summary>
    /// Incremental Latch-9 armed presenter. The current authored package contains
    /// directional Idle only, so every unsupported locomotion state deliberately
    /// releases the shared overlay renderer back to the unarmed presenter.
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

        private LongwatchAimSelection _selection = LongwatchAimSelection.Default;
        private bool _hasValidAim;
        private bool _ownsRenderer;
        private bool _configurationValid;
        private bool _hasObservedAimRevision;
        private uint _lastAimRevision;
        private Sprite _lastBodySprite;
        private int _lastDirectionIndex = -1;
        private bool _lastFacingLeft;

        public bool OwnsRenderer => _ownsRenderer;
        public int IdleAimPoseCount => idleAimPoses?.Length ?? 0;
        public LongwatchAimSelection Selection => _selection;

        public void Configure(
            PlayerAim2D aim,
            PlayerAnimator2D animator,
            PlayerUnarmedArmsPresenter2D unarmed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            IReadOnlyList<Sprite> idleBodyFrames,
            IReadOnlyList<Latch9IdleAimPose> poses)
        {
            ReleaseRenderer();

            playerAim = aim;
            playerAnimator = animator;
            unarmedPresenter = unarmed;
            bodySpriteRenderer = bodyRenderer;
            armsWeaponSpriteRenderer = armsRenderer;
            bodyIdleFrames = Copy(idleBodyFrames);
            idleAimPoses = Copy(poses);

            _configurationValid = ValidateConfiguration();
            ResetCachedState();
            if (!_configurationValid)
            {
                Debug.LogError("Latch-9 Idle presenter received an incomplete preview configuration.", this);
            }
        }

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
            if (directionIndex < 0 || directionIndex >= idleAimPoses.Length)
            {
                ReleaseRenderer();
                return;
            }

            Sprite bodySprite = bodySpriteRenderer.sprite;
            if (!TryResolveIdleFrame(bodySprite, out int frameIndex))
            {
                ReleaseRenderer();
                return;
            }

            AcquireRenderer();
            bool facingLeft = armsWeaponSpriteRenderer.flipX;
            if (bodySprite == _lastBodySprite && directionIndex == _lastDirectionIndex &&
                facingLeft == _lastFacingLeft)
            {
                return;
            }

            armsWeaponSpriteRenderer.sprite = idleAimPoses[directionIndex].GetFrame(frameIndex);
            _lastBodySprite = bodySprite;
            _lastDirectionIndex = directionIndex;
            _lastFacingLeft = facingLeft;
        }

        private bool CanOwnRenderer()
        {
            return _configurationValid && playerAnimator.CurrentState == PlayerAnimationState.Idle;
        }

        private bool TryResolveIdleFrame(Sprite displayedBody, out int frameIndex)
        {
            for (int index = 0; index < bodyIdleFrames.Length; index++)
            {
                if (displayedBody == bodyIdleFrames[index])
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
                idleAimPoses == null || idleAimPoses.Length != 19)
            {
                return false;
            }

            for (int index = 0; index < idleAimPoses.Length; index++)
            {
                if (idleAimPoses[index].Frame0 == null || idleAimPoses[index].Frame1 == null)
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
        }

        private static Sprite[] Copy(IReadOnlyList<Sprite> source)
        {
            if (source == null)
            {
                return Array.Empty<Sprite>();
            }

            Sprite[] copy = new Sprite[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }

        private static Latch9IdleAimPose[] Copy(IReadOnlyList<Latch9IdleAimPose> source)
        {
            if (source == null)
            {
                return Array.Empty<Latch9IdleAimPose>();
            }

            Latch9IdleAimPose[] copy = new Latch9IdleAimPose[source.Count];
            for (int index = 0; index < source.Count; index++)
            {
                copy[index] = source[index];
            }

            return copy;
        }
    }
}
