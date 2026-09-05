using NUnit.Framework;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class PlayerAimTests
    {
        [Test]
        public void ValidAim_PreservesContinuousNormalizedDirection()
        {
            Vector2 input = new Vector2(2.75f, -4.25f);

            Assert.That(PlayerAimMath.TryResolve(
                input, false, false, out Vector2 direction, out bool flipX), Is.True);
            Assert.That(direction, Is.EqualTo(input.normalized));
            Assert.That(flipX, Is.False);
        }

        [TestCase(90f, false, false)]
        [TestCase(90f, true, true)]
        [TestCase(86f, false, false)]
        [TestCase(86f, true, true)]
        [TestCase(84f, true, false)]
        [TestCase(94f, false, false)]
        [TestCase(94f, true, true)]
        [TestCase(96f, false, true)]
        [TestCase(-90f, false, false)]
        [TestCase(-90f, true, true)]
        [TestCase(-86f, false, false)]
        [TestCase(-86f, true, true)]
        [TestCase(-84f, true, false)]
        [TestCase(-94f, false, false)]
        [TestCase(-94f, true, true)]
        [TestCase(-96f, false, true)]
        public void VerticalHysteresis_RetainsPriorHemisphereInsideFiveDegrees(
            float degrees,
            bool previousFlipX,
            bool expectedFlipX)
        {
            Vector2 direction = new Vector2(
                Mathf.Cos(degrees * Mathf.Deg2Rad),
                Mathf.Sin(degrees * Mathf.Deg2Rad));

            Assert.That(PlayerAimMath.TryResolve(
                direction, true, previousFlipX, out Vector2 continuous, out bool flipX), Is.True);
            Assert.That(continuous, Is.EqualTo(direction.normalized));
            Assert.That(flipX, Is.EqualTo(expectedFlipX));
        }

        [Test]
        public void InvalidAim_RetainsFacingAndDoesNotInventDirection()
        {
            Assert.That(PlayerAimMath.TryResolve(
                Vector2.zero, true, true, out Vector2 direction, out bool flipX), Is.False);
            Assert.That(direction, Is.EqualTo(Vector2.zero));
            Assert.That(flipX, Is.True);
        }

        [Test]
        public void NoPriorFacing_DefaultsRightInsideVerticalZone()
        {
            Assert.That(PlayerAimMath.TryResolve(
                Vector2.up, false, true, out _, out bool flipX), Is.True);
            Assert.That(flipX, Is.False);
        }

        [Test]
        public void ComponentInvalidAim_RetainsLastContinuousState()
        {
            GameObject gameObject = new GameObject("Aim test");
            try
            {
                PlayerAim2D aim = gameObject.AddComponent<PlayerAim2D>();
                Vector2 expected = new Vector2(-2f, 1f).normalized;
                Assert.That(aim.ApplyWorldAimVector(new Vector2(-2f, 1f)), Is.True);
                Assert.That(aim.ApplyWorldAimVector(Vector2.zero), Is.False);
                Assert.That(aim.ContinuousAimDirection, Is.EqualTo(expected));
                Assert.That(aim.FacingFlipX, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(gameObject);
            }
        }

        [Test]
        public void AimOriginContract_IsExactlyThirtyEightSourcePixels()
        {
            Assert.That(PlayerAim2D.AimOriginOffsetSourcePixels, Is.EqualTo(38f));
            Assert.That(PlayerAim2D.AimOriginOffsetWorldUnits, Is.EqualTo(2.375f));
            Assert.That(PlayerAimMath.VerticalHemisphereHysteresisDegrees, Is.EqualTo(5f));
        }
    }
}
