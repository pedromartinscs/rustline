using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Weapons;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Environment;
using Rustline.Presentation;
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
        public IEnumerator UnengagedEnemies_RemainVisibleDamageableAndPatrolWithinTheirAuthoredBounds()
        {
            foreach (FirstPlayableEncounter2D encounter in _director.Encounters)
            {
                foreach (CombatHealth2D health in encounter.Members)
                {
                    Assert.That(health.gameObject.activeInHierarchy, Is.True);
                    if (health.TryGetComponent(out Sentry2D _))
                        Assert.That(health.GetComponentInChildren<Rustline.Presentation.SentryPresenter2D>().BodyRenderer.enabled, Is.True);
                    else Assert.That(health.GetComponentInChildren<Rustline.Presentation.BombardierPresenter2D>().BodyRenderer.enabled, Is.True);
                    Assert.That(health.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>().enabled, Is.True);
                    if (health.TryGetComponent(out Sentry2D sentry)) Assert.That(sentry.CombatActive, Is.True);
                    if (health.TryGetComponent(out Bombardier2D ranged)) Assert.That(ranged.CombatActive, Is.True);
                }
            }
            FirstPlayableEncounter2D future = _director.Encounters[3];
            Vector3 sentryPosition = future.Members[0].transform.position;
            Vector3 rangedPosition = future.Members[1].transform.position;
            yield return new WaitForSeconds(0.5f);
            Assert.That(future.IsActive, Is.False, "Distant patrol must not advance engagement.");
            Assert.That(Vector3.Distance(future.Members[0].transform.position,sentryPosition), Is.GreaterThan(0.2f));
            Assert.That(Vector3.Distance(future.Members[1].transform.position,rangedPosition), Is.GreaterThan(0.2f));
            Assert.That(future.Members[0].GetComponent<Sentry2D>().State, Is.EqualTo(SentryState.Patrol));
            var futureRanged = future.Members[1].GetComponent<Bombardier2D>();
            Assert.That(futureRanged.State, Is.EqualTo(BombardierState.Patrol));
            Assert.That(futureRanged.Body.position.x, Is.InRange(futureRanged.PatrolMinimumX,futureRanged.PatrolMaximumX));
            Assert.That(futureRanged.LaunchCount, Is.Zero);
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

        private void HoldPlayerAndCamera(Vector2 player, Vector2 camera)
        {
            _director.enabled=false;
            _director.Player.enabled=false;
            _director.Player.GetComponent<Rigidbody2D>().simulated=false;
            _director.Player.GetComponent<PlayerAim2D>().enabled=false;
            _director.Player.transform.position=player;
            Camera world=Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            world.GetComponent<PixelCameraFollow2D>().enabled=false;
            world.transform.position=new Vector3(camera.x,camera.y,-10);
            Physics2D.SyncTransforms();
        }

        [UnityTest]
        public IEnumerator LocalDetection_RequiresLineOfSightAndWakesProgressionWithoutAreaTrigger()
        {
            var encounter=_director.Encounters[3];
            var sentry=encounter.Members[0].GetComponent<Sentry2D>();
            HoldPlayerAndCamera(new Vector2(55,17.02f),new Vector2(58,20));
            var wall=new GameObject("Patrol LOS obstacle"); wall.layer=6;
            wall.transform.position=new Vector2(58,18.5f);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.25f,4);
            Physics2D.SyncTransforms();
            for(int i=0;i<25;i++) yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsActive,Is.False);
            Assert.That(sentry.CanDetectPlayer(),Is.False);
            Assert.That(sentry.State,Is.EqualTo(SentryState.Patrol));
            Object.Destroy(wall); yield return null;
            for(int i=0;i<5 && !encounter.IsActive;i++) yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsActive,Is.True,"Only the actor's local perception can activate the disabled director's encounter.");
            Assert.That(sentry.CombatEngaged,Is.True);
            Assert.That(sentry.State,Is.Not.EqualTo(SentryState.Patrol));
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
        }

        [UnityTest]
        public IEnumerator BombardierDetection_RespectsWallsAndOffscreenGateBeforeCommittingWindup()
        {
            var encounter=_director.Encounters[3];
            var bombardier=encounter.Members[1].GetComponent<Bombardier2D>();
            HoldPlayerAndCamera(new Vector2(76,20.02f),new Vector2(-20,3));
            for(int i=0;i<15;i++) yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsActive,Is.False);
            Assert.That(bombardier.CanDetectPlayer(),Is.False);
            Assert.That(bombardier.LaunchCount,Is.Zero);
            HoldPlayerAndCamera(new Vector2(76,20.02f),new Vector2(79,22));
            var wall=new GameObject("Bombardier patrol LOS obstacle"); wall.layer=6;
            wall.transform.position=new Vector2(79,22);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.25f,5);
            Physics2D.SyncTransforms();
            for(int i=0;i<15;i++) yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsActive,Is.False);
            Assert.That(bombardier.CanDetectPlayer(),Is.False);
            Object.Destroy(wall); yield return null;
            for(int i=0;i<5 && !encounter.IsActive;i++) yield return new WaitForFixedUpdate();
            Assert.That(encounter.IsActive,Is.True);
            Assert.That(bombardier.State,Is.EqualTo(BombardierState.Windup));
        }

        [UnityTest]
        public IEnumerator Patrol_ObstaclesAndActualFloorBreachesPreventWalkingIntoUnsupportedSpace()
        {
            HoldPlayerAndCamera(new Vector2(-20,1.75f),new Vector2(-20,3));
            var sentry=_director.Encounters[3].Members[0].GetComponent<Sentry2D>();
            var bombardier=_director.Encounters[3].Members[1].GetComponent<Bombardier2D>();
            var wall=new GameObject("Patrol wall fixture"); wall.layer=6;
            wall.transform.position=new Vector2(82.85f,21.5f);
            wall.AddComponent<BoxCollider2D>().size=new Vector2(.25f,3);
            Physics2D.SyncTransforms();
            for(int i=0;i<20;i++) yield return new WaitForFixedUpdate();
            Assert.That(bombardier.Body.position.x,Is.LessThan(82.2f));
            Object.Destroy(wall);
            sentry.ResetForEncounter();
            var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
            // Remove two real supported floor cells through the production destruction path.
            foreach(float x in new[] {58.5f,59.5f})
            {
                Vector2 origin=new Vector2(x,18.5f);
                RaycastHit2D hit=Physics2D.Raycast(origin,Vector2.down,4,1<<6);
                Assert.That(hit.collider,Is.Not.Null);
                var terrain=hit.collider.GetComponent<BreachableTilemap2D>();
                Assert.That(terrain,Is.Not.Null);
                var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
                terrain.ReceiveHit(in info);
            }
            Assert.That(Physics2D.Raycast(new Vector2(59.4f,17.3f),Vector2.down,.7f,1<<6).collider,Is.Null);
            for(int i=0;i<30;i++) yield return new WaitForFixedUpdate();
            Assert.That(sentry.transform.position.x,Is.GreaterThanOrEqualTo(60f));
            Assert.That(sentry.transform.position.y,Is.EqualTo(17.02f).Within(.001f));
            Assert.That(bombardier.Body.position.x,Is.InRange(bombardier.PatrolMinimumX,bombardier.PatrolMaximumX));
        }

        [UnityTest]
        public IEnumerator Patrol_PausesResumesStopsOnDeathAndRestartsAfterCheckpointRetry()
        {
            var encounter=_director.Encounters[6];
            var sentry=encounter.Members[0].GetComponent<Sentry2D>();
            var bombardier=encounter.Members[2].GetComponent<Bombardier2D>();
            FrontendController.Current.Back(); yield return null;
            Vector3 sentryAt=sentry.transform.position, rangedAt=bombardier.transform.position;
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(sentry.transform.position,Is.EqualTo(sentryAt));
            Assert.That(bombardier.transform.position,Is.EqualTo(rangedAt));
            FrontendController.Current.Back();
            yield return new WaitForSeconds(.35f);
            Assert.That(sentry.transform.position,Is.Not.EqualTo(sentryAt));
            Assert.That(bombardier.transform.position,Is.Not.EqualTo(rangedAt));
            foreach(var member in encounter.Members)
            { var damage=new DamageInfo2D(1000,member.transform.position,Vector2.zero,null); member.ApplyDamage(in damage); }
            sentryAt=sentry.transform.position; rangedAt=bombardier.transform.position;
            yield return new WaitForSeconds(.3f);
            Assert.That(sentry.transform.position,Is.EqualTo(sentryAt));
            Assert.That(bombardier.transform.position,Is.EqualTo(rangedAt));
            _director.RetryNow();
            Assert.That(sentry.transform.position,Is.EqualTo((Vector3)sentry.SpawnPosition));
            Assert.That(bombardier.Body.position,Is.EqualTo(bombardier.SpawnPosition));
            Assert.That(encounter.IsActive,Is.False);
            yield return new WaitForSeconds(.35f);
            Assert.That(sentry.transform.position,Is.Not.EqualTo((Vector3)sentry.SpawnPosition));
            Assert.That(bombardier.Body.position,Is.Not.EqualTo(bombardier.SpawnPosition));
            Assert.That(encounter.IsActive,Is.False);
        }

        [UnityTest]
        public IEnumerator BombardierPatrol_ActualRaisedDeckBreachStopsAtTheSupportedEdge()
        {
            HoldPlayerAndCamera(new Vector2(-20,1.75f),new Vector2(-20,3));
            var actor=_director.Encounters[3].Members[1].GetComponent<Bombardier2D>();
            actor.ResetForEncounter();
            var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
            foreach(float x in new[] {83.5f,84.5f})
            {
                Vector2 origin=new Vector2(x,21.5f);
                RaycastHit2D hit=Physics2D.Raycast(origin,Vector2.down,4,1<<6);
                var terrain=hit.collider.GetComponent<BreachableTilemap2D>();
                Assert.That(terrain,Is.Not.Null);
                var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
                terrain.ReceiveHit(in info);
            }
            Assert.That(Physics2D.Raycast(new Vector2(83.4f,20.3f),Vector2.down,.7f,1<<6).collider,Is.Null);
            bool moved=false;
            for(int i=0;i<100;i++)
            {
                yield return new WaitForFixedUpdate();
                moved|=actor.IsMoving;
                Assert.That(actor.Body.position.x,Is.InRange(actor.PatrolMinimumX,82.4f));
                Assert.That(actor.Body.position.y,Is.EqualTo(actor.SpawnPosition.y).Within(.001f));
            }
            Assert.That(moved,Is.True,"Support safety must retain a living patrol on the intact part of the deck.");
            Assert.That(actor.State,Is.EqualTo(BombardierState.Patrol));
            Assert.That(actor.LaunchCount,Is.Zero);
        }
    }
}
