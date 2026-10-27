using System;
using System.Collections;
using System.IO;
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
    /// <summary>Repeatable real URP captures, including the unchanged palette penumbra.</summary>
    public sealed class VisualDirectionSlicePlayModeTests
    {
        [UnityTest]
        public IEnumerator TransferBay_CaptureEquivalentNativePixelViews()
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
            string phase = Environment.GetEnvironmentVariable("RUSTLINE_VISUAL_PHASE") ?? "after";
            string folder = Path.GetFullPath("Logs/VisualDirectionPhase1/" + phase);
            Directory.CreateDirectory(folder);
            var probe = new RenderTexture(Mathf.Max(Screen.width, 1), Mathf.Max(Screen.height, 1), 16,
                RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            probe.Create();
            presentation.ProcessingCamera.targetTexture = probe;
            try
            {
                var positions = new[] { new Vector2(-14f, 1f), new Vector2(8f, 0f) };
                for (int view = 0; view < positions.Length; view++)
                {
                    director.Player.transform.position = positions[view];
                    presentation.WorldCamera.transform.position = new Vector3(positions[view].x, 8f, -10f);
                    for (int frame = 0; frame < 4; frame++) yield return null;
                    Save(presentation.WorldTarget, Path.Combine(folder, "view-" + view + "-world.png"));
                    Save(presentation.ResolvedTarget, Path.Combine(folder, "view-" + view + "-resolved.png"));
                }
                File.WriteAllText(Path.Combine(folder, "capture.txt"),
                    "Actual Unity URP world and palette-resolved targets. Actors held; encounters dormant. " +
                    "Camera centers (-14,8) and (8,8); player roots (-14,1) and (8,0). " +
                    "Logical target: " + presentation.WorldTarget.width + "x" + presentation.WorldTarget.height +
                    ". HUD is ScreenSpaceOverlay and is not included. These are offscreen renders, not window screenshots.");
            }
            finally
            {
                presentation.ProcessingCamera.targetTexture = null;
                probe.Release();
                Object.Destroy(probe);
            }
        }

        private static void Save(RenderTexture target, string path)
        {
            RenderTexture previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0);
                pixels.Apply();
                int visible = 0;
                foreach (Color32 pixel in pixels.GetPixels32())
                    if (pixel.r > 40 || pixel.g > 40 || pixel.b > 40) visible++;
                Assert.That(visible, Is.GreaterThan(500), "Blank capture: " + path);
                File.WriteAllBytes(path, pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(pixels); }
        }
    }
}
