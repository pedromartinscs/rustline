using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class EnvironmentVisualHardeningTests
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string Art = "Assets/Art/Environment/Hardening/";
        private static readonly string[] Assets = Directory.GetFiles(Art,"*.png")
            .Concat(Directory.GetFiles("Assets/Art/Environment/TransferBay/", "*.png")).ToArray();

        [TestCaseSource(nameof(Assets))]
        public void ProductionArt_HasExactCanonicalPixelsAndNativeImport(string path)
        {
            var texture = new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                texture.LoadImage(File.ReadAllBytes(path));
                foreach (Color32 pixel in texture.GetPixels32())
                {
                    Assert.That(pixel.a == 0 || pixel.a == 255, Is.True, path);
                    if (pixel.a != 0) Assert.That(RustlinePalette.IsCanonical(pixel), Is.True, path);
                }
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit, Is.EqualTo(16f));
                Assert.That(importer.filterMode, Is.EqualTo(FilterMode.Point));
                Assert.That(importer.mipmapEnabled || importer.crunchedCompression, Is.False);
                Assert.That(importer.textureCompression, Is.EqualTo(TextureImporterCompression.Uncompressed));
                Assert.That(importer.npotScale, Is.EqualTo(TextureImporterNPOTScale.None));
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    Assert.That(sprite.GetPhysicsShapeCount(), Is.Zero);
            }
            finally { UnityEngine.Object.DestroyImmediate(texture); }
        }

        [Test]
        public void SavedScene_ReinforcedTraversalAndDecorationRetainAcceptedCollision()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Invoke("RustlineEnvironmentVisualHardeningSetup","ValidateScene",scene);
            AssertBaselineCollision(scene);
            Assert.That(SceneRenderers(scene).Count(r=>r.sprite != null && r.sprite.texture.name == "salvage_handler"), Is.EqualTo(1));
        }

        [TestCase("Service Shaft Wall - Right B",32f,0f,2.25f,17f)]
        [TestCase("Maintenance Shaft - Left",119.9375f,21f,2.0625f,10f)]
        [TestCase("Maintenance Shaft - Right",126f,17f,2.25f,12f)]
        [TestCase("Rail Recovery - Left",141.9375f,21f,2.0625f,8f)]
        [TestCase("Rail Recovery - Right",148f,21f,2.25f,8f)]
        [TestCase("Terminal Solid End Stop",195f,29f,1f,6f)]
        [TestCase("Maintenance Solid Ceiling",102f,19.625f,6f,1f)]
        [TestCase("Catwalk Skin - Left",4f,4f,6f,1f)]
        [TestCase("Catwalk Skin - Right",10f,4f,6f,1f)]
        [TestCase("Lower Bay Structural Beam",1f,-2f,18f,2f)]
        [TestCase("Safety Floor Structural Beam",-28f,-4f,62f,2f)]
        public void SolidSkins_CoverCompleteFacesAtNativeScale(string name,float x,float y,float width,float height)
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            SpriteRenderer renderer = SceneRenderers(scene).Single(r=>r.name == name);
            Assert.That(renderer.drawMode, Is.EqualTo(SpriteDrawMode.Tiled));
            Assert.That(renderer.tileMode, Is.EqualTo(SpriteTileMode.Continuous));
            Assert.That(renderer.transform.lossyScale, Is.EqualTo(Vector3.one));
            Assert.That(Vector2.Distance(renderer.bounds.min,new Vector2(x,y)), Is.LessThan(0.001f));
            Assert.That(Vector2.Distance(renderer.bounds.size,new Vector2(width,height)), Is.LessThan(0.001f));
            Assert.That(renderer.sprite.texture.name, Does.StartWith("reinforced_"));
            Assert.That(renderer.GetComponent<Collider2D>(), Is.Null);
        }

        [Test]
        public void RestoredBeamFloors_RetainNativeHorizontalArtworkAndShaftMaterial()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var floors = SceneRenderers(scene).Where(r=>r.sprite != null &&
                r.sprite.texture.name.StartsWith("reinforced_floor_ibeam_")).ToArray();
            Assert.That(floors.Length, Is.EqualTo(5));
            Assert.That(floors.All(r=>r.transform.rotation == Quaternion.identity), Is.True);
            Assert.That(GameObject.Find("Service Shaft Wall - Right B").GetComponent<SpriteRenderer>().sprite.texture.name,
                Is.EqualTo("reinforced_shaft_b"));
        }

        [Test]
        public void ForegroundMaterialLanguage_OnlyIndustrialTilesAreBreachable_NoSmallPlatesOrDamagedOverlays()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var map = GameObject.Find("Industrial Surface - Visual").GetComponent<Tilemap>();
            int count = 0;
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin)
                if (map.HasTile(cell))
                {
                    Assert.That(map.GetSprite(cell).texture.name, Is.EqualTo("industrial_surface"),cell.ToString());
                    count++;
                }
            Assert.That(count, Is.GreaterThan(100),"Retain actual industrial terrain and cover.");
            Assert.That(GameObject.Find("Maintenance Crouch Visual").GetComponent<Tilemap>().GetUsedTilesCount(), Is.Zero);
            Assert.That(GameObject.Find("Expansion Environment - Managed").GetComponentsInChildren<SpriteRenderer>().Length, Is.EqualTo(14));
            Assert.That(SceneRenderers(scene).Any(r=>r.sprite != null && r.sprite.texture.name.StartsWith("reinforced_plate_")), Is.False);
        }

        [Test]
        public void ProductionValidator_RejectsNonIndustrialDestructibleMaterial()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            try
            {
                GameObject.Find("Industrial Surface - Visual").GetComponent<Tilemap>().SetTile(new Vector3Int(60,16,0),
                    AssetDatabase.LoadAssetAtPath<TileBase>(Art+"ReinforcedPlateLeft.asset"));
                var failure = Assert.Throws<TargetInvocationException>(()=>Invoke("RustlineEnvironmentVisualHardeningSetup","ValidateScene",scene));
                Assert.That(failure.InnerException.Message, Does.Contain("Only industrial_surface"));
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test]
        public void VisualRefresh_IsIdempotentWithoutChangingGameplayOrUnrelatedArt()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            var marker = new GameObject("Unrelated M7 authoring fixture");
            marker.transform.SetParent(GameObject.Find("Art Dressing - Preserve").transform);
            try
            {
                string before = CollisionSignature(scene);
                string gameplay = EditorJsonUtility.ToJson(GameObject.Find("First Playable - Managed").GetComponent<MonoBehaviour>());
                for (int i=0;i<2;i++)
                {
                    Invoke("RustlineSalvageIntakeExpansionSetup","RefreshDressing",scene);
                    Invoke("RustlineEnvironmentVisualHardeningSetup","ApplyVisuals",scene);
                    Invoke("RustlineEnvironmentVisualHardeningSetup","ValidateScene",scene);
                    Assert.That(CollisionSignature(scene), Is.EqualTo(before));
                    Assert.That(marker != null, Is.True);
                }
                Assert.That(SceneRenderers(scene).Count(r=>r.name.StartsWith("Service framework ")), Is.EqualTo(7));
                Assert.That(EditorJsonUtility.ToJson(GameObject.Find("First Playable - Managed").GetComponent<MonoBehaviour>()), Is.EqualTo(gameplay));
            }
            finally { EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single); }
        }

        [Test]
        public void ProductionRegeneration_ReappliesArtAndPreservesCollisionFootprint()
        {
            byte[] saved = File.ReadAllBytes(ScenePath);
            try
            {
                Invoke("RustlineProductionTools","ApplyProductionSetup");
                Scene scene = SceneManager.GetActiveScene();
                Invoke("RustlineEnvironmentVisualHardeningSetup","ValidateScene",scene);
                Invoke("RustlineVisualRefinementSetup","ValidateScene",scene);
                Invoke("RustlineIndustrialBackgroundSetup","ValidateScene",scene);
                AssertBaselineCollision(scene);
            }
            finally
            {
                // Production generation is exercised, not left as an incidental authored diff.
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                File.WriteAllBytes(ScenePath,saved);
                AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
            }
        }

        private static void AssertBaselineCollision(Scene scene)
        {
            var maps = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Tilemap>()).ToArray();
            Assert.That(MapHash(maps.Single(m=>m.name=="Ground Collision - Hidden")),
                Is.EqualTo("682e2f083f1f563383aa4aa3b547519e7f265166d8517fbbe44f6e01354e568e"));
            Assert.That(MapHash(maps.Single(m=>m.name=="Maintenance Crouch Collision")),
                Is.EqualTo("328ffd3ad0ddf78bb8d21da54bfdff61d2201acd2161c64b09bd43fbfd5396be"));
        }
        private static string CollisionSignature(Scene scene) => string.Join("|",scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<Tilemap>()).Where(m=>m.GetComponent<Collider2D>()!=null)
            .OrderBy(m=>m.name).Select(m=>m.name+MapHash(m)+m.transform.position));
        private static string MapHash(Tilemap map)
        {
            var occupied = new System.Collections.Generic.List<Vector3Int>();
            foreach (Vector3Int cell in map.cellBounds.allPositionsWithin) if (map.HasTile(cell)) occupied.Add(cell);
            var cells = occupied.OrderBy(p=>p.x).ThenBy(p=>p.y).ThenBy(p=>p.z);
            string input = string.Concat(cells.Select(p=>$"{p.x},{p.y},{p.z};"));
            using (SHA256 hash = SHA256.Create()) return string.Concat(hash.ComputeHash(Encoding.UTF8.GetBytes(input)).Select(b=>b.ToString("x2")));
        }
        private static SpriteRenderer[] SceneRenderers(Scene scene) => scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
        private static void Invoke(string type,string method,params object[] arguments) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Rustline.Editor."+type)).First(t=>t!=null)
                .GetMethod(method,BindingFlags.Public|BindingFlags.Static).Invoke(null,arguments);
    }
}
