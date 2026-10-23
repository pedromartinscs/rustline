using System.Collections;
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Rustline.Diagnostics;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class CombatVfxPlayModeTests
    {
        private Bombardier2D _enemy;
        private BombardierBomb2D _bomb;
        private CombatHealth2D _playerHealth;
        private Rigidbody2D _player;
        private FirstPlayableDirector2D _director;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("MovementLab"); yield return null;
            _enemy = Object.FindAnyObjectByType<Bombardier2D>();
            _enemy.SetCombatActive(false);
            var respawn = Object.FindAnyObjectByType<MovementLabRespawn>();
            _player = respawn.GetComponent<Rigidbody2D>();
            _playerHealth = respawn.GetComponent<CombatHealth2D>();
            PlacePlayer(new Vector2(158f, 0.02f));
        }

        private void PlacePlayer(Vector2 position)
        {
            _player.transform.position = position; _player.position = position;
            _player.linearVelocity = Vector2.zero; Physics2D.SyncTransforms();
        }

        private IEnumerator Launch()
        {
            _enemy.ResetForEncounter();
            int before = _enemy.LaunchCount;
            _enemy.SetCombatActive(true);
            for (int i = 0; i < 90 && _enemy.LaunchCount == before; i++) yield return new WaitForFixedUpdate();
            Assert.That(_enemy.LaunchCount, Is.EqualTo(before + 1));
            _enemy.SetCombatActive(false);
            _bomb = Object.FindAnyObjectByType<BombardierBomb2D>();
            Assert.That(_bomb, Is.Not.Null);
        }

        private IEnumerator Detonate()
        {
            for (int i = 0; i < 100 && _bomb != null && !_bomb.HasExploded; i++) yield return new WaitForFixedUpdate();
            Assert.That(_bomb, Is.Not.Null);
            Assert.That(_bomb.HasExploded, Is.True);
            Assert.That(_bomb.ExplosionVisual, Is.Not.Null);
            Assert.That(_bomb.GetComponent<LineRenderer>().enabled, Is.False);
            Assert.That(_bomb.GetComponentsInChildren<Collider2D>(), Is.Empty);
            Assert.That(_bomb.ExplosionVisual.transform.parent, Is.EqualTo(_bomb.transform));
            Assert.That((Vector2)_bomb.transform.position, Is.EqualTo(_bomb.ImpactPoint));
            Assert.That(Vector2.Distance(_bomb.ExplosionVisual.transform.position, _bomb.ImpactPoint), Is.LessThan(0.045f));
        }

        [UnityTest]
        public IEnumerator Detonation_AppliesDamageAtFlashExactlyOnceWhileSixFramesFinish()
        {
            int hits = 0;
            _playerHealth.Damaged += hit =>
            {
                if (!hit.DidApply) return;
                hits++;
                Assert.That(_bomb.HasExploded, Is.True);
                Assert.That(_bomb.ExplosionVisual.FrameIndex, Is.Zero, "Damage must occur at the flash, not at animation end.");
            };
            var seen = new HashSet<int>();
            Camera world = Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            void Observe(ScriptableRenderContext context, Camera camera)
            {
                if (camera == world && _bomb != null && _bomb.ExplosionVisual != null && !_bomb.ExplosionVisual.IsComplete)
                    seen.Add(_bomb.ExplosionVisual.FrameIndex);
            }
            RenderPipelineManager.beginCameraRendering += Observe;
            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f; // Deterministic 60 Hz observation, not a measured FPS claim.
            try
            {
                yield return Launch(); yield return Detonate();
                Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(75));
                Assert.That(_bomb.ExplosionVisual.BodyRenderer.sharedMaterial, Is.SameAs(_enemy.ProjectileMaterial));
                for (int i = 0; i < 100 && _bomb != null; i++) yield return null;
            }
            finally { RenderPipelineManager.beginCameraRendering -= Observe; Time.captureDeltaTime = capture; }
            Assert.That(_bomb == null, Is.True);
            Assert.That(seen, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5 }));
            Assert.That(hits, Is.EqualTo(1));
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(75));
            Assert.That(_enemy.ActiveBombCount, Is.Zero);
            Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator LethalDetonation_SynchronousRespawnCancelsAlreadyCreatedVisual()
        {
            var initial = new DamageInfo2D(75, _player.position, Vector2.right, null);
            _playerHealth.ApplyDamage(in initial);
            var respawn = _player.GetComponent<MovementLabRespawn>();
            int before = respawn.RespawnCount;
            bool cancelledInDeathCallback = false;
            _playerHealth.Died += hit => cancelledInDeathCallback = _bomb != null &&
                _bomb.ExplosionVisual != null && _bomb.ExplosionVisual.IsComplete &&
                !_bomb.ExplosionVisual.BodyRenderer.enabled;
            yield return Launch();
            for (int i = 0; i < 100 && respawn.RespawnCount == before; i++) yield return new WaitForFixedUpdate();
            Assert.That(respawn.RespawnCount, Is.EqualTo(before + 1));
            Assert.That(cancelledInDeathCallback, Is.True, "Reset during the damage callback left a live visual.");
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));
            Assert.That(_enemy.ActiveBombCount, Is.Zero);
            yield return null; yield return null;
            Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator EncounterReset_DisabledVisualAndSceneUnloadCleanOwnedEffects()
        {
            var encounter = Object.FindAnyObjectByType<MovementLabBombardierEncounter2D>();
            yield return Launch();
            encounter.ResetNow();
            Assert.That(_bomb.GetComponent<LineRenderer>().enabled, Is.False);
            yield return null;
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);

            PlacePlayer(new Vector2(158f, 0.02f));
            yield return Launch(); yield return Detonate();
            var visual = _bomb.ExplosionVisual;
            encounter.ResetNow();
            Assert.That(visual.IsComplete && !visual.BodyRenderer.enabled, Is.True);
            yield return null;
            Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);

            PlacePlayer(new Vector2(158f, 0.02f));
            yield return Launch(); yield return Detonate();
            _bomb.ExplosionVisual.enabled = false;
            yield return new WaitForFixedUpdate(); yield return null;
            Assert.That(_enemy.ActiveBombCount, Is.Zero, "A disabled visual leaked its spent bomb.");

            PlacePlayer(new Vector2(158f, 0.02f));
            yield return Launch(); yield return Detonate();
            SceneManager.LoadScene("SalvageIntake"); yield return null;
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);
        }

        private IEnumerator LoadSalvage()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            _director.enabled = false;
            _enemy = _director.Encounters[1].Members[1].GetComponent<Bombardier2D>();
            _player = _director.Player.GetComponent<Rigidbody2D>();
            _playerHealth = _director.PlayerHealth;
            PlacePlayer(new Vector2(9f, 0.05f));
        }

        [UnityTest]
        public IEnumerator ProductionRetryRestartAndCompletion_CancelOwnedExplosion()
        {
            yield return LoadSalvage();
            for (int action = 0; action < 3; action++)
            {
                PlacePlayer(new Vector2(9f, 0.05f));
                yield return Launch(); yield return Detonate();
                var visual = _bomb.ExplosionVisual;
                if (action == 0) _director.RetryNow();
                else if (action == 1) _director.RestartRun();
                else
                {
                    // Focused completion boundary: real dead members and owned blast,
                    // staged extraction progress; this does not prove human traversal.
                    float scale = Time.timeScale;
                    try
                    {
                        Time.timeScale = 0f;
                        foreach (var encounter in _director.Encounters)
                            foreach (var health in encounter.Members)
                            {
                                var hit = new DamageInfo2D(1000, health.transform.position, Vector2.right, null);
                                health.ApplyDamage(in hit);
                            }
                        PlacePlayer(new Vector2(193f, 29.08f));
                        _director.enabled = true;
                        for (int i = 0; i < 20 && _director.Progress.ClearedEncounters < 7; i++) yield return null;
                        Assert.That(_director.Progress.ClearedEncounters, Is.EqualTo(7));
                        Assert.That(_director.Progress.TickExtraction(1.9f, true, true), Is.False);
                    }
                    finally { Time.timeScale = scale; }
                    for (int i = 0; i < 30 && !_director.Progress.IsComplete; i++) yield return null;
                    Assert.That(_director.Progress.IsComplete, Is.True);
                }
                Assert.That(visual == null || (visual.IsComplete && !visual.BodyRenderer.enabled), Is.True);
                yield return null; yield return null;
                Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);
                Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);
            }
        }

        [UnityTest]
        public IEnumerator CheckpointDeath_CleansPendingBlastAndKeepsCommittedDefeats()
        {
            yield return LoadSalvage();
            for (int i = 0; i < 2; i++)
                foreach (var health in _director.Encounters[i].Members)
                {
                    var hit = new DamageInfo2D(1000, health.transform.position, Vector2.right, null);
                    health.ApplyDamage(in hit);
                }
            PlacePlayer(new Vector2(22f, 1.08f));
            _director.enabled = true;
            for (int i = 0; i < 20 && !_director.Progress.HasCheckpoint; i++) yield return null;
            Assert.That(_director.Progress.CheckpointIndex, Is.Zero);
            Assert.That(_director.Progress.CommittedEncounters, Is.EqualTo(2));
            _director.enabled = false;
            _enemy = _director.Encounters[6].Members[2].GetComponent<Bombardier2D>();
            PlacePlayer(new Vector2(184f, 29.05f));
            yield return Launch(); yield return Detonate();
            var visual = _bomb.ExplosionVisual;
            _director.enabled = true; // Restore its normal health-event subscriptions.
            var lethal = new DamageInfo2D(1000, _player.position, Vector2.right, null);
            _playerHealth.ApplyDamage(in lethal);
            Assert.That(_director.IsRetrying, Is.True);
            Assert.That(visual.IsComplete && !visual.BodyRenderer.enabled, Is.True);
            _director.RetryNow();
            Assert.That(_playerHealth.CurrentHealth, Is.EqualTo(100));
            Assert.That(_player.position.x, Is.EqualTo(22f));
            Assert.That(_director.Progress.ClearedEncounters, Is.EqualTo(2));
            for (int i = 0; i < 2; i++)
                foreach (var health in _director.Encounters[i].Members) Assert.That(health.IsDead, Is.True);
            Assert.That(_enemy.Health.IsAlive, Is.True);
            Assert.That(_enemy.GetComponentInChildren<BombardierPresenter2D>().FrameIndex, Is.Zero);
            yield return null; yield return null;
            Assert.That(Object.FindObjectsByType<BombardierExplosionPresenter2D>(), Is.Empty);
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator BombardierDeath_RetryAndRestartRestoreEveryVisualPhase()
        {
            yield return LoadSalvage();
            var enemy = _director.Encounters[6].Members[2].GetComponent<Bombardier2D>();
            var visual = enemy.GetComponentInChildren<BombardierPresenter2D>();
            float[] ages = { 0.2f, 1f, 1.46f, 1.59f, 1.72f, 1.9f };
            int[] frames = { 13, 15, 16, 17, 18, -1 };
            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            yield return null;
            try
            {
                for (int phase = 0; phase < ages.Length; phase++)
                    for (int restart = 0; restart < 2; restart++)
                    {
                        _director.RestartRun();
                        var hit = new DamageInfo2D(1000, enemy.transform.position, Vector2.right, null);
                        enemy.Health.ApplyDamage(in hit);
                        yield return new WaitForSeconds(ages[phase]);
                        Assert.That(visual.FrameIndex, Is.EqualTo(frames[phase]));
                        Assert.That(visual.BodyRenderer.enabled, Is.EqualTo(phase < 5));
                        Assert.That(enemy.Health.IsDead && enemy.gameObject.activeInHierarchy, Is.True);
                        Assert.That(enemy.WeaponHitCollider.enabled, Is.False);
                        Assert.That(_director.Progress.ClearedEncounters, Is.Zero);
                        if (restart == 0) _director.RetryNow(); else _director.RestartRun();
                        Assert.That(visual.FrameIndex, Is.Zero);
                        Assert.That(visual.BodyRenderer.enabled && enemy.Health.IsAlive, Is.True);
                        Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("bombardier_idle_0"));
                    }
            }
            finally { Time.captureDeltaTime = capture; }
        }

        [UnityTest]
        public IEnumerator RenderedExplosion_AllSixKeysUseNativeCanonicalPresentation()
        {
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create(); presentation.ProcessingCamera.targetTexture = probe;
            float scale = Time.timeScale;
            Directory.CreateDirectory("Logs/CombatVfx");
            try
            {
                yield return Launch(); yield return Detonate();
                var visual = _bomb.ExplosionVisual;
                presentation.WorldCamera.transform.position = (Vector3)_bomb.ImpactPoint + Vector3.back * 10f;
                for (int frame = 0; frame < 6; frame++)
                {
                    for (int i = 0; i < 30 && visual.FrameIndex < frame; i++) yield return null;
                    Assert.That(visual.FrameIndex, Is.EqualTo(frame));
                    Time.timeScale = 0f; // Freeze only for an exact resolved-target foreground comparison.
                    visual.BodyRenderer.enabled = true;
                    yield return null; yield return null;
                    var shown = ReadRegion(presentation.ResolvedTarget, "Logs/CombatVfx/native-explosion-" + frame + ".png");
                    visual.BodyRenderer.enabled = false;
                    yield return null; yield return null;
                    var hidden = ReadRegion(presentation.ResolvedTarget, null);
                    int changed = 0;
                    for (int i = 0; i < shown.Length; i++) if (!shown[i].Equals(hidden[i]))
                    {
                        changed++; Assert.That(RustlinePalette.IsCanonical(shown[i]), Is.True);
                    }
                    Assert.That(changed, Is.GreaterThan(10), "No rendered explosion key " + frame);
                    visual.BodyRenderer.enabled = true;
                    Time.timeScale = scale;
                }
            }
            finally
            {
                Time.timeScale = scale;
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        [UnityTest]
        public IEnumerator RenderedBombardierDeath_ProgressivelyRemovesWreckThenHides()
        {
            yield return LoadSalvage();
            _director.Player.enabled = false;
            _player.simulated = false;
            _director.Player.transform.position = _enemy.transform.position - Vector3.right * 4f;
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            presentation.WorldCamera.transform.position = _enemy.transform.position + new Vector3(0, 2, -10);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create(); presentation.ProcessingCamera.targetTexture = probe;
            Directory.CreateDirectory("Logs/CombatVfx");
            float capture = Time.captureDeltaTime;
            Time.captureDeltaTime = 1f / 60f;
            try
            {
                var hit = new DamageInfo2D(1000, _enemy.transform.position, Vector2.right, null);
                float start = Time.time;
                _enemy.Health.ApplyDamage(in hit);
                foreach (var actor in Object.FindObjectsByType<Sentry2D>()) actor.SetCombatActive(false);
                foreach (var actor in Object.FindObjectsByType<Bombardier2D>()) actor.SetCombatActive(false);
                float[] ages = { 0.85f, 1.46f, 1.59f, 1.72f, 1.92f };
                var regions = new Color32[5][];
                for (int i = 0; i < ages.Length; i++)
                {
                    while (Time.time - start < ages[i]) yield return null;
                    yield return null; yield return null;
                    regions[i] = ReadRegion(presentation.ResolvedTarget, "Logs/CombatVfx/native-death-" + i + ".png");
                }
                int previous = int.MaxValue;
                for (int frame = 0; frame < 4; frame++)
                {
                    int count = 0;
                    for (int pixel = 0; pixel < regions[4].Length; pixel++)
                        if (!regions[frame][pixel].Equals(regions[4][pixel]))
                        {
                            count++; Assert.That(RustlinePalette.IsCanonical(regions[frame][pixel]), Is.True);
                        }
                    Assert.That(count, Is.InRange(1, previous - 1)); previous = count;
                }
                Assert.That(_enemy.Health.IsDead && _enemy.gameObject.activeInHierarchy, Is.True);
                Assert.That(_enemy.GetComponentInChildren<BombardierPresenter2D>().BodyRenderer.enabled, Is.False);
            }
            finally
            {
                Time.captureDeltaTime = capture;
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        private static Color32[] ReadRegion(RenderTexture target, string path)
        {
            var previous = RenderTexture.active;
            var readback = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); readback.Apply();
                if (path != null) File.WriteAllBytes(path, readback.EncodeToPNG());
                var pixels = readback.GetPixels32(); var region = new Color32[96 * 96];
                for (int y = 0; y < 96; y++)
                    System.Array.Copy(pixels, (target.height / 2 - 48 + y) * target.width + target.width / 2 - 48, region, y * 96, 96);
                return region;
            }
            finally { RenderTexture.active = previous; Object.Destroy(readback); }
        }
    }
}
