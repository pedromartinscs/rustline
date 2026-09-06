using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class FramePacingPlayModeTests
    {
        [Test]
        public void RuntimePolicy_CapsAtSixtyWithoutVSync()
        {
            Assert.That(RustlineFramePacing.MaximumFrameRate, Is.EqualTo(60));
            Assert.That(QualitySettings.vSyncCount, Is.Zero);
            Assert.That(Application.targetFrameRate, Is.EqualTo(RustlineFramePacing.MaximumFrameRate));
        }
    }
}
