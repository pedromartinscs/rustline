using System;
using System.Reflection;
using System.Runtime.ExceptionServices;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Compatibility attribute that intentionally makes the old per-step Rustline editor commands
    /// inert. Existing setup methods remain callable from code and command-line entry points, but
    /// they no longer register dozens of MenuItems in Unity's Tools/Rustline menu.
    ///
    /// New user-facing Rustline menu entries must explicitly use UnityEditor.MenuItem.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
    internal sealed class MenuItem : Attribute
    {
        internal MenuItem(string itemName)
            : this(itemName, false, 0)
        {
        }

        internal MenuItem(string itemName, bool isValidateFunction)
            : this(itemName, isValidateFunction, 0)
        {
        }

        internal MenuItem(string itemName, bool isValidateFunction, int priority)
        {
            ItemName = itemName;
            IsValidateFunction = isValidateFunction;
            Priority = priority;
        }

        internal string ItemName { get; }
        internal bool IsValidateFunction { get; }
        internal int Priority { get; }
    }

    /// <summary>
    /// Single user-facing production command for the current Rustline vertical slice.
    /// The specialized deterministic builders stay separate internally; this orchestrator simply
    /// runs them in their required order and surfaces one success/failure result to the author.
    /// </summary>
    public static class RustlineProductionTools
    {
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";

        private readonly struct ProductionStep
        {
            internal ProductionStep(string label, Type setupType, string methodName)
            {
                Label = label;
                SetupType = setupType;
                MethodName = methodName;
            }

            internal string Label { get; }
            internal Type SetupType { get; }
            internal string MethodName { get; }
        }

        private static readonly ProductionStep[] DressingSteps =
        {
            // Geometry migration runs immediately after the canonical graybox rebuild. Keeping it
            // here lets the production command own the accepted raised crane/handler support without
            // exposing another authoring menu entry while we finish visual evaluation of the joint.
            new ProductionStep(
                "Connecting handler overhead support to service shaft",
                typeof(RustlineSalvageIntakeHandlerOverheadSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying macro environment dressing",
                typeof(RustlineSalvageIntakeArtSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying lower-bay floor dressing",
                typeof(RustlineSalvageIntakeFloorDressingSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying west wall dressing",
                typeof(RustlineSalvageIntakeWallDressingSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying service-shaft wall dressing",
                typeof(RustlineSalvageIntakeServiceShaftDressingSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying horizontal far parallax",
                typeof(RustlineSalvageIntakeParallaxSetup),
                "ApplyAndValidate"),
            new ProductionStep(
                "Applying vertical depth backdrop",
                typeof(RustlineSalvageIntakeVerticalDepthSetup),
                "ApplyAndValidate"),
        };

        [UnityEditor.MenuItem("Tools/Rustline/Apply Production Setup", false, 1)]
        public static void ApplyProductionSetup()
        {
            try
            {
                int totalSteps = DressingSteps.Length + 3;
                int completedSteps = 0;

                ShowProgress("Rebuilding canonical Salvage Intake", completedSteps++, totalSteps);
                // The frozen M1A validator inside the Salvage builder still expects its historical
                // MovementLab/ArtShowcase-first build order. Present that view only for the duration
                // of foundation validation, then automatically restore the production release order.
                RustlineBuildSceneOrder.RunWithFoundationValidationOrder(
                    () => InvokeNonPublicStatic(typeof(RustlineSalvageIntakeSetup), "BuildAndValidate"));

                ShowProgress("Applying industrial-surface variation", completedSteps++, totalSteps);
                ApplyIndustrialSurfaceVariation();

                foreach (ProductionStep step in DressingSteps)
                {
                    ShowProgress(step.Label, completedSteps++, totalSteps);
                    InvokeNonPublicStatic(step.SetupType, step.MethodName);
                }

                ShowProgress("Enforcing release build-scene order", completedSteps, totalSteps);
                RustlineBuildSceneOrder.EnsureCanonicalOrder();
                RustlineBuildSceneOrder.ValidateOrThrow();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                Debug.Log(
                    "RUSTLINE_PRODUCTION_SETUP_OK: Salvage Intake rebuilt, handler support finalized, production dressing refreshed, " +
                    "parallax layers applied, and release build-scene order validated.");
                EditorUtility.DisplayDialog(
                    "Rustline Production Setup",
                    "Production setup applied successfully.\n\n" +
                    "Salvage Intake was rebuilt, the accepted handler/shaft structural connection was applied, " +
                    "all current production dressing/parallax passes were refreshed, and the release scene order was validated.",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Rustline Production Setup Failed",
                    "The production setup stopped at the first failing deterministic check. " +
                    "Nothing after that step was applied. See the Console for the exact error.",
                    "OK");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

        private static void ApplyIndustrialSurfaceVariation()
        {
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            if (ruleTile == null)
            {
                throw new InvalidOperationException(
                    "IndustrialSurfaceRuleTile.asset is missing. The accepted M0 foundation must exist before production setup.");
            }

            if (RustlineIndustrialSurfaceVariationSetup.EnsureInteriorVariation(ruleTile))
            {
                EditorUtility.SetDirty(ruleTile);
                AssetDatabase.SaveAssets();
            }

            RustlineIndustrialSurfaceVariationSetup.ValidateOrThrow(ruleTile);
        }

        private static void InvokeNonPublicStatic(Type setupType, string methodName)
        {
            MethodInfo method = setupType.GetMethod(
                methodName,
                BindingFlags.Static | BindingFlags.NonPublic);
            if (method == null)
            {
                throw new MissingMethodException(setupType.FullName, methodName);
            }

            try
            {
                method.Invoke(null, null);
            }
            catch (TargetInvocationException exception) when (exception.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw;
            }
        }

        private static void ShowProgress(string label, int completedSteps, int totalSteps)
        {
            float progress = totalSteps <= 0 ? 0f : completedSteps / (float)totalSteps;
            EditorUtility.DisplayProgressBar("Rustline Production Setup", label, progress);
        }
    }
}
