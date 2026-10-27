using System;
using System.Collections;
using System.IO;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;

namespace Rustline.Diagnostics
{
    /// <summary>Explicit production-player UX/input review; never installed by F3 or ordinary play.</summary>
    public sealed class GameplayUxReleaseReview : MonoBehaviour
    {
        private FirstPlayableDirector2D _director;
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--gameplay-ux-review")<0) return;
            Application.runInBackground=true;
            InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;
            if(Keyboard.current==null) InputSystem.AddDevice<Keyboard>();
            if(Mouse.current==null) InputSystem.AddDevice<Mouse>();
            var review=new GameObject("Gameplay UX Release Review");
            DontDestroyOnLoad(review); review.AddComponent<GameplayUxReleaseReview>();
        }
        private void Awake() => Application.logMessageReceived+=OnLog;
        private void OnDestroy() => Application.logMessageReceived-=OnLog;
        private void OnLog(string message,string stack,LogType kind)
        { if(kind==LogType.Error || kind==LogType.Exception || kind==LogType.Assert) Application.Quit(1); }

        private IEnumerator Start()
        {
            Require(!Debug.isDebugBuild,"Expected a non-development Release.");
            yield return null;
            FrontendController.Current.StartGame();
            while(SceneManager.GetActiveScene().path!=FrontendController.PlayScene) yield return null;
            yield return null; yield return null; yield return null;
            _director=FindAnyObjectByType<FirstPlayableDirector2D>();
            var equipment=_director.Player.GetComponent<PlayerWeaponEquipment2D>();
            var hud=FindAnyObjectByType<FirstPlayableHud2D>();
            Require(!hud.DeveloperVisible,"Developer mode must default off.");
            var distant=_director.Encounters[3];
            Vector3 sentryAt=distant.Members[0].transform.position,rangedAt=distant.Members[1].transform.position;
            yield return new WaitForSeconds(.5f);
            Require(!distant.IsActive && Vector3.Distance(distant.Members[0].transform.position,sentryAt)>.2f &&
                Vector3.Distance(distant.Members[1].transform.position,rangedAt)>.2f,"Distant patrols did not move without engagement.");
            yield return Tap(Key.Digit1); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==1,"Digit1 did not equip Latch-9.");
            yield return Tap(Key.Digit0); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==0 && equipment.GetComponent<PlayerWeaponController2D>().WeaponDefinition==null,"Digit0 did not equip Unarmed.");
            yield return Tap(Key.Digit2); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==2,"Digit2 did not equip Longwatch.");
            InputSystem.QueueStateEvent(Mouse.current,new MouseState { scroll=new Vector2(0,120) }); yield return null;
            InputSystem.QueueStateEvent(Mouse.current,new MouseState()); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==0,"Mouse wheel stopped cycling.");
            yield return Tap(Key.Digit2); yield return new WaitForSeconds(.3f);
            yield return Tap(Key.Digit9); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==2,"Unavailable slot changed equipment.");
            Debug.Log("GAMEPLAY_UX_RELEASE_INPUT_OK: real Input System 0/1/2, unavailable 9, wheel and title-to-play.");
            while(_director.ElapsedSeconds<18.5f) yield return null; // Show the settled player HUD after the short tutorial.
            var health=_director.Encounters[0].Members[0];
            var hit=new DamageInfo2D(1,health.transform.position,Vector2.zero,this); health.ApplyDamage(in hit);
            yield return null; yield return null;
            string output=Path.GetFullPath("Logs/GameplayUxPhase4/Release"); Directory.CreateDirectory(output);
            CaptureFrame(output+"/player-hud-off.png");
            yield return Tap(Key.F3);
            Require(hud.DeveloperVisible && _director.Checkpoints[0].Beacon.enabled,"F3 did not enable read-only developer presentation.");
            CaptureFrame(output+"/developer-hud-on.png");
            yield return Tap(Key.F3);
            for(int i=0;i<2;i++) foreach(var member in _director.Encounters[i].Members)
            { var lethal=new DamageInfo2D(1000,member.transform.position,Vector2.zero,this); member.ApplyDamage(in lethal); }
            yield return null;
            var weapon=equipment.GetComponent<PlayerWeaponController2D>();
            var ammo=equipment.GetComponent<PlayerWeaponAmmo2D>(); ammo.ConsumeAcceptedShot(weapon.WeaponDefinition);
            var damage=new DamageInfo2D(30,_director.Player.transform.position,Vector2.zero,this); _director.PlayerHealth.ApplyDamage(in damage);
            var body=_director.Player.GetComponent<Rigidbody2D>();
            _director.Player.transform.position=_director.Checkpoints[0].transform.position;
            body.position=_director.Player.transform.position; body.linearVelocity=Vector2.zero;
            Physics2D.SyncTransforms();
            for(int i=0;i<30;i++) yield return null;
            Require(_director.Progress.CheckpointIndex==0 && hud.FeedbackText=="CHECKPOINT SAVED" &&
                _director.PlayerHealth.CurrentHealth==100 && ammo.GetSnapshot(weapon.WeaponDefinition).Rounds==50,
                "Checkpoint feedback/resources failed.");
            Require(!_director.Checkpoints[0].Beacon.enabled,"Normal gameplay exposed checkpoint bounds.");
            CaptureFrame(output+"/checkpoint-saved.png");
            yield return Tap(Key.F3); CaptureFrame(output+"/checkpoint-developer.png");
            yield return Tap(Key.Escape);
            Require(Time.timeScale==0f && !hud.DeveloperVisible,"Pause did not hide developer presentation.");
            yield return Tap(Key.F3);
            Require(FrontendController.Current.DeveloperHudEnabled,"Modal F3 leaked into gameplay state.");
            CaptureFrame(output+"/paused.png");
            yield return Tap(Key.Escape);
            Require(hud.DeveloperVisible,"Pause lost the prior presentation preference.");
            _director.BeginRetry(); _director.RetryNow(); yield return null;
            Require(_director.Progress.ClearedEncounters==2 && Vector2.Distance(body.position,_director.Checkpoints[0].transform.position)<.15f,
                "Checkpoint retry changed progression/spawn.");
            yield return Tap(Key.Digit1); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==1,"Numeric bindings did not survive retry.");
            _director.RestartRun(); yield return null;
            Require(!hud.DeveloperVisible,"Restart retained developer mode.");
            yield return Tap(Key.Digit2); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==2,"Numeric bindings did not survive restart.");
            File.WriteAllText(output+"/capture.txt","Actual non-development Windows player "+Screen.width+"x"+Screen.height+
                "; world/carousel final presentation rendered to target, then actual player/menu canvases drawn by Unity in their native viewport positions. "+
                "Equivalent offscreen composition; not desktop screenshots. Checkpoint enemies were defeated by diagnostic staging. Normal input and patrol assertions used the production systems.");
            Debug.Log("GAMEPLAY_UX_RELEASE_PASS: numeric/wheel input, living distant patrols, checkpoint feedback/resources, F3/modal/reset and rendered HUD review.");
            Application.Quit(0);
        }

        private static IEnumerator Tap(Key key)
        {
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(key)); yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState()); yield return null; yield return null;
        }
        private static void Require(bool condition,string message)
        { if(!condition) throw new InvalidOperationException("GAMEPLAY_UX_RELEASE_FAIL: "+message); }

        /// <summary>Batch-mode display screenshots are black; render the same world/UI composition explicitly.</summary>
        public static void CaptureFrame(string path)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var presentation=FindAnyObjectByType<NativePixelPresentation>();
            var hud=FindAnyObjectByType<FirstPlayableHud2D>();
            var frontend=FrontendController.Current.View;
            int width=Screen.width,height=Screen.height;
            var world=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) { filterMode=FilterMode.Point };
            var complete=new RenderTexture(width,height,24,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB) { filterMode=FilterMode.Point };
            world.Create(); complete.Create();
            var cameraObject=new GameObject("UX Capture Camera",typeof(Camera));
            Camera camera=cameraObject.GetComponent<Camera>();
            camera.enabled=false;
            camera.transform.position=new Vector3(10000,10000,-10); camera.orthographic=true;
            camera.clearFlags=CameraClearFlags.SolidColor; camera.backgroundColor=RustlinePalette.DeepSpace;
            camera.allowHDR=false; camera.allowMSAA=false; camera.targetTexture=complete;
            var background=new GameObject("Captured presentation",typeof(RectTransform),typeof(Canvas));
            Canvas backdrop=background.GetComponent<Canvas>();
            backdrop.renderMode=RenderMode.ScreenSpaceCamera; backdrop.worldCamera=camera; backdrop.planeDistance=2; backdrop.sortingOrder=900;
            var quadObject=new GameObject("Native presentation pixels",typeof(RectTransform),typeof(CanvasRenderer));
            quadObject.transform.SetParent(background.transform,false);
            var rect=quadObject.GetComponent<RectTransform>(); rect.anchorMin=rect.anchorMax=rect.pivot=new Vector2(0,1);
            var mesh=new Mesh();
            mesh.vertices=new[] {Vector3.zero,new Vector3(width,0),new Vector3(0,-height),new Vector3(width,-height)};
            float topV=SystemInfo.graphicsUVStartsAtTop ? 0f : 1f;
            mesh.uv=new[] {new Vector2(0,topV),new Vector2(1,topV),new Vector2(0,1f-topV),new Vector2(1,1f-topV)};
            mesh.triangles=new[] {0,2,1,1,2,3};
            var material=new Material(Shader.Find("UI/Default")) { mainTexture=world };
            var renderer=quadObject.GetComponent<CanvasRenderer>(); renderer.SetMaterial(material,world); renderer.SetMesh(mesh);
            Canvas[] canvases={hud.Canvas,frontend.Canvas};
            var readback=new Texture2D(width,height,TextureFormat.RGB24,false);
            RenderTexture prior=RenderTexture.active;
            try
            {
                // The standalone batch loop can skip automatic camera rendering. The
                // carousel deliberately disables its camera once cached, so explicitly
                // populate its real target before composing an offscreen review frame.
                Camera carousel=GameObject.Find("Weapon Carousel HUD Camera")?.GetComponent<Camera>();
                Require(carousel!=null,"Missing production carousel camera during capture.");
                carousel.Render();
                presentation.ProcessingCamera.targetTexture=world;
                presentation.WorldCamera.Render(); presentation.ProcessingCamera.Render();
                foreach(Canvas canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceCamera; canvas.worldCamera=camera; canvas.planeDistance=1; }
                Canvas.ForceUpdateCanvases(); camera.Render();
                RenderTexture.active=complete; readback.ReadPixels(new Rect(0,0,width,height),0,0); readback.Apply();
                if(Path.GetFileName(path)=="player-hud-off.png")
                {
                    int white=0;
                    for(int y=0;y<height/3;y++) for(int x=0;x<Mathf.Min(540,width);x++)
                        if(((Color32)readback.GetPixel(x,y)).Equals(RustlinePalette.GetColor(24))) white++;
                    Require(white>100,"Captured gameplay frame is missing the actual carousel resource pixels.");
                }
                File.WriteAllBytes(path,readback.EncodeToPNG());
            }
            finally
            {
                RenderTexture.active=prior;
                foreach(Canvas canvas in canvases) { canvas.renderMode=RenderMode.ScreenSpaceOverlay; canvas.worldCamera=null; }
                presentation.ProcessingCamera.targetTexture=null; camera.targetTexture=null;
                world.Release(); complete.Release(); Destroy(world); Destroy(complete); Destroy(readback);
                Destroy(material); Destroy(mesh); Destroy(background); Destroy(cameraObject);
            }
        }
    }
}
