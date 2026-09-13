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
    /// keeps canonical rules in slots 00-15, normal material variants in slots 16-31, and rare
    /// authored wear/mechanical events in slots 32-47. Border connectivity never changes.
    /// </summary>
    public static class RustlineIndustrialSurfaceVariationSetup
    {
        private const string AtlasPath = "Assets/Art/Environment/Tiles/industrial_surface.png";
        private const string RuleTilePath =
            "Assets/Art/Environment/Tiles/Generated/IndustrialSurfaceRuleTile.asset";
        private const int TileSize = 16;
        private const int AtlasColumns = 8;
        private const int AtlasRows = 6;
        private const int AtlasSlotCount = AtlasColumns * AtlasRows;
        private const float SurfacePerlinScale = 0.37f;

        // Normal slots preserve the blue structural material language established in rows 1-4.
        // Rare slots are intentionally sparse visual events from rows 5-6. RareStride means one
        // rare entry per N entries in the deterministic RuleTile random sprite array.
        //
        // 09 NS: vertical strip (normal variation only).
        // 10 EW: horizontal strip (normal variation only).
        // 11 NES: exposed west/left edge; rare west-edge rust/bolt = 42/46.
        // 12 ESW: exposed north/top edge; rare top rust/bolt = 40/44.
        // 13 SWN: exposed east/right edge; rare right-edge rust/bolt = 43/47.
        // 14 WNE: exposed south/underside edge; rare underside rust/bolt = 41/45.
        // 15 ALL: fully surrounded interior; rare authored events = 32-39.
        private static readonly VariationSpec[] VariationSpecs =
        {
            new VariationSpec(9,  new[] { 24, 30 }, Array.Empty<int>(), 0, false),
            new VariationSpec(10, new[] { 23, 29 }, Array.Empty<int>(), 0, false),
            new VariationSpec(11, new[] { 21, 27 }, new[] { 42, 46 }, 20, false),
            new VariationSpec(12, new[] { 19, 25 }, new[] { 40, 44 }, 20, false),
            new VariationSpec(13, new[] { 22, 28 }, new[] { 43, 47 }, 20, false),
            new VariationSpec(14, new[] { 20, 26 }, new[] { 41, 45 }, 20, false),
            new VariationSpec(15, new[] { 16, 17, 18, 31 },
                new[] { 32, 33, 34, 35, 36, 37, 38, 39 }, 16, true),
        };

        private readonly struct VariationSpec
        {
            internal VariationSpec(
                int canonicalSlot,
                int[] normalVariantSlots,
                int[] rareVariantSlots,
                int rareStride,
                bool allowQuarterTurns)
            {
                CanonicalSlot = canonicalSlot;
                NormalVariantSlots = normalVariantSlots;
                RareVariantSlots = rareVariantSlots;
                RareStride = rareStride;
                AllowQuarterTurns = allowQuarterTurns;
            }

            internal int CanonicalSlot { get; }
            internal int[] NormalVariantSlots { get; }
            internal int[] RareVariantSlots { get; }
            internal int RareStride { get; }
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
                "Deterministic structural variants and rare rust/bolt/hole/dent/seam events are active.",
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
        /// only while that validator runs, then restores production variation even when validation
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
                Sprite[] desiredSprites = BuildDesiredSprites(atlasSlots, spec);

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

                Sprite[] expectedSprites = BuildDesiredSprites(atlasSlots, spec);
                Require(SpriteArraysMatch(rule.m_Sprites, expectedSprites),
                    "Variant sprite mapping is incorrect for canonical slot " + spec.CanonicalSlot + ".");

                if (spec.RareVariantSlots.Length > 0)
                {
                    int expectedLength = spec.RareVariantSlots.Length * spec.RareStride;
                    Require(rule.m_Sprites.Length == expectedLength,
                        "Rare-event weighting array has the wrong length for canonical slot " +
                        spec.CanonicalSlot + ".");
                }
            }
        }

        private static Sprite[] BuildDesiredSprites(Sprite[] atlasSlots, VariationSpec spec)
        {
            int[] normalSlots = new int[1 + spec.NormalVariantSlots.Length];
            normalSlots[0] = spec.CanonicalSlot;
            Array.Copy(spec.NormalVariantSlots, 0, normalSlots, 1, spec.NormalVariantSlots.Length);

            if (spec.RareVariantSlots.Length == 0)
            {
                Sprite[] normalSprites = new Sprite[normalSlots.Length];
                for (int index = 0; index < normalSlots.Length; index++)
                {
                    normalSprites[index] = atlasSlots[normalSlots[index]];
                }
                return normalSprites;
            }

            Require(spec.RareStride > 1,
                "Rare-event stride must be greater than one for canonical slot " + spec.CanonicalSlot + ".");

            int totalEntries = spec.RareVariantSlots.Length * spec.RareStride;
            Sprite[] weighted = new Sprite[totalEntries];
            int normalIndex = 0;
            int rareIndex = 0;
            int firstRareEntry = spec.RareStride / 2;

            for (int entry = 0; entry < totalEntries; entry++)
            {
                bool isRareEntry = rareIndex < spec.RareVariantSlots.Length &&
                    entry == firstRareEntry + rareIndex * spec.RareStride;
                if (isRareEntry)
                {
                    weighted[entry] = atlasSlots[spec.RareVariantSlots[rareIndex]];
                    rareIndex++;
                }
                else
                {
                    weighted[entry] = atlasSlots[normalSlots[normalIndex % normalSlots.Length]];
                    normalIndex++;
                }
            }

            Require(rareIndex == spec.RareVariantSlots.Length,
                "Rare-event weighting failed to place every authored detail slot for canonical slot " +
                spec.CanonicalSlot + ".");
            return weighted;
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
            Require(atlas.width == AtlasColumns * TileSize && atlas.height == AtlasRows * TileSize,
                "Industrial surface atlas must remain exactly 128x96 px / 48 slots.");

            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(AtlasPath).OfType<Sprite>().ToArray();
            Require(sprites.Length == AtlasSlotCount,
                "Industrial surface atlas must expose exactly 48 sliced sprites.");

            Sprite[] slots = new Sprite[AtlasSlotCount];
            foreach (Sprite sprite in sprites)
            {
                int column = Mathf.RoundToInt(sprite.rect.x / TileSize);
                int unityRow = Mathf.RoundToInt(sprite.rect.y / TileSize);
                int logicalRow = AtlasRows - 1 - unityRow;
                int slot = logicalRow * AtlasColumns + column;
                Require(slot >= 0 && slot < slots.Length,
                    "Industrial surface sprite rect resolves outside the atlas slot range: " + sprite.name);
                Require(slots[slot] == null,
                    "Industrial surface atlas exposes duplicate sprites for slot " + slot + ".");
                slots[slot] = sprite;
            }

            for (int slot = 0; slot < AtlasSlotCount; slot++)
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
