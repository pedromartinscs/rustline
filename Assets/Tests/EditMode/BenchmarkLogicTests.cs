using System.Collections.Generic;
using NUnit.Framework;
using Rustline.Diagnostics.Benchmarking;

namespace Rustline.Tests
{
    public sealed class BenchmarkLogicTests
    {
        [Test]
        public void Statistics_EmptyAndSingleSampleAreDefined()
        {
            BenchmarkMetricSummary empty = BenchmarkStatistics.Calculate(new double[0], 0);
            Assert.That(empty.hasSamples, Is.False);
            Assert.That(empty.sampleCount, Is.Zero);

            BenchmarkMetricSummary single = BenchmarkStatistics.Calculate(new[] { 12.5 }, 1);
            Assert.That(single.hasSamples, Is.True);
            Assert.That(single.meanMs, Is.EqualTo(12.5));
            Assert.That(single.medianMs, Is.EqualTo(12.5));
            Assert.That(single.standardDeviationMs, Is.Zero);
            Assert.That(single.minMs, Is.EqualTo(12.5));
            Assert.That(single.p99Ms, Is.EqualTo(12.5));
            Assert.That(single.maxMs, Is.EqualTo(12.5));
            Assert.That(single.equivalentFps, Is.EqualTo(80.0).Within(0.000001));
        }

        [Test]
        public void Statistics_CalculatesInterpolatedPercentilesAndPopulationDeviation()
        {
            BenchmarkMetricSummary summary =
                BenchmarkStatistics.Calculate(new[] { 4.0, 1.0, 3.0, 2.0 }, 4);

            Assert.That(summary.meanMs, Is.EqualTo(2.5));
            Assert.That(summary.medianMs, Is.EqualTo(2.5));
            Assert.That(summary.standardDeviationMs, Is.EqualTo(1.1180339887).Within(0.000000001));
            Assert.That(summary.p90Ms, Is.EqualTo(3.7).Within(0.000000001));
            Assert.That(summary.p95Ms, Is.EqualTo(3.85).Within(0.000000001));
            Assert.That(summary.p99Ms, Is.EqualTo(3.97).Within(0.000000001));
        }

        [Test]
        public void Protocol_GeneratesFixedCounterbalancedSequence()
        {
            BenchmarkBlockPlan[] plans =
                BenchmarkProtocol.CreateBlockPlans(BenchmarkMode.PenumbraAb, 3);

            Assert.That(plans, Has.Length.EqualTo(6));
            Assert.That(
                BenchmarkProtocol.DescribeSequence(plans),
                Is.EqualTo("ON>OFF | OFF>ON | ON>OFF"));
            Assert.That(plans[0].pairIndex, Is.EqualTo(1));
            Assert.That(plans[0].slot, Is.EqualTo("A"));
            Assert.That(plans[5].orderIndex, Is.EqualTo(6));
        }

        [Test]
        public void Protocol_ControlOffPreservesSlotsWhileDisablingBothConditions()
        {
            BenchmarkBlockPlan[] plans =
                BenchmarkProtocol.CreateBlockPlans(BenchmarkMode.ControlOff, 2);

            Assert.That(plans, Has.All.Matches<BenchmarkBlockPlan>(plan => !plan.penumbraEnabled));
            Assert.That(plans[0].slot, Is.EqualTo("A"));
            Assert.That(plans[1].slot, Is.EqualTo("B"));
            Assert.That(BenchmarkProtocol.DescribeSequence(plans), Is.EqualTo("OFF>OFF | OFF>OFF"));
        }

        [Test]
        public void Analysis_CalculatesOnMinusOffRegardlessOfChronologicalOrder()
        {
            List<BenchmarkBlockResult> blocks = new List<BenchmarkBlockResult>
            {
                CreateBlock(1, "A", true, 10.0, 9.0),
                CreateBlock(1, "B", false, 8.0, 7.0),
                CreateBlock(2, "A", false, 9.0, 8.0),
                CreateBlock(2, "B", true, 12.0, 10.0)
            };

            List<BenchmarkPairDelta> deltas = BenchmarkAnalysis.CalculatePairDeltas(
                blocks,
                BenchmarkMode.PenumbraAb,
                2);

            Assert.That(deltas[0].meanDeltaMs, Is.EqualTo(2.0));
            Assert.That(deltas[0].medianDeltaMs, Is.EqualTo(2.0));
            Assert.That(deltas[1].meanDeltaMs, Is.EqualTo(3.0));
            Assert.That(deltas[1].medianDeltaMs, Is.EqualTo(2.0));

            BenchmarkPairedSummary summary =
                BenchmarkAnalysis.SummarizePairDeltas(deltas, offMeanMs: 8.5);
            Assert.That(summary.meanOfMeanDeltasMs, Is.EqualTo(2.5));
            Assert.That(summary.medianOfMeanDeltasMs, Is.EqualTo(2.5));
            Assert.That(summary.percentageRelativeToOff, Is.EqualTo(29.4117647).Within(0.000001));
        }

        [Test]
        public void Options_ParsesDefaultsOverridesAndActivation()
        {
            string[] arguments =
            {
                "RustlineBenchmark.exe",
                "--rustline-benchmark",
                "--benchmark-mode=control-off",
                "--benchmark-warmup-seconds", "1.5",
                "--benchmark-settle-seconds=0.25",
                "--benchmark-block-seconds", "2",
                "--benchmark-pairs=3",
                "--benchmark-auto-quit"
            };

            bool parsed = BenchmarkOptions.TryParse(
                arguments,
                out BenchmarkOptions options,
                out string error);

            Assert.That(parsed, Is.True, error);
            Assert.That(options.requested, Is.True);
            Assert.That(options.mode, Is.EqualTo(BenchmarkMode.ControlOff));
            Assert.That(options.warmupSeconds, Is.EqualTo(1.5));
            Assert.That(options.settleSeconds, Is.EqualTo(0.25));
            Assert.That(options.blockSeconds, Is.EqualTo(2.0));
            Assert.That(options.pairCount, Is.EqualTo(3));
            Assert.That(options.autoQuit, Is.True);
        }

        [Test]
        public void Options_RejectsInvalidNumericOverrides()
        {
            bool parsed = BenchmarkOptions.TryParse(
                new[] { "--benchmark-block-seconds", "0" },
                out _,
                out string error);

            Assert.That(parsed, Is.False);
            Assert.That(error, Does.Contain("greater than 0"));
        }

        [Test]
        public void QualityLookup_ResolvesExactNameAndRejectsMissingLevel()
        {
            string[] names = { "Very Low", "Low", "Ultra" };
            Assert.That(BenchmarkQuality.FindLevelIndex(names, "Very Low"), Is.Zero);
            Assert.That(BenchmarkQuality.FindLevelIndex(names, "very low"), Is.EqualTo(-1));
            Assert.That(BenchmarkQuality.FindLevelIndex(names, "Missing"), Is.EqualTo(-1));
        }

        [Test]
        public void ReportSerialization_ContainsMachineReadableBlocksAndInvariantCsv()
        {
            BenchmarkRunReport report = new BenchmarkRunReport
            {
                utcTimestamp = "2026-09-01T00:00:00.0000000Z",
                mode = "penumbra-ab",
                status = "SUCCEEDED"
            };
            report.blocks.Add(CreateBlock(1, "A", true, 12.25, 12.0));

            string json = BenchmarkReportWriter.SerializeJson(report);
            string csv = BenchmarkReportWriter.CreateCsv(report);

            Assert.That(json, Does.Contain("\"schemaVersion\": 1"));
            Assert.That(json, Does.Contain("\"pairIndex\": 1"));
            Assert.That(csv, Does.Contain("12.250000"));
            Assert.That(csv, Does.Not.Contain("12,250000"));
        }

        private static BenchmarkBlockResult CreateBlock(
            int pairIndex,
            string slot,
            bool penumbraEnabled,
            double meanMs,
            double medianMs)
        {
            return new BenchmarkBlockResult
            {
                pairIndex = pairIndex,
                slot = slot,
                penumbraEnabled = penumbraEnabled,
                valid = true,
                frameTime = new BenchmarkMetricSummary
                {
                    hasSamples = true,
                    sampleCount = 1,
                    meanMs = meanMs,
                    medianMs = medianMs
                }
            };
        }
    }
}
