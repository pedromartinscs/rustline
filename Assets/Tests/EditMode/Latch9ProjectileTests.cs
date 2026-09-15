using NUnit.Framework;
using Rustline.Gameplay.Environment;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class Latch9ProjectileTests
    {
        private const string LatchDefinitionPath = "Assets/Config/Weapons/Latch9.asset";
        private const string LongwatchDefinitionPath = "Assets/Config/Weapons/LongwatchDMR.asset";
        private const float ReflectionTolerance = 0.00001f;

        [Test]
        public void LatchDefinition_UsesVisibleProjectileDelivery()
        {
            WeaponDefinition2D definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LatchDefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.IsSane(out string reason), Is.True, reason);
            Assert.That(definition.DeliveryMode, Is.EqualTo(WeaponDeliveryMode2D.Projectile));
            Assert.That(definition.ProjectileSpeed, Is.EqualTo(30f));
            Assert.That(definition.MaxBounces, Is.EqualTo(3));
        }

        [Test]
        public void Longwatch_RemainsHitscan()
        {
            WeaponDefinition2D definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchDefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.IsSane(out string reason), Is.True, reason);
            Assert.That(definition.DeliveryMode, Is.EqualTo(WeaponDeliveryMode2D.Hitscan));
            Assert.That(definition.ProjectileSpeed, Is.Zero);
        }

        [Test]
        public void ProjectileVisual_UsesCanonicalCyanAndViolet()
        {
            Color32 conventional = Latch9Projectile2D.ResolveVisualColor(WeaponShotMode2D.Conventional);
            Color32 bouncing = Latch9Projectile2D.ResolveVisualColor(WeaponShotMode2D.Bouncing);

            Assert.That(conventional, Is.EqualTo(RustlinePalette.GetColor(20)));
            Assert.That(bouncing, Is.EqualTo(RustlinePalette.GetColor(22)));
            Assert.That(RustlinePalette.IsCanonical(conventional), Is.True);
            Assert.That(RustlinePalette.IsCanonical(bouncing), Is.True);
            Assert.That(Latch9Projectile2D.VisualLength, Is.EqualTo(4f / 16f));
            Assert.That(Latch9Projectile2D.VisualWidth, Is.EqualTo(1f / 16f));
        }

        [Test]
        public void LatchCombatTargetContract_SeparatesCombatTargetsFromEnvironmentalReceivers()
        {
            Assert.That(
                typeof(IWeaponCombatTarget2D).IsAssignableFrom(typeof(WeaponHitbox2D)),
                Is.True);
            Assert.That(
                typeof(IWeaponCombatTarget2D).IsAssignableFrom(typeof(DiagnosticCombatTarget2D)),
                Is.True);
            Assert.That(
                typeof(IWeaponHitReceiver2D).IsAssignableFrom(typeof(BreachableTilemap2D)),
                Is.True);
            Assert.That(
                typeof(IWeaponCombatTarget2D).IsAssignableFrom(typeof(BreachableTilemap2D)),
                Is.False);
        }

        [Test]
        public void PostBounceGuard_UsesSubPixelSeparationButMeaningfulClearance()
        {
            Assert.That(Latch9Projectile2D.PostBounceSeparation, Is.EqualTo(0.25f / 16f));
            Assert.That(Latch9Projectile2D.ImmediateRehitGuardDistance, Is.EqualTo(0.5f / 16f));
            Assert.That(
                Latch9Projectile2D.ImmediateRehitGuardDistance,
                Is.GreaterThan(Latch9Projectile2D.PostBounceSeparation));
        }

        [Test]
        public void PostBounceGuard_IgnoresOnlyImmediateSameColliderRehits()
        {
            var firstObject = new GameObject("Bounce Surface A");
            var secondObject = new GameObject("Bounce Surface B");
            BoxCollider2D first = firstObject.AddComponent<BoxCollider2D>();
            BoxCollider2D second = secondObject.AddComponent<BoxCollider2D>();

            try
            {
                Assert.That(
                    Latch9Projectile2D.ShouldIgnoreImmediateRehit(
                        first,
                        first,
                        0f,
                        0f),
                    Is.True);
                Assert.That(
                    Latch9Projectile2D.ShouldIgnoreImmediateRehit(
                        second,
                        first,
                        0f,
                        0f),
                    Is.False);
                Assert.That(
                    Latch9Projectile2D.ShouldIgnoreImmediateRehit(
                        first,
                        first,
                        Latch9Projectile2D.ImmediateRehitGuardDistance * 2f,
                        0f),
                    Is.False);
                Assert.That(
                    Latch9Projectile2D.ShouldIgnoreImmediateRehit(
                        first,
                        first,
                        0f,
                        Latch9Projectile2D.ImmediateRehitGuardDistance * 2f),
                    Is.False);
            }
            finally
            {
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(secondObject);
            }
        }

        [Test]
        public void BounceReflection_FloorPreservesTangentAndInvertsNormalComponent()
        {
            Vector2 incoming = new Vector2(3f, -4f).normalized;
            Vector2 reflected = WeaponBounceMath2D.Reflect(incoming, Vector2.up);

            Assert.That(reflected.x, Is.EqualTo(incoming.x).Within(ReflectionTolerance));
            Assert.That(reflected.y, Is.EqualTo(-incoming.y).Within(ReflectionTolerance));
            Assert.That(reflected.magnitude, Is.EqualTo(1f).Within(ReflectionTolerance));
        }

        [Test]
        public void BounceReflection_WallPreservesVerticalComponentAndReversesHorizontalComponent()
        {
            Vector2 incoming = new Vector2(4f, 3f).normalized;
            Vector2 reflected = WeaponBounceMath2D.Reflect(incoming, Vector2.left);

            Assert.That(reflected.x, Is.EqualTo(-incoming.x).Within(ReflectionTolerance));
            Assert.That(reflected.y, Is.EqualTo(incoming.y).Within(ReflectionTolerance));
            Assert.That(reflected.magnitude, Is.EqualTo(1f).Within(ReflectionTolerance));
        }

        [Test]
        public void BounceReflection_ObliqueSurfaceUsesSurfaceNormal()
        {
            Vector2 normal = new Vector2(-1f, 1f).normalized;
            Vector2 reflected = WeaponBounceMath2D.Reflect(Vector2.right, normal);

            Assert.That(reflected.x, Is.EqualTo(0f).Within(ReflectionTolerance));
            Assert.That(reflected.y, Is.EqualTo(1f).Within(ReflectionTolerance));
        }

        [Test]
        public void BounceReflection_IncidenceAndReflectionAnglesMatch()
        {
            Vector2 incoming = new Vector2(0.8f, -0.6f).normalized;
            Vector2 normal = new Vector2(0.2f, 1f).normalized;
            Vector2 reflected = WeaponBounceMath2D.Reflect(incoming, normal);

            float incomingNormalComponent = Vector2.Dot(incoming, normal);
            float reflectedNormalComponent = Vector2.Dot(reflected, normal);
            Vector2 incomingTangent = incoming - incomingNormalComponent * normal;
            Vector2 reflectedTangent = reflected - reflectedNormalComponent * normal;

            Assert.That(
                reflectedNormalComponent,
                Is.EqualTo(-incomingNormalComponent).Within(ReflectionTolerance));
            Assert.That(
                reflectedTangent.x,
                Is.EqualTo(incomingTangent.x).Within(ReflectionTolerance));
            Assert.That(
                reflectedTangent.y,
                Is.EqualTo(incomingTangent.y).Within(ReflectionTolerance));
        }

        [Test]
        public void BouncingMode_AllowsExactlyThreeReflections()
        {
            const int maxBounces = 3;

            Assert.That(
                WeaponBounceMath2D.CanReflect(WeaponShotMode2D.Bouncing, 0, maxBounces),
                Is.True);
            Assert.That(
                WeaponBounceMath2D.CanReflect(WeaponShotMode2D.Bouncing, 1, maxBounces),
                Is.True);
            Assert.That(
                WeaponBounceMath2D.CanReflect(WeaponShotMode2D.Bouncing, 2, maxBounces),
                Is.True);
            Assert.That(
                WeaponBounceMath2D.CanReflect(WeaponShotMode2D.Bouncing, 3, maxBounces),
                Is.False);
            Assert.That(
                WeaponBounceMath2D.CanReflect(WeaponShotMode2D.Conventional, 0, maxBounces),
                Is.False);
        }

        [Test]
        public void BounceDamage_RemainsFourThreeTwoOne()
        {
            WeaponDefinition2D definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LatchDefinitionPath);

            Assert.That(definition.ResolveDamage(WeaponShotMode2D.Bouncing, 0), Is.EqualTo(4));
            Assert.That(definition.ResolveDamage(WeaponShotMode2D.Bouncing, 1), Is.EqualTo(3));
            Assert.That(definition.ResolveDamage(WeaponShotMode2D.Bouncing, 2), Is.EqualTo(2));
            Assert.That(definition.ResolveDamage(WeaponShotMode2D.Bouncing, 3), Is.EqualTo(1));
        }
    }
}
