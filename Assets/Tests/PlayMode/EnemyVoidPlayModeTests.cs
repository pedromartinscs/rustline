using System.Collections;
using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Environment;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class EnemyVoidPlayModeTests
    {
        private FirstPlayableDirector2D _director;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null; yield return null;
            _director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            HoldPlayer(new Vector2(-24,1.02f));
        }

        private void HoldPlayer(Vector2 position)
        {
            _director.Player.enabled=false;
            _director.Player.GetComponent<PlayerAim2D>().enabled=false;
            _director.Player.GetComponent<Rigidbody2D>().simulated=false;
            _director.Player.GetComponent<Collider2D>().enabled=false;
            _director.Player.transform.position=position;
            _director.Player.GetComponent<Rigidbody2D>().position=position;
            Physics2D.SyncTransforms();
        }

        private void OpenRealVoid(CombatHealth2D actor)
        {
            Vector2 start=actor.transform.position;
            var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
            bool cleared=false;
            for(int depth=0;depth<8 && !cleared;depth++)
            {
                cleared=true;
                foreach(float dx in new[] {-1.5f,-.5f,.5f,1.5f})
                {
                    Vector2 origin=new Vector2(Mathf.Floor(start.x)+dx,start.y+.3f);
                    var hit=Physics2D.Raycast(origin,Vector2.down,80,1<<6);
                    if(hit.collider==null) continue;
                    cleared=false;
                    var terrain=hit.collider.GetComponent<BreachableTilemap2D>();
                    Assert.That(terrain,Is.Not.Null,"Reproduction must remove real terrain through the production receiver.");
                    var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
                    terrain.ReceiveHit(in info);
                }
            }
            Assert.That(cleared,Is.True,"Real production shaft must be open all the way down.");
            Assert.That(Physics2D.Raycast(start+Vector2.up*.3f,Vector2.down,100,1<<6).collider,Is.Null);
        }

        [UnityTest]
        public IEnumerator ProductionGround_AuditPlayableExtentsAndExistingFailureHeight()
        {
            float lowest=float.PositiveInfinity;
            foreach(var c in Object.FindObjectsByType<Collider2D>())
                if(c.gameObject.layer==6 && !c.isTrigger && c.enabled && c.bounds.size.sqrMagnitude>0)
                {
                    Debug.Log($"VOID_GROUND_AUDIT {c.name} min={c.bounds.min:F3} max={c.bounds.max:F3}");
                    lowest=Mathf.Min(lowest,c.bounds.min.y);
                }
            Assert.That(_director.EnemyVoidTopHeight,Is.EqualTo(lowest-46f));
            Assert.That(_director.EnemyVoidTopHeight,Is.EqualTo(-50f).Within(.001f));
            var policy=typeof(FirstPlayableDirector2D).GetField("failureHeight",System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance);
            Debug.Log("VOID_PLAYER_FAILURE_HEIGHT "+policy.GetValue(_director));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Sentry_RealVoidDiesOnceAndStops()
        { yield return VerifyVoid(0); }

        [UnityTest]
        public IEnumerator Bombardier_RealVoidDiesOnceAndStops()
        { yield return VerifyVoid(1); }

        [UnityTest]
        public IEnumerator BothTypes_TerminalDeathOccursBelowLargestVisibleViewport()
        {
            // Failure is checked in Update, after physics catch-up. At 18u/s and
            // maximumDeltaTime=.333s, allow 6u plus half a unit of conservative slack.
            _director.enabled=false;
            var config=(PlayerMovementConfig)typeof(PlayerMotor2D).GetField("config",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(_director.Player);
            Assert.That(config.MaxFallSpeed*Time.maximumDeltaTime,Is.LessThanOrEqualTo(6.01f));
            HoldPlayer(new Vector2(60,-16.5f));
            var camera=Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            yield return new WaitForSeconds(.6f);
            Assert.That(camera.transform.position.y,Is.EqualTo(-14.5f).Within(.125f));
            foreach(var actor in _director.Encounters[3].Members)
            {
                var visual=actor.TryGetComponent(out Sentry2D _) ? actor.GetComponentInChildren<SentryPresenter2D>().BodyRenderer : actor.GetComponentInChildren<BombardierPresenter2D>().BodyRenderer;
                Bounds atDeath=default; float deathTime=-1;
                actor.Died+=_=> { atDeath=visual.bounds; deathTime=Time.fixedTime; };
                float start=Time.fixedTime;
                OpenRealVoid(actor);
                for(int i=0;i<300 && actor.IsAlive;i++) yield return new WaitForFixedUpdate();
                Assert.That(actor.IsDead,Is.True,"Terminal cleanup must remain bounded.");
                Debug.Log($"VOID_PRESENTATION {actor.name} rootY={actor.transform.position.y:F3} renderedTop={atDeath.max.y:F3} cameraY={camera.transform.position.y:F3} elapsed={deathTime-start:F3}");
                foreach(var size in new[] {new Vector2Int(640,480),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080)})
                {
                    var viewport=NativePixelViewportMath.Calculate(size.x,size.y);
                    float bottom=camera.transform.position.y-viewport.LogicalHeight/(2f*NativePixelPresentation.PixelsPerUnit);
                    Assert.That(atDeath.max.y,Is.LessThan(bottom),$"{actor.name} still rendered above {size} bottom {bottom} on death.");
                }
                Assert.That(deathTime-start,Is.LessThan(6f));
            }
        }

        private IEnumerator VerifyVoid(int index)
        {
            var encounter=_director.Encounters[3];
            var actor=encounter.Members[index];
            int died=0; actor.Died+=_=>died++;
            OpenRealVoid(actor);
            for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
            Debug.Log($"VOID_REPRO {actor.name} y={actor.transform.position.y:F3} alive={actor.IsAlive} deaths={died} cleared={_director.Progress.ClearedEncounters}");
            Assert.That(actor.IsDead,Is.True,"An enemy below every production floor remained alive and blocked progression.");
            Assert.That(died,Is.EqualTo(1));
            Assert.That(actor.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>().enabled,Is.False);
            Vector2 terminal=actor.GetComponent<Rigidbody2D>().position;
            for(int i=0;i<30;i++) yield return new WaitForFixedUpdate();
            Assert.That(actor.GetComponent<Rigidbody2D>().position,Is.EqualTo(terminal));
            Assert.That(died,Is.EqualTo(1));
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero,"A future death cannot skip the ordered prefix.");
            var again=new DamageInfo2D(1000,terminal,Vector2.down,null);
            Assert.That(actor.ApplyDamage(in again).DidApply,Is.False);
            Assert.That(died,Is.EqualTo(1));
            foreach(var member in encounter.Members) if(member!=actor) Kill(member);
            yield return null;
            Assert.That(encounter.IsDefeated,Is.True,"Final environmental death must participate in normal encounter bookkeeping.");
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
        }

        private static void Kill(CombatHealth2D actor)
        {
            var damage=new DamageInfo2D(actor.CurrentHealth,actor.transform.position,Vector2.zero,null);
            actor.ApplyDamage(in damage);
        }

        [UnityTest]
        public IEnumerator BothTypes_LongRecoverableFallsSurviveAndResumePatrol()
        {
            foreach(var actor in _director.Encounters[3].Members)
            {
                int deaths=0; actor.Died+=_=>deaths++;
                var body=actor.GetComponent<Rigidbody2D>();
                Vector2 start=body.position+Vector2.up*25;
                actor.transform.position=start; body.position=start; Physics2D.SyncTransforms();
                for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
                Assert.That(actor.IsAlive,Is.True,"A long but recoverable fall must not be terminal.");
                Assert.That(actor.CurrentHealth,Is.EqualTo(actor.MaximumHealth));
                Assert.That(deaths,Is.Zero);
                Assert.That(body.position.y,Is.LessThan(start.y-20));
                Assert.That(Physics2D.Raycast(body.position+Vector2.up*.1f,Vector2.down,.15f,1<<6).collider,Is.Not.Null);
                float x=body.position.x;
                for(int i=0;i<25;i++) yield return new WaitForFixedUpdate();
                Assert.That(Mathf.Abs(body.position.x-x),Is.GreaterThan(.1f));
                Assert.That(actor.IsAlive,Is.True);
            }
        }

        [UnityTest]
        public IEnumerator Pause_FreezesBothFallsAndDefersEnvironmentalDeath()
        {
            var actors=_director.Encounters[3].Members;
            foreach(var actor in actors) OpenRealVoid(actor);
            for(int i=0;i<25;i++) yield return new WaitForFixedUpdate();
            FrontendController.Current.Back(); yield return null;
            Vector3[] paused=actors.Select(a=>a.transform.position).ToArray();
            yield return new WaitForSecondsRealtime(.3f);
            for(int i=0;i<actors.Length;i++)
            {
                Assert.That(actors[i].transform.position,Is.EqualTo(paused[i]));
                Assert.That(actors[i].IsAlive,Is.True);
            }
            FrontendController.Current.Back();
            for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
            Assert.That(actors.All(a=>a.IsDead),Is.True);
        }

        [UnityTest]
        public IEnumerator CombatDeathDuringFall_IsNotEmittedAgainAtTheVoidBoundary()
        {
            foreach(var actor in _director.Encounters[3].Members)
            {
                int deaths=0; actor.Died+=_=>deaths++;
                OpenRealVoid(actor);
                for(int i=0;i<10;i++) yield return new WaitForFixedUpdate();
                Kill(actor);
                for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
                Assert.That(deaths,Is.EqualTo(1)); Assert.That(actor.IsDead,Is.True);
                var body=actor.GetComponent<Rigidbody2D>(); Vector2 stopped=body.position;
                for(int i=0;i<10;i++) yield return new WaitForFixedUpdate();
                Assert.That(body.position,Is.EqualTo(stopped));
                var visual=actor.TryGetComponent(out Sentry2D _) ? actor.GetComponentInChildren<SentryPresenter2D>().BodyRenderer : actor.GetComponentInChildren<BombardierPresenter2D>().BodyRenderer;
                Assert.That(visual.enabled,Is.False,"Normal death finish must remain intact.");
            }
        }

        [UnityTest]
        public IEnumerator FutureVoidDeaths_PreserveOrderedProgressRetryCheckpointAndFullExtraction()
        {
            var all=_director.Encounters.SelectMany(e=>e.Members).ToArray();
            var future=_director.Encounters[3]; int[] deaths=new int[future.Members.Length];
            for(int i=0;i<future.Members.Length;i++)
            { int member=i; future.Members[i].Died+=_=>deaths[member]++; OpenRealVoid(future.Members[i]); }
            for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
            Assert.That(future.IsDefeated,Is.True); Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            for(int i=0;i<2;i++) foreach(var member in _director.Encounters[i].Members) Kill(member);
            yield return null;
            HoldPlayer(_director.Checkpoints[0].transform.position); yield return null;
            Assert.That(_director.Progress.CheckpointIndex,Is.Zero);
            _director.BeginRetry(); _director.RetryNow(); HoldPlayer(_director.Checkpoints[0].transform.position);
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(2));
            Assert.That(_director.Encounters.Take(2).All(e=>e.IsDefeated),Is.True);
            Assert.That(future.Members.All(a=>a.IsAlive),Is.True);
            for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
            Assert.That(future.IsDefeated,Is.True,"Persistent breached terrain must lead to another normal environmental death after retry.");
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(2));
            foreach(var member in _director.Encounters[2].Members) Kill(member);
            yield return null;
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(4));
            for(int i=4;i<7;i++) foreach(var member in _director.Encounters[i].Members) Kill(member);
            yield return null;
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(7));
            HoldPlayer(new Vector2(193,29.08f)); yield return new WaitForSeconds(2.2f);
            Assert.That(_director.Progress.IsComplete,Is.True);
            Assert.That(deaths,Is.All.EqualTo(2));
            Assert.That(all.SequenceEqual(_director.Encounters.SelectMany(e=>e.Members)),Is.True);
            _director.RestartRun(); HoldPlayer(new Vector2(-24,1.02f));
            Assert.That(all.All(a=>a.IsAlive),Is.True);
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            Assert.That(_director.Progress.IsComplete,Is.False); Assert.That(_director.Progress.HasCheckpoint,Is.False);
            for(int i=0;i<250;i++) yield return new WaitForFixedUpdate();
            Assert.That(future.IsDefeated,Is.True); Assert.That(deaths,Is.All.EqualTo(3));
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(),Is.Empty);
        }
    }
}
