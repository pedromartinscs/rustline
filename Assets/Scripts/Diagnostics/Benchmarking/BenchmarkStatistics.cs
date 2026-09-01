using System;

namespace Rustline.Diagnostics.Benchmarking
{
    [Serializable]
    public sealed class BenchmarkMetricSummary
    {
        public bool hasSamples;
        public int sampleCount;
        public double meanMs;
        public double medianMs;
        public double standardDeviationMs;
        public double minMs;
        public double p90Ms;
        public double p95Ms;
        public double p99Ms;
        public double maxMs;
        public double equivalentFps;
    }

    [Serializable]
    public sealed class BenchmarkBlockBalancedSummary
    {
        public bool hasBlocks;
        public int validBlockCount;
        public double meanOfBlockMeansMs;
        public double medianOfBlockMeansMs;
        public double standardDeviationOfBlockMeansMs;
        public double minBlockMeanMs;
        public double maxBlockMeanMs;
    }

    [Serializable]
    public sealed class BenchmarkBlockStabilitySummary
    {
        public bool hasBlocks;
        public int validBlockCount;
        public double minBlockMeanMs;
        public double maxBlockMeanMs;
        public bool hasMaxMinRatio;
        public double maxMinRatio;
        public double meanBlockMeanMs;
        public double standardDeviationOfBlockMeansMs;
        public bool hasCoefficientOfVariation;
        public double coefficientOfVariation;
    }

    [Serializable]
    public sealed class BenchmarkAllocationSummary
    {
        public bool available;
        public string availabilityStatus;
        public int sampleCount;
        public long totalAllocatedBytes;
        public double meanAllocatedBytesPerFrame;
        public double medianAllocatedBytesPerFrame;
        public double p95AllocatedBytesPerFrame;
        public long maxAllocatedBytesPerFrame;
        public int nonZeroFrameCount;
        public double nonZeroFramePercent;
    }

    public static class BenchmarkStatistics
    {
        public static BenchmarkMetricSummary Calculate(double[] samples, int count)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (count < 0 || count > samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return Calculate(samples, count, new double[count]);
        }

        public static BenchmarkMetricSummary Calculate(
            double[] samples,
            int count,
            double[] scratch)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (count < 0 || count > samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (scratch == null || scratch.Length < count)
            {
                throw new ArgumentException("Scratch buffer is smaller than the sample count.", nameof(scratch));
            }

            BenchmarkMetricSummary summary = new BenchmarkMetricSummary
            {
                sampleCount = count,
                hasSamples = count > 0
            };
            if (count == 0)
            {
                return summary;
            }

            Array.Copy(samples, scratch, count);
            Array.Sort(scratch, 0, count);

            double sum = 0.0;
            for (int index = 0; index < count; index++)
            {
                sum += scratch[index];
            }

            double mean = sum / count;
            double squaredDeviationSum = 0.0;
            for (int index = 0; index < count; index++)
            {
                double difference = scratch[index] - mean;
                squaredDeviationSum += difference * difference;
            }

            summary.meanMs = mean;
            summary.medianMs = PercentileSorted(scratch, count, 0.5);
            summary.standardDeviationMs = Math.Sqrt(squaredDeviationSum / count);
            summary.minMs = scratch[0];
            summary.p90Ms = PercentileSorted(scratch, count, 0.90);
            summary.p95Ms = PercentileSorted(scratch, count, 0.95);
            summary.p99Ms = PercentileSorted(scratch, count, 0.99);
            summary.maxMs = scratch[count - 1];
            summary.equivalentFps = mean > 0.0 ? 1000.0 / mean : 0.0;
            return summary;
        }

        public static BenchmarkBlockBalancedSummary CalculateBlockBalanced(
            double[] blockMeans,
            int count)
        {
            BenchmarkMetricSummary values = Calculate(blockMeans, count);
            return new BenchmarkBlockBalancedSummary
            {
                hasBlocks = values.hasSamples,
                validBlockCount = values.sampleCount,
                meanOfBlockMeansMs = values.meanMs,
                medianOfBlockMeansMs = values.medianMs,
                standardDeviationOfBlockMeansMs = values.standardDeviationMs,
                minBlockMeanMs = values.minMs,
                maxBlockMeanMs = values.maxMs
            };
        }

        public static BenchmarkBlockStabilitySummary CalculateBlockStability(
            double[] blockMeans,
            int count)
        {
            BenchmarkMetricSummary values = Calculate(blockMeans, count);
            bool hasPositiveMinimum = values.hasSamples && values.minMs > 0.0;
            bool hasPositiveMean = values.hasSamples && values.meanMs > 0.0;
            return new BenchmarkBlockStabilitySummary
            {
                hasBlocks = values.hasSamples,
                validBlockCount = values.sampleCount,
                minBlockMeanMs = values.minMs,
                maxBlockMeanMs = values.maxMs,
                hasMaxMinRatio = hasPositiveMinimum,
                maxMinRatio = hasPositiveMinimum ? values.maxMs / values.minMs : 0.0,
                meanBlockMeanMs = values.meanMs,
                standardDeviationOfBlockMeansMs = values.standardDeviationMs,
                hasCoefficientOfVariation = hasPositiveMean,
                coefficientOfVariation = hasPositiveMean
                    ? values.standardDeviationMs / values.meanMs
                    : 0.0
            };
        }

        public static BenchmarkAllocationSummary CalculateAllocation(
            long[] samples,
            int count,
            bool available,
            string availabilityStatus)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (count < 0 || count > samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            return CalculateAllocation(
                samples,
                count,
                available,
                availabilityStatus,
                new long[count]);
        }

        public static BenchmarkAllocationSummary CalculateAllocation(
            long[] samples,
            int count,
            bool available,
            string availabilityStatus,
            long[] scratch)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (count < 0 || count > samples.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(count));
            }

            if (scratch == null || scratch.Length < count)
            {
                throw new ArgumentException("Scratch buffer is smaller than the sample count.", nameof(scratch));
            }

            BenchmarkAllocationSummary summary = new BenchmarkAllocationSummary
            {
                available = available,
                availabilityStatus = availabilityStatus,
                sampleCount = available ? count : 0
            };
            if (!available || count == 0)
            {
                return summary;
            }

            Array.Copy(samples, scratch, count);
            Array.Sort(scratch, 0, count);

            long total = 0;
            int nonZeroCount = 0;
            for (int index = 0; index < count; index++)
            {
                long value = Math.Max(0L, scratch[index]);
                total += value;
                if (value > 0L)
                {
                    nonZeroCount++;
                }
            }

            summary.totalAllocatedBytes = total;
            summary.meanAllocatedBytesPerFrame = (double)total / count;
            summary.medianAllocatedBytesPerFrame = PercentileSorted(scratch, count, 0.5);
            summary.p95AllocatedBytesPerFrame = PercentileSorted(scratch, count, 0.95);
            summary.maxAllocatedBytesPerFrame = Math.Max(0L, scratch[count - 1]);
            summary.nonZeroFrameCount = nonZeroCount;
            summary.nonZeroFramePercent = (double)nonZeroCount / count * 100.0;
            return summary;
        }

        public static double PercentileSorted(double[] sortedSamples, double percentile)
        {
            if (sortedSamples == null)
            {
                throw new ArgumentNullException(nameof(sortedSamples));
            }

            if (sortedSamples.Length == 0)
            {
                return 0.0;
            }

            double clamped = Math.Max(0.0, Math.Min(1.0, percentile));
            double position = (sortedSamples.Length - 1) * clamped;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            if (lowerIndex == upperIndex)
            {
                return sortedSamples[lowerIndex];
            }

            double fraction = position - lowerIndex;
            return sortedSamples[lowerIndex] +
                   (sortedSamples[upperIndex] - sortedSamples[lowerIndex]) * fraction;
        }

        private static double PercentileSorted(
            double[] sortedSamples,
            int count,
            double percentile)
        {
            if (count == 0)
            {
                return 0.0;
            }

            double clamped = Math.Max(0.0, Math.Min(1.0, percentile));
            double position = (count - 1) * clamped;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            if (lowerIndex == upperIndex)
            {
                return sortedSamples[lowerIndex];
            }

            double fraction = position - lowerIndex;
            return sortedSamples[lowerIndex] +
                   (sortedSamples[upperIndex] - sortedSamples[lowerIndex]) * fraction;
        }

        private static double PercentileSorted(
            long[] sortedSamples,
            int count,
            double percentile)
        {
            if (count == 0)
            {
                return 0.0;
            }

            double clamped = Math.Max(0.0, Math.Min(1.0, percentile));
            double position = (count - 1) * clamped;
            int lowerIndex = (int)Math.Floor(position);
            int upperIndex = (int)Math.Ceiling(position);
            if (lowerIndex == upperIndex)
            {
                return sortedSamples[lowerIndex];
            }

            double fraction = position - lowerIndex;
            return sortedSamples[lowerIndex] +
                   (sortedSamples[upperIndex] - sortedSamples[lowerIndex]) * fraction;
        }
    }
}
