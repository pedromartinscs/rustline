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
    public sealed class BombardierPresentationPlayModeTests
    {
        private FirstPlayableDirector2D _director;
        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
        }

        [UnityTest]
        public IEnumerator SavedBombardiers_AreCompleteDormantSpritesAndResetAfterEarlyDeath()
        {
            var enemies = Object.FindObjectsByType<Bombardier2D>();
            Assert.That(enemies.Length, Is.EqualTo(6));
            foreach (var enemy in enemies)
            {
                var visual = enemy.GetComponentInChildren<BombardierPresenter2D>();
                Assert.That(visual.HasCompleteFrames, Is.True);
                Assert.That(visual.FrameCount, Is.EqualTo(16));
                Assert.That(visual.BodyRenderer.sprite, Is.Not.Null);
                Assert.That(visual.BodyRenderer.enabled, Is.True);
                Assert.That(visual.BodyRenderer.color, Is.EqualTo(Color.white));
                Assert.That(visual.transform.localScale, Is.EqualTo(Vector3.one));
                Assert.That(enemy.ProjectileMaterial, Is.Not.Null);
                Assert.That(visual.TargetMarker.sharedMaterial, Is.SameAs(enemy.ProjectileMaterial));
                Assert.That(visual.TargetMarker.enabled, Is.False);
                Assert.That(enemy.GetComponentInChildren<LineRenderer>(), Is.Null, "No obsolete body outline may remain.");
                Assert.That(enemy.CombatActive, Is.False);
                Assert.That(enemy.WeaponHitCollider.enabled, Is.True);
            }
            var future = _director.Encounters[6].Members[2].GetComponent<Bombardier2D>();
            var presenter = future.GetComponentInChildren<BombardierPresenter2D>();
            var hit = new DamageInfo2D(1000, future.transform.position, Vector2.right, null);
            future.Health.ApplyDamage(in hit);
            Assert.That(presenter.FrameIndex, Is.EqualTo(12));
            Assert.That(presenter.BodyRenderer.enabled && presenter.IsDeadVisual, Is.True);
            yield return new WaitForSeconds(0.65f);
            Assert.That(presenter.FrameIndex, Is.EqualTo(15));
            Assert.That(presenter.BodyRenderer.sprite.name, Is.EqualTo("bombardier_death_3"));
            _director.Encounters[6].Activate();
            Assert.That(future.Health.IsDead, Is.True);
            Assert.That(_director.Progress.ClearedEncounters, Is.Zero);
            _director.RetryNow();
            Assert.That(presenter.FrameIndex, Is.Zero);
            Assert.That(presenter.BodyRenderer.enabled && future.Health.IsAlive, Is.True);
            future.Health.ApplyDamage(in hit);
            _director.RestartRun();
            Assert.That(presenter.FrameIndex, Is.Zero);
            Assert.That(presenter.BodyRenderer.sprite.name, Is.EqualTo("bombardier_idle_0"));
            Assert.That(presenter.TargetMarker.enabled, Is.False);
        }

        [UnityTest]
        public IEnumerator LaunchPose_MatchesOneActualProjectileAndPreservesWarningHitPauseAndCleanup()
        {
            _director.enabled = false;
            var enemy = _director.Encounters[1].Members[1].GetComponent<Bombardier2D>();
            var visual = enemy.GetComponentInChildren<BombardierPresenter2D>();
            var body = _director.Player.GetComponent<Rigidbody2D>();
            _director.Player.transform.position = new Vector2(9f, 0.05f);
            body.position = _director.Player.transform.position;
            body.linearVelocity = Vector2.zero;
            _director.Player.ResetAfterRespawn();
            Physics2D.SyncTransforms();
            enemy.SetCombatActive(true);
            for (int i = 0; i < 60 && enemy.State != BombardierState.Windup; i++) yield return new WaitForFixedUpdate();
            yield return null; yield return null;
            Assert.That(enemy.State, Is.EqualTo(BombardierState.Windup));
            Assert.That(visual.FrameIndex, Is.InRange(6, 8));
            Assert.That(visual.TargetMarker.enabled, Is.True);
            Vector2 locked = enemy.LockedTargetPoint;
            Assert.That((Vector2)visual.TargetMarker.transform.position, Is.EqualTo(locked));
            float remaining = enemy.StateTimeRemaining;
            var hit = new DamageInfo2D(5, enemy.transform.position, Vector2.right, null);
            enemy.Health.ApplyDamage(in hit);
            Assert.That(visual.FrameIndex, Is.EqualTo(11));
            Assert.That(visual.IsHitReacting, Is.True);
            for (int i = 0; i < 3; i++) yield return new WaitForFixedUpdate();
            Assert.That(enemy.StateTimeRemaining, Is.EqualTo(remaining));
            Assert.That(enemy.LaunchCount, Is.Zero);
            body.position = new Vector2(6f, 0.05f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int i = 0; i < 60 && enemy.LaunchCount == 0; i++) yield return new WaitForFixedUpdate();
            yield return null; yield return null;
            Assert.That(enemy.State, Is.EqualTo(BombardierState.Recover));
            Assert.That(enemy.LaunchCount, Is.EqualTo(1));
            Assert.That(visual.FrameIndex, Is.EqualTo(9));
            Assert.That(visual.BodyRenderer.sprite.name, Is.EqualTo("bombardier_launch"));
            Assert.That(visual.BodyRenderer.flipX, Is.EqualTo(enemy.PatrolDirection < 0));
            Assert.That(visual.TargetMarker.enabled, Is.False);
            Assert.That(enemy.LockedTargetPoint, Is.EqualTo(locked));
            var bomb = Object.FindAnyObjectByType<BombardierBomb2D>();
            Assert.That(bomb, Is.Not.Null);
            Assert.That(bomb.GetComponent<LineRenderer>().sharedMaterial, Is.SameAs(enemy.ProjectileMaterial));
            Assert.That(bomb.InitialVelocity, Is.EqualTo(BombardierBallistics2D.InitialVelocity(enemy.ThrowOrigin.position, locked)));
            yield return new WaitForSeconds(0.18f);
            Assert.That(visual.FrameIndex, Is.EqualTo(10));
            Assert.That(enemy.LaunchCount, Is.EqualTo(1));
            var lethal = new DamageInfo2D(1000, enemy.transform.position, Vector2.right, null);
            enemy.Health.ApplyDamage(in lethal);
            Assert.That(visual.FrameIndex, Is.EqualTo(12));
            Assert.That(visual.TargetMarker.enabled, Is.False);
            _director.RetryNow();
            Assert.That(enemy.ActiveBombCount, Is.Zero);
            Assert.That(visual.FrameIndex, Is.Zero);
            Assert.That(visual.BodyRenderer.enabled && enemy.Health.IsAlive, Is.True);
            yield return null;
            Assert.That(Object.FindObjectsByType<BombardierBomb2D>(), Is.Empty);
        }

        [UnityTest]
        public IEnumerator Patrol_UsesCommandedMovementAndPixelSnapsOnlyTheVisualChild()
        {
            _director.enabled = false;
            var enemy = _director.Encounters[3].Members[1].GetComponent<Bombardier2D>();
            var visual = enemy.GetComponentInChildren<BombardierPresenter2D>();
            float startX = enemy.Body.position.x;
            enemy.SetCombatActive(true);
            var frames = new System.Collections.Generic.HashSet<int>();
            var positions = new System.Collections.Generic.List<float>();
            var world = Object.FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            void Observe(ScriptableRenderContext context, Camera camera)
            {
                if (camera != world || !enemy.IsMoving) return;
                frames.Add(visual.FrameIndex); positions.Add(visual.transform.position.x);
            }
            RenderPipelineManager.beginCameraRendering += Observe;
            try { for (int i = 0; i < 20; i++) { yield return new WaitForFixedUpdate(); yield return null; } }
            finally { RenderPipelineManager.beginCameraRendering -= Observe; }
            Assert.That(positions.Count, Is.GreaterThan(2));
            foreach (float x in positions) Assert.That(x * 16f, Is.EqualTo(Mathf.Round(x * 16f)).Within(0.001f));
            foreach (int frame in frames) Assert.That(frame, Is.InRange(2, 5));
            Assert.That(frames.Count, Is.GreaterThan(1));
            Assert.That(enemy.Body.position.x, Is.GreaterThan(startX + 0.2f));
            var collider = (BoxCollider2D)enemy.WeaponHitCollider;
            Assert.That(collider.size, Is.EqualTo(new Vector2(1.2f, 2.5f)));
            Assert.That(collider.transform.localPosition, Is.EqualTo(Vector3.up * 1.25f));
            Assert.That(enemy.ThrowOrigin.localPosition, Is.EqualTo(Vector3.up * 2f));
        }

        [UnityTest]
        public IEnumerator RenderedBombardier_RespectsNativePenumbraAndProducesInspectableWorldEvidence()
        {
            _director.enabled = false;
            foreach (var encounter in _director.Encounters) encounter.ResetEncounter();
            _director.Player.enabled = false;
            _director.Player.GetComponent<Rigidbody2D>().simulated = false;
            var enemy = _director.Encounters[1].Members[1].GetComponent<Bombardier2D>();
            var visual = enemy.GetComponentInChildren<BombardierPresenter2D>();
            yield return null;
            visual.enabled = false; // Freeze one authored pose for exact foreground-on/off comparison.
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            presentation.WorldCamera.transform.position = enemy.transform.position + new Vector3(0, 2, -10);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            Directory.CreateDirectory("Logs/BombardierArt");
            try
            {
                float[] distances = { 4f, 30.5f, 35f };
                string[] labels = { "visible", "penumbra", "dark" };
                for (int stage = 0; stage < distances.Length; stage++)
                {
                    _director.Player.transform.position = enemy.transform.position - Vector3.right * distances[stage];
                    visual.BodyRenderer.enabled = true;
                    yield return null; yield return null; yield return null;
                    Color32[] shown = ReadRegion(presentation.ResolvedTarget, "Logs/BombardierArt/world-" + labels[stage] + ".png");
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
                    else Assert.That(changed, Is.GreaterThan(stage == 0 ? 100 : 0), "Bombardier was not present in the rendered region.");
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

