using System;
using System.IO;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class IndustrialBackgroundTests
    {
        private const string Art="Assets/Art/Environment/IndustrialBackground/";
        private const string ScenePath="Assets/Scenes/Demo/SalvageIntake.unity";
        private static readonly string[] Images=Directory.GetFiles(Art,"*.png");

        [TearDown]
        public void CloseReviewScene()
        {
            // Scene inspection must not leave route colliders in later isolated physics fixtures.
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
        }

        [TestCaseSource(nameof(Images))]
        public void BackgroundArt_IsCanonicalNativeAndCompact(string path)
        {
            var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
            try
            {
                image.LoadImage(File.ReadAllBytes(path));
                Assert.That(image.width,Is.EqualTo(512)); Assert.That(image.height,Is.EqualTo(512));
                Assert.That(image.GetPixels32().All(p=>p.a==0 || p.a==255 && RustlinePalette.IsCanonical(p)),Is.True,path);
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                Assert.That(importer.spritePixelsPerUnit,Is.EqualTo(16));
                Assert.That(importer.filterMode,Is.EqualTo(FilterMode.Point));
                Assert.That(importer.mipmapEnabled,Is.False);
                Assert.That(importer.textureCompression,Is.EqualTo(TextureImporterCompression.Uncompressed));
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>())
                    Assert.That(sprite.GetPhysicsShapeCount(),Is.Zero);
                if (!path.Contains("architecture_modules"))
                {
                    for(int i=0;i<512;i++)
                    {
                        Assert.That(image.GetPixel(0,i),Is.EqualTo(image.GetPixel(511,i)),path+" horizontal seam");
                        Assert.That(image.GetPixel(i,0),Is.EqualTo(image.GetPixel(i,511)),path+" vertical seam");
                    }
                }
                if (path.Contains("far_infrastructure")) Assert.That(image.GetPixels32().All(p=>p.a==255),Is.True,"Coverage plane must be opaque.");
            }
            finally { UnityEngine.Object.DestroyImmediate(image); }
        }

        [Test]
        public void Scene_PreservesPhase2AndHasNoOldBackdropTextureDependencies()
        {
            var scene=EditorSceneManager.OpenScene(ScenePath,OpenSceneMode.Single);
            Invoke("RustlineIndustrialBackgroundSetup","ValidateScene",scene);
            Invoke("RustlineVisualRefinementSetup","ValidateScene",scene);
            string[] dependencies=AssetDatabase.GetDependencies(ScenePath,true);
            Assert.That(dependencies.Any(p=>p.EndsWith("far_industrial_silhouette_a.png") || p.EndsWith("vertical_depth_backdrop_a.png")),Is.False);
            Assert.That(GameObject.Find("Facility Architecture - World Anchored").GetComponentInChildren<Tilemap>().GetComponent<Collider2D>(),Is.Null);
        }

        [Test]
        public void Reapplication_PreservesSceneIdsSpriteGuidsAndConfiguration()
        {
            byte[] original=File.ReadAllBytes(ScenePath);
            try
            {
                Invoke("RustlineIndustrialBackgroundSetup","Apply");
                byte[] first=File.ReadAllBytes(ScenePath);
                string meta=File.ReadAllText(Art+"architecture_modules.png.meta");
                Invoke("RustlineIndustrialBackgroundSetup","Apply");
                Assert.That(File.ReadAllBytes(ScenePath),Is.EqualTo(first));
                Assert.That(File.ReadAllText(Art+"architecture_modules.png.meta"),Is.EqualTo(meta));
            }
            finally
            {
                EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
                File.WriteAllBytes(ScenePath,original); AssetDatabase.ImportAsset(ScenePath,ImportAssetOptions.ForceUpdate);
            }
        }

        [Test]
        public void ParallaxDepths_HaveDifferentHorizontalAndVerticalScreenTravel()
        {
            Vector3 camera=new Vector3(10,10,0);
            Vector3 far=PixelSnappedBackgroundPlane2D.PositionForCamera(camera,new Vector2(.94f,.96f),new Vector2(32,32));
            Vector3 mid=PixelSnappedBackgroundPlane2D.PositionForCamera(camera,new Vector2(.80f,.86f),new Vector2(32,32));
            Assert.That(far,Is.EqualTo(new Vector3(9.375f,9.625f,0)));
            Assert.That(mid,Is.EqualTo(new Vector3(8,8.625f,0)));
            Assert.That(camera.x-mid.x,Is.GreaterThan(camera.x-far.x));
            Assert.That(camera.y-mid.y,Is.GreaterThan(camera.y-far.y));
        }

        [Test]
        public void ParallaxMath_CoversTheMaximumViewportAcrossNegativeCoordinatesAndWraps()
        {
            foreach (Vector2 follow in new[] {new Vector2(.94f,.96f),new Vector2(.80f,.86f)})
                for(int x=-2000;x<=4000;x+=7) for(int y=-1000;y<=1200;y+=101)
                {
                    Vector3 camera=new Vector3(x/16f,y/16f,-10);
                    Vector3 position=PixelSnappedBackgroundPlane2D.PositionForCamera(camera,follow,new Vector2(32,32));
                    Assert.That(position.x*16,Is.EqualTo(Mathf.Round(position.x*16)));
                    Assert.That(position.y*16,Is.EqualTo(Mathf.Round(position.y*16)));
                    Assert.That(Mathf.Abs(position.x-camera.x)+33.5f,Is.LessThan(64f));
                    Assert.That(Mathf.Abs(position.y-camera.y)+33.5f,Is.LessThan(64f));
                }
        }

        private static void Invoke(string type,string method,params object[] args) =>
            AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Rustline.Editor."+type)).First(t=>t!=null)
                .GetMethod(method,BindingFlags.Public|BindingFlags.Static).Invoke(null,args);
    }
}
