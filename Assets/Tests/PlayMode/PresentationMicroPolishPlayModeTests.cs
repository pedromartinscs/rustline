using System.Collections;
using System.IO;
using System.Reflection;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class PresentationMicroPolishPlayModeTests
    {
        private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;

        [UnityTest]
        public IEnumerator FinalRenderGraphOutput_DarkWorldAndSurroundMatchCanonicalDeepSpace()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null; yield return null;
            var presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            var camera = presentation.WorldCamera;
            Vector3 originalPosition = camera.transform.position;
            Rect originalRect = presentation.ProcessingCamera.rect;
            var hud = (RenderTexture)typeof(NativePixelPresentation).GetField("_weaponCarouselHudTarget", Private).GetValue(presentation);
            presentation.SetWeaponCarouselHudSource(null);
            try
            {
                // Drive the actual world, logical resolve and final RenderGraph passes at
                // each physical size. Editor batch Game View cannot be resized reliably.
                foreach (var size in new[] { new Vector2Int(640,480), new Vector2Int(1280,720),
                    new Vector2Int(1920,1080), new Vector2Int(2560,1080) })
                foreach (bool enabled in new[] { true, false })
                {
                    NativePixelViewport viewport = NativePixelViewportMath.Calculate(size.x,size.y);
                    typeof(NativePixelPresentation).GetField("_viewport",Private).SetValue(presentation,viewport);
                    Invoke(presentation,"RecreateTargets",viewport.LogicalWidth,viewport.LogicalHeight);
                    Invoke(presentation,"SetLogicalSizeIfChanged",new Vector4(viewport.LogicalWidth,viewport.LogicalHeight,0,0));
                    camera.targetTexture = presentation.WorldTarget;
                    camera.orthographicSize = viewport.LogicalHeight / 32f;
                    // A legitimate camera catch-up configuration exposes all three bands
                    // even at 640x480, where a centered player sees no full-darkness band.
                    camera.transform.position = presentation.PlayerTarget.position + new Vector3(
                        (viewport.LogicalWidth*.5f-96)/16f,(viewport.LogicalHeight*.5f-96)/16f,-10);
                    presentation.SetPenumbraEnabled(enabled);
                    Invoke(presentation,"ApplyPenumbraState");
                    Invoke(presentation,"UpdatePenumbraParameters");
                    var output = new RenderTexture(size.x,size.y,16,RenderTextureFormat.ARGB32,RenderTextureReadWrite.sRGB);
                    output.Create();
                    var pixels = new Texture2D(size.x,size.y,TextureFormat.RGBA32,false,true);
                    RenderTexture previous = RenderTexture.active;
                    try
                    {
                        presentation.ProcessingCamera.targetTexture = output;
                        // Include a constrained driver viewport: the final pass must still
                        // own every physical pixel, including the surrounding corners.
                        presentation.ProcessingCamera.rect = new Rect(.125f,.125f,.75f,.75f);
                        camera.Render(); presentation.ProcessingCamera.Render();
                        RenderTexture.active = output;
                        pixels.ReadPixels(new Rect(0,0,size.x,size.y),0,0,false); pixels.Apply();
                        string label = $"{size.x}x{size.y}-{(enabled ? "on" : "off")}";
                        Directory.CreateDirectory("Logs/GameplayUxPhase43");
                        File.WriteAllBytes($"Logs/GameplayUxPhase43/presentation-{label}.png",pixels.EncodeToPNG());
                        Color32[] data = pixels.GetPixels32();
                        int worldX = viewport.OutputOffsetX + viewport.OutputWidth - 2;
                        int worldY = viewport.OutputOffsetY + viewport.OutputHeight - 2;
                        Debug.Log($"DEEP_SPACE_OUTPUT {label} format={output.graphicsFormat} darkWorld={data[worldY*size.x+worldX]} corner={data[0]} edge={data[worldY*size.x+viewport.OutputOffsetX]}");
                        if(enabled) AssertDeepSpace(data[worldY*size.x+worldX],label+" full-dark world");
                        // All unobstructed physical surround pixels must match, not just
                        // the configured material color. No zero-black pixels are allowed.
                        int incorrect=0, black=0;
                        for(int y=0;y<size.y;y++) for(int x=0;x<size.x;x++)
                            if(x<viewport.OutputOffsetX || x>=viewport.OutputOffsetX+viewport.OutputWidth ||
                               y<viewport.OutputOffsetY || y>=viewport.OutputOffsetY+viewport.OutputHeight)
                            {
                                Color32 value=data[y*size.x+x];
                                if(Mathf.Abs(value.r-1)>1 || Mathf.Abs(value.g-2)>1 || Mathf.Abs(value.b-11)>1) incorrect++;
                                if(value.r+value.g+value.b==0) black++;
                            }
                        Assert.That(incorrect,Is.Zero,label+" noncanonical surround pixels");
                        Assert.That(black,Is.Zero,label+" black surround pixels");
                    }
                    finally
                    {
                        RenderTexture.active = previous;
                        presentation.ProcessingCamera.targetTexture = null;
                        output.Release(); Object.Destroy(output); Object.Destroy(pixels);
                    }
                }
            }
            finally
            {
                camera.transform.position = originalPosition;
                presentation.ProcessingCamera.rect = originalRect;
                presentation.SetPenumbraEnabled(true);
                presentation.SetWeaponCarouselHudSource(hud);
                Invoke(presentation,"RefreshViewportAndTargets");
            }
        }

        private static void Invoke(NativePixelPresentation target,string method,params object[] args) =>
            typeof(NativePixelPresentation).GetMethod(method,Private).Invoke(target,args);

        private static void AssertDeepSpace(Color32 value,string context)
        {
            // R8G8B8A8 sRGB stores exact byte endpoints; allow one code value for
            // backend encoding rounding, while explicitly rejecting true black.
            Assert.That(value.r+value.g+value.b,Is.GreaterThan(0),context+" is black");
            Assert.That((int)value.r,Is.EqualTo(1).Within(1),context);
            Assert.That((int)value.g,Is.EqualTo(2).Within(1),context);
            Assert.That((int)value.b,Is.EqualTo(11).Within(1),context);
        }
    }
}
