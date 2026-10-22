using System.Collections;
using NUnit.Framework;
using Rustline.Diagnostics;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class FirstPlayablePlayModeTests
    {
        private FirstPlayableDirector2D _director;
        private Rigidbody2D _body;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            Assert.That(_director, Is.Not.Null);
            _body = _director.Player.GetComponent<Rigidbody2D>();
            yield return new WaitForFixedUpdate();
        }

        [UnityTest]
        public IEnumerator Scene_HasOnePlayerFiveRoutedEnemiesAndSupportedReleaseFloor()
        {
            Assert.That(Object.FindObjectsByType<PlayerMotor2D>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
            Assert.That(Object.FindAnyObjectByType<MovementLabRespawn>(), Is.Null);
            int members = 0;
            foreach (FirstPlayableEncounter2D wave in _director.Encounters)
            {
                Assert.That(wave.IsActive, Is.False);
                foreach (CombatHealth2D health in wave.Members)
                {
                    members++;
                    Assert.That(health.GetComponentInChildren<WeaponHitbox2D>(true).Health, Is.SameAs(health));
                }
            }
            Assert.That(members, Is.EqualTo(5));
            var initializer = Object.FindAnyObjectByType<Rustline.Physics.TilemapCompositeColliderInitializer2D>();
            Assert.That(initializer.HasGeneratedGeometry, Is.True);
            for (int i = 0; i < 100; i++) yield return new WaitForFixedUpdate();
            Assert.That(_body.position.y, Is.GreaterThan(0.95f));
            Assert.That(_director.Progress.Retries, Is.Zero);
        }

        [UnityTest]
        public IEnumerator Longwatch_ActualHitscanKillsSentryAndEncounterStaysCleared()
        {
            Place(new Vector2(-17f, 1.02f));
            for (int i = 0; i < 25; i++) yield return new WaitForFixedUpdate();
            var wave = _director.Encounters[0];
            Assert.That(wave.IsActive, Is.True);
            var aim = _director.Player.GetComponent<PlayerAim2D>();
            aim.enabled = false;
            var weapon = _director.Player.GetComponent<PlayerWeaponController2D>();
            var health = wave.Members[0];
            var collider = health.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>();
            for (int i = 0; i < 2; i++)
            {
                aim.ApplyWorldAimVector((Vector2)collider.bounds.center - (Vector2)aim.AimOriginWorld);
                Assert.That(weapon.TryFire(Time.time), Is.True);
                for (int frame = 0; frame < 7; frame++) yield return new WaitForFixedUpdate();
            }
            Assert.That(health.IsDead, Is.True);
            Assert.That(_director.Progress.ClearedEncounters, Is.EqualTo(1));
            yield return new WaitForSeconds(1.5f);
            Assert.That(health.IsDead, Is.True, "Production enemies must not use the lab's resurrection owner.");
        }

        [UnityTest]
        public IEnumerator Death_CheckpointRetryAndCompletionRestoreCorrectStateWithoutDuplicates()
        {
            Place(new Vector2(-17f, 1.02f)); yield return null;
            Defeat(0); yield return null;
            Place(new Vector2(3f, 0.1f)); yield return null;
            Defeat(1); yield return null;
            Assert.That(_director.Progress.ClearedEncounters, Is.EqualTo(2));
            Place(new Vector2(22f, 1.08f)); yield return null;
            Assert.That(_director.Progress.HasCheckpoint, Is.True);
            var weapon = _director.Player.GetComponent<PlayerWeaponController2D>();
            var ammo = _director.Player.GetComponent<PlayerWeaponAmmo2D>();
            ammo.ConsumeAcceptedShot(weapon.WeaponDefinition);
            Place(new Vector2(35f, 17.08f)); yield return null;
            Assert.That(_director.Encounters[2].IsActive, Is.True);
            int playerId = _director.Player.GetInstanceID();
            var damage = new DamageInfo2D(100, _body.position, Vector2.zero, null);
            _director.PlayerHealth.ApplyDamage(in damage);
            Assert.That(_director.IsRetrying, Is.True);
            Assert.That(weapon.enabled, Is.False);
            _director.RetryNow();
            Assert.That(_director.Player.GetInstanceID(), Is.EqualTo(playerId));
            Assert.That(_body.position, Is.EqualTo((Vector2)_director.ActiveSpawn.position));
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(100));
            Assert.That(ammo.GetSnapshot(weapon.WeaponDefinition).Rounds, Is.EqualTo(50));
            Assert.That(_director.Progress.ClearedEncounters, Is.EqualTo(2));
            Assert.That(_director.Encounters[0].Members[0].IsDead, Is.True);
            Assert.That(_director.Encounters[2].Members[0].IsAlive, Is.True);
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(FindObjectsSortMode.None), Is.Empty);
            Place(new Vector2(35f, 17.08f)); yield return null;
            Defeat(2); yield return null;
            Place(new Vector2(43f, 0.1f)); yield return null;
            Assert.That(_director.Progress.ExtractionTime, Is.Zero, "Lower floor cannot complete the level.");
            Place(new Vector2(43f, 17.08f));
            yield return new WaitForSeconds(2.2f);
            Assert.That(_director.Progress.IsComplete, Is.True);
            Assert.That(_body.simulated, Is.False);
            _director.RestartRun();
            Assert.That(_director.Progress.HasCheckpoint, Is.False);
            Assert.That(_director.Progress.IsComplete, Is.False);
            Assert.That(_director.Progress.Retries, Is.Zero);
            Assert.That(_body.simulated, Is.True);
        }

        [UnityTest]
        public IEnumerator Latch_ActualProjectileDamagesSentryThroughItsExplicitHitbox()
        {
            Place(new Vector2(-17f, 1.02f));
            var equipment = _director.Player.GetComponent<PlayerWeaponEquipment2D>();
            Assert.That(equipment.RequestSlot(1), Is.True);
            yield return new WaitForSeconds(0.5f);
            var aim = _director.Player.GetComponent<PlayerAim2D>();
            aim.enabled = false;
            var health = _director.Encounters[0].Members[0];
            var collider = health.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>();
            aim.ApplyWorldAimVector((Vector2)collider.bounds.center - (Vector2)aim.AimOriginWorld);
            var weapon = _director.Player.GetComponent<PlayerWeaponController2D>();
            Assert.That(weapon.WeaponDefinition.DeliveryMode, Is.EqualTo(WeaponDeliveryMode2D.Projectile));
            Assert.That(weapon.TryFire(Time.time), Is.True);
            yield return new WaitForSeconds(0.5f);
            Assert.That(health.CurrentHealth, Is.EqualTo(75));
        }

        [UnityTest]
        public IEnumerator Sentry_LandedStrikeDealsTwentyOnceAndGroundBlocksDetection()
        {
            Place(new Vector2(-11.5f, 1.05f)); yield return null;
            var sentry = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            var wall = new GameObject("Sentry LOS fixture");
            wall.layer = 6;
            wall.transform.position = new Vector2(-10.75f, 2.4f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.1f, 2.5f);
            Physics2D.SyncTransforms();
            for (int i = 0; i < 35; i++) yield return new WaitForFixedUpdate();
            Assert.That(sentry.State, Is.EqualTo(SentryState.Patrol));
            Object.Destroy(wall);
            yield return null;
            for (int i = 0; i < 90 && sentry.StrikeCount == 0; i++) yield return new WaitForFixedUpdate();
            Assert.That(sentry.StrikeCount, Is.EqualTo(1));
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(80));
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(80));
        }

        [UnityTest]
        public IEnumerator Sentry_AttackUsesHealthAndDodgingBehindLockedFacingAvoidsDamage()
        {
            Place(new Vector2(-11.5f, 1.05f)); yield return null;
            var sentry = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            for (int i = 0; i < 80 && sentry.State != SentryState.Windup; i++) yield return new WaitForFixedUpdate();
            Assert.That(sentry.State, Is.EqualTo(SentryState.Windup));
            Place(new Vector2(sentry.transform.position.x + 1.2f, 1.05f));
            for (int i = 0; i < 32; i++) yield return new WaitForFixedUpdate();
            Assert.That(sentry.StrikeCount, Is.EqualTo(1));
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(100));
        }

        private void Place(Vector2 position)
        {
            _body.position = position;
            _body.linearVelocity = Vector2.zero;
            _director.Player.ResetAfterRespawn();
            Physics2D.SyncTransforms();
        }

        private void Defeat(int wave)
        {
            Assert.That(_director.Encounters[wave].IsActive, Is.True);
            foreach (CombatHealth2D member in _director.Encounters[wave].Members)
            {
                var damage = new DamageInfo2D(1000, member.transform.position, Vector2.right, null);
                member.ApplyDamage(in damage);
            }
        }
    }
}
