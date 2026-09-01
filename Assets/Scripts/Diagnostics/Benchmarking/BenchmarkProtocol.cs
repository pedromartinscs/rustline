using System;
using System.Collections.Generic;

namespace Rustline.Diagnostics.Benchmarking
{
    [Serializable]
    public sealed class BenchmarkBlockPlan
    {
        public int pairIndex;
        public string slot;
        public int orderIndex;
        public bool penumbraEnabled;
    }

    [Serializable]
    public sealed class BenchmarkPairDelta
    {
        public int pairIndex;
        public bool valid;
        public string invalidReason;
        public string calculation;
        public double meanDeltaMs;
        public double medianDeltaMs;
        public bool hasRelativeCostPercent;
        public double relativeCostPercent;
    }

    [Serializable]
    public sealed class BenchmarkPairedSummary
    {
        public bool valid;
        public string calculation;
        public int pairCount;
        public double meanOfMeanDeltasMs;
        public double medianOfMeanDeltasMs;
        public double standardDeviationOfMeanDeltasMs;
        public double meanOfMedianDeltasMs;
        public double medianOfMedianDeltasMs;
        public double meanAbsoluteMeanDeltaMs;
        public double medianAbsoluteMeanDeltaMs;
        public double maxAbsoluteMeanDeltaMs;
        public bool hasPairRelativeCostPercent;
        public int pairRelativeCostPercentCount;
        public double meanPairRelativeCostPercent;
        public double medianPairRelativeCostPercent;
        public double standardDeviationOfPairRelativeCostPercent;
        public double minPairRelativeCostPercent;
        public double maxPairRelativeCostPercent;
    }

    public static class BenchmarkProtocol
    {
        public static BenchmarkBlockPlan[] CreateBlockPlans(BenchmarkMode mode, int pairCount)
        {
            if (pairCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(pairCount));
            }

            BenchmarkBlockPlan[] plans = new BenchmarkBlockPlan[pairCount * 2];
            int orderIndex = 0;
            for (int pair = 0; pair < pairCount; pair++)
            {
                bool firstEnabled;
                bool secondEnabled;
                switch (mode)
                {
                    case BenchmarkMode.ControlOff:
                        firstEnabled = false;
                        secondEnabled = false;
                        break;
                    case BenchmarkMode.ControlOn:
                        firstEnabled = true;
                        secondEnabled = true;
                        break;
                    default:
                        firstEnabled = pair % 2 == 0;
                        secondEnabled = !firstEnabled;
                        break;
                }

                plans[orderIndex] = CreatePlan(pair + 1, "A", orderIndex + 1, firstEnabled);
                orderIndex++;
                plans[orderIndex] = CreatePlan(pair + 1, "B", orderIndex + 1, secondEnabled);
                orderIndex++;
            }

            return plans;
        }

        public static string DescribeSequence(BenchmarkBlockPlan[] plans)
        {
            if (plans == null || plans.Length == 0)
            {
                return string.Empty;
            }

            System.Text.StringBuilder builder = new System.Text.StringBuilder(plans.Length * 8);
            for (int index = 0; index < plans.Length; index++)
            {
                if (index > 0)
                {
                    builder.Append(index % 2 == 0 ? " | " : ">");
                }

                builder.Append(plans[index].penumbraEnabled ? "ON" : "OFF");
            }

            return builder.ToString();
        }

        private static BenchmarkBlockPlan CreatePlan(
            int pairIndex,
            string slot,
            int orderIndex,
            bool penumbraEnabled)
        {
            return new BenchmarkBlockPlan
            {
                pairIndex = pairIndex,
                slot = slot,
                orderIndex = orderIndex,
                penumbraEnabled = penumbraEnabled
            };
        }
    }

    public static class BenchmarkAnalysis
    {
        public static List<BenchmarkPairDelta> CalculatePairDeltas(
            IList<BenchmarkBlockResult> blocks,
            BenchmarkMode mode,
            int expectedPairCount)
        {
            List<BenchmarkPairDelta> deltas = new List<BenchmarkPairDelta>(expectedPairCount);
            for (int pairIndex = 1; pairIndex <= expectedPairCount; pairIndex++)
            {
                BenchmarkBlockResult first = null;
                BenchmarkBlockResult second = null;
                for (int blockIndex = 0; blockIndex < blocks.Count; blockIndex++)
                {
                    BenchmarkBlockResult block = blocks[blockIndex];
                    if (block.pairIndex != pairIndex)
                    {
                        continue;
                    }

                    if (first == null)
                    {
                        first = block;
                    }
                    else
                    {
                        second = block;
                        break;
                    }
                }

                BenchmarkPairDelta delta = new BenchmarkPairDelta
                {
                    pairIndex = pairIndex,
                    calculation = mode == BenchmarkMode.PenumbraAb ? "ON - OFF" : "A - B"
                };
                if (first == null || second == null)
                {
                    delta.invalidReason = "Pair is incomplete.";
                    deltas.Add(delta);
                    continue;
                }

                if (!first.valid || !second.valid ||
                    !first.frameTime.hasSamples || !second.frameTime.hasSamples)
                {
                    delta.invalidReason = "Pair contains an invalid or empty block.";
                    deltas.Add(delta);
                    continue;
                }

                BenchmarkBlockResult minuend;
                BenchmarkBlockResult subtrahend;
                if (mode == BenchmarkMode.PenumbraAb)
                {
                    minuend = first.penumbraEnabled ? first : second;
                    subtrahend = first.penumbraEnabled ? second : first;
                    if (!minuend.penumbraEnabled || subtrahend.penumbraEnabled)
                    {
                        delta.invalidReason = "Pair does not contain one ON and one OFF block.";
                        deltas.Add(delta);
                        continue;
                    }
                }
                else
                {
                    minuend = string.Equals(first.slot, "A", StringComparison.Ordinal) ? first : second;
                    subtrahend = ReferenceEquals(minuend, first) ? second : first;
                }

                delta.valid = true;
                delta.meanDeltaMs = minuend.frameTime.meanMs - subtrahend.frameTime.meanMs;
                delta.medianDeltaMs = minuend.frameTime.medianMs - subtrahend.frameTime.medianMs;
                if (mode == BenchmarkMode.PenumbraAb &&
                    IsFinitePositive(subtrahend.frameTime.meanMs))
                {
                    delta.hasRelativeCostPercent = true;
                    delta.relativeCostPercent =
                        delta.meanDeltaMs / subtrahend.frameTime.meanMs * 100.0;
                }

                deltas.Add(delta);
            }

            return deltas;
        }

        public static BenchmarkPairedSummary SummarizePairDeltas(
            IList<BenchmarkPairDelta> deltas)
        {
            int validCount = 0;
            for (int index = 0; index < deltas.Count; index++)
            {
                if (deltas[index].valid)
                {
                    validCount++;
                }
            }

            BenchmarkPairedSummary summary = new BenchmarkPairedSummary
            {
                valid = validCount > 0,
                pairCount = validCount,
                calculation = deltas.Count > 0 ? deltas[0].calculation : string.Empty
            };
            if (validCount == 0)
            {
                return summary;
            }

            double[] meanDeltas = new double[validCount];
            double[] medianDeltas = new double[validCount];
            double[] absoluteMeanDeltas = new double[validCount];
            int relativeCount = 0;
            for (int index = 0; index < deltas.Count; index++)
            {
                if (deltas[index].valid && deltas[index].hasRelativeCostPercent)
                {
                    relativeCount++;
                }
            }

            double[] relativePercentages = new double[relativeCount];
            int destinationIndex = 0;
            int relativeDestinationIndex = 0;
            for (int index = 0; index < deltas.Count; index++)
            {
                if (!deltas[index].valid)
                {
                    continue;
                }

                meanDeltas[destinationIndex] = deltas[index].meanDeltaMs;
                medianDeltas[destinationIndex] = deltas[index].medianDeltaMs;
                absoluteMeanDeltas[destinationIndex] = Math.Abs(deltas[index].meanDeltaMs);
                if (deltas[index].hasRelativeCostPercent)
                {
                    relativePercentages[relativeDestinationIndex] = deltas[index].relativeCostPercent;
                    relativeDestinationIndex++;
                }

                destinationIndex++;
            }

            BenchmarkMetricSummary meanSummary = BenchmarkStatistics.Calculate(meanDeltas, validCount);
            BenchmarkMetricSummary medianSummary = BenchmarkStatistics.Calculate(medianDeltas, validCount);
            BenchmarkMetricSummary absoluteSummary =
                BenchmarkStatistics.Calculate(absoluteMeanDeltas, validCount);
            summary.meanOfMeanDeltasMs = meanSummary.meanMs;
            summary.medianOfMeanDeltasMs = meanSummary.medianMs;
            summary.standardDeviationOfMeanDeltasMs = meanSummary.standardDeviationMs;
            summary.meanOfMedianDeltasMs = medianSummary.meanMs;
            summary.medianOfMedianDeltasMs = medianSummary.medianMs;
            summary.meanAbsoluteMeanDeltaMs = absoluteSummary.meanMs;
            summary.medianAbsoluteMeanDeltaMs = absoluteSummary.medianMs;
            summary.maxAbsoluteMeanDeltaMs = absoluteSummary.maxMs;
            summary.hasPairRelativeCostPercent = relativeCount > 0;
            summary.pairRelativeCostPercentCount = relativeCount;
            if (relativeCount > 0)
            {
                BenchmarkMetricSummary relativeSummary =
                    BenchmarkStatistics.Calculate(relativePercentages, relativeCount);
                summary.meanPairRelativeCostPercent = relativeSummary.meanMs;
                summary.medianPairRelativeCostPercent = relativeSummary.medianMs;
                summary.standardDeviationOfPairRelativeCostPercent =
                    relativeSummary.standardDeviationMs;
                summary.minPairRelativeCostPercent = relativeSummary.minMs;
                summary.maxPairRelativeCostPercent = relativeSummary.maxMs;
            }

            return summary;
        }

        private static bool IsFinitePositive(double value)
        {
            return value > 0.0 && !double.IsNaN(value) && !double.IsInfinity(value);
        }
    }
}
