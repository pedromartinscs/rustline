using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Weapons;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class CombatHealthTests
    {
        private const string LongwatchDefinitionPath = "Assets/Config/Weapons/LongwatchDMR.asset";

        private GameObject _root;
        private CombatHealth2D _health;

        [SetUp]
        public void SetUp()
        {
            _root = new GameObject("Combat Health Test Root");
            _health = _root.AddComponent<CombatHealth2D>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_root);
        }

        [Test]
        public void Health_ThreeLongwatchHitsProduceExpectedRhythmAndSingleDeath()
        {
            int damageEvents = 0;
            int deathEvents = 0;
            _health.Damaged += _ => damageEvents++;
            _health.Died += _ => deathEvents++;

            Assert.That(_health.MaximumHealth, Is.EqualTo(100));
            Assert.That(_health.CurrentHealth, Is.EqualTo(100));
            Assert.That(_health.IsAlive, Is.True);
            Assert.That(_health.IsDead, Is.False);

            DamageResult2D first = Apply(40);
            Assert.That(first.PreviousHealth, Is.EqualTo(100));
            Assert.That(first.CurrentHealth, Is.EqualTo(60));
            Assert.That(first.AppliedAmount, Is.EqualTo(40));
            Assert.That(first.Killed, Is.False);
            Assert.That(_health.CurrentHealth, Is.EqualTo(60));

            DamageResult2D second = Apply(40);
            Assert.That(second.CurrentHealth, Is.EqualTo(20));
            Assert.That(second.Killed, Is.False);
            Assert.That(_health.CurrentHealth, Is.EqualTo(20));

            DamageResult2D third = Apply(40);
            Assert.That(third.CurrentHealth, Is.Zero);
            Assert.That(third.AppliedAmount, Is.EqualTo(20));
            Assert.That(third.Killed, Is.True);
            Assert.That(_health.CurrentHealth, Is.Zero);
            Assert.That(_health.IsDead, Is.True);
            Assert.That(_health.IsAlive, Is.False);
            Assert.That(damageEvents, Is.EqualTo(3));
            Assert.That(deathEvents, Is.EqualTo(1));

            DamageResult2D afterDeath = Apply(40);
            Assert.That(afterDeath.DidApply, Is.False);
            Assert.That(_health.CurrentHealth, Is.Zero);
            Assert.That(damageEvents, Is.EqualTo(3));
            Assert.That(deathEvents, Is.EqualTo(1));
        }

        [Test]
        public void Health_NonPositiveDamageDoesNothingAndHealthNeverBecomesNegative()
        {
            int damageEvents = 0;
            _health.Damaged += _ => damageEvents++;

            Assert.That(Apply(0).DidApply, Is.False);
            Assert.That(Apply(-25).DidApply, Is.False);
            Assert.That(_health.CurrentHealth, Is.EqualTo(100));
            Assert.That(damageEvents, Is.Zero);

            DamageResult2D lethal = Apply(1000);
            Assert.That(lethal.AppliedAmount, Is.EqualTo(100));
            Assert.That(_health.CurrentHealth, Is.Zero);
            Assert.That(_health.CurrentHealth, Is.GreaterThanOrEqualTo(0));
        }

        [Test]
        public void ResetHealth_RestoresExactMaximumAndClearsDeadState()
        {
            int resetEvents = 0;
            int deathEvents = 0;
            _health.HealthReset += () => resetEvents++;
            _health.Died += _ => deathEvents++;

            Apply(100);
            Assert.That(_health.IsDead, Is.True);
            Assert.That(deathEvents, Is.EqualTo(1));

            _health.ResetHealth();

            Assert.That(_health.CurrentHealth, Is.EqualTo(_health.MaximumHealth));
            Assert.That(_health.CurrentHealth, Is.EqualTo(100));
            Assert.That(_health.IsAlive, Is.True);
            Assert.That(_health.IsDead, Is.False);
            Assert.That(resetEvents, Is.EqualTo(1));

            Apply(100);
            Assert.That(deathEvents, Is.EqualTo(2),
                "A reset must permit exactly one death event for the next life.");
        }

        [Test]
        public void WeaponHitbox_ExplicitChildReferenceRoutesExactDamageAndMetadataToRootHealth()
        {
            GameObject child = new GameObject("Hitbox");
            child.transform.SetParent(_root.transform, false);
            BoxCollider2D collider = child.AddComponent<BoxCollider2D>();
            WeaponHitbox2D hitbox = child.AddComponent<WeaponHitbox2D>();
            hitbox.Configure(_health);
            WeaponDefinition2D weapon =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchDefinitionPath);
            Vector2 point = new Vector2(12.5f, -3.25f);
            Vector2 direction = new Vector2(0.8f, 0.6f);
            var weaponHit = new WeaponHitInfo2D(
                weapon,
                new Vector2(4f, 2f),
                direction,
                point,
                Vector2.left,
                8.5f,
                40,
                collider);
            DamageResult2D observedResult = default;
            int observedDamageEvents = 0;
            _health.Damaged += result =>
            {
                observedResult = result;
                observedDamageEvents++;
            };

            hitbox.ReceiveHit(in weaponHit);

            Assert.That(child.GetComponent<CombatHealth2D>(), Is.Null,
                "The child hitbox must not acquire or discover its own health component.");
            Assert.That(_root.GetComponent<CombatHealth2D>(), Is.SameAs(_health));
            Assert.That(hitbox.Health, Is.SameAs(_health),
                "Child-to-root routing must be the explicit serialized/component reference.");
            Assert.That(hitbox.GetComponent<Collider2D>(), Is.SameAs(collider));
            Assert.That(hitbox.ReceivedHitCount, Is.EqualTo(1));
            Assert.That(observedDamageEvents, Is.EqualTo(1));
            Assert.That(_health.CurrentHealth, Is.EqualTo(60));
            Assert.That(observedResult.Info.Amount, Is.EqualTo(40));
            Assert.That(observedResult.Info.Point, Is.EqualTo(point));
            Assert.That(observedResult.Info.Direction, Is.EqualTo(direction));
            Assert.That(observedResult.Info.Source, Is.SameAs(weapon));
            Assert.That(hitbox.LastDamageResult.Info.Point, Is.EqualTo(point));
            Assert.That(hitbox.LastDamageResult.Info.Direction, Is.EqualTo(direction));
            Assert.That(hitbox.LastDamageResult.Info.Source, Is.SameAs(weapon));
        }

        private DamageResult2D Apply(int amount)
        {
            var info = new DamageInfo2D(amount, Vector2.one, Vector2.right, null);
            return _health.ApplyDamage(in info);
        }
    }
}
