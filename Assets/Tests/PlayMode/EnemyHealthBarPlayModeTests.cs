using System.Collections;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Rustline.Tests
{
    public sealed class EnemyHealthBarPlayModeTests
    {
        private FirstPlayableDirector2D _director;

        [UnitySetUp]
        public IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            _director.enabled = false;
            _director.Player.enabled = false;
            _director.Player.GetComponent<Rigidbody2D>().simulated = false;
            _director.Player.GetComponent<PlayerAim2D>().enabled = false;
        }

        [UnityTest]
        public IEnumerator Damage_Inactivity_Death_ResetAndReenable_WorkForBothEnemyTypes()
        {
            var all = Object.FindObjectsByType<EnemyHealthBar2D>(FindObjectsSortMode.None);
            Assert.That(all.Length, Is.EqualTo(14));
            foreach (var bar in all)
            {
                bool patrol = bar.TryGetComponent(out Sentry2D actor) ? actor.State == SentryState.Patrol :
                    bar.GetComponent<Bombardier2D>().State == BombardierState.Patrol;
                if (patrol) Assert.That(bar.IsVisible, Is.False, "Unengaged patrols must stay quiet.");
            }
            CombatHealth2D[] enemies = { _director.Encounters[0].Members[0], _director.Encounters[1].Members[1] };
            foreach (CombatHealth2D health in enemies)
            {
                Freeze(health);
                var damage = new DamageInfo2D(health.MaximumHealth / 2, health.transform.position, Vector2.zero, null);
                health.ApplyDamage(in damage);
            }
            yield return null; yield return null;
            foreach (CombatHealth2D health in enemies)
            {
                var bar = health.GetComponent<EnemyHealthBar2D>();
                Assert.That(bar.IsVisible, Is.True);
                Assert.That(bar.FilledPixels, Is.EqualTo(12));
                for (int step = 0; step < 8; step++)
                {
                    health.transform.position += new Vector3(0.037f, 0.021f, 0);
                    yield return null;
                    AssertAnchor(bar, health.transform.position);
                }
            }
            yield return new WaitForSeconds(EnemyHealthBar2D.InactivitySeconds + 0.1f);
            foreach (CombatHealth2D health in enemies)
            {
                var bar = health.GetComponent<EnemyHealthBar2D>();
                Assert.That(bar.IsVisible, Is.False);
                var zero = new DamageInfo2D(0, health.transform.position, Vector2.zero, null);
                health.ApplyDamage(in zero);
                yield return null;
                Assert.That(bar.IsVisible, Is.False, "Ignored damage must not reveal a bar.");
                var smallHit = new DamageInfo2D(1, health.transform.position, Vector2.zero, null);
                health.ApplyDamage(in smallHit);
                yield return null;
                Assert.That(bar.IsVisible, Is.True, "Revealed before testing immediate death hiding.");
                var lethal = new DamageInfo2D(1000, health.transform.position, Vector2.zero, null);
                health.ApplyDamage(in lethal);
                Assert.That(bar.IsVisible, Is.False);
                Assert.That(bar.FilledPixels, Is.Zero);
                health.GetComponent<Sentry2D>()?.ResetForEncounter();
                health.GetComponent<Bombardier2D>()?.ResetForEncounter();
                Assert.That(bar.FilledPixels, Is.EqualTo(24));
                Assert.That(bar.IsVisible, Is.False);
                bar.enabled = false;
                bar.enabled = true;
                var hit = new DamageInfo2D(1, health.transform.position, Vector2.zero, null);
                health.ApplyDamage(in hit);
                yield return null;
                Assert.That(bar.IsVisible, Is.True, "Re-enabled bar must subscribe exactly once.");
            }
        }

        [UnityTest]
        public IEnumerator ActualDetectionAndAttackStates_RevealAndTrackWithoutChangingCombat()
        {
            var sentry = _director.Encounters[0].Members[0].GetComponent<Sentry2D>();
            _director.Player.transform.position = new Vector3(-14, 1, 0);
            Physics2D.SyncTransforms();
            _director.Encounters[0].Activate();
            for (int i = 0; i < 30 && sentry.State == SentryState.Patrol; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(sentry.State, Is.Not.EqualTo(SentryState.Patrol));
            var bar = sentry.GetComponent<EnemyHealthBar2D>();
            Assert.That(bar.IsVisible, Is.True);
            Assert.That(bar.FilledPixels, Is.EqualTo(24));
            for (int i = 0; i < 30; i++)
            {
                yield return new WaitForFixedUpdate(); yield return null;
                Assert.That(bar.AnchorPosition.x * 16, Is.EqualTo(Mathf.Round(bar.AnchorPosition.x * 16)));
                Assert.That(bar.AnchorPosition.y * 16, Is.EqualTo(Mathf.Round(bar.AnchorPosition.y * 16)));
                Assert.That(Vector2.Distance(bar.AnchorPosition,
                    sentry.transform.position + Vector3.up * EnemyHealthBar2D.HeadHeight), Is.LessThan(0.15f));
            }
            var bombardier = _director.Encounters[1].Members[1].GetComponent<Bombardier2D>();
            _director.Player.transform.position = new Vector3(20, 1, 0);
            Physics2D.SyncTransforms();
            _director.Encounters[1].Activate();
            for (int i = 0; i < 30 && bombardier.State == BombardierState.Patrol; i++) yield return new WaitForFixedUpdate();
            yield return null;
            Assert.That(bombardier.State, Is.EqualTo(BombardierState.Windup));
            Assert.That(bombardier.GetComponent<EnemyHealthBar2D>().IsVisible, Is.True);
            Assert.That(bombardier.Health.CurrentHealth, Is.EqualTo(bombardier.Health.MaximumHealth));
        }

        [UnityTest]
        public IEnumerator NativeRender_ShowsExactHealthPixelsAndRestoredBeamFlooring()
        {
            foreach (FirstPlayableEncounter2D encounter in _director.Encounters)
                foreach (CombatHealth2D health in encounter.Members) Freeze(health);
            _director.Player.transform.position = new Vector3(8, 0, 0);
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            presentation.WorldCamera.transform.position = new Vector3(8, 8, -10);
            string output = "Logs/VisualRefinementPhase2";
            Directory.CreateDirectory(output);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            try
            {
                for (int i = 0; i < 4; i++) yield return null;
                Render(presentation);
                Read(presentation.ResolvedTarget, output + "/bay-quiet.png");
                var enemies = _director.Encounters[1].Members;
                foreach (CombatHealth2D health in enemies)
                {
                    var hit = new DamageInfo2D(health.MaximumHealth / 2, health.transform.position, Vector2.zero, null);
                    health.ApplyDamage(in hit);
                }
                for (int i = 0; i < 4; i++) yield return null;
                Render(presentation);
                Color32[] shown = Read(presentation.ResolvedTarget, output + "/bay-damaged.png");
                foreach (CombatHealth2D health in enemies) health.GetComponent<EnemyHealthBar2D>().enabled = false;
                Render(presentation);
                Color32[] hidden = Read(presentation.ResolvedTarget, null);
                int red = 0, changed = 0;
                for (int i = 0; i < shown.Length; i++)
                {
                    if (shown[i].Equals(hidden[i])) continue;
                    changed++;
                    Assert.That(RustlinePalette.IsCanonical(shown[i]), Is.True, "Health bar introduced a noncanonical pixel.");
                    if (shown[i].Equals(RustlinePalette.GetColor(14))) red++;
                }
                Assert.That(changed, Is.GreaterThan(100), "Health bars must actually render.");
                Assert.That(red, Is.EqualTo(48), "Two half-health bars, each 12x2 native red pixels.");
                File.WriteAllText(output + "/capture.txt", "Actual Unity URP palette-resolved " +
                    presentation.ResolvedTarget.width + "x" + presentation.ResolvedTarget.height +
                    " targets. Camera (8,8); player root (8,0); actors held. Quiet and two half-health damage bars. " +
                    "Screen-space overlay HUD excluded. Offscreen renders, not window screenshots.");
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        private static void Freeze(CombatHealth2D health)
        {
            if (health.TryGetComponent(out Sentry2D sentry)) sentry.enabled = false;
            if (health.TryGetComponent(out Bombardier2D bombardier)) bombardier.enabled = false;
            health.GetComponent<Rigidbody2D>().simulated = false;
        }

        private static void AssertAnchor(EnemyHealthBar2D bar, Vector3 root)
        {
            Assert.That(bar.AnchorPosition.x, Is.EqualTo(Mathf.Round(root.x * 16) / 16f));
            Assert.That(bar.AnchorPosition.y, Is.EqualTo(Mathf.Round((root.y + EnemyHealthBar2D.HeadHeight) * 16) / 16f));
        }

        private static void Render(NativePixelPresentation presentation)
        {
            presentation.WorldCamera.Render();
            presentation.ProcessingCamera.Render();
        }

        private static Color32[] Read(RenderTexture target, string path)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height), 0, 0); image.Apply();
                if (path != null) File.WriteAllBytes(path, image.EncodeToPNG());
                return image.GetPixels32();
            }
            finally { RenderTexture.active = previous; Object.Destroy(image); }
        }
    }
}
