using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Profiling;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Rustline.Tests
{
    public sealed class IndustrialBackgroundPlayModeTests
    {
        private static readonly Vector2[] Views = {
            new Vector2(-20,1), new Vector2(8,1), new Vector2(30,8),
            new Vector2(62,17), new Vector2(124,25), new Vector2(146,24), new Vector2(188,29) };
        private static readonly string[] Names = { "west", "transfer", "shaft", "gallery", "maintenance", "rail", "extraction" };

#if UNITY_EDITOR
        [UnityTest]
        public IEnumerator BackgroundOnly_ComparePhase2AndPhase3RenderingCost()
        {
            const string scenePath="Assets/Scenes/Demo/SalvageIntake.unity";
            byte[] saved=File.ReadAllBytes(scenePath);
            string baseline;
            var start=new System.Diagnostics.ProcessStartInfo("git","show 2a39ec4:"+scenePath)
                { UseShellExecute=false,CreateNoWindow=true,RedirectStandardOutput=true,RedirectStandardError=true };
            using(var process=System.Diagnostics.Process.Start(start))
            {
                baseline=process.StandardOutput.ReadToEnd(); process.WaitForExit();
                Assert.That(process.ExitCode,Is.Zero,"Cannot load Phase 2 performance baseline.");
            }
            var report=new StringBuilder("phase,view,active_background_sprites,active_background_tilemaps,unique_textures,rgba32_source_bytes,unity_texture_runtime_bytes,median_draw_calls,median_batches,median_main_thread_ms\n");
            try
            {
                foreach(bool old in new[] {true,false})
                {
                    if(old) File.WriteAllText(scenePath,baseline); else File.WriteAllBytes(scenePath,saved);
                    UnityEditor.AssetDatabase.ImportAsset(scenePath,UnityEditor.ImportAssetOptions.ForceUpdate);
                    SceneManager.LoadScene("SalvageIntake"); yield return null;
                    var director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
                    director.enabled=false; director.Player.enabled=false;
                    director.Player.GetComponent<Rigidbody2D>().simulated=false;
                    director.Player.GetComponent<PlayerAim2D>().enabled=false;
                    var presentation=Object.FindAnyObjectByType<NativePixelPresentation>();
                    presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled=false;
                    Renderer[] renderers=SceneManager.GetActiveScene().GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<Renderer>(true)).ToArray();
                    foreach(Renderer renderer in renderers) if(!IsBackground(renderer.transform)) renderer.enabled=false;
                    yield return Resources.UnloadUnusedAssets();
                    var sprites=renderers.OfType<SpriteRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy&&r.sprite!=null).ToArray();
                    var maps=renderers.OfType<UnityEngine.Tilemaps.TilemapRenderer>().Where(r=>r.enabled&&r.gameObject.activeInHierarchy).ToArray();
                    var textures=sprites.Select(r=>r.sprite.texture).ToList();
                    foreach(var mapRenderer in maps)
                    {
                        var map=mapRenderer.GetComponent<UnityEngine.Tilemaps.Tilemap>();
                        foreach(Vector3Int cell in map.cellBounds.allPositionsWithin) if(map.HasTile(cell)) textures.Add(map.GetSprite(cell).texture);
                    }
                    Texture2D[] unique=textures.Distinct().ToArray();
                    long bytes=unique.Sum(t=>(long)t.width*t.height*4);
                    long runtimeBytes=unique.Sum(t=>UnityEngine.Profiling.Profiler.GetRuntimeMemorySizeLong(t));
                    using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count"))
                    using(var batches=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Batches Count"))
                    using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread"))
                    {
                        foreach(int view in new[] {1,3,6})
                        {
                            director.Player.transform.position=Views[view];
                            presentation.WorldCamera.transform.position=new Vector3(Views[view].x,Views[view].y+2,-10);
                            for(int i=0;i<16;i++) yield return null;
                            var drawSamples=new long[40]; var batchSamples=new long[40]; var timeSamples=new long[40];
                            for(int i=0;i<40;i++)
                            {
                                yield return null;
                                drawSamples[i]=draws.Valid ? draws.LastValue : -1;
                                batchSamples[i]=batches.Valid ? batches.LastValue : -1;
                                timeSamples[i]=main.Valid ? main.LastValue : -1;
                            }
                            Array.Sort(drawSamples); Array.Sort(batchSamples); Array.Sort(timeSamples);
                            report.AppendLine((old?"phase2":"phase3")+","+Names[view]+","+sprites.Length+","+maps.Length+","+unique.Length+","+bytes+","+runtimeBytes+","+
                                drawSamples[20]+","+batchSamples[20]+","+(timeSamples[20]/1000000.0).ToString("F3",System.Globalization.CultureInfo.InvariantCulture));
                        }
                    }
                }
                Directory.CreateDirectory("Logs/IndustrialBackground");
                File.WriteAllText("Logs/IndustrialBackground/render-cost.csv",report.ToString());
            }
            finally
            {
                File.WriteAllBytes(scenePath,saved);
                UnityEditor.AssetDatabase.ImportAsset(scenePath,UnityEditor.ImportAssetOptions.ForceUpdate);
            }
        }

        private static bool IsBackground(Transform item)
        {
            if(item.name=="Background Structure - Visual") return true;
            for(Transform current=item;current!=null;current=current.parent)
                if(current.name=="Transfer Bay Visual Direction - Phase 1"||current.name=="Industrial Background System - Phase 3"||
                    current.name=="Far Parallax v0 - Managed"||current.name=="Vertical Depth Backdrop v0 - Managed") return true;
            return false;
        }
#endif

        [UnityTest]
        public IEnumerator RealCameraFollow_TraversesHorizontalVerticalAndWrapsWithNativeCoverage()
        {
            SceneManager.LoadScene("SalvageIntake"); yield return null;
            var director=Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            director.enabled=false; director.Player.enabled=false;
            director.Player.GetComponent<Rigidbody2D>().simulated=false;
            director.Player.GetComponent<PlayerAim2D>().enabled=false;
            var presentation=Object.FindAnyObjectByType<NativePixelPresentation>();
            var planes=Object.FindObjectsByType<PixelSnappedBackgroundPlane2D>(FindObjectsSortMode.None);
            Assert.That(planes.Length,Is.EqualTo(2));
            Assert.That(presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled,Is.True);
            Vector3 localPosition=GameObject.Find("Transfer Bay Visual Direction - Phase 1").transform.position;
            Vector2[] route={new Vector2(-24,1),new Vector2(22,1),new Vector2(30,1),new Vector2(30,18),new Vector2(40,17),new Vector2(90,17),
                new Vector2(114,17),new Vector2(124,17),new Vector2(124,29),new Vector2(136,29),new Vector2(146,24),new Vector2(154,29),new Vector2(193,29)};
            int frames=0,wraps=0;
            Vector3[] previous=planes.Select(p=>p.transform.position).ToArray();
            Transform localWall=GameObject.Find("Transfer Bay Visual Direction - Phase 1").transform;
            string output="Logs/IndustrialBackground/motion"; Directory.CreateDirectory(output);
            var probe=new RenderTexture(Mathf.Max(Screen.width,1),Mathf.Max(Screen.height,1),16); probe.Create();
            presentation.ProcessingCamera.targetTexture=probe;
            try
            {
                foreach(Vector2 destination in route)
                {
                    int segmentFrames=0;
                    while(Vector2.Distance(director.Player.transform.position,destination)>0.001f)
                    {
                        director.Player.transform.position=Vector2.MoveTowards(director.Player.transform.position,destination,7f*Time.unscaledDeltaTime);
                        yield return null;
                        Vector3 camera=presentation.WorldCamera.transform.position;
                        for(int i=0;i<planes.Length;i++)
                        {
                            Vector3 position=planes[i].transform.position;
                            Vector3 expected=PixelSnappedBackgroundPlane2D.PositionForCamera(camera,planes[i].CameraFollow,planes[i].RepeatWorldSize);
                            Assert.That(Vector3.Distance(position,expected),Is.LessThan(0.00001f));
                            Assert.That(Mathf.Abs(position.x-camera.x)+33.5f,Is.LessThan(64f));
                            Assert.That(Mathf.Abs(position.y-camera.y)+33.5f,Is.LessThan(64f));
                            if(frames>0 && Mathf.Abs(position.x-previous[i].x)>16f) wraps++;
                            previous[i]=position;
                        }
                        Assert.That(localWall.position,Is.EqualTo(localPosition));
                        bool detail=(destination==new Vector2(30,18) && segmentFrames<180) ||
                            (director.Player.transform.position.x>74 && director.Player.transform.position.x<86);
                        if(frames%60==0 || detail && frames%6==0)
                        {
                            presentation.WorldCamera.Render(); presentation.ProcessingCamera.Render();
                            Save(presentation.ResolvedTarget,output+"/frame-"+frames.ToString("D4")+".png");
                        }
                        frames++; segmentFrames++;
                        Assert.That(segmentFrames,Is.LessThan(2400),"Camera route did not advance.");
                    }
                }
                Assert.That(wraps,Is.GreaterThan(0),"Route must cross at least one parallax period wrap.");
                Vector3 accumulator=Vector3.zero;
                // Measure only the positioning calculation, outside NUnit/logging/render readback.
                PixelSnappedBackgroundPlane2D.PositionForCamera(Vector3.zero,Vector2.one,new Vector2(32,32));
                long before=GC.GetAllocatedBytesForCurrentThread();
                for(int i=0;i<10000;i++) accumulator+=PixelSnappedBackgroundPlane2D.PositionForCamera(new Vector3(i/16f,i/32f,0),new Vector2(.8f,.86f),new Vector2(32,32));
                long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
                Assert.That(allocated,Is.Zero); Assert.That(accumulator.x,Is.GreaterThan(0));
                File.WriteAllText(output+"/validation.txt","Actual PixelCameraFollow2D traversal at 7u/s; frames="+frames+
                    "; observed period wraps="+wraps+"; per-sample native snapping and maximum 1072-square coverage passed; static wall stayed anchored. "+
                    "Position math: 10000 calls / "+allocated+" managed bytes. Actual resolved PNGs every 60 frames, plus every 6 frames during shaft ascent / gallery wrap.");
            }
            finally { presentation.ProcessingCamera.targetTexture=null; probe.Release(); Object.Destroy(probe); }
        }

        [UnityTest]
        public IEnumerator Route_CaptureMatchingNativeViews()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            var director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            director.enabled = false;
            director.Player.enabled = false;
            director.Player.GetComponent<Rigidbody2D>().simulated = false;
            director.Player.GetComponent<PlayerAim2D>().enabled = false;
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            presentation.WorldCamera.GetComponent<PixelCameraFollow2D>().enabled = false;
            string phase = Environment.GetEnvironmentVariable("RUSTLINE_BACKGROUND_PHASE") ?? "after";
            string folder = "Logs/IndustrialBackground/" + phase;
            Directory.CreateDirectory(folder);
            var probe = new RenderTexture(Mathf.Max(Screen.width,1),Mathf.Max(Screen.height,1),16);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            try
            {
                for (int view = 0; view < Views.Length; view++)
                {
                    director.Player.transform.position = Views[view];
                    presentation.WorldCamera.transform.position = new Vector3(Views[view].x,Views[view].y+2,-10);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    presentation.WorldCamera.Render(); presentation.ProcessingCamera.Render();
                    Save(presentation.ResolvedTarget,folder+"/"+Names[view]+".png");
                }
                File.WriteAllText(folder+"/capture.txt", "Actual Unity URP palette-resolved targets; " +
                    presentation.ResolvedTarget.width+"x"+presentation.ResolvedTarget.height+
                    "; held actors, dormant encounters; camera=root+(0,2). Overlay status HUD excluded.");
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release(); Object.Destroy(probe);
            }
        }

        internal static void Save(RenderTexture target, string path)
        {
            RenderTexture previous = RenderTexture.active;
            var image = new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);
            try
            {
                RenderTexture.active = target;
                image.ReadPixels(new Rect(0,0,target.width,target.height),0,0); image.Apply();
                Assert.That(image.GetPixels32().Count(p=>p.r>40||p.g>40||p.b>40),Is.GreaterThan(100),path);
                File.WriteAllBytes(path,image.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(image); }
        }
    }
}
