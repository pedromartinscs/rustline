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
        [TestCase(0.0999f, 0)]
        [TestCase(0.1f, 1)]
        [TestCase(0.1999f, 1)]
        [TestCase(0.2f, 2)]
        [TestCase(0.3f, 3)]
        [TestCase(0.4f, 4)]
        [TestCase(10f, 4)]
        public void FrameIndex_ChangesAtExactTenthSecondBoundaries(float elapsed, int expected)
        {
            Assert.That(PlayerLedgeClimbMotion2D.GetFrameIndex(elapsed), Is.EqualTo(expected));
        }

        [Test]
        public void FinalFrame_RemainsAtAuthoredContactUntilCommittedTraversalBoundary()
        {
            Vector2 capture = new Vector2(2f, 3f);
            Vector2 frameFour = capture + PlayerLedgeClimbMotion2D.GetRootOffset(4, 1);

            Assert.That(PlayerLedgeClimbMotion2D.TotalDuration, Is.EqualTo(0.5f).Within(0.0001f));
            Assert.That(PlayerLedgeClimbMotion2D.GetPosition(capture, 1, 0.4f),
                Is.EqualTo(frameFour));
            Assert.That(PlayerLedgeClimbMotion2D.GetPosition(capture, 1, 0.4999f),
                Is.EqualTo(frameFour),
                "Authored frame 4 moved after its calibrated hands reached the ledge.");
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
