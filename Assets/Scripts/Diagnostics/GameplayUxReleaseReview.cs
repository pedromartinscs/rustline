using System;
using System.Collections;
using System.IO;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Environment;
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
            string[] args=Environment.GetCommandLineArgs();
            if(Array.IndexOf(args,"--gameplay-ux-review")<0 && Array.IndexOf(args,"--enemy-void-review")<0) return;
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
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--enemy-void-review")>=0)
            {
                yield return VerifyVoidExtraction();
                Application.Quit(0); yield break;
            }
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
            yield return VerifyNumpad(equipment);
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
            yield return Tap(Key.Numpad1); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==1,"Numpad bindings did not survive restart.");
            yield return Tap(Key.Numpad2); yield return new WaitForSeconds(.3f);
            yield return VerifyGroundSupport(output);
            File.WriteAllText(output+"/capture.txt","Actual non-development Windows player "+Screen.width+"x"+Screen.height+
                "; world/carousel final presentation rendered to target, then actual player/menu canvases drawn by Unity in their native viewport positions. "+
                "Equivalent offscreen composition; not desktop screenshots. Checkpoint enemies were defeated by diagnostic staging. Normal input and patrol assertions used the production systems.");
            Debug.Log("GAMEPLAY_UX_RELEASE_PASS: numeric/numpad/wheel input, living distant patrols, physical support loss/landing, checkpoint feedback/resources, F3/modal/reset and rendered HUD review.");
            Application.Quit(0);
        }

        private IEnumerator VerifyNumpad(PlayerWeaponEquipment2D equipment)
        {
            var reader=equipment.GetComponent<PlayerInputReader>();
            int actions=InputSystem.ListEnabledActions().Count;
            for(int i=0;i<3;i++) { reader.enabled=false; reader.enabled=true; }
            Require(actions==InputSystem.ListEnabledActions().Count,"Reader re-enable duplicated actions.");
            foreach(Key key in new[] {Key.Numpad0,Key.Numpad1,Key.Numpad2})
            {
                yield return Tap(key); yield return new WaitForSeconds(.3f);
                int slot=key==Key.Numpad0 ? 0 : key==Key.Numpad1 ? 1 : 2;
                Require(equipment.SelectedSlot==slot,"Physical numpad binding failed: "+key);
            }
            yield return Tap(Key.Numpad9); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==2,"Unavailable numpad slot changed equipment.");
            yield return Tap(Key.Escape);
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Numpad0)); yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Numpad0,Key.Escape)); yield return null;
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState(Key.Numpad0)); yield return null;
            Require(equipment.SelectedSlot==2,"Held modal numpad input changed equipment.");
            InputSystem.QueueStateEvent(Keyboard.current,new KeyboardState()); yield return null; yield return null;
            Require(equipment.SelectedSlot==2,"Stale numpad input replayed after resume.");
            _director.BeginRetry(); _director.RetryNow(); yield return null;
            yield return Tap(Key.Numpad1); yield return new WaitForSeconds(.3f);
            Require(equipment.SelectedSlot==1,"Numpad input failed after retry.");
            yield return Tap(Key.Digit2); yield return new WaitForSeconds(.3f);
            Debug.Log("GAMEPLAY_UX_RELEASE_NUMPAD_OK: physical Numpad0/1/2 actions, unavailable 9, reader re-enable, modal held-key barrier and retry; synthetic device events, no hardware/NumLock LED claim.");
        }

        private IEnumerator VerifyGroundSupport(string output)
        {
            _director.enabled=false; _director.Player.enabled=false;
            _director.Player.GetComponent<Rigidbody2D>().simulated=false;
            _director.Player.GetComponent<Collider2D>().enabled=false;
            _director.Player.GetComponent<PlayerAim2D>().enabled=false;
            var camera=FindAnyObjectByType<NativePixelPresentation>().WorldCamera;
            camera.GetComponent<PixelCameraFollow2D>().enabled=false;
            foreach(var actor in _director.Encounters[3].Members)
            {
                Vector2 start=actor.transform.position;
                _director.Player.transform.position=start+Vector2.left*5;
                camera.transform.position=new Vector3(start.x,start.y+2,-10);
                for(int i=0;i<3;i++) yield return null;
                start=actor.transform.position; // Patrol continues while the camera settles.
                var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
                foreach(float dx in new[] {-1.5f,-.5f,.5f,1.5f})
                {
                    Vector2 origin=new Vector2(Mathf.Floor(start.x)+dx,start.y+.5f);
                    var hit=Physics2D.Raycast(origin,Vector2.down,2,1<<6);
                    var terrain=hit.collider!=null ? hit.collider.GetComponent<BreachableTilemap2D>() : null;
                    Require(terrain!=null,"Expected real breachable production deck.");
                    var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
                    terrain.ReceiveHit(in info);
                }
                Require(Physics2D.Raycast(start+Vector2.up*.3f,Vector2.down,.7f,1<<6).collider==null,"Breach did not remove current support.");
                var footprint=actor.GetComponentInChildren<WeaponHitbox2D>().GetComponent<Collider2D>().bounds;
                Require(Physics2D.BoxCast(footprint.center,footprint.size,0,Vector2.down,.04f,1<<6).collider==null,
                    "Recoverable-fall fixture retained support under part of the hitbox.");
                for(int i=0;i<110;i++) yield return new WaitForFixedUpdate();
                Require(actor.transform.position.y<start.y-.5f,"Actor hovered after real ground loss.");
                Require(actor.IsAlive,"Recoverable production landing incorrectly killed an enemy.");
                var support=Physics2D.Raycast((Vector2)actor.transform.position+Vector2.up*.1f,Vector2.down,.15f,1<<6);
                Require(support.collider!=null && actor.transform.position.y>=support.point.y,"Actor did not land above real lower terrain.");
                float landed=actor.transform.position.y;
                for(int i=0;i<25;i++) yield return new WaitForFixedUpdate();
                Require(actor.transform.position.y<=landed+.001f,"Patrol restored original height after landing.");
                CaptureFrame(output+"/"+actor.name+"-support.png");
                Debug.Log($"GAMEPLAY_UX_RELEASE_SUPPORT_OK {actor.name} startY={start.y:F3} landedY={landed:F3} Ground={support.collider.name} body={actor.GetComponent<Rigidbody2D>().bodyType}");
            }
        }

        private void HoldReviewPlayer(Vector2 position)
        {
            _director.Player.enabled=false; _director.Player.GetComponent<PlayerAim2D>().enabled=false;
            var body=_director.Player.GetComponent<Rigidbody2D>(); body.simulated=false;
            _director.Player.GetComponent<Collider2D>().enabled=false;
            _director.Player.transform.position=position; body.position=position;
            Physics2D.SyncTransforms();
        }

        private static void ReviewKill(CombatHealth2D actor)
        {
            var hit=new DamageInfo2D(actor.CurrentHealth,actor.transform.position,Vector2.zero,null);
            actor.ApplyDamage(in hit);
        }

        private IEnumerator VerifyVoidExtraction()
        {
            string output=Path.GetFullPath("Logs/GameplayUxPhase42/Release");
            yield return VerifyGroundSupport(output); // Both real lower landings must remain survivable.
            SceneManager.LoadScene(FrontendController.PlayScene);
            yield return null; yield return null; yield return null;
            _director=FindAnyObjectByType<FirstPlayableDirector2D>();
            HoldReviewPlayer(new Vector2(-24,1.02f));
            var future=_director.Encounters[3]; var deaths=new int[future.Members.Length];
            var weapon=_director.Player.GetComponent<PlayerWeaponController2D>().WeaponDefinition;
            for(int member=0;member<future.Members.Length;member++)
            {
                int index=member; var actor=future.Members[member]; actor.Died+=_=>deaths[index]++;
                Vector2 start=actor.transform.position; bool cleared=false;
                for(int depth=0;depth<8 && !cleared;depth++)
                {
                    cleared=true;
                    foreach(float dx in new[] {-1.5f,-.5f,.5f,1.5f})
                    {
                        Vector2 origin=new Vector2(Mathf.Floor(start.x)+dx,start.y+.3f);
                        var hit=Physics2D.Raycast(origin,Vector2.down,80,1<<6);
                        if(hit.collider==null) continue;
                        cleared=false;
                        var terrain=hit.collider.GetComponent<BreachableTilemap2D>();
                        Require(terrain!=null,"Void reproduction must use the real breaching receiver.");
                        var info=new WeaponHitInfo2D(weapon,origin,Vector2.down,hit.point,hit.normal,hit.distance,weapon.Damage,hit.collider);
                        terrain.ReceiveHit(in info);
                    }
                }
                Require(cleared,"Production floor shaft did not open to the void.");
            }
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Require(future.IsDefeated && _director.Progress.ClearedEncounters==0,"Future void deaths skipped progress or remained alive.");
            for(int i=0;i<future.Members.Length;i++)
            {
                var actor=future.Members[i]; Vector2 stopped=actor.GetComponent<Rigidbody2D>().position;
                Require(deaths[i]==1,"Environmental death event was duplicated.");
                yield return new WaitForFixedUpdate();
                Require(actor.GetComponent<Rigidbody2D>().position==stopped,"Terminal void actor kept falling.");
                Debug.Log($"ENEMY_VOID_RELEASE_DEATH_OK {actor.name} rootY={stopped.y:F3} policyTop={_director.EnemyVoidTopHeight:F3} deaths={deaths[i]}");
            }
            for(int i=0;i<2;i++) foreach(var member in _director.Encounters[i].Members) ReviewKill(member);
            yield return null;
            HoldReviewPlayer(_director.Checkpoints[0].transform.position); yield return null;
            Require(_director.Progress.CheckpointIndex==0,"Checkpoint did not commit after the first two encounters.");
            _director.BeginRetry(); _director.RetryNow(); HoldReviewPlayer(_director.Checkpoints[0].transform.position);
            foreach(var actor in future.Members) Require(actor.IsAlive,"Retry failed to restore pending void actors.");
            Require(_director.Progress.ClearedEncounters==2,"Retry lost committed progress.");
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Require(future.IsDefeated && _director.Progress.ClearedEncounters==2,"Persistent void did not reset/death correctly or skipped encounter three.");
            foreach(var member in _director.Encounters[2].Members) ReviewKill(member);
            yield return null;
            Require(_director.Progress.ClearedEncounters==4,"Defeated-prefix progression did not include environmental deaths.");
            for(int i=4;i<7;i++) foreach(var member in _director.Encounters[i].Members) ReviewKill(member);
            yield return null;
            HoldReviewPlayer(new Vector2(193,29.08f)); yield return new WaitForSeconds(2.2f);
            Require(_director.Progress.ClearedEncounters==7 && _director.Progress.IsComplete,"Void deaths still blocked final extraction.");
            CaptureFrame(output+"/void-extraction.png");
            _director.RestartRun(); HoldReviewPlayer(new Vector2(-24,1.02f));
            foreach(var encounter in _director.Encounters) foreach(var actor in encounter.Members)
                Require(actor.IsAlive,"Full restart did not restore an encounter member.");
            for(int i=0;i<200;i++) yield return new WaitForFixedUpdate();
            Require(future.IsDefeated && _director.Progress.ClearedEncounters==0 && deaths[0]==3 && deaths[1]==3,
                "Terminal state or death events did not reset once per life.");
            Debug.Log("ENEMY_VOID_RELEASE_PASS: recoverable falls survived; real breached shafts caused one authoritative death per life; future order, checkpoint retry, seven encounters, extraction and restart passed. Automated staging, not human traversal.");
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
