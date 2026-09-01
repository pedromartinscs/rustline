using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Rustline.Diagnostics.Benchmarking
{
    [Serializable]
    public sealed class BenchmarkBuildMetadata
    {
        public string gitCommit = "unknown";
        public string gitDirtyState = "unknown";
    }

    [Serializable]
    public sealed class BenchmarkSystemMetadata
    {
        public string operatingSystem;
        public string processorType;
        public int processorCount;
        public int systemMemoryMb;
        public string graphicsDeviceName;
        public string graphicsDeviceVendor;
        public string graphicsDeviceType;
        public string graphicsDeviceVersion;
        public int graphicsMemoryMb;
    }

    [Serializable]
    public sealed class BenchmarkRuntimeMetadata
    {
        public int requestedPhysicalWidth;
        public int requestedPhysicalHeight;
        public int actualPhysicalWidth;
        public int actualPhysicalHeight;
        public int logicalWidth;
        public int logicalHeight;
        public int integerScale;
        public int outputOffsetX;
        public int outputOffsetY;
        public int outputWidth;
        public int outputHeight;
        public string qualityName;
        public int qualityIndex;
        public int vSyncCount;
        public int targetFrameRate;
        public int captureFrameRate;
        public string fullScreenMode;
    }

    [Serializable]
    public sealed class BenchmarkProtocolMetadata
    {
        public double warmupSeconds;
        public double settleSeconds;
        public double blockSeconds;
        public int pairCount;
        public string sequence;
        public string timingSource;
        public string percentileMethod;
        public string scenario;
    }

    [Serializable]
    public sealed class BenchmarkDiagnosticsMetadata
    {
        public bool diagnosticsRequested;
        public string allocationCounterName;
        public bool allocationCounterAvailable;
        public string allocationCounterStatus;
        public bool frameTimingFeatureAvailable;
        public string frameTimingStatus;
    }

    [Serializable]
    public sealed class BenchmarkFrameTimingSummary
    {
        public bool requested;
        public bool featureAvailable;
        public string status;
        public BenchmarkMetricSummary cpuFrameTime = new BenchmarkMetricSummary();
        public BenchmarkMetricSummary cpuMainThreadFrameTime = new BenchmarkMetricSummary();
        public BenchmarkMetricSummary cpuRenderThreadFrameTime = new BenchmarkMetricSummary();
        public BenchmarkMetricSummary cpuMainThreadPresentWaitTime = new BenchmarkMetricSummary();
        public BenchmarkMetricSummary gpuFrameTime = new BenchmarkMetricSummary();
    }

    [Serializable]
    public sealed class BenchmarkBlockResult
    {
        public int pairIndex;
        public string slot;
        public int orderIndex;
        public bool penumbraEnabled;
        public double elapsedBenchmarkSecondsAtStart;
        public double requestedDurationSeconds;
        public double measuredDurationSeconds;
        public bool valid;
        public string invalidReason;
        public BenchmarkMetricSummary frameTime = new BenchmarkMetricSummary();
        public BenchmarkAllocationSummary managedAllocation = new BenchmarkAllocationSummary();
        public long managedHeapBytesBefore;
        public long managedHeapBytesAfter;
        public BenchmarkFrameTimingSummary diagnosticFrameTiming = new BenchmarkFrameTimingSummary();
        public int gcGen0Collections;
        public int gcGen1Collections;
        public int gcGen2Collections;
    }

    [Serializable]
    public sealed class BenchmarkConditionAggregate
    {
        public string condition;
        public int blockCount;
        public BenchmarkMetricSummary pooledFrameTime = new BenchmarkMetricSummary();
        public BenchmarkBlockBalancedSummary blockBalancedFrameTime =
            new BenchmarkBlockBalancedSummary();
        public int gcGen0Collections;
        public int gcGen1Collections;
        public int gcGen2Collections;
    }

    [Serializable]
    public sealed class BenchmarkRunReport
    {
        public int schemaVersion = 2;
        public string utcTimestamp;
        public string mode;
        public string unityVersion;
        public bool developmentBuild;
        public string status;
        public string failureReason;
        public BenchmarkSystemMetadata system = new BenchmarkSystemMetadata();
        public BenchmarkBuildMetadata build = new BenchmarkBuildMetadata();
        public BenchmarkRuntimeMetadata runtime = new BenchmarkRuntimeMetadata();
        public BenchmarkProtocolMetadata protocol = new BenchmarkProtocolMetadata();
        public BenchmarkDiagnosticsMetadata diagnostics = new BenchmarkDiagnosticsMetadata();
        public List<BenchmarkBlockResult> blocks = new List<BenchmarkBlockResult>();
        public List<BenchmarkConditionAggregate> aggregates = new List<BenchmarkConditionAggregate>();
        public List<BenchmarkPairDelta> pairDeltas = new List<BenchmarkPairDelta>();
        public BenchmarkPairedSummary paired = new BenchmarkPairedSummary();
        public BenchmarkBlockStabilitySummary blockStability =
            new BenchmarkBlockStabilitySummary();
    }

    public static class BenchmarkReportWriter
    {
        public const string BuildMetadataFileName = "RustlineBenchmarkBuildMetadata.json";

        public static string SerializeJson(BenchmarkRunReport report)
        {
            return JsonUtility.ToJson(report, true) + Environment.NewLine;
        }

        public static string CreateCsv(BenchmarkRunReport report)
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine(
                "pair,slot,order,penumbra,valid,invalid_reason,start_elapsed_s,requested_s,measured_s,frames,mean_ms,median_ms,stddev_ms,min_ms,p90_ms,p95_ms,p99_ms,max_ms,equivalent_fps,gc0,gc1,gc2,allocation_available,allocation_samples,total_allocated_bytes,mean_allocated_bytes_per_frame,median_allocated_bytes_per_frame,p95_allocated_bytes_per_frame,max_allocated_bytes_per_frame,nonzero_allocation_frames,nonzero_allocation_percent,managed_heap_bytes_before,managed_heap_bytes_after,cpu_frame_timing_samples,cpu_frame_timing_mean_ms,gpu_frame_timing_samples,gpu_frame_timing_mean_ms");
            for (int index = 0; index < report.blocks.Count; index++)
            {
                BenchmarkBlockResult block = report.blocks[index];
                BenchmarkMetricSummary stats = block.frameTime;
                BenchmarkAllocationSummary allocation = block.managedAllocation;
                AppendCsv(builder, block.pairIndex);
                AppendCsv(builder, block.slot);
                AppendCsv(builder, block.orderIndex);
                AppendCsv(builder, block.penumbraEnabled ? "ON" : "OFF");
                AppendCsv(builder, block.valid ? "true" : "false");
                AppendCsv(builder, block.invalidReason);
                AppendCsv(builder, Format(block.elapsedBenchmarkSecondsAtStart));
                AppendCsv(builder, Format(block.requestedDurationSeconds));
                AppendCsv(builder, Format(block.measuredDurationSeconds));
                AppendCsv(builder, stats.sampleCount);
                AppendCsv(builder, Format(stats.meanMs));
                AppendCsv(builder, Format(stats.medianMs));
                AppendCsv(builder, Format(stats.standardDeviationMs));
                AppendCsv(builder, Format(stats.minMs));
                AppendCsv(builder, Format(stats.p90Ms));
                AppendCsv(builder, Format(stats.p95Ms));
                AppendCsv(builder, Format(stats.p99Ms));
                AppendCsv(builder, Format(stats.maxMs));
                AppendCsv(builder, Format(stats.equivalentFps));
                AppendCsv(builder, block.gcGen0Collections);
                AppendCsv(builder, block.gcGen1Collections);
                AppendCsv(builder, block.gcGen2Collections);
                AppendCsv(builder, allocation.available ? "true" : "false");
                AppendCsv(builder, allocation.sampleCount);
                AppendCsv(builder, allocation.totalAllocatedBytes);
                AppendCsv(builder, Format(allocation.meanAllocatedBytesPerFrame));
                AppendCsv(builder, Format(allocation.medianAllocatedBytesPerFrame));
                AppendCsv(builder, Format(allocation.p95AllocatedBytesPerFrame));
                AppendCsv(builder, allocation.maxAllocatedBytesPerFrame);
                AppendCsv(builder, allocation.nonZeroFrameCount);
                AppendCsv(builder, Format(allocation.nonZeroFramePercent));
                AppendCsv(builder, block.managedHeapBytesBefore);
                AppendCsv(builder, block.managedHeapBytesAfter);
                AppendCsv(builder, block.diagnosticFrameTiming.cpuFrameTime.sampleCount);
                AppendCsv(builder, Format(block.diagnosticFrameTiming.cpuFrameTime.meanMs));
                AppendCsv(builder, block.diagnosticFrameTiming.gpuFrameTime.sampleCount);
                AppendCsv(
                    builder,
                    Format(block.diagnosticFrameTiming.gpuFrameTime.meanMs),
                    endRow: true);
            }

            return builder.ToString();
        }

        public static string CreateSummary(BenchmarkRunReport report, string reportDirectory)
        {
            StringBuilder builder = new StringBuilder(4096);
            builder.AppendLine("RUSTLINE BENCHMARK v2");
            builder.Append("STATUS ").AppendLine(report.status);
            builder.Append("MODE ").AppendLine(report.mode);
            builder.Append("COMMIT ").Append(report.build.gitCommit)
                .Append(" (").Append(report.build.gitDirtyState).AppendLine(")");
            builder.Append("PHYSICAL ").Append(report.runtime.actualPhysicalWidth).Append('x')
                .AppendLine(report.runtime.actualPhysicalHeight.ToString(CultureInfo.InvariantCulture));
            builder.Append("LOGICAL ").Append(report.runtime.logicalWidth).Append('x')
                .Append(report.runtime.logicalHeight).Append(" SCALE ")
                .Append(report.runtime.integerScale).AppendLine("x");
            builder.Append("QUALITY ").Append(report.runtime.qualityName).Append(" [")
                .Append(report.runtime.qualityIndex).AppendLine("]");
            builder.Append("VSync ").Append(report.runtime.vSyncCount).Append(" Target ")
                .AppendLine(report.runtime.targetFrameRate.ToString(CultureInfo.InvariantCulture));

            for (int index = 0; index < report.aggregates.Count; index++)
            {
                BenchmarkConditionAggregate aggregate = report.aggregates[index];
                BenchmarkMetricSummary stats = aggregate.pooledFrameTime;
                BenchmarkBlockBalancedSummary balanced = aggregate.blockBalancedFrameTime;
                builder.AppendLine();
                builder.Append(aggregate.condition).AppendLine(":");
                builder.Append("POOLED FRAMES mean ").Append(Format(stats.meanMs)).Append(" ms | median ")
                    .Append(Format(stats.medianMs)).Append(" ms | p95 ")
                    .Append(Format(stats.p95Ms)).Append(" ms | p99 ")
                    .Append(Format(stats.p99Ms)).Append(" ms | FPS ")
                    .AppendLine(Format(stats.equivalentFps));
                builder.Append("BLOCK-BALANCED (").Append(balanced.validBlockCount)
                    .Append(" blocks) mean ").Append(Format(balanced.meanOfBlockMeansMs))
                    .Append(" ms | median ").Append(Format(balanced.medianOfBlockMeansMs))
                    .Append(" ms | stddev ")
                    .Append(Format(balanced.standardDeviationOfBlockMeansMs))
                    .Append(" ms | min ").Append(Format(balanced.minBlockMeanMs))
                    .Append(" ms | max ").Append(Format(balanced.maxBlockMeanMs))
                    .AppendLine(" ms");
                builder.Append("GC ").Append(aggregate.gcGen0Collections).Append('/')
                    .Append(aggregate.gcGen1Collections).Append('/')
                    .AppendLine(aggregate.gcGen2Collections.ToString(CultureInfo.InvariantCulture));
            }

            builder.AppendLine();
            builder.Append("CHRONOLOGICAL BLOCK MEANS [");
            for (int index = 0; index < report.blocks.Count; index++)
            {
                if (index > 0)
                {
                    builder.Append(", ");
                }

                BenchmarkBlockResult block = report.blocks[index];
                builder.Append(block.valid && block.frameTime.hasSamples
                    ? Format(block.frameTime.meanMs)
                    : "INVALID");
            }

            builder.AppendLine("] ms");
            BenchmarkBlockStabilitySummary stability = report.blockStability;
            builder.Append("BLOCK STABILITY min ").Append(Format(stability.minBlockMeanMs))
                .Append(" ms | max ").Append(Format(stability.maxBlockMeanMs))
                .Append(" ms | max/min ")
                .Append(stability.hasMaxMinRatio ? Format(stability.maxMinRatio) : "unavailable")
                .Append(" | mean ").Append(Format(stability.meanBlockMeanMs))
                .Append(" ms | stddev ")
                .Append(Format(stability.standardDeviationOfBlockMeansMs))
                .Append(" ms | CV ")
                .AppendLine(stability.hasCoefficientOfVariation
                    ? Format(stability.coefficientOfVariation)
                    : "unavailable");

            builder.Append("ALLOCATION COUNTER ")
                .Append(report.diagnostics.allocationCounterAvailable ? "AVAILABLE" : "UNAVAILABLE")
                .Append(" — ").AppendLine(report.diagnostics.allocationCounterStatus);
            builder.Append("OPTIONAL FRAME TIMING ")
                .Append(report.diagnostics.diagnosticsRequested ? "REQUESTED" : "NOT REQUESTED")
                .Append(" | feature ")
                .Append(report.diagnostics.frameTimingFeatureAvailable ? "AVAILABLE" : "UNAVAILABLE")
                .Append(" — ").AppendLine(report.diagnostics.frameTimingStatus);
            for (int index = 0; index < report.blocks.Count; index++)
            {
                BenchmarkBlockResult block = report.blocks[index];
                BenchmarkAllocationSummary allocation = block.managedAllocation;
                builder.Append("BLOCK ").Append(block.orderIndex).Append(" ALLOC ");
                if (!allocation.available)
                {
                    builder.AppendLine("unavailable");
                    continue;
                }

                builder.Append(Format(allocation.meanAllocatedBytesPerFrame))
                    .Append(" B/frame mean | p95 ")
                    .Append(Format(allocation.p95AllocatedBytesPerFrame))
                    .Append(" B | max ").Append(allocation.maxAllocatedBytesPerFrame)
                    .Append(" B | non-zero ").Append(Format(allocation.nonZeroFramePercent))
                    .AppendLine("%");
            }

            builder.AppendLine();
            builder.Append("PAIRED MEAN DELTAS (").Append(report.paired.calculation).AppendLine("):");
            builder.Append('[');
            bool wroteDelta = false;
            for (int index = 0; index < report.pairDeltas.Count; index++)
            {
                BenchmarkPairDelta delta = report.pairDeltas[index];
                if (!delta.valid)
                {
                    continue;
                }

                if (wroteDelta)
                {
                    builder.Append(", ");
                }

                builder.Append(Format(delta.meanDeltaMs));
                wroteDelta = true;
            }

            builder.AppendLine("] ms");
            builder.Append("PAIRED DELTA mean ").Append(Format(report.paired.meanOfMeanDeltasMs))
                .Append(" ms | median ").Append(Format(report.paired.medianOfMeanDeltasMs))
                .Append(" ms | spread ").Append(Format(report.paired.standardDeviationOfMeanDeltasMs))
                .AppendLine(" ms");
            builder.Append("ABSOLUTE PAIRED DELTA mean ")
                .Append(Format(report.paired.meanAbsoluteMeanDeltaMs))
                .Append(" ms | median ")
                .Append(Format(report.paired.medianAbsoluteMeanDeltaMs))
                .Append(" ms | max ")
                .Append(Format(report.paired.maxAbsoluteMeanDeltaMs))
                .AppendLine(" ms");
            if (report.paired.hasPairRelativeCostPercent)
            {
                builder.Append("PAIR-RELATIVE TO EACH OFF mean ")
                    .Append(Format(report.paired.meanPairRelativeCostPercent))
                    .Append("% | median ")
                    .Append(Format(report.paired.medianPairRelativeCostPercent))
                    .Append("% | stddev ")
                    .Append(Format(report.paired.standardDeviationOfPairRelativeCostPercent))
                    .Append("% | min ").Append(Format(report.paired.minPairRelativeCostPercent))
                    .Append("% | max ").Append(Format(report.paired.maxPairRelativeCostPercent))
                    .AppendLine("%");
            }

            if (!string.IsNullOrEmpty(report.failureReason))
            {
                builder.Append("ERROR ").AppendLine(report.failureReason);
            }

            builder.Append("REPORT ").AppendLine(reportDirectory);
            return builder.ToString();
        }

        public static string WriteAll(BenchmarkRunReport report, out string summary)
        {
            string directory = Path.Combine(Application.persistentDataPath, "RustlineBenchmarks");
            Directory.CreateDirectory(directory);
            string timestamp = DateTime.UtcNow.ToString("yyyyMMdd'T'HHmmssfff'Z'", CultureInfo.InvariantCulture);
            string stem = timestamp + "_rustline-benchmark_" + report.mode;
            string jsonPath = Path.Combine(directory, stem + ".json");
            string csvPath = Path.Combine(directory, stem + ".csv");
            string textPath = Path.Combine(directory, stem + ".txt");

            summary = CreateSummary(report, directory);
            File.WriteAllText(jsonPath, SerializeJson(report), new UTF8Encoding(false));
            File.WriteAllText(csvPath, CreateCsv(report), new UTF8Encoding(false));
            File.WriteAllText(textPath, summary, new UTF8Encoding(false));
            return directory;
        }

        private static string Format(double value)
        {
            return value.ToString("0.000000", CultureInfo.InvariantCulture);
        }

        private static void AppendCsv(StringBuilder builder, object value, bool endRow = false)
        {
            string text = value?.ToString() ?? string.Empty;
            bool quote = text.IndexOfAny(new[] { ',', '"', '\r', '\n' }) >= 0;
            if (quote)
            {
                builder.Append('"').Append(text.Replace("\"", "\"\"")).Append('"');
            }
            else
            {
                builder.Append(text);
            }

            builder.Append(endRow ? Environment.NewLine : ",");
        }
    }
}
