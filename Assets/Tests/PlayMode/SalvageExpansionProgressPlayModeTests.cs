using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class SalvageExpansionProgressPlayModeTests
    {
        private FirstPlayableDirector2D _director;
        private Rigidbody2D _body;
        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            _body = _director.Player.GetComponent<Rigidbody2D>();
        }

        [UnityTest]
        public IEnumerator EarlyFutureDefeat_DoesNotSkipEarlierObjectivesOrSoftlockFinalExtraction()
        {
            int last = _director.Encounters.Length - 1;
            Defeat(last); yield return null;
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            Assert.That(_director.Encounters[last].IsDefeated,Is.True);
            Place(193f,29.08f); yield return new WaitForSeconds(2.2f);
            Assert.That(_director.Progress.IsComplete,Is.False);
            for(int i=0;i<last;i++) { Defeat(i); yield return null; }
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(_director.Encounters.Length));
            Assert.That(_director.Objective,Does.Contain("EXTRACT"), "Skipped optional stations must not hide the final objective.");
            yield return new WaitForSeconds(2.2f);
            Assert.That(_director.Progress.IsComplete,Is.True);
            _director.RestartRun(); yield return null;
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            foreach(var encounter in _director.Encounters)
                foreach(var health in encounter.Members)
                    Assert.That(health.IsAlive && health.gameObject.activeInHierarchy,Is.True);
        }

        [UnityTest]
        public IEnumerator EveryCheckpoint_RestoresCommittedStateAmmoAndSameVisibleActors()
        {
            int playerId = _director.Player.GetInstanceID();
            int[][] ids = new int[_director.Encounters.Length][];
            for(int i=0;i<ids.Length;i++)
            {
                ids[i]=new int[_director.Encounters[i].Members.Length];
                for(int j=0;j<ids[i].Length;j++) ids[i][j]=_director.Encounters[i].Members[j].GetInstanceID();
            }
            for(int stationIndex=0;stationIndex<_director.Checkpoints.Length;stationIndex++)
            {
                _director.RestartRun(); yield return null;
                var station = _director.Checkpoints[stationIndex];
                for(int i=0;i<station.RequiredClears;i++) { Defeat(i); yield return null; }
                Place(station.transform.position.x,station.transform.position.y); yield return null;
                Assert.That(_director.Progress.CheckpointIndex,Is.EqualTo(stationIndex));
                // Early defeat of an uncommitted future encounter must reset on retry.
                Defeat(_director.Encounters.Length-1); yield return null;
                var weapon = _director.Player.GetComponent<PlayerWeaponController2D>();
                var ammo = weapon.Ammo;
                ammo.ConsumeAcceptedShot(weapon.WeaponDefinition);
                var damage = new DamageInfo2D(1000,_body.position,Vector2.zero,null);
                _director.PlayerHealth.ApplyDamage(in damage);
                Assert.That(_director.IsRetrying,Is.True);
                _director.RetryNow();
                Assert.That(_body.position,Is.EqualTo((Vector2)station.transform.position));
                Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(station.RequiredClears));
                Assert.That(_director.PlayerHealth.CurrentHealth,Is.EqualTo(100));
                Assert.That(ammo.GetSnapshot(weapon.WeaponDefinition).Rounds,Is.EqualTo(50));
                Assert.That(ammo.GetSnapshot(weapon.WeaponDefinition).ReserveMagazines,Is.EqualTo(3));
                Assert.That(_director.Player.GetInstanceID(),Is.EqualTo(playerId));
                for(int i=0;i<ids.Length;i++)
                    for(int j=0;j<ids[i].Length;j++)
                    {
                        var health=_director.Encounters[i].Members[j];
                        Assert.That(health.GetInstanceID(),Is.EqualTo(ids[i][j]));
                        Assert.That(health.IsAlive,Is.EqualTo(i>=station.RequiredClears));
                        Assert.That(health.gameObject.activeInHierarchy,Is.True);
                    }
                Assert.That(Object.FindObjectsByType<BombardierBomb2D>(FindObjectsSortMode.None),Is.Empty);
            }
        }

        private void Defeat(int index)
        {
            foreach(var health in _director.Encounters[index].Members)
            {
                var hitbox=health.GetComponentInChildren<WeaponHitbox2D>();
                var hit=new WeaponHitInfo2D(null,Vector2.zero,Vector2.right,health.transform.position,
                    Vector2.left,1f,1000,hitbox.GetComponent<Collider2D>());
                hitbox.ReceiveHit(in hit);
            }
        }
        private void Place(float x,float y)
        {
            _director.Player.transform.position=new Vector2(x,y); _body.position=new Vector2(x,y);
            _body.linearVelocity=Vector2.zero; _director.Player.ResetAfterRespawn(); Physics2D.SyncTransforms();
        }
    }
}
