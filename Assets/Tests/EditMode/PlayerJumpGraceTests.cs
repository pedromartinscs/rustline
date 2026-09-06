using NUnit.Framework;
using Rustline.Gameplay.Player;

namespace Rustline.Tests
{
    public sealed class PlayerJumpGraceTests
    {
        [Test]
        public void CoyoteWindow_AllowsJumpShortlyAfterLeavingGround()
        {
            var grace = new PlayerJumpGrace();
            grace.Tick(true, 0.02f, 0.12f);
            grace.Tick(false, 0.05f, 0.12f);
            grace.Buffer(0.12f);
            Assert.That(grace.TryConsume(), Is.True);
            Assert.That(grace.TryConsume(), Is.False);
        }

        [Test]
        public void ExpiredCoyoteWindow_DoesNotAllowJump()
        {
            var grace = new PlayerJumpGrace();
            grace.Tick(true, 0.02f, 0.12f);
            grace.Tick(false, 0.13f, 0.12f);
            grace.Buffer(0.12f);
            Assert.That(grace.TryConsume(), Is.False);
        }

        [Test]
        public void BufferedJump_IsConsumedOnLanding()
        {
            var grace = new PlayerJumpGrace();
            grace.Buffer(0.12f);
            grace.Tick(false, 0.05f, 0.12f);
            Assert.That(grace.TryConsume(), Is.False);
            grace.Tick(true, 0.02f, 0.12f);
            Assert.That(grace.TryConsume(), Is.True);
        }

        [Test]
        public void ExpiredBuffer_IsNotConsumedOnLanding()
        {
            var grace = new PlayerJumpGrace();
            grace.Buffer(0.1f);
            grace.Tick(false, 0.11f, 0.12f);
            grace.Tick(true, 0.02f, 0.12f);
            Assert.That(grace.TryConsume(), Is.False);
        }

        [Test]
        public void BufferedJump_CanBeConsumedByWallBraceWithoutGroundCoyote()
        {
            var grace = new PlayerJumpGrace();
            grace.Buffer(0.12f);
            grace.Tick(false, 0.02f, 0.12f);

            Assert.That(grace.HasBufferedJump, Is.True);
            Assert.That(grace.TryConsumeBuffered(), Is.True);
            Assert.That(grace.HasBufferedJump, Is.False);
            Assert.That(grace.TryConsume(), Is.False);
        }
    }
}
