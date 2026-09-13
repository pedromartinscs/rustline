using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Adds deterministic visual variation to the high-frequency industrial-surface connectivity
    /// rules while preserving the accepted 16-rule cardinal-neighbor contract. The source atlas
    /// keeps the canonical rules in slots 00-15; rows 3-4 (slots 16-31) contain presentation-only
    /// variants whose border pixels are authored to match their corresponding canonical rule.
    /// </summary>
    public static class RustlineIndustrialSurfaceVariationSetup
    {
        private const string AtlasPath = "Assets/Art/Environment/Tiles/industrial_surface.png";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const int TileSize = 16;
        private const int AtlasColumns = 8;
        private const float SurfacePerlinScale = 0.37f;

        // Canonical slot -> additional atlas slots.
        // 09 NS: vertical strip.
        // 10 EW: horizontal strip.
        // 11 NES: exposed west/left edge.
        // 12 ESW: exposed north/top edge.
        // 13 SWN: exposed east/right edge.
        // 14 WNE: exposed south/underside edge.
        // 15 ALL: fully surrounded interior.
        private static readonly VariationSpec[] VariationSpecs =
        {
            new VariationSpec(9,  new[] { 24, 30 }, false),
            new VariationSpec(10, new[] { 23, 29 }, false),
            new VariationSpec(11, new[] { 21, 27 }, false),
            new VariationSpec(12, new[] { 19, 25 }, false),
            new VariationSpec(13, new[] { 22, 28 }, false),
            new VariationSpec(14, new[] { 20, 26 }, false),
            new VariationSpec(15, new[] { 16, 17, 18, 31 }, true),
        };

        private readonly struct VariationSpec
        {
            internal VariationSpec(int canonicalSlot, int[] variantSlots, bool allowQuarterTurns)
            {
                CanonicalSlot = canonicalSlot;
                VariantSlots = variantSlots;
                AllowQuarterTurns = allowQuarterTurns;
            }

            internal int CanonicalSlot { get; }
            internal int[] VariantSlots { get; }
            internal bool AllowQuarterTurns { get; }
            internal int RuleId => 1000 + CanonicalSlot;
        }

        [MenuItem("Tools/Rustline/Apply Industrial Surface Variation")]
        public static void ApplyFromMenu()
        {
            RuleTile ruleTile = RequireRuleTile();
            bool changed = EnsureSurfaceVariation(ruleTile);
            if (changed)
            {
                EditorUtility.SetDirty(ruleTile);
                AssetDatabase.SaveAssets();
            }

            RefreshLoadedTilemaps();
            ValidateOrThrow(ruleTile);
            EditorUtility.DisplayDialog(
                "Rustline Industrial Surface",
                "Deterministic variants are active for ALL, exposed cardinal edges, and thin horizontal/vertical runs.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Industrial Surface Variation")]
        public static void ValidateFromMenu()
        {
            ValidateOrThrow(RequireRuleTile());
            EditorUtility.DisplayDialog(
                "Rustline Industrial Surface",
                "Industrial-surface deterministic variant mapping is valid.",
                "OK");
        }

        /// <summary>
        /// The frozen M0 validator predates multi-sprite RuleTile outputs and still requires one
        /// canonical sprite per connectivity rule. Production setup enters this compatibility scope
        /// only while that validator runs, then restores the visual variants even when validation
        /// throws. This changes presentation data only; connectivity and collision never change.
        /// </summary>
        internal static void RunWithFoundationCompatibility(Action action)
        {
            Require(action != null, "Foundation compatibility action is missing.");
            RuleTile ruleTile = RequireRuleTile();

            try
            {
                if (EnsureCanonicalSingles(ruleTile))
                {
                    EditorUtility.SetDirty(ruleTile);
                }

                action();
            }
            finally
            {
                // The Salvage builder may save assets while the canonical compatibility view is
                // active, so always restore and persist the production presentation contract here.
                if (EnsureSurfaceVariation(ruleTile))
                {
                    EditorUtility.SetDirty(ruleTile);
                }

                AssetDatabase.SaveAssets();
                RefreshLoadedTilemaps();
            }
        }

        internal static bool EnsureSurfaceVariation(RuleTile ruleTile)
        {
            Require(ruleTile != null, "IndustrialSurfaceRuleTile is missing.");
            Require(ruleTile.m_TilingRules.Count == 16,
                "IndustrialSurfaceRuleTile must keep exactly 16 canonical connectivity rules.");

            Sprite[] atlasSlots = LoadAtlasSlots();
            bool changed = false;

            foreach (VariationSpec spec in VariationSpecs)
            {
                RuleTile.TilingRule rule = RequireRule(ruleTile, spec.RuleId);
                Sprite[] desiredSprites = new Sprite[1 + spec.VariantSlots.Length];
                desiredSprites[0] = atlasSlots[spec.CanonicalSlot];
                for (int index = 0; index < spec.VariantSlots.Length; index++)
                {
                    desiredSprites[index + 1] = atlasSlots[spec.VariantSlots[index]];
                }

                if (rule.m_Output != RuleTile.TilingRuleOutput.OutputSprite.Random)
                {
                    rule.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Random;
                    changed = true;
                }

                RuleTile.TilingRuleOutput.Transform desiredTransform = spec.AllowQuarterTurns
                    ? RuleTile.TilingRuleOutput.Transform.Rotated
                    : RuleTile.TilingRuleOutput.Transform.Fixed;
                if (rule.m_RandomTransform != desiredTransform)
                {
                    rule.m_RandomTransform = desiredTransform;
                    changed = true;
                }

                if (!Mathf.Approximately(rule.m_PerlinScale, SurfacePerlinScale))
                {
                    rule.m_PerlinScale = SurfacePerlinScale;
                    changed = true;
                }

                if (!SpriteArraysMatch(rule.m_Sprites, desiredSprites))
                {
                    rule.m_Sprites = desiredSprites;
                    changed = true;
                }
            }

            return changed;
        }

        internal static void ValidateOrThrow(RuleTile ruleTile)
        {
            Require(ruleTile != null, "IndustrialSurfaceRuleTile is missing.");
            Require(ruleTile.m_TilingRules.Count == 16,
                "IndustrialSurfaceRuleTile must keep exactly 16 canonical connectivity rules.");

            Sprite[] atlasSlots = LoadAtlasSlots();
            HashSet<int> variedSlots = new HashSet<int>(VariationSpecs.Select(spec => spec.CanonicalSlot));

            for (int slot = 0; slot < 16; slot++)
            {
                RuleTile.TilingRule rule = RequireRule(ruleTile, 1000 + slot);
                if (!variedSlots.Contains(slot))
                {
                    Require(rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Single,
                        "Canonical slot " + slot + " must remain a fixed single-sprite rule.");
                    Require(rule.m_RandomTransform == RuleTile.TilingRuleOutput.Transform.Fixed,
                        "Canonical slot " + slot + " must not receive random transforms.");
                    Require(rule.m_Sprites != null && rule.m_Sprites.Length == 1 &&
                        rule.m_Sprites[0] == atlasSlots[slot],
                        "Canonical slot " + slot + " no longer uses its canonical atlas sprite.");
                }
            }

            foreach (VariationSpec spec in VariationSpecs)
            {
                RuleTile.TilingRule rule = RequireRule(ruleTile, spec.RuleId);
                Require(rule.m_Output == RuleTile.TilingRuleOutput.OutputSprite.Random,
                    "Variant slot " + spec.CanonicalSlot + " must use deterministic RuleTile random output.");
                Require(Mathf.Approximately(rule.m_PerlinScale, SurfacePerlinScale),
                    "Variant slot " + spec.CanonicalSlot + " uses the wrong Perlin scale.");

                RuleTile.TilingRuleOutput.Transform expectedTransform = spec.AllowQuarterTurns
                    ? RuleTile.TilingRuleOutput.Transform.Rotated
                    : RuleTile.TilingRuleOutput.Transform.Fixed;
                Require(rule.m_RandomTransform == expectedTransform,
                    "Variant slot " + spec.CanonicalSlot + " uses an unsafe random transform.");

                Sprite[] expectedSprites = new Sprite[1 + spec.VariantSlots.Length];
                expectedSprites[0] = atlasSlots[spec.CanonicalSlot];
                for (int index = 0; index < spec.VariantSlots.Length; index++)
                {
                    expectedSprites[index + 1] = atlasSlots[spec.VariantSlots[index]];
                }

                Require(SpriteArraysMatch(rule.m_Sprites, expectedSprites),
                    "Variant sprite mapping is incorrect for canonical slot " + spec.CanonicalSlot + ".");
            }
        }

        private static bool EnsureCanonicalSingles(RuleTile ruleTile)
        {
            Sprite[] atlasSlots = LoadAtlasSlots();
            bool changed = false;

            foreach (VariationSpec spec in VariationSpecs)
            {
                RuleTile.TilingRule rule = RequireRule(ruleTile, spec.RuleId);
                Sprite[] canonical = { atlasSlots[spec.CanonicalSlot] };

                if (rule.m_Output != RuleTile.TilingRuleOutput.OutputSprite.Single)
                {
                    rule.m_Output = RuleTile.TilingRuleOutput.OutputSprite.Single;
                    changed = true;
                }
                if (rule.m_RandomTransform != RuleTile.TilingRuleOutput.Transform.Fixed)
                {
                    rule.m_RandomTransform = RuleTile.TilingRuleOutput.Transform.Fixed;
                    changed = true;
                }
                if (!SpriteArraysMatch(rule.m_Sprites, canonical))
                {
                    rule.m_Sprites = canonical;
                    changed = true;
                }
            }

            return changed;
        }

        private static Sprite[] LoadAtlasSlots()
        {
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(AtlasPath);
            Require(atlas != null, "Industrial surface atlas is missing: " + AtlasPath);
            Require(atlas.width == AtlasColumns * TileSize && atlas.height >= 4 * TileSize &&
                atlas.height % TileSize == 0,
                "Industrial surface atlas must remain 128 px wide and contain at least four 16 px rows.");

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            Require(sprites.Length >= 32,
                "Industrial surface atlas must expose at least the 32 core + variation slots.");

            Sprite[] slots = new Sprite[sprites.Length];
            int logicalRows = atlas.height / TileSize;
            foreach (Sprite sprite in sprites)
            {
                int column = Mathf.RoundToInt(sprite.rect.x / TileSize);
                int unityRow = Mathf.RoundToInt(sprite.rect.y / TileSize);
                int logicalRow = logicalRows - 1 - unityRow;
                int slot = logicalRow * AtlasColumns + column;
                Require(slot >= 0 && slot < slots.Length,
                    "Industrial surface sprite rect resolves outside the atlas slot range: " + sprite.name);
                Require(slots[slot] == null,
                    "Industrial surface atlas exposes duplicate sprites for slot " + slot + ".");
                slots[slot] = sprite;
            }

            for (int slot = 0; slot <= 31; slot++)
            {
                Require(slots[slot] != null,
                    "Industrial surface atlas is missing required slot " + slot + ".");
            }

            return slots;
        }

        private static RuleTile.TilingRule RequireRule(RuleTile ruleTile, int ruleId)
        {
            RuleTile.TilingRule rule = ruleTile.m_TilingRules.FirstOrDefault(candidate => candidate.m_Id == ruleId);
            Require(rule != null,
                "IndustrialSurfaceRuleTile is missing canonical rule " + ruleId + ".");
            return rule;
        }

        private static bool SpriteArraysMatch(Sprite[] actual, Sprite[] expected)
        {
            if (actual == null || expected == null || actual.Length != expected.Length)
            {
                return false;
            }

            for (int index = 0; index < actual.Length; index++)
            {
                if (actual[index] != expected[index])
                {
                    return false;
                }
            }

            return true;
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
}
