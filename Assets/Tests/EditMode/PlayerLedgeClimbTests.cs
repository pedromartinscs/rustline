using NUnit.Framework;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class PlayerLedgeClimbTests
    {
        private PlayerMovementConfig _config;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_config);
        }

        [TestCase(0, 0f, 0f)]
        [TestCase(1, 8f, 8f)]
        [TestCase(2, 14f, 14f)]
        [TestCase(3, 18f, 17f)]
        [TestCase(4, 19f, 19f)]
        public void AuthoredRootOffsets_MatchSourcePixelsAndMirrorOnlyX(
            int frame,
            float expectedX,
            float expectedY)
        {
            Vector2 right = PlayerLedgeClimbMotion2D.GetRootOffset(frame, 1) *
                PlayerLedgeClimbMotion2D.PixelsPerUnit;
            Vector2 left = PlayerLedgeClimbMotion2D.GetRootOffset(frame, -1) *
                PlayerLedgeClimbMotion2D.PixelsPerUnit;

            Assert.That(right.x, Is.EqualTo(expectedX).Within(0.0001f));
            Assert.That(right.y, Is.EqualTo(expectedY).Within(0.0001f));
            Assert.That(left.x, Is.EqualTo(-expectedX).Within(0.0001f));
            Assert.That(left.y, Is.EqualTo(expectedY).Within(0.0001f));
        }

        [TestCase(0f, 0)]
        [TestCase(0.1f, 1)]
        [TestCase(0.2f, 2)]
        [TestCase(0.3f, 3)]
        [TestCase(0.4f, 4)]
        [TestCase(0.4999f, 4)]
        [TestCase(0.5f, 5)]
        [TestCase(0.5999f, 5)]
        public void FrameIndex_ChangesAtExactTenthSecondBoundaries(float elapsed, int expected)
        {
            Assert.That(PlayerLedgeClimbMotion2D.GetFrameIndex(elapsed), Is.EqualTo(expected));
        }

        [Test]
        public void MotionContract_IsSixFramesAtTenFpsForSixTenthsOfASecond()
        {
            Assert.That(PlayerLedgeClimbMotion2D.FrameCount, Is.EqualTo(6));
            Assert.That(PlayerLedgeClimbMotion2D.FrameDuration, Is.EqualTo(0.1f));
            Assert.That(PlayerLedgeClimbMotion2D.TotalDuration, Is.EqualTo(0.6f).Within(0.0001f));
        }

        [Test]
        public void FrameFour_RemainsAtAuthoredContactForItsEntireInterval()
        {
            Vector2 capture = new Vector2(2f, 3f);
            Vector2 final = new Vector2(8f, 9f);
            Vector2 frameFour = capture + PlayerLedgeClimbMotion2D.GetRootOffset(4, 1);

            Assert.That(PlayerLedgeClimbMotion2D.GetPosition(capture, final, 1, 0.4f),
                Is.EqualTo(frameFour));
            Assert.That(PlayerLedgeClimbMotion2D.GetPosition(capture, final, 1, 0.4999f),
                Is.EqualTo(frameFour),
                "Authored frame 4 moved after its calibrated hands reached the ledge.");
        }

        [TestCase(1, 8.25f)]
        [TestCase(-1, -4.75f)]
        public void RecoveryFrame_UsesCalibratedCaptureYAndGeometryDerivedFinalX(
            int side,
            float finalRootX)
        {
            Vector2 capture = new Vector2(2f, 3f);
            Vector2 final = new Vector2(finalRootX, 123f);
            Vector2 recovery = PlayerLedgeClimbMotion2D.GetPosition(capture, final, side, 0.5f);

            Assert.That(PlayerLedgeClimbMotion2D.RecoveryFrameYOffsetPixels, Is.EqualTo(31f));
            Assert.That(recovery.x, Is.EqualTo(finalRootX).Within(0.0001f),
                "Recovery X must come from the independently validated standing geometry.");
            Assert.That(recovery.y,
                Is.EqualTo(capture.y + 31f / PlayerLedgeClimbMotion2D.PixelsPerUnit)
                    .Within(0.0001f));
        }

        [TestCase(false, -1f, 1f, false, 0f, true)]
        [TestCase(false, -1f, -1f, true, 0f, true)]
        [TestCase(true, -1f, 1f, false, 0f, false)]
        [TestCase(false, 0.11f, 1f, false, 0f, false)]
        [TestCase(false, -1f, 0.09f, false, 0f, false)]
        [TestCase(false, -1f, 1f, true, 0f, false)]
        [TestCase(false, -1f, -1f, false, 0f, false)]
        [TestCase(false, -1f, 1f, false, 0.01f, false)]
        public void Eligibility_UsesAirStateInputFacingAndWallKickLock(
            bool grounded,
            float verticalVelocity,
            float horizontalInput,
            bool facingLeft,
            float lockRemaining,
            bool expected)
        {
            Assert.That(PlayerMovementMath.CanAttemptLedgeClimb(
                grounded,
                verticalVelocity,
                horizontalInput,
                facingLeft,
                lockRemaining,
                _config), Is.EqualTo(expected));
        }
    }
}
