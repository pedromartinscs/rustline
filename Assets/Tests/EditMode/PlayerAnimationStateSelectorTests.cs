using NUnit.Framework;
using Rustline.Presentation;

namespace Rustline.Tests
{
    public sealed class PlayerAnimationStateSelectorTests
    {
        [TestCase(true, 0f, 0f, false, PlayerAnimationState.Idle)]
        [TestCase(true, 1f, 0f, false, PlayerAnimationState.Run)]
        [TestCase(true, 0f, 0f, true, PlayerAnimationState.Land)]
        [TestCase(false, 1f, 1f, false, PlayerAnimationState.Jump)]
        [TestCase(false, 1f, -0.1f, false, PlayerAnimationState.Fall)]
        public void Select_ReturnsPhysicsDrivenState(bool grounded, float horizontalVelocity,
            float verticalVelocity, bool showingLanding, PlayerAnimationState expected)
        {
            PlayerAnimationState state = PlayerAnimationStateSelector.Select(
                grounded, horizontalVelocity, verticalVelocity, showingLanding, 0.2f, 0.15f);
            Assert.That(state, Is.EqualTo(expected));
        }
    }
}
