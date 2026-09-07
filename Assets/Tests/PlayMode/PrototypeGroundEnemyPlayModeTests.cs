using System.Collections;
using NUnit.Framework;
using Rustline.Diagnostics;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class PrototypeGroundEnemyPlayModeTests
    {
        [UnityTest]
        public IEnumerator LongwatchHitscan_ThreeShotsKillAndEncounterRestoresSameEnemy()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PrototypeGroundEnemy2D enemy = Object.FindAnyObjectByType<PrototypeGroundEnemy2D>();
            MovementLabPrototypeEnemyEncounter2D encounter =
                Object.FindAnyObjectByType<MovementLabPrototypeEnemyEncounter2D>();
            PlayerWeaponController2D weapon = Object.FindAnyObjectByType<PlayerWeaponController2D>();
            PlayerAim2D aim = weapon?.GetComponent<PlayerAim2D>();
            Rigidbody2D playerBody = weapon?.GetComponent<Rigidbody2D>();
            WeaponHitbox2D hitbox = enemy?.GetComponentInChildren<WeaponHitbox2D>();
            PrototypeGroundEnemyPresenter2D presenter =
                enemy?.GetComponentInChildren<PrototypeGroundEnemyPresenter2D>();

            Assert.That(enemy, Is.Not.Null);
            Assert.That(encounter, Is.Not.Null);
            Assert.That(encounter.Enemy, Is.SameAs(enemy));
            Assert.That(weapon, Is.Not.Null);
            Assert.That(aim, Is.Not.Null);
            Assert.That(playerBody, Is.Not.Null);
            Assert.That(hitbox, Is.Not.Null);
            Assert.That(hitbox.Health, Is.SameAs(enemy.Health));
            Assert.That(hitbox.transform.parent, Is.SameAs(enemy.transform));
            Assert.That(presenter, Is.Not.Null);

            yield return PlaceGrounded(playerBody, 158f);
            enemy.ResetForEncounter();
            aim.enabled = false;
            Assert.That(aim.ApplyWorldAimVector(Vector2.right), Is.True);
            weapon.ResetTransientState();
            int shotCountBefore = weapon.ShotCount;
            int recoilCountBefore = weapon.GetComponent<LongwatchRecoilPresenter2D>().ImpulseCount;
            int cameraImpulseCountBefore =
                Object.FindAnyObjectByType<LongwatchCameraImpulse2D>().ImpulseCount;
            int resetPresentationCountBefore = presenter.ResetPresentationCount;
            float fireTime = Time.time;

            Assert.That(enemy.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(enemy.Health.IsAlive, Is.True);
            Assert.That(enemy.WeaponHitCollider.enabled, Is.True);

            Assert.That(weapon.TryFire(fireTime), Is.True);
            AssertEnemyHit(weapon, enemy, hitbox, expectedHealth: 60);
            Assert.That(presenter.IsHitReacting, Is.True);
            Assert.That(presenter.HitReactionCount, Is.EqualTo(1));
            Assert.That(enemy.HitPauseRemaining, Is.GreaterThan(0f));

            Assert.That(weapon.TryFire(fireTime + 0.25f), Is.True);
            AssertEnemyHit(weapon, enemy, hitbox, expectedHealth: 20);
            Assert.That(enemy.Health.IsAlive, Is.True);

            Assert.That(weapon.TryFire(fireTime + 0.5f), Is.True);
            Assert.That(enemy.Health.CurrentHealth, Is.Zero);
            Assert.That(enemy.Health.IsDead, Is.True);
            Assert.That(encounter.DeathCount, Is.EqualTo(1));
            Assert.That(encounter.IsResetPending, Is.True);
            Assert.That(enemy.WeaponHitCollider.enabled, Is.False);
            Assert.That(enemy.IsPatrolling, Is.False);
            Assert.That(presenter.IsDeadVisual, Is.True);
            Assert.That(presenter.DeathPresentationCount, Is.EqualTo(1));

            Vector2 deadPosition = enemy.Body.position;
            for (int index = 0; index < 3; index++)
            {
                yield return new WaitForFixedUpdate();
            }
            Assert.That(enemy.Body.position, Is.EqualTo(deadPosition),
                "Patrol continued after death.");

            Physics2D.SyncTransforms();
            Assert.That(weapon.TryFire(fireTime + 0.75f), Is.True);
            Assert.That(weapon.LastShotResult.HitCollider, Is.Not.SameAs(enemy.WeaponHitCollider));
            Assert.That(weapon.LastShotResult.HitReceiverNotified, Is.False,
                "The dead enemy continued acting as a living weapon target.");
            Assert.That(enemy.Health.CurrentHealth, Is.Zero);
            Assert.That(encounter.DeathCount, Is.EqualTo(1));

            for (int frame = 0; frame < 180 && encounter.IsResetPending; frame++)
            {
                yield return null;
            }

            Assert.That(encounter.IsResetPending, Is.False,
                "The bounded reset window expired before the reusable enemy returned.");
            Assert.That(encounter.ResetCount, Is.EqualTo(1));
            Assert.That(enemy.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(enemy.Health.IsAlive, Is.True);
            Assert.That(enemy.Body.position, Is.EqualTo(enemy.SpawnPosition));
            Assert.That(enemy.PatrolDirection, Is.EqualTo(enemy.InitialDirection));
            Assert.That(enemy.WeaponHitCollider.enabled, Is.True);
            Assert.That(enemy.IsPatrolling, Is.True);
            Assert.That(presenter.IsDeadVisual, Is.False);
            Assert.That(presenter.IsHitReacting, Is.False);
            Assert.That(
                presenter.ResetPresentationCount - resetPresentationCountBefore,
                Is.EqualTo(1));
            Assert.That(weapon.ShotCount - shotCountBefore, Is.EqualTo(4));
            Assert.That(
                weapon.GetComponent<LongwatchRecoilPresenter2D>().ImpulseCount - recoilCountBefore,
                Is.EqualTo(4));
            Assert.That(
                Object.FindAnyObjectByType<LongwatchCameraImpulse2D>().ImpulseCount -
                cameraImpulseCountBefore,
                Is.EqualTo(4));
        }

        [UnityTest]
        public IEnumerator Patrol_StaysInBoundsReversesStopsOnDeathAndResetsExactly()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PrototypeGroundEnemy2D enemy = Object.FindAnyObjectByType<PrototypeGroundEnemy2D>();
            MovementLabPrototypeEnemyEncounter2D encounter =
                Object.FindAnyObjectByType<MovementLabPrototypeEnemyEncounter2D>();
            Assert.That(enemy, Is.Not.Null);
            Assert.That(encounter, Is.Not.Null);

            enemy.ResetForEncounter();
            Vector2 spawn = enemy.SpawnPosition;
            int startingDirection = enemy.PatrolDirection;
            float minimumObservedX = enemy.Body.position.x;
            float maximumObservedX = enemy.Body.position.x;
            bool reversed = false;

            for (int frame = 0; frame < 180 && !reversed; frame++)
            {
                yield return new WaitForFixedUpdate();
                Vector2 position = enemy.Body.position;
                minimumObservedX = Mathf.Min(minimumObservedX, position.x);
                maximumObservedX = Mathf.Max(maximumObservedX, position.x);
                Assert.That(position.x, Is.InRange(
                    enemy.PatrolMinimumX - 0.001f,
                    enemy.PatrolMaximumX + 0.001f));
                Assert.That(position.y, Is.EqualTo(spawn.y).Within(0.0001f),
                    "The horizontal diagnostic patrol drifted vertically.");
                reversed = enemy.PatrolDirection != startingDirection;
            }

            Assert.That(maximumObservedX - minimumObservedX, Is.GreaterThan(0.5f),
                "The alive enemy did not begin moving.");
            Assert.That(reversed, Is.True,
                "The enemy did not reverse within the bounded patrol window.");

            var lethalDamage = new DamageInfo2D(100, enemy.Body.position, Vector2.right, null);
            enemy.Health.ApplyDamage(in lethalDamage);
            Vector2 deathPosition = enemy.Body.position;
            for (int frame = 0; frame < 4; frame++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(enemy.Body.position, Is.EqualTo(deathPosition));
            Assert.That(enemy.IsPatrolling, Is.False);
            Assert.That(encounter.IsResetPending, Is.True);

            encounter.ResetNow();
            Assert.That(enemy.Body.position, Is.EqualTo(spawn));
            Assert.That(enemy.Body.position.y, Is.EqualTo(spawn.y));
            Assert.That(enemy.PatrolMinimumX, Is.EqualTo(spawn.x - enemy.PatrolHalfDistance));
            Assert.That(enemy.PatrolMaximumX, Is.EqualTo(spawn.x + enemy.PatrolHalfDistance));
            Assert.That(enemy.PatrolDirection, Is.EqualTo(enemy.InitialDirection));
            Assert.That(enemy.Health.CurrentHealth, Is.EqualTo(100));
            Assert.That(enemy.WeaponHitCollider.enabled, Is.True);
        }

        private static void AssertEnemyHit(
            PlayerWeaponController2D weapon,
            PrototypeGroundEnemy2D enemy,
            WeaponHitbox2D hitbox,
            int expectedHealth)
        {
            Assert.That(weapon.LastShotResult.Hit, Is.True);
            Assert.That(weapon.LastShotResult.HitCollider, Is.SameAs(enemy.WeaponHitCollider));
            Assert.That(weapon.LastShotResult.HitReceiverNotified, Is.True);
            Assert.That(hitbox.LastDamageResult.Info.Amount, Is.EqualTo(40));
            Assert.That(hitbox.LastDamageResult.Info.Point, Is.EqualTo(weapon.LastShotResult.EndPoint));
            Assert.That(hitbox.LastDamageResult.Info.Direction, Is.EqualTo(Vector2.right));
            Assert.That(hitbox.LastDamageResult.Info.Source, Is.SameAs(weapon.WeaponDefinition));
            Assert.That(enemy.Health.CurrentHealth, Is.EqualTo(expectedHealth));
        }

        private static IEnumerator PlaceGrounded(Rigidbody2D body, float x)
        {
            body.position = new Vector2(x, 0.02f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int index = 0; index < 30; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return null;
        }
    }
}
