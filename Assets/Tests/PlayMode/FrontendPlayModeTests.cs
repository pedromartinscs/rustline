using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
using Object = UnityEngine.Object;

namespace Rustline.Tests
{
    public sealed class FrontendPlayModeTests : InputTestFixture
    {
        private string _oldMusic, _oldSfx;
        private bool _hadMusic, _hadSfx;
        private Keyboard _keyboard;
        private Mouse _mouse;
        private readonly List<GameObject> _fixtures = new List<GameObject>();
        private FrontendController Menu => FrontendController.Current;

        public override void Setup()
        {
            base.Setup();
            _hadMusic = PlayerPrefs.HasKey(RustlineAudioSettings.MusicKey);
            _hadSfx = PlayerPrefs.HasKey(RustlineAudioSettings.SfxKey);
            _oldMusic = PlayerPrefs.GetString(RustlineAudioSettings.MusicKey, "100");
            _oldSfx = PlayerPrefs.GetString(RustlineAudioSettings.SfxKey, "100");
            _keyboard = InputSystem.AddDevice<Keyboard>();
            _mouse = InputSystem.AddDevice<Mouse>();
        }

        [UnityTearDown]
        public IEnumerator Cleanup()
        {
            if (Menu != null) Menu.PrepareForRunRestart();
            Time.timeScale = 1f; AudioListener.pause = false;
            foreach (GameObject fixture in _fixtures) if (fixture != null) Object.Destroy(fixture);
            _fixtures.Clear();
            // Release scene-owned action maps before InputTestFixture restores its saved
            // input runtime. Otherwise a live reader can bind the following fixture to
            // a disposed test runtime after an asynchronous title/play round trip.
            Scene loaded = SceneManager.GetActiveScene();
            Scene clean = SceneManager.CreateScene("Frontend Test Cleanup");
            SceneManager.SetActiveScene(clean);
            yield return SceneManager.UnloadSceneAsync(loaded);
            if (_hadMusic) PlayerPrefs.SetString(RustlineAudioSettings.MusicKey,_oldMusic); else PlayerPrefs.DeleteKey(RustlineAudioSettings.MusicKey);
            if (_hadSfx) PlayerPrefs.SetString(RustlineAudioSettings.SfxKey,_oldSfx); else PlayerPrefs.DeleteKey(RustlineAudioSettings.SfxKey);
            RustlineAudioSettings.Reload(); RustlineAudioSettings.Save();
            yield return null;
        }

        private IEnumerator Load(string path)
        {
            SceneManager.LoadScene(path);
            yield return null; yield return null; yield return null;
            Assert.That(Menu, Is.Not.Null);
        }
        private IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(key));
            yield return null;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
            yield return null; yield return null;
        }

        [UnityTest]
        public IEnumerator DiagnosticsScene_RetainsGameplayInputAfterLeavingTitle()
        {
            yield return Load(FrontendController.TitleScene);
            SceneManager.LoadScene("MovementLab");
            yield return null; yield return null;
            Assert.That(FrontendController.Current, Is.Null);
            Assert.That(FrontendController.BlockGameplayInput, Is.False);
            var input = Object.FindAnyObjectByType<PlayerInputReader>();
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.D));
            InputSystem.Update();
            Assert.That(input.MoveX,Is.GreaterThan(0.5f),"Lab reader did not receive D. Map=" +
                _keyboard.added + " actions=" + InputSystem.ListEnabledActions().Count);
            var body = input.GetComponent<Rigidbody2D>();
            float x = body.position.x;
            for(int i=0;i<12;i++) yield return new WaitForFixedUpdate();
            Assert.That(body.position.x,Is.GreaterThan(x));
        }

        [UnityTest]
        public IEnumerator Title_KeyboardOptionsCreditsAndFreshRun_RoundTrip()
        {
            yield return Load(FrontendController.TitleScene);
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.Title));
            Assert.That(Menu.View.ControlCount, Is.EqualTo(4));
            yield return Tap(Key.DownArrow); yield return Tap(Key.Enter);
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.TitleOptions));
            int music = RustlineAudioSettings.Music;
            yield return Tap(Key.LeftArrow);
            Assert.That(RustlineAudioSettings.Music, Is.EqualTo(Mathf.Max(0,music-5)));
            yield return Tap(Key.Escape);
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.Title));
            Menu.OpenCredits(); yield return null;
            Assert.That(Menu.View.ControlCount, Is.EqualTo(1));
            yield return Tap(Key.Escape);
            Menu.StartGame(); Menu.StartGame(); // Duplicate click must not issue a second load.
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.Transition));
            yield return WaitForScene(FrontendController.PlayScene);
            var director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            Assert.That(director.Progress.ClearedEncounters, Is.Zero);
            Assert.That(director.Progress.HasCheckpoint, Is.False);
            director.Progress.ClearEncounter(0);
            director.Progress.ClearEncounter(1);
            director.Progress.TryCheckpoint(0,2,true);
            Menu.Back(); Menu.OpenOptions();
            Assert.That(Time.timeScale, Is.Zero);
            yield return Tap(Key.Escape);
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.Paused));
            Menu.QuitToTitle();
            yield return WaitForScene(FrontendController.TitleScene);
            Assert.That(Time.timeScale, Is.EqualTo(1f)); Assert.That(AudioListener.pause, Is.False);
            Assert.That(RustlineAudioSettings.Music, Is.EqualTo(Mathf.Max(0,music-5)));
            Menu.StartGame(); yield return WaitForScene(FrontendController.PlayScene);
            director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            Assert.That(director.Progress.HasCheckpoint, Is.False);
            Assert.That(director.Progress.ClearedEncounters, Is.Zero);
            Assert.That(director.Progress.IsComplete, Is.False);
            Assert.That(Object.FindObjectsByType<FrontendController>(FindObjectsSortMode.None).Length, Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator DirectPlay_PauseFreezesPhysicsEnemyBombProjectileTimers_AndInput()
        {
            yield return Load(FrontendController.PlayScene);
            var director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            var body = director.Player.GetComponent<Rigidbody2D>();
            var weapon = director.Player.GetComponent<PlayerWeaponController2D>();
            var input = director.Player.GetComponent<PlayerInputReader>();
            var enemy = director.Encounters[0].Members[0];
            director.Encounters[0].Activate();
            var probe = new GameObject("Pause Physics Probe", typeof(Rigidbody2D)); _fixtures.Add(probe);
            probe.transform.position = new Vector3(-1000,100,0);
            Rigidbody2D probeBody = probe.GetComponent<Rigidbody2D>();
            probeBody.linearVelocity = Vector2.right;
            var equipment = director.Player.GetComponent<PlayerWeaponEquipment2D>();
            equipment.RequestSlot(1);
            yield return new WaitForSeconds(0.25f);
            var bombMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            var projectileObject = new GameObject("Pause Latch Probe"); _fixtures.Add(projectileObject);
            var projectile = projectileObject.AddComponent<Latch9Projectile2D>();
            projectile.Initialize(weapon,weapon.WeaponDefinition,WeaponShotMode2D.Conventional,
                new Vector2(-1000,50),Vector2.up,0,director.Player.GetComponent<Collider2D>(),
                director.Player.transform,null,bombMaterial,0,20);
            var bombardier = Object.FindAnyObjectByType<Bombardier2D>();
            var bombObject = new GameObject("Pause Bomb Probe"); _fixtures.Add(bombObject);
            var bomb = bombObject.AddComponent<BombardierBomb2D>();
            bomb.Launch(bombardier,new Vector2(-1000,80),new Vector2(-990,80),director.PlayerHealth,
                director.Player.GetComponent<CapsuleCollider2D>(),bombMaterial,bombardier.ExplosionFrames);
            yield return new WaitForFixedUpdate();
            yield return Tap(Key.Escape);
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.Paused));
            Assert.That(Time.timeScale, Is.Zero);
            Menu.OpenOptions(); // Space on a slider is navigation, never a gameplay jump.
            Vector2 playerPosition = body.position, enemyPosition = enemy.transform.position;
            Vector2 physicsPosition = probeBody.position, projectilePosition = projectile.Position;
            Vector3 bombPosition = bomb.transform.position;
            float age = bomb.Age, time = director.ElapsedSeconds, scaled = Time.time, unscaled = Time.unscaledTime;
            var pausedAim = director.Player.GetComponent<PlayerAim2D>();
            Vector2 aimDirection = pausedAim.ContinuousAimDirection;
            uint aimRevision = pausedAim.AimRevision;
            int shots = weapon.ShotCount;
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(Key.D,Key.Space,Key.R,Key.F5,Key.Digit2));
            InputSystem.QueueStateEvent(_mouse,new MouseState { position = new Vector2(1000,0) }.WithButton(MouseButton.Left));
            yield return new WaitForSecondsRealtime(0.25f);
            Assert.That(body.position, Is.EqualTo(playerPosition));
            Assert.That((Vector2)enemy.transform.position, Is.EqualTo(enemyPosition));
            Assert.That(probeBody.position, Is.EqualTo(physicsPosition));
            Assert.That(projectile.Position, Is.EqualTo(projectilePosition));
            Assert.That(bomb.transform.position, Is.EqualTo(bombPosition)); Assert.That(bomb.Age, Is.EqualTo(age));
            Assert.That(director.ElapsedSeconds, Is.EqualTo(time)); Assert.That(Time.time, Is.EqualTo(scaled));
            Assert.That(Time.unscaledTime, Is.GreaterThan(unscaled));
            Assert.That(input.MoveX, Is.Zero); Assert.That(input.JumpHeld, Is.False);
            Assert.That(input.ConsumeFirePressed(), Is.False); Assert.That(weapon.ShotCount, Is.EqualTo(shots));
            Assert.That(pausedAim.ContinuousAimDirection, Is.EqualTo(aimDirection));
            Assert.That(pausedAim.AimRevision, Is.EqualTo(aimRevision), "Menu pointer movement must not aim or turn the player.");
            Assert.That(equipment.SelectedSlot, Is.EqualTo(1));
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState());
            InputSystem.QueueStateEvent(_mouse,new MouseState());
            yield return null;
            Menu.OpenOptions(); Menu.AdjustVolume(1,-10); yield return null;
            Assert.That(Menu.Navigation.State, Is.EqualTo(FrontendState.PauseOptions));
            Assert.That(Time.timeScale, Is.Zero);
            yield return Tap(Key.Escape); yield return Tap(Key.Escape);
            Assert.That(Time.timeScale, Is.EqualTo(1));
            yield return new WaitForSeconds(0.1f);
            Assert.That(probeBody.position, Is.Not.EqualTo(physicsPosition));
            Assert.That(director.ElapsedSeconds, Is.GreaterThan(time));
            Assert.That(weapon.ShotCount, Is.EqualTo(shots), "No queued menu click may fire after resume.");
            Object.Destroy(bombMaterial);
        }

        [UnityTest]
        public IEnumerator Pause_RepeatedCyclesDeathRetryRestartCompletionAndExternalSceneCleanup()
        {
            yield return Load(FrontendController.PlayScene);
            var director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            for(int i=0;i<5;i++)
            {
                yield return Tap(Key.Escape); Assert.That(Time.timeScale,Is.Zero);
                yield return Tap(Key.Escape); Assert.That(Time.timeScale,Is.EqualTo(1));
            }
            Menu.Back();
            InputSystem.QueueStateEvent(_mouse,new MouseState().WithButton(MouseButton.Left));
            yield return null;
            float resumedTime = director.ElapsedSeconds;
            Menu.Back();
            yield return new WaitForSeconds(0.1f);
            Assert.That(FrontendController.BlockGameplayInput,Is.True,"Held menu click must stay suppressed.");
            Assert.That(director.ElapsedSeconds,Is.GreaterThan(resumedTime),"Input release barrier must not pause run timers after Resume.");
            InputSystem.QueueStateEvent(_mouse,new MouseState());
            yield return null; yield return null; yield return null;
            director.BeginRetry();
            Menu.Back();
            yield return new WaitForSecondsRealtime(1.1f);
            Assert.That(director.IsRetrying,Is.True);
            Menu.Back(); yield return new WaitForSeconds(1f);
            Assert.That(director.IsRetrying,Is.False);
            Menu.Back(); Menu.OpenOptions(); director.RestartRun();
            Assert.That(Menu.Navigation.State,Is.EqualTo(FrontendState.Playing));
            Assert.That(Time.timeScale,Is.EqualTo(1));
            foreach (var encounter in director.Encounters)
                foreach (var health in encounter.Members)
                {
                    var damage = new DamageInfo2D(1000,health.transform.position,Vector2.zero,null);
                    health.ApplyDamage(in damage);
                }
            for(int i=0;i<director.Encounters.Length;i++) director.Progress.ClearEncounter(i);
            var body=director.Player.GetComponent<Rigidbody2D>();
            body.position=new Vector2(193,29.08f); director.Player.transform.position=body.position;
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.5f);
            Menu.Back(); float extraction=director.Progress.ExtractionTime;
            yield return new WaitForSecondsRealtime(0.3f);
            Assert.That(director.Progress.ExtractionTime,Is.EqualTo(extraction));
            Menu.Back(); yield return new WaitForSeconds(2.2f);
            Assert.That(director.Progress.IsComplete,Is.True);
            Menu.Back(); Menu.OpenOptions();
            yield return Load(FrontendController.TitleScene); // External transition while options is paused.
            Assert.That(Time.timeScale,Is.EqualTo(1)); Assert.That(AudioListener.pause,Is.False);
            Assert.That(Object.FindObjectsByType<FrontendView>(FindObjectsSortMode.None).Length,Is.EqualTo(1));
        }

        [UnityTest]
        public IEnumerator Audio_DefaultsClampsMalformedStorageRealRoutingAndScenePersistence()
        {
            PlayerPrefs.DeleteKey(RustlineAudioSettings.MusicKey); PlayerPrefs.DeleteKey(RustlineAudioSettings.SfxKey);
            RustlineAudioSettings.Reload();
            Assert.That(RustlineAudioSettings.Music,Is.EqualTo(100)); Assert.That(RustlineAudioSettings.Sfx,Is.EqualTo(100));
            var musicObject=new GameObject("Controlled Music Source",typeof(AudioSource),typeof(AudioBusSource));
            var sfxObject=new GameObject("Controlled SFX Source",typeof(AudioSource),typeof(AudioBusSource));
            _fixtures.Add(musicObject); _fixtures.Add(sfxObject);
            var music=musicObject.GetComponent<AudioBusSource>(); var sfx=sfxObject.GetComponent<AudioBusSource>();
            music.Configure(RustlineAudioBus.Music,0.8f); sfx.Configure(RustlineAudioBus.Sfx,0.5f);
            RustlineAudioSettings.Set(RustlineAudioBus.Music,25); RustlineAudioSettings.Set(RustlineAudioBus.Sfx,60);
            Assert.That(music.Source.volume,Is.EqualTo(0.2f).Within(0.001f)); Assert.That(sfx.Source.volume,Is.EqualTo(0.3f).Within(0.001f));
            RustlineAudioSettings.Set(RustlineAudioBus.Music,-10); Assert.That(music.Source.volume,Is.Zero);
            Assert.That(sfx.Source.volume,Is.EqualTo(0.3f).Within(0.001f));
            RustlineAudioSettings.Set(RustlineAudioBus.Music,200); Assert.That(music.Source.volume,Is.EqualTo(0.8f).Within(0.001f));
            RustlineAudioSettings.Save();
            yield return Load(FrontendController.TitleScene);
            Menu.OpenOptions(); Menu.Back(); Menu.OpenOptions();
            Assert.That(RustlineAudioSettings.Sfx,Is.EqualTo(60));
            RustlineAudioSettings.Reload(); Assert.That(RustlineAudioSettings.Sfx,Is.EqualTo(60));
            Menu.Back(); Menu.StartGame(); yield return WaitForScene(FrontendController.PlayScene);
            Assert.That(RustlineAudioSettings.Sfx,Is.EqualTo(60));
            PlayerPrefs.SetString(RustlineAudioSettings.MusicKey,"NaN"); PlayerPrefs.SetString(RustlineAudioSettings.SfxKey,"-20");
            RustlineAudioSettings.Reload();
            Assert.That(RustlineAudioSettings.Music,Is.EqualTo(100)); Assert.That(RustlineAudioSettings.Sfx,Is.EqualTo(100));
        }

        [UnityTest]
        public IEnumerator Presentation_CanonicalPixelsGlyphsFocusMouseHitAndRepresentativeLayouts()
        {
            yield return Load(FrontendController.TitleScene);
            Assert.That(Object.FindAnyObjectByType<NativePixelPresentation>().PenumbraEnabled,Is.False);
            FrontendView view=Menu.View;
            Assert.That(view.MaskAtlas.filterMode,Is.EqualTo(FilterMode.Point));
            Assert.That(view.Canvas.sortingOrder,Is.GreaterThan(1000));
            var cameraObject=new GameObject("Frontend Render Verification",typeof(Camera)); _fixtures.Add(cameraObject);
            Camera camera=cameraObject.GetComponent<Camera>();
            camera.transform.position=new Vector3(10000,10000,-10);
            camera.orthographic=true; camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RustlinePalette.DeepSpace;
            camera.allowMSAA=false; camera.allowHDR=false;
            Canvas canvas=view.Canvas; canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1;
            string output=Path.GetFullPath("Logs/FrontendRenders"); Directory.CreateDirectory(output);
            foreach(Vector2Int size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(320,180),new Vector2Int(128,128)})
            {
                var target=new RenderTexture(size.x,size.y,24,RenderTextureFormat.ARGB32){antiAliasing=1,filterMode=FilterMode.Point};
                target.Create(); camera.targetTexture=target;
                for(int page=0;page<3;page++)
                {
                    if(page==1) Menu.OpenOptions();
                    if(page==2) { Menu.Back(); Menu.OpenCredits(); }
                    view.RefreshViewport(size.x,size.y);
                    Canvas.ForceUpdateCanvases(); camera.Render();
                    RenderTexture prior=RenderTexture.active; RenderTexture.active=target;
                    var image=new Texture2D(size.x,size.y,TextureFormat.RGB24,false);
                    image.ReadPixels(new Rect(0,0,size.x,size.y),0,0); image.Apply(); RenderTexture.active=prior;
                    File.WriteAllBytes(Path.Combine(output,$"frontend-{size.x}x{size.y}-{page}.png"),image.EncodeToPNG());
                    var palette=new HashSet<Color32>(Enumerable.Range(0,RustlinePalette.ColorCount).Select(RustlinePalette.GetColor));
                    Color32[] pixels=image.GetPixels32();
                    Assert.That(pixels.All(pixel=>palette.Contains(pixel)),Is.True,"Rendered frontend introduced noncanonical colors.");
                    Assert.That(pixels.Count(pixel=>pixel.Equals(RustlinePalette.GetColor(19))),Is.GreaterThan(10),"Missing cyan UI/focus pixels.");
                    foreach(Vector3 vertex in view.PixelMesh.vertices)
                    {
                        Assert.That(vertex.x,Is.EqualTo(Mathf.Round(vertex.x)).Within(0.0001));
                        Assert.That(vertex.y,Is.EqualTo(Mathf.Round(vertex.y)).Within(0.0001));
                    }
                    Object.Destroy(image);
                }
                Menu.Back(); camera.targetTexture=null; target.Release(); Object.Destroy(target);
            }
            canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null;
            yield return null;
            RectInt button=view.Layout.Controls[1];
            NativePixelViewport viewport=NativePixelViewportMath.Calculate(Screen.width,Screen.height);
            Vector2 position=new Vector2(viewport.OutputOffsetX+button.center.x*viewport.IntegerScale,
                viewport.OutputOffsetY+(viewport.LogicalHeight-button.center.y)*viewport.IntegerScale);
            Assert.That(view.HitTest(position),Is.EqualTo(1));
            InputSystem.QueueStateEvent(_mouse,new MouseState{position=position}); yield return null;
            Assert.That(Menu.Selected,Is.EqualTo(1));
            InputSystem.QueueStateEvent(_mouse,new MouseState{position=position}.WithButton(MouseButton.Left)); yield return null;
            InputSystem.QueueStateEvent(_mouse,new MouseState{position=position}); yield return null; yield return null;
            Assert.That(Menu.Navigation.State,Is.EqualTo(FrontendState.TitleOptions));
        }

        private IEnumerator WaitForScene(string path)
        {
            float deadline=Time.realtimeSinceStartup+20;
            while(SceneManager.GetActiveScene().path!=path && Time.realtimeSinceStartup<deadline) yield return null;
            Assert.That(SceneManager.GetActiveScene().path,Is.EqualTo(path));
            yield return null; yield return null; yield return null;
        }
    }
}
