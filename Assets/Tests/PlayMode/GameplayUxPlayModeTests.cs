using System.Collections;
using System.Linq;
using System.IO;
using Rustline.Diagnostics;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class GameplayUxPlayModeTests : InputTestFixture
    {
        private Keyboard _keyboard;
        private Mouse _mouse;
        private FirstPlayableDirector2D _director;
        private PlayerWeaponEquipment2D Equipment => _director.Player.GetComponent<PlayerWeaponEquipment2D>();

        public override void Setup()
        {
            base.Setup();
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            FrontendController.Current?.PrepareForRunRestart();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            Scene loaded = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("Gameplay UX Test Cleanup");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(loaded);
            yield return null;
        }

        private IEnumerator Load()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null; yield return null; yield return null;
            _director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            Assert.That(FrontendController.BlockGameplayInput, Is.False);
        }

        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard, new KeyboardState());
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator NumericKeys_ResolveRealActionsAndEquipProductionLoadout()
        {
            yield return Load();
            Assert.That(Equipment.SelectedSlot, Is.EqualTo(2));
            var reader=_director.Player.GetComponent<PlayerInputReader>();
            var runtime=(InputActionAsset)typeof(PlayerInputReader).GetField("_runtimeActions",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(reader);
            Key[] digits={Key.Digit0,Key.Digit1,Key.Digit2,Key.Digit3,Key.Digit4,Key.Digit5,Key.Digit6,Key.Digit7,Key.Digit8,Key.Digit9};
            for(int slot=0;slot<digits.Length;slot++)
            {
                // Unity also auto-enables its project-wide definition asset. Inspect the
                // scene reader's owned clone, rather than conflating these two assets.
                var action=runtime.FindAction("Player/WeaponSlot"+slot,true);
                Assert.That(action.controls.Contains(_keyboard[digits[slot]]),Is.True,"Unresolved production digit binding "+slot);
            }
            yield return Tap(Key.Digit1);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Equipment.SelectedSlot, Is.EqualTo(1), "Actual Digit1 Input System event did not equip Latch-9.");
            yield return Tap(Key.Digit0);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Equipment.SelectedSlot, Is.Zero);
            Assert.That(Equipment.GetComponent<PlayerWeaponController2D>().WeaponDefinition, Is.Null);
            yield return Tap(Key.Digit2);
            yield return new WaitForSecondsRealtime(0.4f);
            Assert.That(Equipment.SelectedSlot, Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator NumpadKeys_ResolveOwnedActionsAndMixWithDigitsWheelAndTransitions()
        {
            yield return Load();
            var reader=_director.Player.GetComponent<PlayerInputReader>();
            var runtime=(InputActionAsset)typeof(PlayerInputReader).GetField("_runtimeActions",
                System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(reader);
            Key[] keys={Key.Numpad0,Key.Numpad1,Key.Numpad2,Key.Numpad3,Key.Numpad4,Key.Numpad5,Key.Numpad6,Key.Numpad7,Key.Numpad8,Key.Numpad9};
            // Exercise equipment first so the baseline records the human-reported failure.
            yield return Tap(Key.Numpad1); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1),"Actual Numpad1 did not select Latch-9.");
            for(int i=0;i<keys.Length;i++)
                Assert.That(runtime.FindAction("Player/WeaponSlot"+i,true).controls.Contains(_keyboard[keys[i]]),Is.True,"Unresolved physical numpad "+i);
            foreach(Key key in new[] {Key.Numpad0,Key.Numpad2,Key.Digit1,Key.Digit0,Key.Digit2})
            {
                yield return Tap(key); yield return new WaitForSecondsRealtime(.3f);
                int expected=key==Key.Numpad0||key==Key.Digit0 ? 0 : key==Key.Digit1 ? 1 : 2;
                Assert.That(Equipment.SelectedSlot,Is.EqualTo(expected));
            }
            int steps=0; Equipment.StepStarted+=(a,b,d)=>steps++;
            yield return Tap(Key.Numpad1);
            Assert.That(Equipment.IsStepActive,Is.True);
            yield return Tap(Key.Digit0); yield return Tap(Key.Numpad2); yield return Tap(Key.Numpad9);
            yield return new WaitForSecondsRealtime(.6f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2));
            Assert.That(steps,Is.EqualTo(2),"Mixed requests must retain visible committed steps and latest valid target.");
            for(int i=3;i<10;i++) yield return Tap(keys[i]);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2));
            InputSystem.QueueStateEvent(_mouse,new MouseState { scroll=new Vector2(0,120) }); yield return null;
            InputSystem.QueueStateEvent(_mouse,new MouseState()); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
            // NumLock is a separate physical button in KeyboardState, not a simulated OS LED.
            yield return Tap(Key.NumLock);
            yield return Tap(Key.Numpad1); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            yield return Tap(Key.NumLock);
            yield return Tap(Key.Numpad0); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
        }

        [UnityTest]
        public IEnumerator NumpadSelection_SurvivesModalHeldKeysReenableRetryRestartAndMenu()
        {
            yield return Load();
            var reader=_director.Player.GetComponent<PlayerInputReader>();
            int actionCount=InputSystem.ListEnabledActions().Count;
            int steps=0; Equipment.StepStarted+=(a,b,d)=>steps++;
            for(int i=0;i<3;i++) { reader.enabled=false; reader.enabled=true; Equipment.enabled=false; Equipment.enabled=true; }
            Assert.That(InputSystem.ListEnabledActions().Count,Is.EqualTo(actionCount));
            yield return Tap(Key.Numpad1); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1)); Assert.That(steps,Is.EqualTo(1));
            yield return Tap(Key.Escape);
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Numpad2)); yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Numpad2,Key.Escape)); yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Numpad2));
            yield return new WaitForSecondsRealtime(.3f);
            Assert.That(FrontendController.BlockGameplayInput,Is.True,"Existing modal release barrier must hold until the keypad key is released.");
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1),"Held modal input must not replay.");
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState()); yield return null; yield return null;
            Assert.That(FrontendController.BlockGameplayInput,Is.False);
            yield return Tap(Key.Numpad2); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2));
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Numpad1)); InputSystem.Update();
            _director.BeginRetry(); _director.RetryNow();
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState()); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2),"Retry must discard stale keypad edges.");
            yield return Tap(Key.Numpad0); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
            _director.RestartRun(); yield return null;
            yield return Tap(Key.Numpad1); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            SceneManager.LoadScene(FrontendController.TitleScene); yield return null; yield return null;
            yield return Tap(Key.Numpad2);
            FrontendController.Current.StartGame();
            for(int i=0;i<180 && SceneManager.GetActiveScene().path!=FrontendController.PlayScene;i++) yield return null;
            yield return null; yield return null; yield return null;
            _director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            yield return Tap(Key.Numpad0); yield return new WaitForSecondsRealtime(.3f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
        }

        [UnityTest]
        public IEnumerator NumericAndWheel_LatestNumericTargetWinsWithoutSkippingVisibleSteps()
        {
            yield return Load();
            int steps=0; Equipment.StepStarted+=(a,b,d)=>steps++;
            yield return Tap(Key.Digit1);
            Assert.That(Equipment.IsStepActive,Is.True);
            yield return Tap(Key.Digit0);
            yield return Tap(Key.Digit9); // Unavailable keys cannot replace the valid pending target.
            yield return new WaitForSecondsRealtime(0.5f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
            Assert.That(steps,Is.EqualTo(2));
            InputSystem.QueueStateEvent(_mouse,new MouseState { scroll=new Vector2(0,120) });
            yield return null;
            InputSystem.QueueStateEvent(_mouse,new MouseState());
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            yield return Tap(Key.Digit2);
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator Selection_SurvivesReenablePauseRetryRestartAndTitleRoundTrip()
        {
            yield return Load();
            var input=_director.Player.GetComponent<PlayerInputReader>();
            int actions=InputSystem.ListEnabledActions().Count;
            for(int i=0;i<3;i++) { input.enabled=false; input.enabled=true; Equipment.enabled=false; Equipment.enabled=true; }
            Assert.That(InputSystem.ListEnabledActions().Count,Is.EqualTo(actions));
            yield return Tap(Key.Digit1); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            yield return Tap(Key.Escape);
            Assert.That(Time.timeScale,Is.Zero);
            yield return Tap(Key.Digit2); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            yield return Tap(Key.Escape); yield return null;
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1),"Paused selection must not replay after resume.");
            yield return Tap(Key.Digit2); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2));
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.Digit1)); InputSystem.Update();
            _director.BeginRetry(); _director.RetryNow();
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(2),"Retry must discard the unconsumed edge.");
            yield return Tap(Key.Digit1); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
            _director.RestartRun(); yield return null;
            yield return Tap(Key.Digit0); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.Zero);
            SceneManager.LoadScene(FrontendController.TitleScene); yield return null; yield return null;
            FrontendController.Current.StartGame();
            for(int i=0;i<180 && SceneManager.GetActiveScene().path!=FrontendController.PlayScene;i++) yield return null;
            yield return null; yield return null; yield return null;
            _director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            yield return Tap(Key.Digit1); yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator F3_IsPresentationOnlyModalSafeAndResetsOnRunAndSceneRestart()
        {
            yield return Load();
            var hud=Object.FindAnyObjectByType<FirstPlayableHud2D>();
            Assert.That(hud.DeveloperVisible,Is.False);
            Assert.That(_director.Checkpoints.All(c=>!c.Beacon.enabled),Is.True);
            int health=_director.PlayerHealth.CurrentHealth,slot=Equipment.SelectedSlot;
            yield return Tap(Key.F3);
            Assert.That(hud.DeveloperVisible,Is.True);
            foreach(var cp in _director.Checkpoints)
            {
                Assert.That(cp.DebugBoundsVisible,Is.True);
                Assert.That(cp.Beacon.GetPosition(0),Is.EqualTo(new Vector3(cp.Area.xMin,cp.Area.yMin,0)));
                Assert.That(cp.Beacon.GetPosition(2),Is.EqualTo(new Vector3(cp.Area.xMax,cp.Area.yMax,0)));
            }
            Assert.That(_director.PlayerHealth.CurrentHealth,Is.EqualTo(health));
            Assert.That(Equipment.SelectedSlot,Is.EqualTo(slot));
            Assert.That(_director.Progress.ClearedEncounters,Is.Zero);
            yield return Tap(Key.Escape);
            Assert.That(hud.DeveloperVisible,Is.False);
            yield return Tap(Key.F3); // Ignored by the modal, not queued.
            Assert.That(FrontendController.Current.DeveloperHudEnabled,Is.True);
            Assert.That(_director.Checkpoints.All(c=>!c.Beacon.enabled),Is.True);
            Assert.That(FrontendController.Current.View.Canvas.sortingOrder,Is.GreaterThan(hud.Canvas.sortingOrder));
            yield return Tap(Key.Escape);
            Assert.That(hud.DeveloperVisible,Is.True);
            _director.BeginRetry(); _director.RetryNow(); yield return null;
            Assert.That(hud.DeveloperVisible,Is.True,"Retry preserves the presentation preference.");
            _director.RestartRun(); yield return null;
            Assert.That(hud.DeveloperVisible,Is.False);
            yield return Tap(Key.F3);
            SceneManager.LoadScene("SalvageIntake"); yield return null; yield return null;
            Assert.That(FrontendController.Current.DeveloperHudEnabled,Is.False);
        }

        [UnityTest]
        public IEnumerator CheckpointFeedback_ExpiresHoldsDuringPauseAndPreservesResourcesAndSpawn()
        {
            yield return Load();
            var hud=Object.FindAnyObjectByType<FirstPlayableHud2D>();
            for(int i=0;i<2;i++) foreach(var member in _director.Encounters[i].Members)
            { var hit=new DamageInfo2D(1000,member.transform.position,Vector2.zero,null); member.ApplyDamage(in hit); }
            yield return null;
            var weapon=Equipment.GetComponent<PlayerWeaponController2D>();
            var ammo=Equipment.GetComponent<PlayerWeaponAmmo2D>();
            ammo.ConsumeAcceptedShot(weapon.WeaponDefinition);
            var damage=new DamageInfo2D(30,_director.Player.transform.position,Vector2.zero,null);
            _director.PlayerHealth.ApplyDamage(in damage);
            var body=_director.Player.GetComponent<Rigidbody2D>();
            _director.Player.transform.position=_director.Checkpoints[0].transform.position;
            body.position=_director.Player.transform.position; Physics2D.SyncTransforms();
            yield return null; yield return null;
            Assert.That(_director.Progress.CheckpointIndex,Is.Zero);
            Assert.That(hud.FeedbackText,Is.EqualTo("CHECKPOINT SAVED"));
            Assert.That(_director.PlayerHealth.CurrentHealth,Is.EqualTo(100));
            Assert.That(ammo.GetSnapshot(weapon.WeaponDefinition).Rounds,Is.EqualTo(50));
            Assert.That(_director.Checkpoints[0].PresentationState,Is.EqualTo("SAVED"));
            Assert.That(_director.Checkpoints[0].Beacon.enabled,Is.False);
            yield return Tap(Key.Escape); yield return new WaitForSecondsRealtime(3.2f);
            Assert.That(hud.FeedbackText,Is.EqualTo("CHECKPOINT SAVED"));
            yield return Tap(Key.Escape); yield return new WaitForSeconds(3.2f);
            Assert.That(hud.FeedbackText,Is.Empty);
            _director.BeginRetry(); _director.RetryNow();
            Assert.That(body.position,Is.EqualTo((Vector2)_director.Checkpoints[0].transform.position));
            Assert.That(_director.Progress.ClearedEncounters,Is.EqualTo(2));
        }

        [UnityTest]
        public IEnumerator RenderedHud_OffOnPausePreserveCanonicalPixelsNativeLayoutAndCarousel()
        {
            yield return Load();
            var hud=Object.FindAnyObjectByType<FirstPlayableHud2D>();
            string output="Logs/GameplayUxPhase4/Editor";
            int normalVertices=hud.PixelMesh.vertexCount;
            Assert.That(normalVertices,Is.GreaterThan(0));
            Assert.That(hud.MaskAtlas.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(hud.MaskAtlas.GetPixels32().All(p=>p.a==0||p.a==255),Is.True);
            GameplayUxReleaseReview.CaptureFrame(output+"/player-hud-off.png");
            var orientation=new Texture2D(2,2,TextureFormat.RGBA32,false);
            orientation.LoadImage(File.ReadAllBytes(output+"/player-hud-off.png"));
            int carouselWhite=0;
            for(int y=0;y<orientation.height/3;y++) for(int x=0;x<Mathf.Min(540,orientation.width);x++)
                if(((Color32)orientation.GetPixel(x,y)).Equals(RustlinePalette.GetColor(24))) carouselWhite++;
            Object.Destroy(orientation);
            Assert.That(carouselWhite,Is.GreaterThan(100),"The actual carousel must be visible at the bottom of the captured presentation.");
            yield return Tap(Key.F3);
            Assert.That(hud.PixelMesh.vertexCount,Is.GreaterThan(normalVertices));
            GameplayUxReleaseReview.CaptureFrame(output+"/developer-hud-on.png");
            foreach(Vector2Int size in new[] {new Vector2Int(640,480),new Vector2Int(800,600),new Vector2Int(1072,1072)})
            {
                hud.Rebuild(size.x,size.y);
                foreach(Vector3 point in hud.PixelMesh.vertices)
                {
                    Assert.That(point.x,Is.EqualTo(Mathf.Round(point.x)));
                    Assert.That(point.y,Is.EqualTo(Mathf.Round(point.y)));
                    Assert.That(point.x,Is.InRange(0f,(float)size.x));
                    Assert.That(-point.y,Is.InRange(0f,(float)size.y));
                }
                Assert.That(hud.PixelMesh.colors32.All(RustlinePalette.IsCanonical),Is.True);
            }
            yield return Tap(Key.Escape);
            GameplayUxReleaseReview.CaptureFrame(output+"/paused.png");
            foreach(string path in Directory.GetFiles(output,"*.png"))
            {
                var image=new Texture2D(2,2,TextureFormat.RGBA32,false);
                try
                {
                    image.LoadImage(File.ReadAllBytes(path));
                    Color32[] pixels=image.GetPixels32();
                    Assert.That(pixels.All(RustlinePalette.IsCanonical),Is.True,path+" noncanonical composition pixels");
                    Assert.That(pixels.Count(p=>p.r>60||p.g>60||p.b>60),Is.GreaterThan(2000),path+" missing rendered world/UI");
                }
                finally { Object.Destroy(image); }
            }
            Assert.That(Object.FindAnyObjectByType<WeaponCarouselHud2D>().enabled,Is.True);
        }
    }
}
