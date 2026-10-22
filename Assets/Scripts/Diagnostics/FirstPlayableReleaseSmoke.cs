using System;
using System.Collections;
using System.IO;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace Rustline.Diagnostics
{
    /// <summary>Opt-in normal-Release regression smoke. Never runs during ordinary play.</summary>
    public sealed class FirstPlayableReleaseSmoke : MonoBehaviour
    {
        private FirstPlayableDirector2D _director;
        private Rigidbody2D _body;
        private PlayerAim2D _aim;
        private PlayerWeaponController2D _weapon;
        private int _landings;
        private static string _outputDirectory;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--first-playable-smoke") < 0) return;
            Application.runInBackground = true;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
            if (Keyboard.current == null) InputSystem.AddDevice<Keyboard>();
            new GameObject("First Playable Release Smoke").AddComponent<FirstPlayableReleaseSmoke>();
        }

        private void Awake() => Application.logMessageReceived += OnLog;
        private void OnDestroy() => Application.logMessageReceived -= OnLog;
        private void OnLog(string message, string stack, LogType type)
        {
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) Application.Quit(1);
        }

        private IEnumerator Start()
        {
            _outputDirectory = Path.GetFullPath("Logs/FirstPlayableSmoke");
            Directory.CreateDirectory(_outputDirectory);
            yield return null;
            _director = FindAnyObjectByType<FirstPlayableDirector2D>();
            Require(_director != null && !Debug.isDebugBuild, "Expected the production scene in a non-development Release.");
            _body = _director.Player.GetComponent<Rigidbody2D>();
            _aim = _director.Player.GetComponent<PlayerAim2D>();
            _weapon = _director.Player.GetComponent<PlayerWeaponController2D>();
            _director.Player.Landed += () => _landings++;
            for (int i = 0; i < 120; i++) yield return new WaitForFixedUpdate();
            Require(_body.position.y > 0.95f && _director.Player.IsGrounded, "Spawn fell through the real Tilemap.");
            var collisionMaps = FindObjectsByType<Rustline.Physics.TilemapCompositeColliderInitializer2D>();
            Require(collisionMaps.Length == 2, "Expected the main terrain and precision ceiling collision maps.");
            foreach (var collisionMap in collisionMaps)
            {
                var composite = collisionMap.GetComponent<CompositeCollider2D>();
                Require(composite.pathCount > 0 && composite.pointCount > 0, "Release Composite is empty: " + collisionMap.name);
                Debug.Log($"FIRST_PLAYABLE_SMOKE_COLLISION_OK object='{collisionMap.name}' paths={composite.pathCount} points={composite.pointCount}");
            }
            Debug.Log("FIRST_PLAYABLE_SMOKE_SPAWN_OK supported by real Tilemap");
            Capture("01-entry");
            int landedBefore = _landings;
            float startX = _body.position.x;
            Keys(Key.D);
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
            Require(_body.position.x > startX + 1f && _director.Player.IsGrounded && _landings == landedBefore,
                "Real Tilemap traversal failed or produced a phantom landing on the entry deck.");
            Keys(Key.D, Key.Space);
            for (int i = 0; i < 8; i++) yield return new WaitForFixedUpdate();
            Require(_body.position.y > 1.5f, "Keyboard jump did not lift the player.");
            Keys();
            for (int i = 0; i < 90; i++) yield return new WaitForFixedUpdate();
            Require(_director.Player.IsGrounded && _body.position.y > -0.1f && _landings > landedBefore,
                "Jump/landing or Composite support failed.");
            Debug.Log("FIRST_PLAYABLE_SMOKE_TRAVERSAL_OK real keyboard movement/jump/landing; no entry seam phantom Land");

            yield return Fight(0, new Vector2(-17f, 1.05f));
            var death = new DamageInfo2D(1000, _body.position, Vector2.zero, this);
            _director.PlayerHealth.ApplyDamage(in death);
            Capture("02-retry");
            yield return new WaitForSeconds(1.2f);
            Require(_director.Progress.Retries == 1 && _director.Progress.ClearedEncounters == 0 &&
                _director.PlayerHealth.CurrentHealth == 100, "Pre-checkpoint auto retry failed.");
            yield return Fight(0, new Vector2(-17f, 1.05f));
            yield return Fight(1, new Vector2(3f, 0.08f));
            Place(new Vector2(22f, 1.08f));
            yield return new WaitForSeconds(0.3f);
            Require(_director.Progress.HasCheckpoint, "Service station did not commit the checkpoint.");
            Capture("03-checkpoint");
            _director.PlayerHealth.ApplyDamage(in death);
            yield return new WaitForSeconds(1.2f);
            Require(_director.Progress.ClearedEncounters == 2 && _director.Progress.Retries == 2 &&
                Vector2.Distance(_body.position, _director.ActiveSpawn.position) < 0.2f,
                "Checkpoint auto retry did not preserve clears or restore its spawn.");
            Debug.Log("FIRST_PLAYABLE_SMOKE_RETRY_OK before/after checkpoint");
            yield return Fight(2, new Vector2(34.75f, 17.08f));
            Place(new Vector2(43f,17.08f)); yield return new WaitForSeconds(0.3f);
            Require(_director.Progress.CheckpointIndex == 1, "Former extraction did not become the upper relay checkpoint.");
            Require(!_director.Progress.IsComplete, "Former extraction completed the expanded level.");
            Place(new Vector2(40f,17.05f));
            Keys(Key.D);
            for (int i=0;i<80;i++) yield return new WaitForFixedUpdate();
            Keys();
            Require(_body.position.x > 49f && _body.position.y > 16.9f, "Release extension seam traversal failed.");
            Debug.Log("FIRST_PLAYABLE_SMOKE_EXPANDED_SEAM_OK actual input from old exit into gallery");
            Vector2[] fightPositions = { new Vector2(55f,17.08f),new Vector2(110f,17.08f),
                new Vector2(152f,29.08f),new Vector2(174.5f,29.08f) };
            for(int index=3;index<_director.Encounters.Length;index++)
            {
                yield return Fight(index,fightPositions[index-3]);
                for(int stationIndex=1;stationIndex<_director.Checkpoints.Length;stationIndex++)
                {
                    var station=_director.Checkpoints[stationIndex];
                    if (station.RequiredClears != index+1) continue;
                    Place(station.transform.position); yield return new WaitForSeconds(0.3f);
                    Require(_director.Progress.CheckpointIndex == stationIndex,"Expanded checkpoint not saved.");
                    _director.PlayerHealth.ApplyDamage(in death);
                    yield return new WaitForSeconds(1.2f);
                    Require(_director.Progress.ClearedEncounters == station.RequiredClears &&
                        Vector2.Distance(_body.position,station.transform.position)<0.2f,"Expanded checkpoint retry failed.");
                }
            }
            Rect extraction=_director.ExtractionArea;
            Place(new Vector2(extraction.center.x,extraction.yMin+0.28f));
            yield return new WaitForSeconds(2.3f);
            Require(_director.Progress.IsComplete && !_body.simulated, "Extraction did not complete/freeze the run.");
            Capture("05-complete");
            yield return new WaitForSeconds(0.4f);
            _director.RestartRun();
            Require(!_director.Progress.HasCheckpoint && !_director.Progress.IsComplete &&
                _director.Progress.ClearedEncounters == 0 && _body.simulated, "Full restart failed.");
            Debug.Log("FIRST_PLAYABLE_SMOKE_PASS: expanded Release collision, input across original/extension Tilemaps, all 7 actual hitscan encounters, checkpoints, retry, extraction and restart. Fight staging was automated; full-route human traversal/balance remain manual gates.");
            Keys();
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "--smoke-auto-quit") >= 0) Application.Quit(0);
        }

        private IEnumerator Fight(int index, Vector2 position)
        {
            Place(position);
            _aim.enabled = false;
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
            FirstPlayableEncounter2D encounter = _director.Encounters[index];
            Require(encounter.IsActive, "Encounter did not activate: " + index);
            if (index == 2) Capture("04-upper-defense");
            foreach (CombatHealth2D health in encounter.Members)
            {
                Collider2D target = health.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>();
                int attempts = 0;
                while (health.IsAlive && attempts++ < 8)
                {
                    _aim.ApplyWorldAimVector((Vector2)target.bounds.center - (Vector2)_aim.AimOriginWorld);
                    Require(_weapon.TryFire(Time.time), "A valid Longwatch smoke shot was rejected.");
                    for (int i = 0; i < 7; i++) yield return new WaitForFixedUpdate();
                }
                Require(health.IsDead, "Weapon did not defeat the actual scene enemy: " + health.name);
            }
            yield return null;
            Require(_director.Progress.ClearedEncounters == index + 1, "Ordered encounter completion failed.");
        }

        private void Place(Vector2 position)
        {
            _director.Player.transform.position = position;
            _body.position = position;
            _body.linearVelocity = Vector2.zero;
            _director.Player.ResetAfterRespawn();
            Physics2D.SyncTransforms();
        }

        private static void Keys(params Key[] keys)
        {
            Require(Keyboard.current != null, "Smoke needs a keyboard device.");
            InputSystem.QueueStateEvent(Keyboard.current, new KeyboardState(keys));
        }
        private static void Capture(string name) => ScreenCapture.CaptureScreenshot(Path.Combine(_outputDirectory, name + ".png"));
        private static void Require(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException("FIRST_PLAYABLE_SMOKE_FAIL: " + message);
        }
    }
}
