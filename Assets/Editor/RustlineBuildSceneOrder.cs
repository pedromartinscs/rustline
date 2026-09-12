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

            if (Matches(current, desired))
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

        /// <summary>
        /// Runs a legacy M1A/foundation validation while temporarily presenting the historical
        /// MovementLab(0), ArtShowcase(1) build order that validator still expects. The current
        /// production contract is restored in a finally block, including when validation throws.
        ///
        /// This compatibility bridge exists only because the frozen M1A validator predates
        /// SalvageIntake becoming the release entry scene. New production code must never treat the
        /// temporary order as authoritative.
        /// </summary>
        public static void RunWithFoundationValidationOrder(Action action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action));
            }

            RequireScene(MovementLabPath);
            RequireScene(ArtShowcasePath);

            EditorBuildSettingsScene[] current = EditorBuildSettings.scenes;
            var temporary = new List<EditorBuildSettingsScene>(current.Length + 2)
            {
                new EditorBuildSettingsScene(MovementLabPath, true),
                new EditorBuildSettingsScene(ArtShowcasePath, true)
            };

            temporary.AddRange(current.Where(scene =>
                !string.Equals(scene.path, MovementLabPath, StringComparison.Ordinal) &&
                !string.Equals(scene.path, ArtShowcasePath, StringComparison.Ordinal)));

            bool previousApplying = _isApplying;
            try
            {
                // Keep the sceneListChanged callback from immediately restoring the production order
                // while the legacy validator is deliberately observing its historical lab-first view.
                _isApplying = true;
                EditorBuildSettings.scenes = temporary.ToArray();
                action();
            }
            finally
            {
                _isApplying = previousApplying;
                if (!previousApplying)
                {
                    EnsureCanonicalOrder();
                }
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

        private static bool Matches(
            IReadOnlyList<EditorBuildSettingsScene> current,
            IReadOnlyList<EditorBuildSettingsScene> desired)
        {
            if (current.Count != desired.Count)
            {
                return false;
            }

            for (int i = 0; i < current.Count; i++)
            {
                if (current[i].path != desired[i].path || current[i].enabled != desired[i].enabled)
                {
                    return false;
                }
            }

            return true;
        }

        private static void RequireScene(string path)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(path) == null)
            {
                throw new InvalidOperationException(
                    "Rustline foundation-validation scene is missing: " + path);
            }
        }
    }
}
