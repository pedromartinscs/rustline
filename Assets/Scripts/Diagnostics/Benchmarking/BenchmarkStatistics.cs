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

            BenchmarkMetricSummary summary = new BenchmarkMetricSummary
            {
                sampleCount = count,
                hasSamples = count > 0
            };
            if (count == 0)
            {
                return summary;
            }

            double[] sorted = new double[count];
            Array.Copy(samples, sorted, count);
            Array.Sort(sorted);

            double sum = 0.0;
            for (int index = 0; index < count; index++)
            {
                sum += sorted[index];
            }

            double mean = sum / count;
            double squaredDeviationSum = 0.0;
            for (int index = 0; index < count; index++)
            {
                double difference = sorted[index] - mean;
                squaredDeviationSum += difference * difference;
            }

            summary.meanMs = mean;
            summary.medianMs = PercentileSorted(sorted, 0.5);
            summary.standardDeviationMs = Math.Sqrt(squaredDeviationSum / count);
            summary.minMs = sorted[0];
            summary.p90Ms = PercentileSorted(sorted, 0.90);
            summary.p95Ms = PercentileSorted(sorted, 0.95);
            summary.p99Ms = PercentileSorted(sorted, 0.99);
            summary.maxMs = sorted[count - 1];
            summary.equivalentFps = mean > 0.0 ? 1000.0 / mean : 0.0;
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
    }
}
