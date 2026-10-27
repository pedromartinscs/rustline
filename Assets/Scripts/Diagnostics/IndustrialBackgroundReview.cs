using System;
using System.Collections;
using System.IO;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Diagnostics
{
    /// <summary>Explicit opt-in Release visual review. Never installed in ordinary gameplay.</summary>
    public sealed class IndustrialBackgroundReview : MonoBehaviour
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"--industrial-background-review")<0) return;
            var go=new GameObject("Industrial Background Release Review");
            DontDestroyOnLoad(go); go.AddComponent<IndustrialBackgroundReview>();
        }

        private IEnumerator Start()
        {
            Application.runInBackground=true;
            yield return SceneManager.LoadSceneAsync("SalvageIntake"); yield return null;
            var director=FindAnyObjectByType<FirstPlayableDirector2D>();
            director.enabled=false; director.Player.enabled=false;
            director.Player.GetComponent<PlayerAim2D>().enabled=false;
            director.Player.GetComponent<Rigidbody2D>().simulated=false;
            foreach(var wave in director.Encounters) foreach(var health in wave.Members)
            {
                if(health.TryGetComponent(out Sentry2D sentry)) sentry.enabled=false;
                if(health.TryGetComponent(out Bombardier2D bombardier)) bombardier.enabled=false;
                health.GetComponent<Rigidbody2D>().simulated=false;
            }
            var presentation=FindAnyObjectByType<NativePixelPresentation>();
            for(int i=0;i<4;i++) yield return null;
            if(Debug.isDebugBuild||Screen.width!=1920||Screen.height!=1080||presentation.WorldTarget.width!=1072||presentation.WorldTarget.height!=1072)
            {
                Debug.LogError("BACKGROUND_RELEASE_REVIEW_FAIL: expected normal Release 1920x1080 window / 1072x1072 logical target; got "+Screen.width+"x"+Screen.height);
                Application.Quit(1); yield break;
            }
            string output=Path.GetFullPath("Logs/IndustrialBackground/Release"); Directory.CreateDirectory(output);
            var physical=new RenderTexture(Screen.width,Screen.height,16,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
            physical.Create(); presentation.ProcessingCamera.targetTexture=physical;
            Vector2[] views={new Vector2(-20,1),new Vector2(8,1),new Vector2(30,8),new Vector2(62,17),new Vector2(124,25),new Vector2(146,24),new Vector2(188,29)};
            string[] names={"west","transfer","shaft","gallery","maintenance","rail","extraction"};
            for(int view=0;view<views.Length;view++)
            {
                director.Player.transform.position=views[view];
                for(int frame=0;frame<50;frame++) yield return null; // Real camera follows and settles.
                presentation.WorldCamera.Render(); presentation.ProcessingCamera.Render();
                Save(presentation.ResolvedTarget,output+"/"+names[view]+"-logical.png");
                Save(physical,output+"/"+names[view]+"-presented.png");
            }
            File.WriteAllText(output+"/capture.txt","Normal Windows Release; actual window "+Screen.width+"x"+Screen.height+
                "; logical "+presentation.WorldTarget.width+"x"+presentation.WorldTarget.height+
                ". Real PixelCameraFollow2D, held actors, dormant encounters. Logical resolved and final native-presentation pass output captured directly; overlay status HUD excluded. Not desktop screenshots.");
            presentation.ProcessingCamera.targetTexture=null;
            physical.Release(); Destroy(physical);
            Debug.Log("BACKGROUND_RELEASE_REVIEW_PASS: normal 1920x1080 Release / 1072x1072 logical presentation; seven views captured with actual camera follow.");
            Application.Quit(0);
        }

        private static void Save(RenderTexture target,string path)
        {
            RenderTexture previous=RenderTexture.active;
            var image=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try { RenderTexture.active=target; image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply(); File.WriteAllBytes(path,image.EncodeToPNG()); }
            finally { RenderTexture.active=previous; Destroy(image); }
        }
    }
}
