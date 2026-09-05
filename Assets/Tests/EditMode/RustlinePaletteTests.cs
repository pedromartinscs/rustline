using System.Collections.Generic;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class RustlinePaletteTests
    {
        private static Color32[] s_DarknessLookup;

        [OneTimeSetUp]
        public void BuildDarknessLookup()
        {
            s_DarknessLookup = RustlinePalette.CreateDarknessLookupPixels();
        }

        [Test]
        public void CanonicalPalette_ContainsExactlyTwentyEightUniqueColors()
        {
            Assert.That(RustlinePalette.CanonicalColors.Count, Is.EqualTo(28));
            HashSet<Color32> uniqueColors = new HashSet<Color32>(RustlinePalette.CanonicalColors);
            Assert.That(uniqueColors, Has.Count.EqualTo(28));
            Assert.That(RustlinePalette.DeepSpace, Is.EqualTo(new Color32(1, 2, 11, 255)));
        }

        [Test]
        public void DarknessLut_UsesOnlyCanonicalColors()
        {
            for (int sourceIndex = 0; sourceIndex < RustlinePalette.ColorCount; sourceIndex++)
            {
                for (int level = 0; level < RustlinePalette.DarknessLevelCount; level++)
                {
                    Color32 mapped = RustlinePalette.GetDarkenedColor(sourceIndex, level);
                    Assert.That(
                        RustlinePalette.IsCanonical(mapped),
                        Is.True,
                        $"Source {sourceIndex}, level {level} mapped outside Canonical 28.");
                }
            }
        }

        [Test]
        public void DarknessLut_HasIdentityAndDeepSpaceEndpoints()
        {
            int finalLevel = RustlinePalette.DarknessLevelCount - 1;
            for (int sourceIndex = 0; sourceIndex < RustlinePalette.ColorCount; sourceIndex++)
            {
                Assert.That(
                    RustlinePalette.GetDarkenedColor(sourceIndex, 0),
                    Is.EqualTo(RustlinePalette.GetColor(sourceIndex)),
                    $"Level 0 is not identity for source {sourceIndex}.");
                Assert.That(
                    RustlinePalette.GetDarkenedColor(sourceIndex, finalLevel),
                    Is.EqualTo(RustlinePalette.DeepSpace),
                    $"Final level is not Deep Space for source {sourceIndex}.");
            }
        }

        [Test]
        public void DeepSpace_RemainsDeepSpaceAtEveryLevel()
        {
            for (int level = 0; level < RustlinePalette.DarknessLevelCount; level++)
            {
                Assert.That(
                    RustlinePalette.GetDarkenedColor(RustlinePalette.DeepSpaceIndex, level),
                    Is.EqualTo(RustlinePalette.DeepSpace));
            }
        }

        [Test]
        public void QuantizedLinearCells_AreDistinctForEveryCanonicalColor()
        {
            HashSet<int> occupiedCells = new HashSet<int>();
            foreach (Color32 color in RustlinePalette.CanonicalColors)
            {
                Color linear = ((Color)color).linear;
                Assert.That(
                    occupiedCells.Add(RustlinePalette.GetQuantizedLinearCellIndex(linear)),
                    Is.True,
                    $"Canonical color {color} shares a 5-bit linear-RGB lookup cell.");
            }
        }

        [Test]
        public void DarknessLookup_CanonicalInputsMatchExistingMappingExactly()
        {
            for (int sourceIndex = 0; sourceIndex < RustlinePalette.ColorCount; sourceIndex++)
            {
                Color sourceLinear = ((Color)RustlinePalette.GetColor(sourceIndex)).linear;
                for (int level = 0; level < RustlinePalette.DarknessLevelCount; level++)
                {
                    int pixelIndex = RustlinePalette.GetDarknessLookupPixelIndex(sourceLinear, level);
                    Assert.That(
                        s_DarknessLookup[pixelIndex],
                        Is.EqualTo(RustlinePalette.GetDarkenedColor(sourceIndex, level)),
                        $"Lookup changed source {sourceIndex}, darkness level {level}.");
                }
            }
        }

        [Test]
        public void DarknessLookup_ContainsOnlyCanonicalColorsAndFinalLevelIsDeepSpace()
        {
            int finalLevelStart =
                (RustlinePalette.DarknessLevelCount - 1) *
                RustlinePalette.LookupChannelSize *
                RustlinePalette.DarknessLookupWidth;

            for (int index = 0; index < s_DarknessLookup.Length; index++)
            {
                if (!RustlinePalette.IsCanonical(s_DarknessLookup[index]))
                {
                    Assert.Fail($"Lookup pixel {index} is outside Canonical 28.");
                }

                if (index >= finalLevelStart &&
                    !s_DarknessLookup[index].Equals(RustlinePalette.DeepSpace))
                {
                    Assert.Fail($"Final-level lookup pixel {index} is not Deep Space.");
                }
            }
        }

        [Test]
        public void DarknessLookup_HasExpectedLayoutAndMemoryCost()
        {
            Assert.That(RustlinePalette.DarknessLookupWidth, Is.EqualTo(1024));
            Assert.That(RustlinePalette.DarknessLookupHeight, Is.EqualTo(160));
            Assert.That(RustlinePalette.DarknessLookupByteCount, Is.EqualTo(655360));
            Assert.That(s_DarknessLookup, Has.Length.EqualTo(1024 * 160));
        }
    }
}
