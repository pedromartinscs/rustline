using System;
using System.IO;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>Authors only the title scene. Never rebuilds accepted gameplay geometry or art.</summary>
    public static class RustlineFrontendSetup
    {
        [UnityEditor.MenuItem("Tools/Rustline/Frontend/Rebuild Main Menu")]
        public static void RebuildMainMenu()
        {
            Directory.CreateDirectory("Assets/Scenes/UI");
            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            GameObject root = new GameObject("RUSTLINE - MAIN MENU");
            var world = new GameObject("World Camera - Title Native Pixels", typeof(Camera));
            world.transform.SetParent(root.transform, false);
            world.transform.position = new Vector3(0, 0, -10);
            Camera worldCamera = world.GetComponent<Camera>();
            worldCamera.orthographic = true;
            worldCamera.orthographicSize = 22.5f;
            worldCamera.clearFlags = CameraClearFlags.SolidColor;
            worldCamera.backgroundColor = RustlinePalette.DeepSpace;
            worldCamera.allowHDR = worldCamera.allowMSAA = false;
            worldCamera.depth = -10;
            worldCamera.cullingMask = ~(1 << WeaponCarouselHud2D.RustlineHudLayerIndex);
            worldCamera.GetUniversalAdditionalCameraData().SetRenderer(0);
            world.AddComponent<AudioListener>();
            var driver = new GameObject("Native Pixel Driver Camera", typeof(Camera));
            driver.transform.SetParent(root.transform, false);
            Camera driverCamera = driver.GetComponent<Camera>();
            driverCamera.cullingMask = 0;
            driverCamera.depth = 100;
            driverCamera.allowHDR = driverCamera.allowMSAA = false;
            driverCamera.clearFlags = CameraClearFlags.SolidColor;
            driverCamera.backgroundColor = RustlinePalette.DeepSpace;
            driverCamera.GetUniversalAdditionalCameraData().SetRenderer(NativePixelPresentation.UtilityRendererIndex);
            var anchor = new GameObject("Title Presentation Anchor");
            anchor.transform.SetParent(root.transform, false);
            NativePixelPresentation presentation = root.AddComponent<NativePixelPresentation>();
            var serialized = new SerializedObject(presentation);
            serialized.FindProperty("worldCamera").objectReferenceValue = worldCamera;
            serialized.FindProperty("processingCamera").objectReferenceValue = driverCamera;
            serialized.FindProperty("playerTarget").objectReferenceValue = anchor.transform;
            serialized.FindProperty("penumbraShader").objectReferenceValue = Shader.Find("Hidden/Rustline/PalettePenumbra");
            serialized.FindProperty("presentationShader").objectReferenceValue = Shader.Find("Hidden/Rustline/NativePixelPresent");
            serialized.FindProperty("penumbraEnabled").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            Material unlit = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            AddArt(root.transform, "Original Refinery Silhouette",
                "Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png", Vector2.zero, -30, unlit);
            AddArt(root.transform, "Salvage Handler",
                "Assets/Art/Environment/Machinery/salvage_handler.png", new Vector2(17, -3), -20, unlit);
            AddArt(root.transform, "Service Gantry",
                "Assets/Art/Environment/Machinery/gantry_upright_a.png", new Vector2(-23, 0), -10, unlit);
            AddArt(root.transform, "Overhead Rail",
                "Assets/Art/Environment/Machinery/gantry_overhead_rail.png", new Vector2(0, 20), -5, unlit);
            FrontendController frontend = root.AddComponent<FrontendController>();
            var controller = new SerializedObject(frontend);
            controller.FindProperty("pixelShader").objectReferenceValue = Shader.Find("Rustline/Frontend Pixel Mask");
            controller.ApplyModifiedPropertiesWithoutUndo();
            if (!EditorSceneManager.SaveScene(scene, FrontendController.TitleScene))
                throw new InvalidOperationException("Failed to save MainMenu.");
            AssetDatabase.Refresh();
            RustlineBuildSceneOrder.EnsureCanonicalOrder();
            Validate();
            Debug.Log("RUSTLINE_FRONTEND_SETUP_OK: saved native-pixel MainMenu; production scene order retained.");
        }

        public static void Validate()
        {
            RustlineBuildSceneOrder.ValidateOrThrow();
            Scene scene = SceneManager.GetSceneByPath(FrontendController.TitleScene);
            bool opened = !scene.IsValid() || !scene.isLoaded;
            if (opened) scene = EditorSceneManager.OpenPreviewScene(FrontendController.TitleScene);
            try
            {
                NativePixelPresentation presentation = null;
                FrontendController frontend = null;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    presentation ??= root.GetComponentInChildren<NativePixelPresentation>();
                    frontend ??= root.GetComponentInChildren<FrontendController>();
                }
                if (frontend == null || frontend.PixelShader == null || presentation == null ||
                    presentation.WorldCamera == null || presentation.ProcessingCamera == null ||
                    presentation.PenumbraShader == null || presentation.PresentationShader == null ||
                    presentation.PlayerTarget == null || presentation.PenumbraEnabled)
                    throw new InvalidOperationException("MainMenu saved presentation references are invalid.");
            }
            finally { if (opened) EditorSceneManager.ClosePreviewScene(scene); }
        }

        private static void AddArt(Transform parent, string name, string path, Vector2 position, int order, Material material)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite == null || material == null) throw new InvalidOperationException("Title art missing: " + path);
            var art = new GameObject(name, typeof(SpriteRenderer));
            art.transform.SetParent(parent, false);
            art.transform.localPosition = position;
            SpriteRenderer renderer = art.GetComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = order;
        }
    }
}
