using System.Collections.Generic;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class RustlinePaletteTests
    {
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
    }
}
