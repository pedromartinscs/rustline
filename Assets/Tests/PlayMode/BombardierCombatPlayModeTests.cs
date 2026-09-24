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
    public sealed class BombardierCombatPlayModeTests
    {
        private Bombardier2D _enemy;
        private MovementLabBombardierEncounter2D _encounter;
        private MovementLabRespawn _respawn;
        private Rigidbody2D _playerBody;
        private CapsuleCollider2D _playerCollider;
        private CombatHealth2D _playerHealth;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;
            _enemy = Object.FindAnyObjectByType<Bombardier2D>();
            _encounter = Object.FindAnyObjectByType<MovementLabBombardierEncounter2D>();
            _respawn = Object.FindAnyObjectByType<MovementLabRespawn>();
            Assert.That(_enemy, Is.Not.Null);
            Assert.That(_encounter, Is.Not.Null);
            Assert.That(_respawn, Is.Not.Null);
            _playerBody = _respawn.GetComponent<Rigidbody2D>();
            _playerCollider = _respawn.GetComponent<CapsuleCollider2D>();
            _playerHealth = _respawn.GetComponent<CombatHealth2D>();
            Assert.That(_playerHealth.MaximumHealth, Is.EqualTo(100));
            Assert.That(_playerCollider.gameObject.layer, Is.EqualTo(9));
            Assert.That(Camera.main.cullingMask & (1 << 9), Is.Not.Zero);
            Assert.That(Camera.main.cullingMask & (1 << 8), Is.Zero);
        }

        [UnityTest]
        public IEnumerator Detection_LocksOnceAndOneLaunchEntersRecovery()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Windup));
            Vector2 locked = _enemy.LockedTargetPoint;
            Assert.That(locked, Is.EqualTo((Vector2)_playerCollider.bounds.center));
            PlacePlayer(153f);
            for (int i = 0; i < 12; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LockedTargetPoint, Is.EqualTo(locked));
            Assert.That(_enemy.LaunchCount, Is.Zero);
            for (int i = 0; i < 40 && _enemy.State == BombardierState.Windup; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Recover));
            Assert.That(_enemy.LaunchCount, Is.EqualTo(1));
            Assert.That(_enemy.ActiveBombCount, Is.EqualTo(1));
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator NonlethalHitPause_FreezesCommittedWindupTimer()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Windup));
            float remaining = _enemy.StateTimeRemaining;
            var hit = new DamageInfo2D(1, _enemy.Body.position, Vector2.right, null);
            _enemy.Health.ApplyDamage(in hit);
            Assert.That(_enemy.HitPauseRemaining, Is.GreaterThan(0f));
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Windup));
            Assert.That(_enemy.StateTimeRemaining, Is.EqualTo(remaining));
            Assert.That(_enemy.LaunchCount, Is.Zero);
            for (int i = 0; i < 50 && _enemy.State == BombardierState.Windup; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator GroundOcclusion_PreventsDetection()
        {
            PlacePlayer(158f);
            GameObject wall = new GameObject("Ground LOS test wall");
            wall.layer = 6;
            wall.transform.position = new Vector2(161f, 2f);
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 4f);
            Physics2D.SyncTransforms();
            _enemy.ResetForEncounter();
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Patrol));
            Assert.That(_enemy.LaunchCount, Is.Zero);
            Object.Destroy(wall);
        }

        [UnityTest]
        public IEnumerator LaunchedBomb_ArcsAndFirstGroundContactExplodesWithoutBounce()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            yield return new WaitForFixedUpdate();
            PlacePlayer(150f);
            for (int i = 0; i < 40 && _enemy.LaunchCount == 0; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(1));
            BombardierBomb2D bomb = Object.FindAnyObjectByType<BombardierBomb2D>();
            Assert.That(bomb, Is.Not.Null);
            float launchY = bomb.transform.position.y;
            float highestY = launchY;
            for (int i = 0; i < 100 && !bomb.HasExploded; i++)
            {
                yield return new WaitForFixedUpdate();
                highestY = Mathf.Max(highestY, bomb.transform.position.y);
            }
            Assert.That(highestY, Is.GreaterThan(launchY + 0.2f));
            Assert.That(bomb.HasExploded, Is.True);
            Assert.That(bomb.FirstContactCollider.gameObject.layer, Is.EqualTo(6));
            Assert.That(bomb.TimedOut, Is.False);
            Vector2 impact = bomb.ImpactPoint;
            yield return new WaitForFixedUpdate();
            Assert.That(bomb == null || bomb.ImpactPoint == impact, Is.True,
                "The bomb moved or bounced after first contact.");
        }

        [UnityTest]
        public IEnumerator DirectPlayerContact_ExplodesAndAppliesOneDamage()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            yield return new WaitForFixedUpdate();
            for (int i = 0; i < 100 && _enemy.ActiveBombCount == 0; i++)
                yield return new WaitForFixedUpdate();
            BombardierBomb2D bomb = Object.FindAnyObjectByType<BombardierBomb2D>();
            Assert.That(bomb, Is.Not.Null);
            for (int i = 0; i < 100 && !bomb.HasExploded; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(bomb.HasExploded, Is.True);
            Assert.That(bomb.FirstContactCollider, Is.SameAs(_playerCollider));
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(75));
            for (int i = 0; i < 5; i++) yield return new WaitForFixedUpdate();
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(75));
        }

        [UnityTest]
        public IEnumerator Blast_RadiusOcclusionAndFourHitsRespawnSameInstances()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            Vector2 playerPoint = _playerCollider.bounds.center;
            Vector2 farPoint = new Vector2(_playerCollider.bounds.max.x + 1.76f, playerPoint.y);
            Assert.That(BombardierExplosion2D.TryDamagePlayer(farPoint, Vector2.right,
                null, _playerHealth, _playerCollider, _enemy), Is.False);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));

            GameObject wall = new GameObject("Blast occluder");
            wall.layer = 6;
            wall.transform.position = new Vector2(159f, playerPoint.y);
            BoxCollider2D wallCollider = wall.AddComponent<BoxCollider2D>();
            wallCollider.size = new Vector2(0.15f, 2f);
            Physics2D.SyncTransforms();
            Assert.That(BombardierExplosion2D.TryDamagePlayer(
                new Vector2(159.5f, playerPoint.y), Vector2.right, wallCollider,
                _playerHealth, _playerCollider, _enemy), Is.False);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));
            Object.Destroy(wall);
            yield return null;

            int[] expected = { 75, 50, 25, 0 };
            int index = 0;
            _playerHealth.Damaged += result =>
            {
                if (result.DidApply) Assert.That(result.CurrentHealth, Is.EqualTo(expected[index++]));
            };
            int playerId = _respawn.GetInstanceID();
            int enemyId = _enemy.GetInstanceID();
            for (int i = 0; i < 4; i++)
                Assert.That(BombardierExplosion2D.TryDamagePlayer(playerPoint, Vector2.up,
                    _playerCollider, _playerHealth, _playerCollider, _enemy), Is.True);
            Assert.That(index, Is.EqualTo(4));
            Assert.That(_respawn.RespawnCount, Is.EqualTo(1));
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));
            Assert.That(_playerBody.position, Is.EqualTo((Vector2)_respawn.SpawnPoint.position));
            Assert.That(_playerBody.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(_enemy.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Patrol));
            Assert.That(_enemy.ActiveBombCount, Is.Zero);
            Assert.That(_respawn.GetInstanceID(), Is.EqualTo(playerId));
            Assert.That(_enemy.GetInstanceID(), Is.EqualTo(enemyId));
            _encounter.ResetNow();
            _encounter.ResetNow();
            Assert.That(Object.FindObjectsByType<Bombardier2D>().Length, Is.EqualTo(1));
            Assert.That(Object.FindObjectsByType<PlayerMotor2D>().Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator UnobstructedGroundBlast_DamagesPlayerInsideRadius()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            Vector2 point = new Vector2(_playerCollider.bounds.max.x + 1.5f,
                _playerCollider.bounds.center.y);
            Assert.That(BombardierExplosion2D.TryDamagePlayer(point, Vector2.up,
                null, _playerHealth, _playerCollider, _enemy), Is.True);
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(75));
            yield return null;
        }

        [UnityTest]
        public IEnumerator CombatRespawn_ClearsWeaponCooldownAndMovementVelocity()
        {
            PlacePlayer(158f);
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            yield return null;
            _enemy.ResetForEncounter();
            PlayerWeaponController2D weapon = _respawn.GetComponent<PlayerWeaponController2D>();
            PlayerAim2D aim = _respawn.GetComponent<PlayerAim2D>();
            aim.enabled = false;
            Assert.That(aim.ApplyWorldAimVector(Vector2.left), Is.True);
            weapon.ResetTransientState();
            float shotTime = Time.time;
            Assert.That(weapon.TryFire(shotTime), Is.True);
            _playerBody.linearVelocity = new Vector2(4f, 5f);
            var lethal = new DamageInfo2D(100, _playerBody.position, Vector2.right, _enemy);
            _playerHealth.ApplyDamage(in lethal);
            Assert.That(_playerBody.linearVelocity, Is.EqualTo(Vector2.zero));
            Assert.That(_playerBody.position, Is.EqualTo((Vector2)_respawn.SpawnPoint.position));
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));
            Assert.That(weapon.TryFire(shotTime), Is.True,
                "The pre-death weapon cooldown survived RespawnNow.");
        }

        [UnityTest]
        public IEnumerator EnemyDeath_CancelsWindupButPreservesLaunchedBombUntilReset()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            yield return new WaitForFixedUpdate();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Windup));
            int launchCount = _enemy.LaunchCount;
            KillEnemy();
            for (int i = 0; i < 40; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(launchCount));
            Assert.That(_enemy.WeaponHitCollider.enabled, Is.False);
            _encounter.ResetNow();
            Assert.That(_enemy.State, Is.EqualTo(BombardierState.Patrol));
            Assert.That(_enemy.WeaponHitCollider.enabled, Is.True);
            yield return new WaitForFixedUpdate();
            PlacePlayer(150f);
            for (int i = 0; i < 40 && _enemy.LaunchCount == launchCount; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(launchCount + 1));
            BombardierBomb2D bomb = Object.FindAnyObjectByType<BombardierBomb2D>();
            KillEnemy();
            Assert.That(bomb, Is.Not.Null);
            Assert.That(_enemy.ActiveBombCount, Is.EqualTo(1));
            _encounter.ResetNow();
            Assert.That(_enemy.ActiveBombCount, Is.Zero);
            Assert.That(_enemy.Health.CurrentHealth, Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator Latch9Projectile_DamagesBombardierThroughExistingHitbox()
        {
            PlacePlayer(158f);
            _enemy.ResetForEncounter();
            PlayerWeaponEquipment2D equipment = _respawn.GetComponent<PlayerWeaponEquipment2D>();
            PlayerWeaponController2D weapon = _respawn.GetComponent<PlayerWeaponController2D>();
            PlayerAim2D aim = _respawn.GetComponent<PlayerAim2D>();
            Assert.That(equipment.RequestSlot(1), Is.True);
            for (int i = 0; i < 80 && equipment.SelectedSlot != 1; i++) yield return null;
            Assert.That(equipment.SelectedSlot, Is.EqualTo(1));
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            yield return null;
            _enemy.ResetForEncounter();
            aim.enabled = false;
            Assert.That(aim.ApplyWorldAimVector(Vector2.right), Is.True);
            weapon.ResetTransientState();
            Assert.That(weapon.TryFire(Time.time), Is.True);
            for (int i = 0; i < 100 && _enemy.Health.CurrentHealth == 100; i++)
                yield return new WaitForFixedUpdate();
            Assert.That(_enemy.Health.CurrentHealth, Is.LessThan(100));
            Assert.That(_enemy.GetComponentInChildren<WeaponHitbox2D>().ReceivedHitCount,
                Is.GreaterThan(0));
        }

        private void PlacePlayer(float x)
        {
            _playerBody.position = new Vector2(x, 0.02f);
            _playerBody.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }

        private void KillEnemy()
        {
            var damage = new DamageInfo2D(100, _enemy.Body.position, Vector2.right, null);
            _enemy.Health.ApplyDamage(in damage);
        }
    }
}
