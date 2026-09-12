using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>
    /// Keeps Rustline's production scene as Build Index 0 while retaining diagnostic scenes
    /// immediately after it. This is intentionally enforced whenever Unity's build-scene list
    /// changes so deterministic scene builders cannot accidentally restore a lab scene as the
    /// release entry point.
    /// </summary>
    [InitializeOnLoad]
    public static class RustlineBuildSceneOrder
    {
        private const string SalvageIntakePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string MovementLabPath = "Assets/Scenes/MovementLab.unity";
        private const string ArtShowcasePath = "Assets/Scenes/ArtShowcase.unity";

        private static readonly string[] CanonicalLeadingScenes =
        {
            SalvageIntakePath,
            MovementLabPath,
            ArtShowcasePath
        };

        private static bool _isApplying;

        static RustlineBuildSceneOrder()
        {
            EditorBuildSettings.sceneListChanged += EnsureCanonicalOrder;
            EditorApplication.delayCall += EnsureCanonicalOrder;
        }

        [MenuItem("Tools/Rustline/Apply Canonical Build Scene Order")]
        public static void ApplyFromMenu()
        {
            EnsureCanonicalOrder();
            ValidateOrThrow();
            EditorUtility.DisplayDialog(
                "Rustline Build Scenes",
                "Canonical build order applied: SalvageIntake (0), MovementLab (1), ArtShowcase (2).",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Canonical Build Scene Order")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow();
            EditorUtility.DisplayDialog(
                "Rustline Build Scenes",
                "Canonical build-scene order is valid. Release starts in SalvageIntake.",
                "OK");
        }

        public static void EnsureCanonicalOrder()
        {
            if (_isApplying)
            {
                return;
            }

            for (int i = 0; i < CanonicalLeadingScenes.Length; i++)
            {
                if (AssetDatabase.LoadAssetAtPath<SceneAsset>(CanonicalLeadingScenes[i]) == null)
                {
                    Debug.LogError(
                        "Rustline build-scene contract cannot be applied because a required scene is missing: " +
                        CanonicalLeadingScenes[i]);
                    return;
                }
            }

            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            var desired = new List<EditorBuildSettingsScene>(current.Length + CanonicalLeadingScenes.Length);

            foreach (string path in CanonicalLeadingScenes)
            {
                desired.Add(new EditorBuildSettingsScene(path, true));
            }

            desired.AddRange(current.Where(scene =>
                !CanonicalLeadingScenes.Contains(scene.path, StringComparer.Ordinal)));

            bool alreadyCanonical = current.Length == desired.Count;
            if (alreadyCanonical)
            {
                for (int i = 0; i < current.Length; i++)
                {
                    if (current[i].path != desired[i].path || current[i].enabled != desired[i].enabled)
                    {
                        alreadyCanonical = false;
                        break;
                    }
                }
            }

            if (alreadyCanonical)
            {
                return;
            }

            try
            {
                _isApplying = true;
                EditorBuildSettings.scenes = desired.ToArray();
            }
            finally
            {
                _isApplying = false;
            }
        }

        public static void ValidateOrThrow()
        {
            EditorBuildSettingsScene[] scenes = EditorBuildSettings.scenes;
            if (scenes.Length < CanonicalLeadingScenes.Length)
            {
                throw new InvalidOperationException(
                    "Rustline build settings do not contain the required production and diagnostic scenes.");
            }

            for (int i = 0; i < CanonicalLeadingScenes.Length; i++)
            {
                if (!scenes[i].enabled || scenes[i].path != CanonicalLeadingScenes[i])
                {
                    throw new InvalidOperationException(
                        "Rustline build-scene order is invalid. Expected index " + i + " to be enabled scene " +
                        CanonicalLeadingScenes[i] + ".");
                }
            }
        }
    }
}
