using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

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
        public void CrouchMovement_UsesThreeUnitGroundLimit()
        {
            float result = PlayerMovementMath.CalculateHorizontalVelocity(
                0f, 1f, true, false, true, _config, 1f);

            Assert.That(_config.MaxCrouchGroundSpeed, Is.EqualTo(3f));
            Assert.That(result, Is.EqualTo(3f).Within(0.0001f));
        }

        [Test]
        public void CrouchCollider_PreservesStandingFootAnchor()
        {
            float standingBottom = _config.StandingColliderOffset.y - _config.StandingColliderSize.y * 0.5f;
            float crouchBottom = _config.CrouchColliderOffset.y - _config.CrouchColliderSize.y * 0.5f;

            Assert.That(_config.StandingColliderSize, Is.EqualTo(new Vector2(1.05f, 2.75f)));
            Assert.That(_config.CrouchColliderSize, Is.EqualTo(new Vector2(1.05f, 1.75f)));
            Assert.That(crouchBottom, Is.EqualTo(standingBottom).Within(0.0001f));
        }

        [Test]
        public void CrouchInput_UsesCentralizedKeyboardAndGamepadBindings()
        {
            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            InputAction crouch = input?.FindActionMap("Player", false)?.FindAction("Crouch", false);

            Assert.That(crouch, Is.Not.Null);
            Assert.That(crouch.bindings.Any(binding => binding.path == "<Keyboard>/s"), Is.True);
            Assert.That(crouch.bindings.Any(binding => binding.path == "<Keyboard>/downArrow"), Is.True);
            Assert.That(crouch.bindings.Any(binding => binding.path == "<Gamepad>/dpad/down"), Is.True);
            Assert.That(crouch.bindings.Any(binding => binding.path == "<Gamepad>/leftStick/down"), Is.True);
        }

        [Test]
        public void LandPresentationDuration_MatchesAcceptedStateGate()
        {
            PlayerMovementConfig asset = AssetDatabase.LoadAssetAtPath<PlayerMovementConfig>(
                "Assets/Config/Player/PlayerMovementConfig.asset");

            Assert.That(_config.LandPresentationDuration, Is.EqualTo(0.22f));
            Assert.That(asset, Is.Not.Null);
            Assert.That(asset.LandPresentationDuration, Is.EqualTo(0.22f));
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
        [TestCase(-1f, false, 4f)]
        [TestCase(-1f, true, 7f)]
        [TestCase(1f, true, 4f)]
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

        [TestCase(true, -1f, 1, false)]
        [TestCase(false, 0f, 1, false)]
        [TestCase(false, -1f, 1, false)]
        [TestCase(false, 1f, 1, true)]
        [TestCase(false, -1f, -1, true)]
        public void WallBrace_RequiresAirborneInputTowardWall(
            bool grounded, float input, int wallSide, bool expected)
        {
            bool result = PlayerMovementMath.CanWallBrace(
                grounded, -2f, input, wallSide, 0, 0f, _config);

            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void WallBrace_RejectsStrongAscentAndLockedSameWall()
        {
            Assert.That(PlayerMovementMath.CanWallBrace(
                false, 1f, 1f, 1, 0, 0f, _config), Is.False);
            Assert.That(PlayerMovementMath.CanWallBrace(
                false, -2f, 1f, 1, 1, 0.1f, _config), Is.False);
            Assert.That(PlayerMovementMath.CanWallBrace(
                false, -2f, -1f, -1, 1, 0.1f, _config), Is.True,
                "The brief same-wall lock must not falsely describe the opposite wall as the same wall.");
        }

        [Test]
        public void WallBrace_CapsDescentWithoutFreezingIt()
        {
            Assert.That(PlayerMovementMath.CapWallBraceFallVelocity(-10f, _config), Is.EqualTo(-4f));
            Assert.That(PlayerMovementMath.CapWallBraceFallVelocity(-2f, _config), Is.EqualTo(-2f));
        }

        [Test]
        public void WallKick_IsSymmetricUpAndAway()
        {
            Assert.That(PlayerMovementMath.GetWallKickVelocity(1, _config),
                Is.EqualTo(new Vector2(-8f, 11.5f)));
            Assert.That(PlayerMovementMath.GetWallKickVelocity(-1, _config),
                Is.EqualTo(new Vector2(8f, 11.5f)));
            Assert.That(_config.JumpSpeed, Is.EqualTo(12.5f),
                "Wall tuning must not change the accepted normal jump impulse.");
        }
    }
}
