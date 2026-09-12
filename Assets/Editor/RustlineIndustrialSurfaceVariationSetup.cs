using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Keeps the fully-connected industrial-surface interior tile visually varied without changing
    /// structural connectivity. Slot 15 (N+E+S+W) is rotationally symmetric for Rule Tile purposes,
    /// so the built-in deterministic RuleTile random transform may safely rotate it in 90-degree steps.
    /// </summary>
    public static class RustlineIndustrialSurfaceVariationSetup
    {
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const int InteriorRuleId = 1015;
        private const float InteriorPerlinScale = 0.5f;

        [MenuItem("Tools/Rustline/Apply Industrial Surface Interior Variation")]
        public static void ApplyFromMenu()
        {
            RuleTile ruleTile = RequireRuleTile();
            bool changed = EnsureInteriorVariation(ruleTile);
            if (changed)
            {
                EditorUtility.SetDirty(ruleTile);
                AssetDatabase.SaveAssets();
            }

            RefreshLoadedTilemaps();
            ValidateOrThrow(ruleTile);
            EditorUtility.DisplayDialog(
                "Rustline Industrial Surface",
                "Slot 15 interior variation is configured as deterministic 0/90/180/270-degree rotation.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Industrial Surface Interior Variation")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow(RequireRuleTile());
            EditorUtility.DisplayDialog(
                "Rustline Industrial Surface",
                "Slot 15 deterministic rotational variation is valid.",
                "OK");
        }

        internal static bool EnsureInteriorVariation(RuleTile ruleTile)
        {
            Require(ruleTile != null, "IndustrialSurfaceRuleTile is missing.");
            RuleTile.TilingRule interiorRule = ruleTile.m_TilingRules
                .FirstOrDefault(rule => rule.m_Id == InteriorRuleId);
            Require(interiorRule != null,
                "IndustrialSurfaceRuleTile is missing canonical interior rule 1015 (slot 15).");

            bool changed = false;
            if (interiorRule.m_Output != RuleTile.TilingRuleOutput.OutputSprite.Random)
            {
                interiorRule.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random;
                changed = true;
            }

            if (interiorRule.m_RandomTransform != RuleTile.TilingRuleOutput.Transform.Rotated)
            {
                interiorRule.m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Rotated;
                changed = true;
            }

            if (!Mathf.Approximately(interiorRule.m_PerlinScale, InteriorPerlinScale))
            {
                interiorRule.m_PerlinScale = InteriorPerlinScale;
                changed = true;
            }

            return changed;
        }

        internal static void ValidateOrThrow(RuleTile ruleTile)
        {
            Require(ruleTile != null, "IndustrialSurfaceRuleTile is missing.");
            Require(ruleTile.m_TilingRules.Count == 16,
                "IndustrialSurfaceRuleTile must keep exactly 16 canonical connectivity rules.");

            RuleTile.TilingRule interiorRule = ruleTile.m_TilingRules
                .FirstOrDefault(rule => rule.m_Id == InteriorRuleId);
            Require(interiorRule != null,
                "IndustrialSurfaceRuleTile is missing canonical interior rule 1015 (slot 15).");
            Require(interiorRule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random,
                "Slot 15 must use RuleTile random output so its transform can vary deterministically by cell.");
            Require(interiorRule.m_RandomTransform == RuleTile.TilingRuleOutput.Transform.Rotated,
                "Slot 15 must use deterministic 90-degree rotational variation only.");
            Require(interiorRule.m_Sprites != null && interiorRule.m_Sprites.Length == 1,
                "Slot 15 rotational variation must continue using exactly one canonical interior sprite.");

            for (int index = 0; index < 15; index++)
            {
                RuleTile.TilingRule rule = ruleTile.m_TilingRules[index];
                Require(rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Single,
                    "Canonical slot " + index + " must remain a fixed single-sprite rule.");
                Require(rule.m_RandomTransform == RuleTile.TilingRuleOutput.Transform.Fixed,
                    "Canonical slot " + index + " must not receive random rotation/mirroring.");
            }
        }

        private static RuleTile RequireRuleTile()
        {
            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            Require(ruleTile != null,
                "IndustrialSurfaceRuleTile.asset is missing. Rebuild M0 Art Showcase first.");
            return ruleTile;
        }

        private static void RefreshLoadedTilemaps()
        {
            foreach (Tilemap tilemap in UnityEngine.Object.FindObjectsByType<Tilemap>(
                         FindObjectsInactive.Include,
                         FindObjectsSortMode.None))
            {
                tilemap.RefreshAllTiles();
            }

            SceneView.RepaintAll();
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }

    /// <summary>
    /// Re-applies the slot-15 variation contract whenever the generated Rule Tile is saved. This
    /// makes the setting survive the deterministic M0 rebuild, whose canonical rule generation still
    /// owns connectivity/sprite assignment for slots 00-15.
    /// </summary>
    internal sealed class RustlineIndustrialSurfaceVariationSaveGuard : AssetModificationProcessor
    {
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";

        private static string[] OnWillSaveAssets(string[] paths)
        {
            if (!paths.Contains(RuleTilePath, StringComparer.OrdinalIgnoreCase))
            {
                return paths;
            }

            RuleTile ruleTile = AssetDatabase.LoadAssetAtPath<RuleTile>(RuleTilePath);
            if (ruleTile != null && RustlineIndustrialSurfaceVariationSetup.EnsureInteriorVariation(ruleTile))
            {
                EditorUtility.SetDirty(ruleTile);
            }

            return paths;
        }
    }
}
