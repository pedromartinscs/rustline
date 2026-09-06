using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace Rustline.Editor.Diagnostics
{
    public enum SpikeClassification
    {
        PlayerLoopCpu,
        EditorLoop,
        RenderThread,
        PresentWait,
        Gc,
        Physics2D,
        RustlineScript,
        ProfilerOverhead,
        Mixed,
        Unclassified
    }

    public sealed class SpikeSampleNode
    {
        public SpikeSampleNode(string name, double durationMs)
        {
            Name = name ?? string.Empty;
            DurationMs = Math.Max(0.0, durationMs);
            Children = new List<SpikeSampleNode>();
        }

        public string Name { get; }
        public double DurationMs { get; }
        public List<SpikeSampleNode> Children { get; }

        public double SelfTimeMs
        {
            get
            {
                double childTime = 0.0;
                for (int i = 0; i < Children.Count; i++)
                {
                    childTime += Children[i].DurationMs;
                }

                return Math.Max(0.0, DurationMs - childTime);
            }
        }
    }

    public readonly struct SpikeContributor
    {
        public SpikeContributor(string name, double milliseconds, int calls = 1)
        {
            Name = name ?? string.Empty;
            Milliseconds = milliseconds;
            Calls = calls;
        }

        public string Name { get; }
        public double Milliseconds { get; }
        public int Calls { get; }
    }

    public sealed class SpikeFrameSummary
    {
        public int FrameIndex;
        public double MainThreadMs;
        public double EditorLoopMs;
        public double PlayerLoopMs;
        public double EditorOnlyMs;
        public double RenderThreadFrameSpanMs;
        public double RenderThreadActiveWorkMs;
        public double RenderLoopMs;
        public double RenderPresentFrameMs;
        public bool RenderThreadActiveWorkAvailable;
        public long GcAllocatedBytes;
        public int GcAllocSampleCount;
        public double GcTimeMs;
        public double Physics2DTimeMs;
        public double RustlineTimeMs;
        public double ProfilerOverheadMs;
        public double PresentWaitMs;
        public SpikeClassification Classification;
        public readonly List<SpikeContributor> PlayerLoopContributors = new List<SpikeContributor>();
        public readonly List<SpikeContributor> EditorLoopContributors = new List<SpikeContributor>();
        public readonly List<SpikeContributor> WaitContributors = new List<SpikeContributor>();
        public readonly List<SpikeContributor> GenericSynchronizationContributors =
            new List<SpikeContributor>();
        public readonly List<SpikeContributor> RenderThreadContributors = new List<SpikeContributor>();
        public readonly List<SpikeContributor> OtherThreads = new List<SpikeContributor>();
        public readonly Dictionary<string, double> RustlineMarkers =
            new Dictionary<string, double>(StringComparer.Ordinal);
        public readonly Dictionary<string, double> SelfTimes =
            new Dictionary<string, double>(StringComparer.Ordinal);
    }

    public readonly struct SpikeClassificationInput
    {
        public SpikeClassificationInput(
            double mainThreadMs,
            double editorOnlyMs,
            double playerLoopMs,
            double renderThreadMs,
            double presentWaitMs,
            double gcMs,
            double physics2DMs,
            double rustlineMs,
            double profilerOverheadMs,
            bool hasRenderThreadWorkEvidence = false)
        {
            MainThreadMs = mainThreadMs;
            EditorOnlyMs = editorOnlyMs;
            PlayerLoopMs = playerLoopMs;
            RenderThreadMs = renderThreadMs;
            PresentWaitMs = presentWaitMs;
            GcMs = gcMs;
            Physics2DMs = physics2DMs;
            RustlineMs = rustlineMs;
            ProfilerOverheadMs = profilerOverheadMs;
            HasRenderThreadWorkEvidence = hasRenderThreadWorkEvidence;
        }

        public double MainThreadMs { get; }
        public double EditorOnlyMs { get; }
        public double PlayerLoopMs { get; }
        public double RenderThreadMs { get; }
        public double PresentWaitMs { get; }
        public double GcMs { get; }
        public double Physics2DMs { get; }
        public double RustlineMs { get; }
        public double ProfilerOverheadMs { get; }
        public bool HasRenderThreadWorkEvidence { get; }
    }

    public readonly struct SpikeDistribution
    {
        public SpikeDistribution(double median, double p95, double p99, double maximum)
        {
            Median = median;
            P95 = p95;
            P99 = p99;
            Maximum = maximum;
        }

        public double Median { get; }
        public double P95 { get; }
        public double P99 { get; }
        public double Maximum { get; }
    }

    public sealed class SpikeReportContext
    {
        public string UnityVersion = "Unknown";
        public string ActiveScene = "Unknown";
        public string GraphicsApi = "Unknown";
        public string Gpu = "Unknown";
        public string ScreenResolution = "Unknown";
        public string LogicalResolution = "Unknown";
        public string QualityLevel = "Unknown";
        public int VSyncCount;
        public int TargetFrameRate;
        public string Penumbra = "Unknown";
        public string ProfilerTarget = "Unknown";
        public string FrameRange = "Unknown";
        public string DeepProfile = "Unknown";
        public string AllocationCallstacks = "Unknown";
    }

    public static class RustlineEditorSpikeAnalysis
    {
        public const double MeaningfulMinimumMs = 1.0;
        public const double SignificantFraction = 0.20;
        public const double DominantFraction = 0.35;

        public static SpikeDistribution CalculateDistribution(IReadOnlyList<double> values)
        {
            if (values == null || values.Count == 0)
            {
                return new SpikeDistribution(0.0, 0.0, 0.0, 0.0);
            }

            double[] sorted = values.OrderBy(value => value).ToArray();
            return new SpikeDistribution(
                Percentile(sorted, 0.50),
                Percentile(sorted, 0.95),
                Percentile(sorted, 0.99),
                sorted[sorted.Length - 1]);
        }

        public static List<SpikeFrameSummary> SelectWorstFrames(
            IEnumerable<SpikeFrameSummary> frames,
            int count)
        {
            if (frames == null || count <= 0)
            {
                return new List<SpikeFrameSummary>();
            }

            return frames
                .OrderByDescending(frame => frame.MainThreadMs)
                .ThenByDescending(frame => frame.RenderThreadActiveWorkAvailable
                    ? frame.RenderThreadActiveWorkMs
                    : -1.0)
                .ThenBy(frame => frame.FrameIndex)
                .Take(count)
                .ToList();
        }

        public static SpikeSampleNode FindFirst(SpikeSampleNode root, string sampleName)
        {
            if (root == null || string.IsNullOrEmpty(sampleName))
            {
                return null;
            }

            if (string.Equals(root.Name, sampleName, StringComparison.Ordinal))
            {
                return root;
            }

            for (int i = 0; i < root.Children.Count; i++)
            {
                SpikeSampleNode match = FindFirst(root.Children[i], sampleName);
                if (match != null)
                {
                    return match;
                }
            }

            return null;
        }

        public static double SumOutermostNamedSamples(SpikeSampleNode root, string sampleName)
        {
            if (root == null || string.IsNullOrEmpty(sampleName))
            {
                return 0.0;
            }

            if (string.Equals(root.Name, sampleName, StringComparison.Ordinal))
            {
                return root.DurationMs;
            }

            double total = 0.0;
            for (int i = 0; i < root.Children.Count; i++)
            {
                total += SumOutermostNamedSamples(root.Children[i], sampleName);
            }

            return total;
        }

        public static List<SpikeContributor> AggregateDirectChildren(SpikeSampleNode root)
        {
            Dictionary<string, (double time, int calls)> totals =
                new Dictionary<string, (double time, int calls)>(StringComparer.Ordinal);

            if (root != null)
            {
                for (int i = 0; i < root.Children.Count; i++)
                {
                    SpikeSampleNode child = root.Children[i];
                    totals.TryGetValue(child.Name, out (double time, int calls) value);
                    totals[child.Name] = (value.time + child.DurationMs, value.calls + 1);
                }
            }

            return totals
                .Select(pair => new SpikeContributor(pair.Key, pair.Value.time, pair.Value.calls))
                .OrderByDescending(contributor => contributor.Milliseconds)
                .ThenBy(contributor => contributor.Name, StringComparer.Ordinal)
                .ToList();
        }

        public static Dictionary<string, double> AggregateSelfTimes(SpikeSampleNode root)
        {
            Dictionary<string, double> totals =
                new Dictionary<string, double>(StringComparer.Ordinal);
            AccumulateSelfTimes(root, totals);
            return totals;
        }

        public static bool IsPresentWaitSample(string sampleName)
        {
            if (string.IsNullOrEmpty(sampleName))
            {
                return false;
            }

            return sampleName.IndexOf("WaitForPresent", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("WaitForLastPresent", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("WaitForTargetFPS", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("Gfx.PresentFrame", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool IsGenericSynchronizationSample(string sampleName)
        {
            return !string.IsNullOrEmpty(sampleName)
                && sampleName.IndexOf(
                    "Semaphore.WaitForSignal",
                    StringComparison.OrdinalIgnoreCase) >= 0;
        }

        public static bool TryDeriveRenderThreadActiveWork(
            SpikeSampleNode threadRoot,
            out double renderLoopMs,
            out double presentFrameMs,
            out double activeWorkMs)
        {
            renderLoopMs = 0.0;
            presentFrameMs = 0.0;
            activeWorkMs = 0.0;

            SpikeSampleNode renderLoop = FindFirst(threadRoot, "RenderLoop");
            if (renderLoop == null)
            {
                return false;
            }

            bool hasPresentFrame = ContainsNamedSample(renderLoop, "Gfx.PresentFrame");
            if (!hasPresentFrame)
            {
                return false;
            }

            renderLoopMs = renderLoop.DurationMs;
            presentFrameMs = SumOutermostNamedSamples(renderLoop, "Gfx.PresentFrame");
            if (presentFrameMs > renderLoopMs)
            {
                renderLoopMs = 0.0;
                presentFrameMs = 0.0;
                return false;
            }

            activeWorkMs = Math.Max(0.0, renderLoopMs - presentFrameMs);
            return true;
        }

        public static SpikeClassification Classify(SpikeClassificationInput input)
        {
            double basis = Math.Max(input.MainThreadMs, MeaningfulMinimumMs);
            double significant = Math.Max(MeaningfulMinimumMs, basis * SignificantFraction);
            double dominant = Math.Max(MeaningfulMinimumMs, basis * DominantFraction);

            var specific = new List<(SpikeClassification classification, double time)>
            {
                (SpikeClassification.Gc, input.GcMs),
                (SpikeClassification.ProfilerOverhead, input.ProfilerOverheadMs),
                (SpikeClassification.RustlineScript, input.RustlineMs),
                (SpikeClassification.Physics2D, input.Physics2DMs),
                (SpikeClassification.PresentWait, input.PresentWaitMs)
            };

            List<(SpikeClassification classification, double time)> significantSpecific =
                specific.Where(item => item.time >= significant).ToList();
            if (significantSpecific.Count >= 2)
            {
                return SpikeClassification.Mixed;
            }

            (SpikeClassification classification, double time) strongestSpecific = specific
                .OrderByDescending(item => item.time)
                .First();
            if (strongestSpecific.time >= dominant)
            {
                return strongestSpecific.classification;
            }

            bool editorSignificant = input.EditorOnlyMs >= significant;
            bool playerSignificant = input.PlayerLoopMs >= significant;
            if (editorSignificant && playerSignificant)
            {
                return SpikeClassification.Mixed;
            }

            if (input.EditorOnlyMs >= dominant)
            {
                return SpikeClassification.EditorLoop;
            }

            if (input.PlayerLoopMs >= dominant)
            {
                return SpikeClassification.PlayerLoopCpu;
            }

            if (input.HasRenderThreadWorkEvidence
                && input.RenderThreadMs >= Math.Max(2.0, basis * 0.75))
            {
                return SpikeClassification.RenderThread;
            }

            return SpikeClassification.Unclassified;
        }

        public static string ClassificationLabel(SpikeClassification classification)
        {
            switch (classification)
            {
                case SpikeClassification.PlayerLoopCpu: return "PLAYERLOOP_CPU";
                case SpikeClassification.EditorLoop: return "EDITORLOOP";
                case SpikeClassification.RenderThread: return "RENDER_THREAD";
                case SpikeClassification.PresentWait: return "PRESENT_WAIT";
                case SpikeClassification.Gc: return "GC";
                case SpikeClassification.Physics2D: return "PHYSICS2D";
                case SpikeClassification.RustlineScript: return "RUSTLINE_SCRIPT";
                case SpikeClassification.ProfilerOverhead: return "PROFILER_OVERHEAD";
                case SpikeClassification.Mixed: return "MIXED";
                default: return "UNCLASSIFIED";
            }
        }

        public static string BuildReport(
            SpikeReportContext context,
            IReadOnlyList<SpikeFrameSummary> frames,
            IReadOnlyList<string> customMarkerNames,
            int requestedFrameLimit)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (frames == null)
            {
                throw new ArgumentNullException(nameof(frames));
            }

            customMarkerNames = customMarkerNames ?? Array.Empty<string>();
            StringBuilder report = new StringBuilder(16384);
            report.AppendLine("RUSTLINE EDITOR SPIKE ANALYSIS");
            report.AppendLine("================================");
            AppendHeader(report, context);
            report.AppendLine();
            report.AppendLine($"Valid frames analyzed: {frames.Count} (latest bounded window, maximum {requestedFrameLimit})");

            if (frames.Count == 0)
            {
                report.AppendLine("No valid CPU profiler frames were available. Capture CPU Usage in the Unity Profiler, stop recording, then run the analyzer again.");
                return report.ToString();
            }

            List<double> mainTimes = frames.Select(frame => frame.MainThreadMs).ToList();
            SpikeDistribution distribution = CalculateDistribution(mainTimes);
            SpikeFrameSummary worst = SelectWorstFrames(frames, 1)[0];
            List<SpikeFrameSummary> worstFrames = SelectWorstFrames(frames, 10);
            SpikeFrameSummary medianFrame = frames
                .OrderBy(frame => Math.Abs(frame.MainThreadMs - distribution.Median))
                .ThenBy(frame => frame.FrameIndex)
                .First();

            report.AppendLine();
            report.AppendLine("MAIN THREAD DISTRIBUTION");
            report.AppendLine($"Median: {FormatMs(distribution.Median)} (representative captured frame {medianFrame.FrameIndex}: {FormatMs(medianFrame.MainThreadMs)})");
            report.AppendLine($"P95:    {FormatMs(distribution.P95)}");
            report.AppendLine($"P99:    {FormatMs(distribution.P99)}");
            report.AppendLine($"Max:    {FormatMs(distribution.Maximum)} (frame {worst.FrameIndex})");

            report.AppendLine();
            report.AppendLine("TOP 10 WORST FRAMES");
            report.AppendLine("Frame | Main ms | EditorLoop | PlayerLoop | Editor-only* | Render active* | GC bytes | Classification");
            foreach (SpikeFrameSummary frame in worstFrames)
            {
                report.AppendLine(string.Format(
                    CultureInfo.InvariantCulture,
                    "{0,5} | {1,7:F3} | {2,10:F3} | {3,10:F3} | {4,12:F3} | {5,14} | {6,8} | {7}",
                    frame.FrameIndex,
                    frame.MainThreadMs,
                    frame.EditorLoopMs,
                    frame.PlayerLoopMs,
                    frame.EditorOnlyMs,
                    frame.RenderThreadActiveWorkAvailable
                        ? frame.RenderThreadActiveWorkMs.ToString("F3", CultureInfo.InvariantCulture)
                        : "N/A",
                    frame.GcAllocatedBytes,
                    ClassificationLabel(frame.Classification)));
            }

            report.AppendLine();
            report.AppendLine("SEVERE FRAME DETAILS");
            foreach (SpikeFrameSummary frame in worstFrames)
            {
                AppendFrameDetails(report, frame, customMarkerNames);
            }

            report.AppendLine();
            report.AppendLine("CLASSIFICATION SUMMARY");
            foreach (IGrouping<SpikeClassification, SpikeFrameSummary> group in worstFrames
                .GroupBy(frame => frame.Classification)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => ClassificationLabel(group.Key), StringComparer.Ordinal))
            {
                report.AppendLine($"- {ClassificationLabel(group.Key)}: {group.Count()}/{worstFrames.Count} top frames");
            }

            report.AppendLine();
            report.AppendLine("RENDER THREAD AND GC SUMMARY");
            List<SpikeFrameSummary> renderEvidenceFrames = worstFrames
                .Where(frame => frame.RenderThreadActiveWorkAvailable)
                .ToList();
            report.AppendLine(renderEvidenceFrames.Count > 0
                ? $"- Maximum derived Render Thread active work in the top frames: {FormatMs(renderEvidenceFrames.Max(frame => frame.RenderThreadActiveWorkMs))}."
                : "- Derived Render Thread active work: unavailable in all top frames because the required RenderLoop + Gfx.PresentFrame hierarchy evidence was absent.");
            report.AppendLine($"- GC.Alloc metadata across top frames: {worstFrames.Sum(frame => frame.GcAllocatedBytes)} bytes in {worstFrames.Sum(frame => frame.GcAllocSampleCount)} samples.");
            report.AppendLine($"- Maximum GC self-time evidence in a top frame: {FormatMs(worstFrames.Max(frame => frame.GcTimeMs))}.");

            report.AppendLine();
            report.AppendLine("RECURRING DOMINANT SELF-TIME CONTRIBUTORS IN TOP FRAMES");
            AppendRecurringContributors(report, worstFrames);

            report.AppendLine();
            report.AppendLine("TYPICAL RUSTLINE MARKER TIMES");
            report.AppendLine("Median values use frames at or below the main-thread P95 and only frames where that marker was present.");
            foreach (string markerName in customMarkerNames)
            {
                List<double> values = frames
                    .Where(frame => frame.MainThreadMs <= distribution.P95 && frame.RustlineMarkers.ContainsKey(markerName))
                    .Select(frame => frame.RustlineMarkers[markerName])
                    .ToList();
                if (values.Count == 0)
                {
                    report.AppendLine($"- {markerName}: not present in the analyzed normal-frame set");
                }
                else
                {
                    report.AppendLine($"- {markerName}: median {FormatMs(CalculateDistribution(values).Median)} across {values.Count} frames");
                }
            }

            report.AppendLine();
            report.AppendLine("NEXT DIAGNOSTIC RECOMMENDATION");
            report.AppendLine(BuildRecommendation(worstFrames));

            report.AppendLine();
            report.AppendLine("METHOD AND CLASSIFICATION THRESHOLDS");
            report.AppendLine("- RawFrameDataView sample parent/child structure is preserved. Direct-child totals are listed only at one hierarchy level.");
            report.AppendLine("- Self time is inclusive sample time minus immediate-child inclusive time, clamped to zero. Aggregated self times are additive and do not include parent/child overlap.");
            report.AppendLine("- Editor-only* is EditorLoop inclusive time minus only outermost nested PlayerLoop samples. It is a conservative estimate, not a claim that all remaining work is avoidable.");
            report.AppendLine("- Raw per-thread frameTimeMs is retained only as frame-span metadata. It is not active Render Thread work and cannot trigger RENDER_THREAD.");
            report.AppendLine("- Render Thread active work is available only when the same captured hierarchy contains RenderLoop and nested Gfx.PresentFrame; the approximation is RenderLoop minus Gfx.PresentFrame.");
            report.AppendLine("- Main, render, and worker thread evidence overlaps in wall-clock time and is never added together.");
            report.AppendLine("- Percentiles use linear interpolation between adjacent sorted observations.");
            report.AppendLine("- Meaningful evidence is >= max(1.0 ms, 20% of main-thread time); dominant evidence is >= max(1.0 ms, 35%).");
            report.AppendLine("- MIXED requires at least two non-overlapping specific evidence groups at the meaningful threshold, or meaningful Editor-only and PlayerLoop time together.");
            report.AppendLine("- RENDER_THREAD requires derived hierarchy evidence, no dominant main-thread category, and active work >= max(2.0 ms, 75% of main-thread time).");
            report.AppendLine("- Semaphore.WaitForSignal self-time >= 0.1 ms is shown as generic synchronization evidence only. It never contributes to PRESENT_WAIT or another automatic root-cause classification; inspect Timeline to determine what was awaited.");
            report.AppendLine("- Classification is diagnostic triage, not proof of causality. Confirm candidates in the Profiler Timeline before changing product code.");
            report.AppendLine("- GC bytes come only from GC.Alloc sample metadata when the capture exposes it; zero can mean none recorded or metadata unavailable.");
            report.AppendLine("- This analyzer reads CPU capture data. Render-thread and present waits are indirect graphics evidence; it does not claim GPU duration or GPU causality.");
            report.AppendLine("- Deep Profile and allocation-callstack flags are current ProfilerDriver settings; Unity does not expose reliable original toggle state for every imported capture through these frame views.");
            return report.ToString();
        }

        private static double Percentile(double[] sorted, double percentile)
        {
            if (sorted.Length == 1)
            {
                return sorted[0];
            }

            double position = (sorted.Length - 1) * percentile;
            int lower = (int)Math.Floor(position);
            int upper = (int)Math.Ceiling(position);
            if (lower == upper)
            {
                return sorted[lower];
            }

            double fraction = position - lower;
            return sorted[lower] + ((sorted[upper] - sorted[lower]) * fraction);
        }

        private static void AccumulateSelfTimes(
            SpikeSampleNode node,
            Dictionary<string, double> totals)
        {
            if (node == null)
            {
                return;
            }

            totals.TryGetValue(node.Name, out double current);
            totals[node.Name] = current + node.SelfTimeMs;
            for (int i = 0; i < node.Children.Count; i++)
            {
                AccumulateSelfTimes(node.Children[i], totals);
            }
        }

        private static bool ContainsNamedSample(SpikeSampleNode node, string sampleName)
        {
            if (node == null)
            {
                return false;
            }

            if (string.Equals(node.Name, sampleName, StringComparison.Ordinal))
            {
                return true;
            }

            for (int i = 0; i < node.Children.Count; i++)
            {
                if (ContainsNamedSample(node.Children[i], sampleName))
                {
                    return true;
                }
            }

            return false;
        }

        private static void AppendHeader(StringBuilder report, SpikeReportContext context)
        {
            report.AppendLine($"Unity: {context.UnityVersion}");
            report.AppendLine($"Active scene: {context.ActiveScene}");
            report.AppendLine($"Graphics API: {context.GraphicsApi}");
            report.AppendLine($"GPU: {context.Gpu}");
            report.AppendLine($"Screen resolution: {context.ScreenResolution}");
            report.AppendLine($"Logical resolution: {context.LogicalResolution}");
            report.AppendLine($"Quality level: {context.QualityLevel}");
            report.AppendLine($"VSync: {context.VSyncCount}");
            report.AppendLine($"Application.targetFrameRate: {context.TargetFrameRate}");
            report.AppendLine($"Penumbra: {context.Penumbra}");
            report.AppendLine($"Profiler target at analysis time: {context.ProfilerTarget}");
            report.AppendLine($"Profiler frame range: {context.FrameRange}");
            report.AppendLine($"Deep Profile: {context.DeepProfile}");
            report.AppendLine($"Allocation call stacks: {context.AllocationCallstacks}");
        }

        private static void AppendFrameDetails(
            StringBuilder report,
            SpikeFrameSummary frame,
            IReadOnlyList<string> customMarkerNames)
        {
            report.AppendLine();
            report.AppendLine($"Frame {frame.FrameIndex} - {ClassificationLabel(frame.Classification)}");
            report.AppendLine($"  Main {FormatMs(frame.MainThreadMs)}; EditorLoop {FormatMs(frame.EditorLoopMs)}; PlayerLoop {FormatMs(frame.PlayerLoopMs)}; Editor-only {FormatMs(frame.EditorOnlyMs)}");
            report.AppendLine($"  Render Thread frame-span metadata: {FormatMs(frame.RenderThreadFrameSpanMs)} (not active work; never classification evidence)");
            report.AppendLine(frame.RenderThreadActiveWorkAvailable
                ? $"  Derived Render Thread active work: {FormatMs(frame.RenderThreadActiveWorkMs)} = RenderLoop {FormatMs(frame.RenderLoopMs)} - Gfx.PresentFrame {FormatMs(frame.RenderPresentFrameMs)}"
                : "  Derived Render Thread active work: unavailable (insufficient RenderLoop + nested Gfx.PresentFrame hierarchy evidence)");
            report.AppendLine($"  GC.Alloc metadata: {frame.GcAllocatedBytes} bytes across {frame.GcAllocSampleCount} samples; GC time evidence {FormatMs(frame.GcTimeMs)}");
            report.AppendLine($"  Classification reason: {BuildClassificationReason(frame)}");
            report.AppendLine($"  Rustline aggregate self-time evidence: {FormatMs(frame.RustlineTimeMs)}. Compare this with the {FormatMs(frame.MainThreadMs)} main-thread hitch before attributing the spike to game code.");
            double meaningful = Math.Max(MeaningfulMinimumMs, frame.MainThreadMs * SignificantFraction);
            report.AppendLine(frame.RustlineTimeMs < meaningful
                ? $"  Rustline marker evidence did not meet the documented meaningful threshold ({FormatMs(meaningful)}) for this hitch."
                : $"  Rustline marker evidence met the documented meaningful threshold ({FormatMs(meaningful)}); expand its captured children before inferring causality.");
            AppendContributors(report, "  PlayerLoop direct children (one-level inclusive totals)", frame.PlayerLoopContributors, 8);
            AppendContributors(report, "  EditorLoop direct children (one-level inclusive totals)", frame.EditorLoopContributors, 8);
            AppendContributors(report, "  Specific present/frame-pacing self-time markers", frame.WaitContributors, 8);
            AppendContributors(report, "  Generic synchronization evidence", frame.GenericSynchronizationContributors, 8);
            if (frame.GenericSynchronizationContributors.Count > 0)
            {
                report.AppendLine("    Inspect Timeline around generic synchronization samples; the analyzer does not infer which thread or resource was awaited.");
            }
            AppendContributors(report, "  Render Thread direct children", frame.RenderThreadContributors, 8);
            AppendContributors(report, "  Other thread frame-span metadata (not active work)", frame.OtherThreads, 8);
            report.AppendLine("  Rustline custom markers (summed inclusive occurrences):");
            foreach (string markerName in customMarkerNames)
            {
                report.AppendLine(frame.RustlineMarkers.TryGetValue(markerName, out double time)
                    ? $"    - {markerName}: {FormatMs(time)}"
                    : $"    - {markerName}: missing/not recorded");
            }
        }

        private static void AppendContributors(
            StringBuilder report,
            string heading,
            IReadOnlyList<SpikeContributor> contributors,
            int limit)
        {
            report.AppendLine(heading + ":");
            if (contributors == null || contributors.Count == 0)
            {
                report.AppendLine("    - none captured");
                return;
            }

            for (int i = 0; i < Math.Min(limit, contributors.Count); i++)
            {
                SpikeContributor contributor = contributors[i];
                report.AppendLine($"    - {contributor.Name}: {FormatMs(contributor.Milliseconds)} ({contributor.Calls} call(s))");
            }
        }

        private static void AppendRecurringContributors(
            StringBuilder report,
            IReadOnlyList<SpikeFrameSummary> frames)
        {
            Dictionary<string, (double total, int frames)> recurring =
                new Dictionary<string, (double total, int frames)>(StringComparer.Ordinal);
            foreach (SpikeFrameSummary frame in frames)
            {
                foreach (KeyValuePair<string, double> pair in frame.SelfTimes)
                {
                    if (pair.Value < 0.1)
                    {
                        continue;
                    }

                    recurring.TryGetValue(pair.Key, out (double total, int frames) value);
                    recurring[pair.Key] = (value.total + pair.Value, value.frames + 1);
                }
            }

            foreach (KeyValuePair<string, (double total, int frames)> pair in recurring
                .OrderByDescending(pair => pair.Value.total)
                .ThenBy(pair => pair.Key, StringComparer.Ordinal)
                .Take(20))
            {
                report.AppendLine($"- {pair.Key}: {FormatMs(pair.Value.total)} aggregate self time; present in {pair.Value.frames}/{frames.Count} top frames");
            }
        }

        private static string BuildClassificationReason(SpikeFrameSummary frame)
        {
            switch (frame.Classification)
            {
                case SpikeClassification.Gc:
                    return $"GC self-time evidence {FormatMs(frame.GcTimeMs)} dominated the main-thread frame.";
                case SpikeClassification.Physics2D:
                    return $"Physics2D self-time evidence {FormatMs(frame.Physics2DTimeMs)} dominated the main-thread frame.";
                case SpikeClassification.RustlineScript:
                    return $"Rustline marker self-time evidence {FormatMs(frame.RustlineTimeMs)} dominated the main-thread frame.";
                case SpikeClassification.ProfilerOverhead:
                    return $"Profiler collection self-time evidence {FormatMs(frame.ProfilerOverheadMs)} dominated; the capture may be perturbing the frame.";
                case SpikeClassification.PresentWait:
                    return $"Specific present/frame-pacing self-time evidence {FormatMs(frame.PresentWaitMs)} dominated the main thread.";
                case SpikeClassification.EditorLoop:
                    return $"Conservative Editor-only estimate {FormatMs(frame.EditorOnlyMs)} dominated while PlayerLoop was {FormatMs(frame.PlayerLoopMs)}.";
                case SpikeClassification.PlayerLoopCpu:
                    return $"PlayerLoop {FormatMs(frame.PlayerLoopMs)} dominated and no more specific captured category met the dominant threshold.";
                case SpikeClassification.RenderThread:
                    return $"Derived Render Thread active work {FormatMs(frame.RenderThreadActiveWorkMs)} was large relative to Main Thread {FormatMs(frame.MainThreadMs)}, with no dominant main-thread category.";
                case SpikeClassification.Mixed:
                    return "At least two documented evidence groups were significant; the capture does not support a single dominant cause.";
                default:
                    return "No documented category met the conservative threshold; inspect this frame in Profiler Timeline before drawing a conclusion.";
            }
        }

        private static string BuildRecommendation(IReadOnlyList<SpikeFrameSummary> worstFrames)
        {
            SpikeClassification dominant = worstFrames
                .GroupBy(frame => frame.Classification)
                .OrderByDescending(group => group.Count())
                .ThenBy(group => ClassificationLabel(group.Key), StringComparer.Ordinal)
                .First()
                .Key;

            switch (dominant)
            {
                case SpikeClassification.ProfilerOverhead:
                    return "Profiler overhead recurs. Repeat with only the CPU module enabled, or attach the Profiler to a Development Player, before treating the observed hitch as game cost.";
                case SpikeClassification.EditorLoop:
                    return "Open the reported frame indices in CPU Timeline and expand the dominant EditorLoop children. Correlate asset import, Inspector/UI repaint, compilation, and Editor callbacks before considering runtime changes.";
                case SpikeClassification.PresentWait:
                    return "Inspect the exact present or frame-pacing marker in Timeline, then cross-check a Development Player. A present wait or WaitForTargetFPS sample does not by itself prove GPU saturation.";
                case SpikeClassification.RenderThread:
                    return "Inspect RenderLoop and its non-present children in Timeline, then cross-check the same scenario in a Development Player and GPU Usage if supported.";
                case SpikeClassification.Gc:
                    return "Repeat a short capture with GC allocation call stacks enabled, then inspect the reported GC.Alloc/GC.Collect frames. Keep Deep Profile off unless the call stacks are still insufficient.";
                case SpikeClassification.RustlineScript:
                    return "Open the reported frame indices in Timeline and expand the dominant Rustline marker to its captured children before proposing a product optimization.";
                case SpikeClassification.Physics2D:
                    return "Inspect Physics2D.Simulate and its neighboring FixedUpdate samples in the reported frames; verify whether fixed-step catch-up is present before changing physics configuration.";
                case SpikeClassification.PlayerLoopCpu:
                    return "Open the reported frames in Timeline and expand the listed PlayerLoop direct children until a non-overlapping self-time contributor is identified.";
                default:
                    return "Inspect the reported frame indices in CPU Timeline and compare them with a second capture. Ambiguous or mixed evidence is not sufficient justification for a product change.";
            }
        }

        private static string FormatMs(double value)
        {
            return value.ToString("F3", CultureInfo.InvariantCulture) + " ms";
        }
    }
}
