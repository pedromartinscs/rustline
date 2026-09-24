using NUnit.Framework;
using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class BombardierEditModeTests
    {
        [TestCase(0f, 0f)]
        [TestCase(-8f, 0f)]
        [TestCase(8f, 0f)]
        [TestCase(5f, 4f)]
        [TestCase(5f, -4f)]
        public void Ballistics_ReachesLockedTargetAtNominalTime(float x, float y)
        {
            Vector2 origin = new Vector2(2f, 3f);
            Vector2 target = origin + new Vector2(x, y);
            Vector2 velocity = BombardierBallistics2D.InitialVelocity(origin, target);
            Vector2 reached = BombardierBallistics2D.Position(origin, velocity,
                BombardierBallistics2D.FlightTime);
            Assert.That(reached.x, Is.EqualTo(target.x).Within(0.0001f));
            Assert.That(reached.y, Is.EqualTo(target.y).Within(0.0001f));
            if (x != 0f) Assert.That(Mathf.Sign(velocity.x), Is.EqualTo(Mathf.Sign(x)));
        }

        [TestCase(12f, 0f, true)]
        [TestCase(12.001f, 0f, false)]
        [TestCase(0f, 6f, true)]
        [TestCase(0f, 6.001f, false)]
        [TestCase(-12f, -6f, true)]
        public void Detection_UsesInclusiveAxisBounds(float x, float y, bool expected)
        {
            Assert.That(Bombardier2D.IsInsideDetectionRange(Vector2.zero,
                new Vector2(x, y)), Is.EqualTo(expected));
        }

        [TestCase(1.75f, true)]
        [TestCase(1.751f, false)]
        public void Explosion_UsesInclusiveRadius(float x, bool expected)
        {
            Assert.That(BombardierBallistics2D.IsInsideExplosion(Vector2.zero,
                new Vector2(x, 0f)), Is.EqualTo(expected));
        }

    }
}
