using System.Collections;
using System.IO;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class SentryPresentationPlayModeTests
    {
        private FirstPlayableDirector2D _director;

        [UnityTest]
        public IEnumerator RenderedDeathFinish_RemovesPixelsProgressivelyThenLeavesNoForeground()
        {
            _director.enabled = false;
            foreach (var encounter in _director.Encounters) encounter.ResetEncounter();
            _director.Player.enabled = false;
            _director.Player.GetComponent<Rigidbody2D>().simulated = false;
            var enemy = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            _director.Player.transform.position = enemy.transform.position - Vector3.right * 4f;
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            presentation.WorldCamera.transform.position = enemy.transform.position + new Vector3(0, 2, -10);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            Directory.CreateDirectory("Logs/SentryArt");
            try
            {
                var hit = new DamageInfo2D(1000, enemy.transform.position, Vector2.right, null);
                float start = Time.time;
                enemy.Health.ApplyDamage(in hit);
                float[] ages = { 0.65f, 1.13f, 1.26f, 1.39f, 1.55f };
                var regions = new Color32[ages.Length][];
                for (int i = 0; i < ages.Length; i++)
                {
                    while (Time.time - start < ages[i]) yield return null;
                    yield return null; yield return null;
                    regions[i] = ReadRegion(presentation.ResolvedTarget, "Logs/SentryArt/m5-death-" + i + ".png");
                }
                Color32[] background = regions[4];
                int previous = int.MaxValue;
                for (int frame = 0; frame < 4; frame++)
                {
                    int count = 0;
                    for (int pixel = 0; pixel < background.Length; pixel++)
                        if (!regions[frame][pixel].Equals(background[pixel]))
                        {
                            count++;
                            Assert.That(RustlinePalette.IsCanonical(regions[frame][pixel]), Is.True);
                        }
                    Assert.That(count, Is.InRange(1, previous - 1), "Rendered wreck/dissolution stage " + frame);
                    previous = count;
                }
                Assert.That(enemy.GetComponentInChildren<SentryPresenter2D>().BodyRenderer.enabled, Is.False);
                Assert.That(enemy.Health.IsDead && enemy.gameObject.activeInHierarchy, Is.True);
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        [UnityTest]
        public IEnumerator DeathFinish_ResetsDuringCollapseHoldEachDissolveFrameAndAfterHidden()
        {
            _director.enabled = false;
            var enemy = _director.Encounters[6].Members[0].GetComponent<Sentry2D>();
            var visual = enemy.GetComponentInChildren<SentryPresenter2D>();
            float[] ages = { 0.15f, 0.75f, 1.13f, 1.26f, 1.39f, 1.55f };
            int[] frames = { 13, 15, 16, 17, 18, -1 };
            for (int phase = 0; phase < ages.Length; phase++)
            {
                _director.RestartRun();
                var hit = new DamageInfo2D(1000, enemy.transform.position, Vector2.right, null);
                enemy.Health.ApplyDamage(in hit);
                yield return new WaitForSeconds(ages[phase]);
                Assert.That(visual.FrameIndex, Is.EqualTo(frames[phase]), "Death phase " + phase);
                Assert.That(visual.BodyRenderer.enabled, Is.EqualTo(phase < 5));
                Assert.That(enemy.Health.IsDead && enemy.gameObject.activeInHierarchy, Is.True);
                Assert.That(enemy.GetComponentInChildren<Collider2D>().enabled, Is.False);
                _director.RetryNow();
                Assert.That(visual.FrameIndex, Is.Zero);
                Assert.That(visual.BodyRenderer.enabled && enemy.Health.IsAlive, Is.True);
                enemy.Health.ApplyDamage(in hit);
                yield return new WaitForSeconds(ages[phase]);
                _director.RestartRun();
                Assert.That(visual.FrameIndex, Is.Zero);
                Assert.That(visual.BodyRenderer.enabled && enemy.Health.IsAlive, Is.True);
                Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("sentry_idle_0"));
            }
        }
        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
        }

        [UnityTest]
        public IEnumerator SavedSentries_AreSpriteBasedDormantVisibleAndRestoreAfterEarlyDeath()
        {
            var enemies = Object.FindObjectsByType<Sentry2D>();
            Assert.That(enemies.Length, Is.EqualTo(8));
            foreach (Sentry2D enemy in enemies)
            {
                var presenter = enemy.GetComponentInChildren<SentryPresenter2D>();
                Assert.That(presenter, Is.Not.Null);
                Assert.That(presenter.FrameCount, Is.EqualTo(16));
                Assert.That(presenter.HasCompleteFrames, Is.True);
                Assert.That(presenter.BodyRenderer.enabled, Is.True);
                Assert.That(presenter.BodyRenderer.sprite, Is.Not.Null);
                Assert.That(presenter.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(enemy.GetComponentInChildren<LineRenderer>(), Is.Null);
                Assert.That(enemy.CombatActive, Is.True, "Unengaged sentries remain active patrol actors.");
            }
            var future = _director.Encounters[6].Members[0].GetComponent<Sentry2D>();
            var visual = future.GetComponentInChildren<SentryPresenter2D>();
            var damage = new DamageInfo2D(1000, future.transform.position, Vector2.right, null);
            future.Health.ApplyDamage(in damage);
            Assert.That(visual.FrameIndex, Is.EqualTo(12));
            yield return new WaitForSeconds(0.5f);
            Assert.That(visual.FrameIndex, Is.EqualTo(15));
            Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("sentry_death_3"));
            Assert.That(visual.BodyRenderer.enabled, Is.True, "Settled wreck is deliberate, not a disabled enemy root.");
            Assert.That(_director.Progress.ClearedEncounters, Is.Zero);
            _director.Encounters[6].Activate();
            Assert.That(future.Health.IsDead, Is.True);
            _director.RestartRun();
            Assert.That(visual.FrameIndex, Is.Zero);
            Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("sentry_idle_0"));
            Assert.That(visual.BodyRenderer.enabled && future.Health.IsAlive, Is.True);
            Assert.That(visual.BodyRenderer.color, Is.EqualTo(Color.white));
            Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
            Assert.That(visual.BodyRenderer.flipX, Is.True);
        }

        [UnityTest]
        public IEnumerator Patrol_AnimatesActualMovementAndSnapsOnlyTheVisualChild()
        {
            _director.enabled = false;
            var encounter = _director.Encounters[3];
            var enemy = encounter.Members[0].GetComponent<Sentry2D>();
            var visual = enemy.GetComponentInChildren<SentryPresenter2D>();
            float startX = enemy.transform.position.x;
            encounter.Activate();
            var seen = new System.Collections.Generic.HashSet<int>();
            var renderedPositions = new System.Collections.Generic.List<float>();
            Camera world = Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            void ObserveRenderedPose(ScriptableRenderContext context, Camera camera)
            {
                if (camera != world || !enemy.IsMoving) return;
                seen.Add(visual.FrameIndex);
                renderedPositions.Add(visual.transform.position.x);
            }
            RenderPipelineManager.beginCameraRendering += ObserveRenderedPose;
            try
            {
                for (int i = 0; i < 20; i++) { yield return new WaitForFixedUpdate(); yield return null; }
            }
            finally { RenderPipelineManager.beginCameraRendering -= ObserveRenderedPose; }
            Assert.That(renderedPositions.Count, Is.GreaterThan(2), "No actual rendered patrol poses observed.");
            foreach (float x in renderedPositions)
                Assert.That(x * 16f, Is.EqualTo(Mathf.Round(x * 16f)).Within(0.001f));
            foreach (int frame in seen) Assert.That(frame, Is.InRange(2, 5));
            Assert.That(seen.Count, Is.GreaterThan(1));
            Assert.That(enemy.transform.position.x, Is.LessThan(startX - 0.2f));
            var hitbox = enemy.GetComponentInChildren<BoxCollider2D>();
            Assert.That(hitbox.size, Is.EqualTo(new Vector2(1.2f, 2.5f)));
            Assert.That(hitbox.transform.localPosition, Is.EqualTo(Vector3.up * 1.25f));
        }

        [UnityTest]
        public IEnumerator AttackAndHit_FollowBrainPauseAndCommittedStrikeWithoutChangingDamage()
        {
            var body = _director.Player.GetComponent<Rigidbody2D>();
            _director.Player.transform.position = new Vector2(-11.5f, 1.05f);
            body.position = _director.Player.transform.position;
            body.linearVelocity = Vector2.zero;
            _director.Player.ResetAfterRespawn();
            Physics2D.SyncTransforms();
            var enemy = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            var visual = enemy.GetComponentInChildren<SentryPresenter2D>();
            for (int i = 0; i < 100 && enemy.State != SentryState.Windup; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(enemy.State, Is.EqualTo(SentryState.Windup));
            Assert.That(visual.FrameIndex, Is.InRange(7, 8));
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(100));
            var damage = new DamageInfo2D(5, enemy.transform.position, Vector2.right, null);
            float timer = enemy.StateTimeRemaining;
            enemy.Health.ApplyDamage(in damage);
            yield return null;
            Assert.That(visual.FrameIndex, Is.EqualTo(11));
            Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("sentry_hit"));
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(enemy.StateTimeRemaining, Is.EqualTo(timer), "Hit presentation must not advance the attack clock.");
            for (int i = 0; i < 100 && enemy.StrikeCount == 0; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(enemy.StrikeCount, Is.EqualTo(1));
            Assert.That(visual.FrameIndex, Is.EqualTo(9));
            Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("sentry_strike"));
            Assert.That(visual.BodyRenderer.flipX, Is.EqualTo(enemy.FacingDirection < 0));
            Assert.That(_director.PlayerHealth.CurrentHealth, Is.EqualTo(80));
            yield return new WaitForSeconds(0.2f);
            Assert.That(visual.FrameIndex, Is.EqualTo(10));
            Assert.That(enemy.StrikeCount, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator RenderedSentry_RespectsNativePenumbraAndProducesInspectableWorldEvidence()
        {
            _director.enabled = false;
            foreach (var encounter in _director.Encounters) encounter.ResetEncounter();
            _director.Player.enabled = false;
            _director.Player.GetComponent<Rigidbody2D>().simulated = false;
            var enemy = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            var visual = enemy.GetComponentInChildren<SentryPresenter2D>();
            yield return null;
            visual.enabled = false; // Freeze one authored pose for exact foreground-on/off comparison.
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            presentation.WorldCamera.transform.position = enemy.transform.position + new Vector3(0, 2, -10);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            Directory.CreateDirectory("Logs/SentryArt");
            try
            {
                float[] distances = { 4f, 30.5f, 35f };
                string[] labels = { "visible", "penumbra", "dark" };
                for (int stage = 0; stage < distances.Length; stage++)
                {
                    _director.Player.transform.position = enemy.transform.position - Vector3.right * distances[stage];
                    visual.BodyRenderer.enabled = true;
                    yield return null; yield return null; yield return null;
                    Color32[] shown = ReadRegion(presentation.ResolvedTarget, "Logs/SentryArt/world-" + labels[stage] + ".png");
                    visual.BodyRenderer.enabled = false;
                    yield return null; yield return null; yield return null;
                    Color32[] hidden = ReadRegion(presentation.ResolvedTarget, null);
                    int changed = 0;
                    for (int i = 0; i < shown.Length; i++)
                        if (!shown[i].Equals(hidden[i]))
                        {
                            changed++;
                            Assert.That(RustlinePalette.IsCanonical(shown[i]), Is.True, "Rendered enemy left Canonical 28.");
                        }
                    if (stage == 2) Assert.That(changed, Is.Zero, "Enemy must disappear into existing full darkness.");
                    else Assert.That(changed, Is.GreaterThan(stage == 0 ? 100 : 0), "Sentry was not present in the rendered region.");
                }
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        private static Color32[] ReadRegion(RenderTexture target, string path)
        {
            RenderTexture previous = RenderTexture.active;
            var readback = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                readback.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                readback.Apply();
                if (path != null) File.WriteAllBytes(path, readback.EncodeToPNG());
                Color32[] pixels = readback.GetPixels32();
                var region = new Color32[80 * 160];
                for (int y = 0; y < 160; y++)
                    System.Array.Copy(pixels, (target.height / 2 - 80 + y) * target.width + target.width / 2 - 40,
                        region, y * 80, 80);
                return region;
            }
            finally { RenderTexture.active = previous; Object.Destroy(readback); }
        }
    }
}
