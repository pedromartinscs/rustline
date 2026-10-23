using System.Collections;
using System.IO;
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
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class EnvironmentVisualHardeningPlayModeTests
    {
        [UnityTest]
        public IEnumerator ReinforcedModularPlates_KeepExistingBreachingWhilePermanentSkinsStaySolid()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null;
            var collision = GameObject.Find("Ground Collision - Hidden").GetComponent<Tilemap>();
            var receiver = collision.GetComponent<BreachableTilemap2D>();
            Tilemap surface = receiver.StructuralVisualTilemap;
            var weapon = Object.FindAnyObjectByType<PlayerWeaponController2D>().WeaponDefinition;
            Assert.That(receiver.AllowsWeapon(weapon), Is.True);
            var cell = new Vector3Int(126,22,0);
            Assert.That(surface.GetSprite(cell).texture.name, Is.EqualTo("reinforced_plate_left"));
            var hit = new WeaponHitInfo2D(weapon,new Vector2(125,22.5f),Vector2.right,
                new Vector2(126,22.5f),Vector2.left,1,40,collision.GetComponent<CompositeCollider2D>());
            receiver.ReceiveHit(in hit);
            Assert.That(surface.HasTile(cell), Is.False);
            Assert.That(collision.HasTile(cell), Is.False, "Reinforcement must not silently disable existing Longwatch breaching.");
            // First-shaft architecture had no visual tile before M7 and remains permanent.
            var permanent = new Vector3Int(27,8,0);
            Assert.That(surface.HasTile(permanent), Is.False);
            hit = new WeaponHitInfo2D(weapon,new Vector2(29,8.5f),Vector2.left,
                new Vector2(28,8.5f),Vector2.right,1,40,collision.GetComponent<CompositeCollider2D>());
            receiver.ReceiveHit(in hit);
            Assert.That(collision.HasTile(permanent), Is.True);
        }

        [UnityTest]
        public IEnumerator RouteArt_RendersNativeSceneAndCanonicalNewSprites()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null; yield return null;
            var director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            director.enabled = false;
            director.Player.enabled = false;
            director.Player.GetComponent<Rigidbody2D>().simulated = false;
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            string output = Path.GetFullPath("Logs/M7/RouteRenders"); Directory.CreateDirectory(output);
            Vector2[] views = {new Vector2(-20,1),new Vector2(8,1),new Vector2(30,8),new Vector2(38,17),
                new Vector2(62,17),new Vector2(100,17),new Vector2(124,23),new Vector2(146,24),
                new Vector2(164,29),new Vector2(185,29)};
            foreach (Vector2 position in views)
            {
                director.Player.transform.position = position;
                Camera camera = presentation.WorldCamera;
                camera.transform.position = new Vector3(position.x,position.y+2,camera.transform.position.z);
                Physics2D.SyncTransforms();
                yield return null; yield return null; yield return null;
                camera.Render();
                // Batch tests explicitly drive both existing cameras: the world pass does
                // not itself execute the utility camera's native-pixel resolve feature.
                presentation.ProcessingCamera.Render();
                Assert.That(presentation.WorldTarget.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(presentation.ResolvedTarget.filterMode, Is.EqualTo(FilterMode.Point));
                Save(presentation.WorldTarget,Path.Combine(output,$"route-{position.x}-world.png"),false);
                Save(presentation.ResolvedTarget,Path.Combine(output,$"route-{position.x}-resolved.png"),false);
                // Full views retain actors/checkpoints/VFX for readability review. Existing
                // gameplay Warning Orange encodes with a one-byte blue difference, also in
                // pre-M7 renders. Check NEW scenery strictly in a separate native render.
                Renderer[] hidden = Object.FindObjectsByType<Renderer>().Where(r=>r.enabled && !IsNewArt(r)).ToArray();
                try
                {
                    foreach (Renderer renderer in hidden) renderer.enabled = false;
                    camera.Render(); presentation.ProcessingCamera.Render();
                    Save(presentation.ResolvedTarget,Path.Combine(output,$"route-{position.x}-new-art.png"),true);
                }
                finally { foreach (Renderer renderer in hidden) renderer.enabled = true; }
            }
        }

        private static bool IsNewArt(Renderer renderer)
        {
            if (!(renderer is SpriteRenderer sprite) || sprite.sprite == null) return false;
            string name = sprite.sprite.texture.name;
            return name.StartsWith("reinforced_") || name == "pressure_reclaimer" || name == "exhaust_station" ||
                name == "conduit_rack" || name == "service_column";
        }

        private static void Save(RenderTexture target,string path,bool canonical)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                Color32[] pixels = image.GetPixels32();
                File.WriteAllBytes(path,image.EncodeToPNG());
                Assert.That(pixels.Count(p=>p.r>40 || p.g>40 || p.b>40), Is.GreaterThan(100),path);
                if (canonical) Assert.That(pixels.All(RustlinePalette.IsCanonical), Is.True,"Noncanonical resolved environment: "+path);
            }
            finally { RenderTexture.active = previous; Object.Destroy(image); }
        }
    }
}
