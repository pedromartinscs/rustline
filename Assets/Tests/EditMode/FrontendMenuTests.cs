using System;
using System.Linq;
using System.Reflection;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class FrontendMenuTests
    {
        [Test]
        public void Navigation_ContextsAndInvalidTransitionsAreExplicit()
        {
            var title = new FrontendStateMachine(true);
            Assert.That(title.Back(), Is.False);
            Assert.That(title.Pause(), Is.False);
            Assert.That(title.OpenOptions(), Is.True);
            Assert.That(title.State, Is.EqualTo(FrontendState.TitleOptions));
            Assert.That(title.BeginTransition(), Is.False);
            Assert.That(title.Back(), Is.True);
            Assert.That(title.OpenCredits(), Is.True);
            Assert.That(title.Back(), Is.True);
            Assert.That(title.BeginTransition(), Is.True);
            Assert.That(title.BeginTransition(), Is.False);
            Assert.That(title.Back(), Is.False);
            var playing = new FrontendStateMachine(false);
            Assert.That(playing.OpenOptions(), Is.False);
            Assert.That(playing.OpenCredits(), Is.False);
            for (int i = 0; i < 20; i++)
            {
                Assert.That(playing.Back(), Is.True);
                Assert.That(playing.IsPaused, Is.True);
                Assert.That(playing.OpenOptions(), Is.True);
                Assert.That(playing.Back(), Is.True);
                Assert.That(playing.State, Is.EqualTo(FrontendState.Paused));
                Assert.That(playing.Back(), Is.True);
                Assert.That(playing.State, Is.EqualTo(FrontendState.Playing));
            }
        }

        [TestCase("", 100)] [TestCase("NaN", 100)] [TestCase("Infinity", 100)]
        [TestCase("-1", 100)] [TestCase("101", 100)] [TestCase("0.5", 100)]
        [TestCase("0", 0)] [TestCase("100", 100)] [TestCase("37", 37)]
        public void Preferences_InvalidStorageDefaultsSafely(string value, int expected) =>
            Assert.That(RustlineAudioSettings.Parse(value), Is.EqualTo(expected));

        [TestCase(800,600)] [TestCase(1280,720)] [TestCase(1920,1080)]
        [TestCase(3840,2160)] [TestCase(320,180)] [TestCase(240,320)] [TestCase(128,128)]
        public void Layout_AllControlsFitCenteredNativeOutput(int width, int height)
        {
            NativePixelViewport viewport = NativePixelViewportMath.Calculate(width,height);
            foreach (FrontendState state in new[] { FrontendState.Title, FrontendState.TitleOptions,
                FrontendState.Credits, FrontendState.Paused, FrontendState.PauseOptions })
            {
                var layout = new FrontendLayout(viewport.LogicalWidth, viewport.LogicalHeight, state);
                Assert.That(layout.Panel.xMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(layout.Panel.yMin, Is.GreaterThanOrEqualTo(0));
                Assert.That(layout.Panel.xMax, Is.LessThanOrEqualTo(viewport.LogicalWidth));
                Assert.That(layout.Panel.yMax, Is.LessThanOrEqualTo(viewport.LogicalHeight));
                for (int i=0;i<layout.Controls.Length;i++)
                {
                    RectInt control=layout.Controls[i];
                    Assert.That(control.xMin, Is.GreaterThan(layout.Panel.xMin));
                    Assert.That(control.xMax, Is.LessThan(layout.Panel.xMax));
                    Assert.That(control.yMin, Is.GreaterThanOrEqualTo(layout.HeaderY + 7 * layout.TitleScale));
                    Assert.That(control.yMax, Is.LessThan(layout.FooterY));
                    if(i>0) Assert.That(control.yMin, Is.GreaterThan(layout.Controls[i-1].yMax));
                }
            }
        }

        [Test]
        public void Font_AllMenuLabelsCreditsAndVolumeDigitsHaveAuthoredGlyphs()
        {
            string all = string.Join("", FrontendView.TitleLabels.Concat(FrontendView.PauseLabels)
                .Concat(FrontendView.OptionLabels).Concat(FrontendView.Credits)) +
                "RUSTLINE PAUSED DEPLOYING ORBITAL SALVAGE / INTAKE 07 AUDIO CONTROL LIVE SETTINGS " +
                "PROJECT CREDITS RUN ON HOLD LEFT RIGHT TO ADJUST ESC BACK SELECT ENTER MOUSE CLICK 0123456789";
            foreach (char glyph in all) Assert.That(WeaponHudBitmapFont.Supports(glyph), Is.True, glyph.ToString());
        }

        [Test]
        public void TitleValidation_PreservesOpenAuthoredSceneAndUnsavedContent()
        {
            var authored = UnityEngine.SceneManagement.SceneManager.GetActiveScene();
            var marker = new GameObject("Unrelated unsaved authoring fixture");
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(marker, authored);
            UnityEngine.SceneManagement.SceneManager.SetActiveScene(authored);
            try
            {
                Type setup = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Rustline.Editor.RustlineFrontendSetup"))
                    .First(t=>t!=null);
                setup.GetMethod("Validate").Invoke(null,null);
                Assert.That(UnityEngine.SceneManagement.SceneManager.GetActiveScene(),Is.EqualTo(authored));
                Assert.That(marker != null,Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(marker);
            }
        }

        [Test]
        public void SavedSceneOrder_AndFoundationBridgeRestoreAfterFailure()
        {
            string[] expected = { FrontendController.TitleScene, FrontendController.PlayScene,
                "Assets/Scenes/MovementLab.unity", "Assets/Scenes/ArtShowcase.unity" };
            for(int i=0;i<expected.Length;i++)
            {
                Assert.That(EditorBuildSettings.scenes[i].path, Is.EqualTo(expected[i]));
                Assert.That(EditorBuildSettings.scenes[i].enabled, Is.True);
                Assert.That(AssetDatabase.LoadAssetAtPath<SceneAsset>(expected[i]), Is.Not.Null);
            }
            Type helper = AppDomain.CurrentDomain.GetAssemblies().Select(a=>a.GetType("Rustline.Editor.RustlineBuildSceneOrder"))
                .First(t=>t!=null);
            MethodInfo bridge = helper.GetMethod("RunWithFoundationValidationOrder");
            Action validate = () =>
            {
                Assert.That(EditorBuildSettings.scenes[0].path, Is.EqualTo(expected[2]));
                Assert.That(EditorBuildSettings.scenes[1].path, Is.EqualTo(expected[3]));
                throw new InvalidOperationException("Intentional bridge failure fixture");
            };
            Assert.Throws<TargetInvocationException>(()=>bridge.Invoke(null,new object[]{validate}));
            Assert.That(EditorBuildSettings.scenes.Take(4).Select(s=>s.path), Is.EqualTo(expected));
            Assert.That(EditorBuildSettings.scenes.Any(s=>s.path=="Assets/Scenes/SampleScene.unity"), Is.True);
        }
    }
}
