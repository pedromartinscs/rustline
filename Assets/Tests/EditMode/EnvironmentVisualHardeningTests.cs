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
        private static readonly string[] Assets = Directory.GetFiles(Art,"*.png");

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
