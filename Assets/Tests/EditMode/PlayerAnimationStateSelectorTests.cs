using NUnit.Framework;
using Rustline.Presentation;

namespace Rustline.Tests
{
    public sealed class PlayerAnimationStateSelectorTests
    {
        [TestCase(true, 0f, 0f, false, false, PlayerAnimationState.Idle)]
        [TestCase(true, 1f, 0f, false, false, PlayerAnimationState.Run)]
        [TestCase(true, -1f, 0f, false, false, PlayerAnimationState.Backpedal)]
        [TestCase(true, -1f, 0f, false, true, PlayerAnimationState.Run)]
        [TestCase(true, 1f, 0f, false, true, PlayerAnimationState.Backpedal)]
        [TestCase(true, 0.19f, 0f, false, true, PlayerAnimationState.Idle)]
        [TestCase(true, 0f, 0f, true, false, PlayerAnimationState.Land)]
        [TestCase(false, 1f, 1f, false, true, PlayerAnimationState.Jump)]
        [TestCase(false, 1f, -0.1f, false, true, PlayerAnimationState.Fall)]
        public void Select_ReturnsPhysicsDrivenState(bool grounded, float horizontalVelocity,
            float verticalVelocity, bool showingLanding, bool facingFlipX, PlayerAnimationState expected)
        {
            PlayerAnimationState state = PlayerAnimationStateSelector.Select(
                grounded, horizontalVelocity, verticalVelocity, showingLanding, facingFlipX, 0.2f, 0.15f);
            Assert.That(state, Is.EqualTo(expected));
        }
    }
}
