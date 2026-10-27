using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Environment;
using Rustline.Gameplay.Weapons;
using Rustline.Diagnostics;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class EnemySupportPlayModeTests
    {
        private FirstPlayableDirector2D _director;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            _director.enabled=false;
            _director.Player.enabled=false;
            _director.Player.GetComponent<Rigidbody2D>().simulated=false;
            _director.Player.GetComponent<Collider2D>().enabled=false;
        }

        [UnityTest]
        public IEnumerator ProductionSpawns_HavePhysicalSupport()
        {
            foreach(var encounter in _director.Encounters)
            foreach(var member in encounter.Members)
            {
                var body=member.GetComponent<Rigidbody2D>();
                Vector2 position=member.transform.position;
                var ground=Physics2D.Raycast(position+Vector2.up*.3f,Vector2.down,60,1<<6);
                var hitbox=member.GetComponentInChildren<WeaponHitbox2D>().GetComponent<BoxCollider2D>();
                Debug.Log($"ENEMY_SUPPORT_AUDIT {member.name} pos={position:F3} body={body.bodyType} gravity={body.gravityScale} constraints={body.constraints} detection={body.collisionDetectionMode} trigger={hitbox.isTrigger} hitbox={hitbox.bounds} ground={ground.point:F3} distance={ground.distance:F3} collider={ground.collider?.name}");
                Assert.That(ground.collider,Is.Not.Null,member.name);
                Assert.That(position.y-ground.point.y,Is.InRange(-.001f,.04f),member.name+" unsupported authored foot position");
            }
            yield return null;
        }

        private void BreachFloor(float x,float y)
        {
            var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
            Vector2 origin=new Vector2(x,y+.5f);
            var hit=Physics2D.Raycast(origin,Vector2.down,2,1<<6);
            Assert.That(hit.collider,Is.Not.Null);
            var terrain=hit.collider.GetComponent<BreachableTilemap2D>();
            Assert.That(terrain,Is.Not.Null);
            var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
            terrain.ReceiveHit(in info);
        }

        [UnityTest]
        public IEnumerator Sentry_CurrentFloorDestroyed_DescendsOntoLowerProductionFloor()
        {
            yield return LoseSupport(0);
        }

        [UnityTest]
        public IEnumerator Bombardier_CurrentFloorDestroyed_DescendsOntoLowerProductionFloor()
        {
            yield return LoseSupport(1);
        }

        [UnityTest]
        public IEnumerator BothActors_DeathWithoutSupportStillLandsAndRetryRestoresAliveWithoutHeightSnap()
        {
            foreach(int index in new[] {0,1})
            {
                var actor=_director.Encounters[3].Members[index];
                // The other actor has patrolled during the first actor's death/reset.
                // Remove support at the reset destination as well as the death position.
                if(actor.TryGetComponent(out Sentry2D sentry)) sentry.ResetForEncounter();
                else actor.GetComponent<Bombardier2D>().ResetForEncounter();
                Physics2D.SyncTransforms();
                Vector2 start=actor.transform.position;
                foreach(float dx in new[] {-1.5f,-.5f,.5f,1.5f}) BreachFloor(Mathf.Floor(start.x)+dx,start.y);
                var damage=new DamageInfo2D(1000,start,Vector2.zero,null); actor.ApplyDamage(in damage);
                var hitbox=actor.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>();
                Assert.That(hitbox.enabled,Is.False);
                for(int i=0;i<130;i++) yield return new WaitForFixedUpdate();
                Assert.That(actor.IsDead,Is.True);
                Assert.That(actor.transform.position.y,Is.LessThan(start.y-.5f));
                AssertSupported(actor);
                Assert.That(actor.GetComponentInChildren<SpriteRenderer>().enabled,Is.False,"Existing death finish must complete while unsupported.");
                _director.RetryNow();
                Vector2 spawn=index==0 ? actor.GetComponent<Sentry2D>().SpawnPosition : actor.GetComponent<Bombardier2D>().SpawnPosition;
                Assert.That((Vector2)actor.transform.position,Is.EqualTo(spawn));
                Assert.That(actor.IsAlive,Is.True); Assert.That(hitbox.enabled,Is.True);
                // Retry does not rebuild destroyed terrain. A restored actor must descend again.
                for(int i=0;i<130;i++) yield return new WaitForFixedUpdate();
                Assert.That(actor.transform.position.y,Is.LessThan(start.y-.5f));
                AssertSupported(actor);
                if(index==1) Assert.That(actor.GetComponent<Bombardier2D>().ActiveBombCount,Is.Zero);
            }
        }

        [UnityTest]
        public IEnumerator BothActors_UnsupportedPlacementSettlesAndPauseFreezesActualPosition()
        {
            foreach(var actor in _director.Encounters[3].Members)
            {
                var body=actor.GetComponent<Rigidbody2D>();
                Vector2 start=body.position+Vector2.up*2;
                actor.transform.position=start; body.position=start; Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                FrontendController.Current.Back(); yield return null;
                Vector2 paused=body.position;
                yield return new WaitForSecondsRealtime(.15f);
                Assert.That(body.position,Is.EqualTo(paused));
                FrontendController.Current.Back();
                for(int i=0;i<60;i++) yield return new WaitForFixedUpdate();
                Assert.That(body.position.y,Is.LessThan(start.y-1));
                AssertSupported(actor);
                Assert.That(body.bodyType,Is.EqualTo(RigidbodyType2D.Kinematic));
            }
        }

        private static void AssertSupported(CombatHealth2D actor)
        {
            var hit=Physics2D.Raycast((Vector2)actor.transform.position+Vector2.up*.1f,Vector2.down,.15f,1<<6);
            Assert.That(hit.collider,Is.Not.Null,actor.name+" did not land on real Ground");
            // Shape casts have contact tolerance; require nonpenetrating support within
            // less than one native pixel, including the authored .02 unit skin.
            Assert.That(actor.transform.position.y-hit.point.y,Is.InRange(0f,.04f));
        }

        private IEnumerator LoseSupport(int memberIndex)
        {
            var actor=_director.Encounters[3].Members[memberIndex];
            Vector2 start=actor.transform.position;
            _director.Player.transform.position=start+Vector2.left*5;
            var camera=Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            camera.GetComponent<PixelCameraFollow2D>().enabled=false;
            camera.transform.position=new Vector3(start.x,start.y+2,-10);
            foreach(float dx in new[] {-1.5f,-.5f,.5f,1.5f}) BreachFloor(Mathf.Floor(start.x)+dx,start.y);
            Assert.That(Physics2D.Raycast(start+Vector2.up*.3f,Vector2.down,.7f,1<<6).collider,Is.Null);
            for(int i=0;i<110;i++) yield return new WaitForFixedUpdate();
            yield return null;
            GameplayUxReleaseReview.CaptureFrame("Logs/GameplayUxPhase41/"+actor.name+"-support.png");
            Debug.Log($"ENEMY_SUPPORT_LOSS {actor.name} start={start:F3} after={actor.transform.position:F3}");
            Assert.That(actor.transform.position.y,Is.LessThan(start.y-.5f),"Actor hovered after its real floor was destroyed.");
            AssertSupported(actor);
            float landedY=actor.transform.position.y;
            for(int i=0;i<25;i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(actor.transform.position.y,Is.LessThanOrEqualTo(landedY+.001f),"Horizontal patrol snapped back to spawn height.");
            }
            var visual=actor.TryGetComponent(out Sentry2D sentry) ? actor.GetComponentInChildren<SentryPresenter2D>().BodyRenderer : actor.GetComponentInChildren<BombardierPresenter2D>().BodyRenderer;
            yield return null;
            Assert.That(visual.transform.position.y,Is.EqualTo(Mathf.Round(actor.transform.position.y*16)/16).Within(.001f));
            if(sentry!=null) sentry.ResetForEncounter(); else actor.GetComponent<Bombardier2D>().ResetForEncounter();
            Vector2 spawn=sentry!=null ? sentry.SpawnPosition : actor.GetComponent<Bombardier2D>().SpawnPosition;
            Assert.That((Vector2)actor.transform.position,Is.EqualTo(spawn));
            for(int i=0;i<130;i++) yield return new WaitForFixedUpdate();
            Assert.That(actor.transform.position.y,Is.LessThan(start.y-.5f),"Reset over a persistent breach must not hover.");
            AssertSupported(actor);
        }
    }
}
