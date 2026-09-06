using System.Collections.Generic;
using NUnit.Framework;
using Rustline.Editor.Diagnostics;

namespace Rustline.Tests
{
    public sealed class RustlineEditorSpikeAnalyzerTests
    {
        [Test]
        public void DistributionAndWorstSelection_AreDeterministic()
        {
            SpikeDistribution distribution = RustlineEditorSpikeAnalysis.CalculateDistribution(
                new[] { 4.0, 1.0, 3.0, 2.0 });

            Assert.That(distribution.Median, Is.EqualTo(2.5));
            Assert.That(distribution.P95, Is.EqualTo(3.85).Within(0.000001));
            Assert.That(distribution.P99, Is.EqualTo(3.97).Within(0.000001));
            Assert.That(distribution.Maximum, Is.EqualTo(4.0));

            List<SpikeFrameSummary> worst = RustlineEditorSpikeAnalysis.SelectWorstFrames(
                new[]
                {
                    Frame(12, 7.0),
                    Frame(10, 9.0),
                    Frame(11, 9.0)
                },
                2);

            Assert.That(worst, Has.Count.EqualTo(2));
            Assert.That(worst[0].FrameIndex, Is.EqualTo(10));
            Assert.That(worst[1].FrameIndex, Is.EqualTo(11));
        }

        [Test]
        public void HierarchyAggregation_DoesNotDoubleCountNestedInclusiveTime()
        {
            SpikeSampleNode playerLoop = new SpikeSampleNode("PlayerLoop", 10.0);
            SpikeSampleNode scripts = new SpikeSampleNode("Scripts", 6.0);
            scripts.Children.Add(new SpikeSampleNode("Rustline.Player.Motor", 4.0));
            playerLoop.Children.Add(scripts);
            playerLoop.Children.Add(new SpikeSampleNode("Rendering", 3.0));

            List<SpikeContributor> direct =
                RustlineEditorSpikeAnalysis.AggregateDirectChildren(playerLoop);
            Dictionary<string, double> self =
                RustlineEditorSpikeAnalysis.AggregateSelfTimes(playerLoop);

            Assert.That(direct, Has.Count.EqualTo(2));
            Assert.That(direct[0].Name, Is.EqualTo("Scripts"));
            Assert.That(direct[0].Milliseconds, Is.EqualTo(6.0));
            Assert.That(direct[1].Name, Is.EqualTo("Rendering"));
            Assert.That(direct[1].Milliseconds, Is.EqualTo(3.0));
            Assert.That(self["Scripts"], Is.EqualTo(2.0));
            Assert.That(self["Rustline.Player.Motor"], Is.EqualTo(4.0));
            Assert.That(self["PlayerLoop"], Is.EqualTo(1.0));
        }

        [Test]
        public void EditorOnlyEstimate_SubtractsOnlyOutermostPlayerLoopSamples()
        {
            SpikeSampleNode editorLoop = new SpikeSampleNode("EditorLoop", 20.0);
            SpikeSampleNode playerLoop = new SpikeSampleNode("PlayerLoop", 8.0);
            playerLoop.Children.Add(new SpikeSampleNode("PlayerLoop", 3.0));
            editorLoop.Children.Add(playerLoop);
            editorLoop.Children.Add(new SpikeSampleNode("Inspector", 6.0));

            double playerTime = RustlineEditorSpikeAnalysis.SumOutermostNamedSamples(
                editorLoop,
                "PlayerLoop");

            Assert.That(playerTime, Is.EqualTo(8.0));
            Assert.That(editorLoop.DurationMs - playerTime, Is.EqualTo(12.0));
        }

        [TestCase(8.0, 0.0, 0.0, 0.0, 0.0, SpikeClassification.Gc)]
        [TestCase(0.0, 8.0, 0.0, 0.0, 0.0, SpikeClassification.Physics2D)]
        [TestCase(0.0, 0.0, 8.0, 0.0, 0.0, SpikeClassification.RustlineScript)]
        [TestCase(0.0, 0.0, 0.0, 8.0, 0.0, SpikeClassification.ProfilerOverhead)]
        [TestCase(0.0, 0.0, 0.0, 0.0, 8.0, SpikeClassification.PresentWait)]
        public void Classification_RecognizesDominantSpecificEvidence(
            double gc,
            double physics,
            double rustline,
            double profiler,
            double present,
            SpikeClassification expected)
        {
            SpikeClassification classification = RustlineEditorSpikeAnalysis.Classify(
                new SpikeClassificationInput(
                    20.0,
                    0.0,
                    0.0,
                    0.0,
                    present,
                    gc,
                    physics,
                    rustline,
                    profiler));

            Assert.That(classification, Is.EqualTo(expected));
        }

        [Test]
        public void Classification_DistinguishesEditorPlayerRenderAndMixed()
        {
            Assert.That(Classify(editor: 9.0), Is.EqualTo(SpikeClassification.EditorLoop));
            Assert.That(Classify(player: 9.0), Is.EqualTo(SpikeClassification.PlayerLoopCpu));
            Assert.That(Classify(render: 16.0), Is.EqualTo(SpikeClassification.RenderThread));
            Assert.That(
                Classify(editor: 5.0, player: 6.0),
                Is.EqualTo(SpikeClassification.Mixed));
            Assert.That(
                Classify(gc: 5.0, physics: 5.0),
                Is.EqualTo(SpikeClassification.Mixed));
        }

        [Test]
        public void ReportGeneration_HandlesMissingCustomMarkerExplicitly()
        {
            SpikeFrameSummary frame = Frame(42, 16.0);
            frame.EditorLoopMs = 16.0;
            frame.PlayerLoopMs = 10.0;
            frame.EditorOnlyMs = 6.0;
            frame.RustlineMarkers["Rustline.Player.Aim"] = 0.125;
            frame.Classification = SpikeClassification.Mixed;

            string report = RustlineEditorSpikeAnalysis.BuildReport(
                new SpikeReportContext
                {
                    UnityVersion = "6000.4.0f1",
                    ActiveScene = "MovementLab",
                    FrameRange = "42..42"
                },
                new[] { frame },
                new[] { "Rustline.Player.Aim", "Rustline.Player.Motor" },
                500);

            Assert.That(report, Does.Contain("RUSTLINE EDITOR SPIKE ANALYSIS"));
            Assert.That(report, Does.Contain("Frame 42 - MIXED"));
            Assert.That(report, Does.Contain("Rustline.Player.Aim: 0.125 ms"));
            Assert.That(report, Does.Contain("Rustline.Player.Motor: missing/not recorded"));
            Assert.That(report, Does.Contain("Editor-only*"));
            Assert.That(report, Does.Contain("thread durations overlap"));
        }

        private static SpikeFrameSummary Frame(int index, double mainThreadMs)
        {
            return new SpikeFrameSummary
            {
                FrameIndex = index,
                MainThreadMs = mainThreadMs
            };
        }

        private static SpikeClassification Classify(
            double editor = 0.0,
            double player = 0.0,
            double render = 0.0,
            double gc = 0.0,
            double physics = 0.0)
        {
            return RustlineEditorSpikeAnalysis.Classify(
                new SpikeClassificationInput(
                    20.0,
                    editor,
                    player,
                    render,
                    0.0,
                    gc,
                    physics,
                    0.0,
                    0.0));
        }
    }
}
