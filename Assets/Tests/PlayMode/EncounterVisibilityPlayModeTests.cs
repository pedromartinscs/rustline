using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class EncounterVisibilityPlayModeTests
    {
        private FirstPlayableDirector2D _director;
        [UnitySetUp]
        public IEnumerator SetUp()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
        }

        [UnityTest]
        public IEnumerator DormantEnemies_RemainVisibleDamageableAndStationary()
        {
            foreach (FirstPlayableEncounter2D encounter in _director.Encounters)
            {
                Assert.That(encounter.IsActive, Is.False);
                foreach (CombatHealth2D health in encounter.Members)
                {
                    Assert.That(health.gameObject.activeInHierarchy, Is.True);
                    if (health.TryGetComponent(out Sentry2D _))
                        Assert.That(health.GetComponentInChildren<Rustline.Presentation.SentryPresenter2D>().BodyRenderer.enabled, Is.True);
                    else Assert.That(health.GetComponentInChildren<LineRenderer>().enabled, Is.True);
                    Assert.That(health.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>().enabled, Is.True);
                    if (health.TryGetComponent(out Sentry2D sentry)) Assert.That(sentry.CombatActive, Is.False);
                    if (health.TryGetComponent(out Bombardier2D ranged)) Assert.That(ranged.CombatActive, Is.False);
                }
            }
            CombatHealth2D future = _director.Encounters[2].Members[0];
            Vector3 position = future.transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.That(future.transform.position, Is.EqualTo(position));
        }

        [UnityTest]
        public IEnumerator EarlyHit_WakesFutureEncounterAndActivationDoesNotHealEarlyDeath()
        {
            FirstPlayableEncounter2D future = _director.Encounters[2];
            CombatHealth2D target = future.Members[0];
            WeaponHitbox2D hitbox = target.GetComponentInChildren<WeaponHitbox2D>();
            var hit = new WeaponHitInfo2D(null, Vector2.zero, Vector2.right, target.transform.position,
                Vector2.left, 1f, 1000, hitbox.GetComponent<Collider2D>());
            hitbox.ReceiveHit(in hit);
            Assert.That(future.IsActive, Is.True);
            Assert.That(target.IsDead, Is.True);
            Assert.That(hitbox.GetComponent<Collider2D>().enabled, Is.False);
            future.Activate();
            Assert.That(target.IsDead, Is.True);
            Assert.That(_director.Progress.ClearedEncounters, Is.Zero);
            _director.RestartRun();
            yield return null;
            Assert.That(target.IsAlive, Is.True);
            Assert.That(target.gameObject.activeInHierarchy, Is.True);
            Assert.That(hitbox.GetComponent<Collider2D>().enabled, Is.True);
            Assert.That(future.IsActive, Is.False);
        }
    }
}
