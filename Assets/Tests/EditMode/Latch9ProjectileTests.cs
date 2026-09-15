using NUnit.Framework;
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
