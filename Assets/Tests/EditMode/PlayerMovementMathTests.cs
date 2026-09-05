using NUnit.Framework;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class PlayerMovementMathTests
    {
        private PlayerMovementConfig _config;

        [SetUp]
        public void SetUp() => _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();

        [TearDown]
        public void TearDown() => Object.DestroyImmediate(_config);

        [Test]
        public void GroundAcceleration_ApproachesConfiguredMaximum()
        {
            float result = PlayerMovementMath.CalculateHorizontalVelocity(0f, 1f, true, false, _config, 1f);
            Assert.That(result, Is.EqualTo(_config.MaxGroundSpeed).Within(0.0001f));
        }

        [Test]
        public void Reversal_UsesDirectionChangeAcceleration()
        {
            float result = PlayerMovementMath.CalculateHorizontalVelocity(5f, -1f, true, false, _config, 0.05f);
            Assert.That(result, Is.EqualTo(0.5f).Within(0.0001f));
        }

        [Test]
        public void AirWithoutInput_PreservesHorizontalMomentum()
        {
            float result = PlayerMovementMath.CalculateHorizontalVelocity(4f, 0f, false, false, _config, 0.5f);
            Assert.That(result, Is.EqualTo(4f).Within(0.0001f));
        }

        [Test]
        public void FallGravity_RespectsTerminalSpeed()
        {
            float result = PlayerMovementMath.ApplyGravity(-17.9f, -9.81f, _config, 1f);
            Assert.That(result, Is.EqualTo(-_config.MaxFallSpeed).Within(0.0001f));
        }

        [Test]
        public void JumpRelease_CutsOnlyPositiveVelocity()
        {
            Assert.That(PlayerMovementMath.CutJumpVelocity(10f, _config), Is.EqualTo(4.5f).Within(0.0001f));
            Assert.That(PlayerMovementMath.CutJumpVelocity(-3f, _config), Is.EqualTo(-3f).Within(0.0001f));
        }

        [TestCase(1f, false, 7f)]
        [TestCase(-1f, false, 5f)]
        [TestCase(-1f, true, 7f)]
        [TestCase(1f, true, 5f)]
        public void GroundSpeedLimit_DependsOnMovementRelativeToAimFacing(
            float input,
            bool facingLeft,
            float expectedSpeed)
        {
            Assert.That(PlayerMovementMath.GetGroundSpeedLimit(input, facingLeft, _config),
                Is.EqualTo(expectedSpeed));
        }

        [Test]
        public void FacingChange_ApproachesNewLimitWithoutVelocitySnap()
        {
            float result = PlayerMovementMath.CalculateHorizontalVelocity(
                7f, 1f, true, true, _config, 0.02f);

            Assert.That(result, Is.EqualTo(5.9f).Within(0.0001f));
            Assert.That(result, Is.GreaterThan(_config.MaxBackpedalGroundSpeed));
        }

        [Test]
        public void AirSpeed_RemainsIndependentOfAimFacing()
        {
            float rightFacing = PlayerMovementMath.CalculateHorizontalVelocity(
                0f, -1f, false, false, _config, 1f);
            float leftFacing = PlayerMovementMath.CalculateHorizontalVelocity(
                0f, -1f, false, true, _config, 1f);

            Assert.That(rightFacing, Is.EqualTo(-_config.MaxAirSpeed));
            Assert.That(leftFacing, Is.EqualTo(rightFacing));
        }
    }
}
