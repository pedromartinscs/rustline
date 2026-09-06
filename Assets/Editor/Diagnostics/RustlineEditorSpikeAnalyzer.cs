using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEditor;
using UnityEditor.Profiling;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace Rustline.Editor.Diagnostics
{
    public static class RustlineEditorSpikeAnalyzer
    {
        private const int MaximumFrames = 500;
        private const int MaximumThreadIndices = 256;
        private const int DetailedFrameCount = 10;
        private const double RelevantWorkerThreadMs = 1.0;

        public static readonly string[] RustlineMarkerNames =
        {
            "Rustline.Player.Aim",
            "Rustline.Player.Motor",
            "Rustline.Player.GroundProbe",
            "Rustline.Presentation.NativePixelUpdate",
            "Rustline.Presentation.Longwatch",
            "Rustline.Presentation.Animator"
        };

        [MenuItem("Rustline/Diagnostics/Analyze Recent Editor Spikes")]
        public static void AnalyzeRecentEditorSpikes()
        {
            try
            {
                List<int> frameIndices = CollectRecentFrameIndices(MaximumFrames);
                SpikeReportContext context = CreateReportContext(frameIndices);
                List<SpikeFrameSummary> frames = AnalyzeMainThreads(frameIndices);
                List<SpikeFrameSummary> worstFrames =
                    RustlineEditorSpikeAnalysis.SelectWorstFrames(frames, DetailedFrameCount);

                for (int i = 0; i < worstFrames.Count; i++)
                {
                    AnalyzeOtherThreads(worstFrames[i]);
                    Classify(worstFrames[i]);
                }

                string report = RustlineEditorSpikeAnalysis.BuildReport(
                    context,
                    frames,
                    RustlineMarkerNames,
                    MaximumFrames);
                string reportPath = WriteReport(report);
                EditorGUIUtility.systemCopyBuffer = BuildClipboardSummary(frames, reportPath);

                Debug.Log($"Rustline Editor spike report written to {reportPath}. A compact summary was copied to the clipboard.");
                EditorUtility.DisplayDialog(
                    "Rustline Editor Spike Analyzer",
                    frames.Count > 0
                        ? $"Analyzed {frames.Count} valid CPU frames.\n\nReport:\n{reportPath}\n\nA compact summary was copied to the clipboard."
                        : $"No valid CPU frames were available. Capture CPU Usage in the Profiler and run this command again.\n\nReport:\n{reportPath}",
                    "OK");
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                EditorUtility.DisplayDialog(
                    "Rustline Editor Spike Analyzer",
                    "Analysis failed. See the Console for the exception. No runtime or project settings were changed.",
                    "OK");
            }
        }

        private static List<int> CollectRecentFrameIndices(int maximumFrames)
        {
            List<int> newestFirst = new List<int>(maximumFrames);
            int frameIndex = ProfilerDriver.lastFrameIndex;
            int firstFrameIndex = ProfilerDriver.firstFrameIndex;

            while (frameIndex >= firstFrameIndex && frameIndex >= 0 && newestFirst.Count < maximumFrames)
            {
                using (RawFrameDataView mainThread = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
                {
                    if (mainThread.valid && mainThread.sampleCount > 0)
                    {
                        newestFirst.Add(frameIndex);
                    }
                }

                int previousFrameIndex = ProfilerDriver.GetPreviousFrameIndex(frameIndex);
                if (previousFrameIndex < 0 || previousFrameIndex >= frameIndex)
                {
                    break;
                }

                frameIndex = previousFrameIndex;
            }

            newestFirst.Reverse();
            return newestFirst;
        }

        private static List<SpikeFrameSummary> AnalyzeMainThreads(IReadOnlyList<int> frameIndices)
        {
            List<SpikeFrameSummary> frames = new List<SpikeFrameSummary>(frameIndices.Count);
            for (int i = 0; i < frameIndices.Count; i++)
            {
                int frameIndex = frameIndices[i];
                using (RawFrameDataView raw = ProfilerDriver.GetRawFrameDataView(frameIndex, 0))
                {
                    if (!raw.valid || raw.sampleCount == 0)
                    {
                        continue;
                    }

                    SpikeFrameSummary frame = new SpikeFrameSummary
                    {
                        FrameIndex = frameIndex,
                        MainThreadMs = raw.frameTimeMs
                    };

                    SpikeSampleNode root = ReadSampleTree(raw, frame);
                    if (root == null)
                    {
                        continue;
                    }

                    // HierarchyFrameDataView is a supported Unity hierarchy projection. Raw data
                    // remains authoritative, but the hierarchy root is a safe duration fallback for
                    // captures whose raw frameTimeMs is unavailable.
                    double hierarchyRootTime = ReadHierarchyRootTime(frameIndex, 0);
                    if (frame.MainThreadMs <= 0.0)
                    {
                        frame.MainThreadMs = hierarchyRootTime;
                    }

                    AnalyzeMainHierarchy(frame, root);
                    Classify(frame);
                    frames.Add(frame);
                }
            }

            return frames;
        }

        private static SpikeSampleNode ReadSampleTree(
            RawFrameDataView raw,
            SpikeFrameSummary frame)
        {
            if (raw.sampleCount <= 0)
            {
                return null;
            }

            SpikeSampleNode threadRoot = new SpikeSampleNode(
                string.IsNullOrEmpty(raw.threadName) ? "Thread" : raw.threadName,
                raw.frameTimeMs);
            int sampleIndex = 0;
            while (sampleIndex < raw.sampleCount)
            {
                threadRoot.Children.Add(ReadSample(raw, ref sampleIndex, frame));
            }

            if (threadRoot.DurationMs <= 0.0)
            {
                double childrenTotal = 0.0;
                for (int i = 0; i < threadRoot.Children.Count; i++)
                {
                    childrenTotal += threadRoot.Children[i].DurationMs;
                }

                return new SpikeSampleNodeWithChildren(threadRoot.Name, childrenTotal, threadRoot.Children).Node;
            }

            return threadRoot;
        }

        private static SpikeSampleNode ReadSample(
            RawFrameDataView raw,
            ref int sampleIndex,
            SpikeFrameSummary frame)
        {
            int currentIndex = sampleIndex++;
            string sampleName = raw.GetSampleName(currentIndex) ?? string.Empty;
            SpikeSampleNode node = new SpikeSampleNode(sampleName, raw.GetSampleTimeMs(currentIndex));

            if (frame != null && string.Equals(sampleName, "GC.Alloc", StringComparison.Ordinal))
            {
                frame.GcAllocSampleCount++;
                if (raw.GetSampleMetadataCount(currentIndex) > 0)
                {
                    try
                    {
                        long allocatedBytes = raw.GetSampleMetadataAsLong(currentIndex, 0);
                        if (allocatedBytes > 0)
                        {
                            frame.GcAllocatedBytes += allocatedBytes;
                        }
                    }
                    catch (Exception exception) when (
                        exception is InvalidOperationException || exception is ArgumentException)
                    {
                        // Some captures expose GC.Alloc without typed allocation metadata.
                    }
                }
            }

            int childCount = raw.GetSampleChildrenCount(currentIndex);
            for (int childIndex = 0; childIndex < childCount && sampleIndex < raw.sampleCount; childIndex++)
            {
                node.Children.Add(ReadSample(raw, ref sampleIndex, frame));
            }

            return node;
        }

        private static double ReadHierarchyRootTime(int frameIndex, int threadIndex)
        {
            using (HierarchyFrameDataView hierarchy = ProfilerDriver.GetHierarchyFrameDataView(
                frameIndex,
                threadIndex,
                HierarchyFrameDataView.ViewModes.Default,
                HierarchyFrameDataView.columnTotalTime,
                false))
            {
                if (!hierarchy.valid)
                {
                    return 0.0;
                }

                int rootId = hierarchy.GetRootItemID();
                return rootId == HierarchyFrameDataView.invalidSampleId
                    ? 0.0
                    : hierarchy.GetItemColumnDataAsDouble(rootId, HierarchyFrameDataView.columnTotalTime);
            }
        }

        private static void AnalyzeMainHierarchy(SpikeFrameSummary frame, SpikeSampleNode root)
        {
            SpikeSampleNode editorLoop = RustlineEditorSpikeAnalysis.FindFirst(root, "EditorLoop");
            SpikeSampleNode playerLoop = RustlineEditorSpikeAnalysis.FindFirst(
                editorLoop ?? root,
                "PlayerLoop");

            frame.EditorLoopMs = editorLoop?.DurationMs ?? 0.0;
            frame.PlayerLoopMs = editorLoop != null
                ? RustlineEditorSpikeAnalysis.SumOutermostNamedSamples(editorLoop, "PlayerLoop")
                : playerLoop?.DurationMs ?? 0.0;
            frame.EditorOnlyMs = editorLoop != null
                ? Math.Max(0.0, frame.EditorLoopMs - frame.PlayerLoopMs)
                : 0.0;

            frame.EditorLoopContributors.AddRange(
                RustlineEditorSpikeAnalysis.AggregateDirectChildren(editorLoop));
            frame.PlayerLoopContributors.AddRange(
                RustlineEditorSpikeAnalysis.AggregateDirectChildren(playerLoop));

            Dictionary<string, double> selfTimes =
                RustlineEditorSpikeAnalysis.AggregateSelfTimes(root);
            foreach (KeyValuePair<string, double> pair in selfTimes)
            {
                frame.SelfTimes[pair.Key] = pair.Value;
                if (IsGcSample(pair.Key))
                {
                    frame.GcTimeMs += pair.Value;
                }

                if (IsPhysics2DSample(pair.Key))
                {
                    frame.Physics2DTimeMs += pair.Value;
                }

                if (IsProfilerOverheadSample(pair.Key))
                {
                    frame.ProfilerOverheadMs += pair.Value;
                }

                if (IsPresentWaitSample(pair.Key))
                {
                    frame.PresentWaitMs += pair.Value;
                    frame.WaitContributors.Add(new SpikeContributor(pair.Key, pair.Value));
                }

                if (pair.Key.StartsWith("Rustline.", StringComparison.Ordinal))
                {
                    frame.RustlineTimeMs += pair.Value;
                }
            }

            frame.WaitContributors.Sort((left, right) =>
                right.Milliseconds.CompareTo(left.Milliseconds));

            for (int i = 0; i < RustlineMarkerNames.Length; i++)
            {
                string markerName = RustlineMarkerNames[i];
                double markerTime = SumNamedSamples(root, markerName);
                if (markerTime > 0.0 || ContainsNamedSample(root, markerName))
                {
                    frame.RustlineMarkers[markerName] = markerTime;
                }
            }
        }

        private static void AnalyzeOtherThreads(SpikeFrameSummary frame)
        {
            for (int threadIndex = 1; threadIndex < MaximumThreadIndices; threadIndex++)
            {
                using (RawFrameDataView raw = ProfilerDriver.GetRawFrameDataView(frame.FrameIndex, threadIndex))
                {
                    if (!raw.valid)
                    {
                        break;
                    }

                    string threadName = string.IsNullOrEmpty(raw.threadName)
                        ? $"Thread {threadIndex}"
                        : raw.threadName;
                    if (threadName.IndexOf("Render Thread", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        frame.RenderThreadMs = Math.Max(frame.RenderThreadMs, raw.frameTimeMs);
                        SpikeSampleNode renderRoot = ReadSampleTree(raw, null);
                        SpikeSampleNode renderContributorRoot = renderRoot != null
                            && renderRoot.Children.Count == 1
                                ? renderRoot.Children[0]
                                : renderRoot;
                        frame.RenderThreadContributors.AddRange(
                            RustlineEditorSpikeAnalysis.AggregateDirectChildren(renderContributorRoot));
                    }
                    else if (raw.frameTimeMs >= RelevantWorkerThreadMs)
                    {
                        frame.OtherThreads.Add(new SpikeContributor(threadName, raw.frameTimeMs));
                    }
                }
            }

            frame.RenderThreadContributors.Sort((left, right) =>
                right.Milliseconds.CompareTo(left.Milliseconds));
            frame.OtherThreads.Sort((left, right) =>
                right.Milliseconds.CompareTo(left.Milliseconds));
        }

        private static void Classify(SpikeFrameSummary frame)
        {
            frame.Classification = RustlineEditorSpikeAnalysis.Classify(
                new SpikeClassificationInput(
                    frame.MainThreadMs,
                    frame.EditorOnlyMs,
                    frame.PlayerLoopMs,
                    frame.RenderThreadMs,
                    frame.PresentWaitMs,
                    frame.GcTimeMs,
                    frame.Physics2DTimeMs,
                    frame.RustlineTimeMs,
                    frame.ProfilerOverheadMs));
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

        private static double SumNamedSamples(SpikeSampleNode node, string sampleName)
        {
            if (node == null)
            {
                return 0.0;
            }

            double total = string.Equals(node.Name, sampleName, StringComparison.Ordinal)
                ? node.DurationMs
                : 0.0;
            for (int i = 0; i < node.Children.Count; i++)
            {
                total += SumNamedSamples(node.Children[i], sampleName);
            }

            return total;
        }

        private static bool IsGcSample(string sampleName)
        {
            return sampleName.StartsWith("GC.", StringComparison.OrdinalIgnoreCase)
                || sampleName.IndexOf("GarbageCollect", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPhysics2DSample(string sampleName)
        {
            return sampleName.IndexOf("Physics2D", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsProfilerOverheadSample(string sampleName)
        {
            return sampleName.StartsWith("Profiler.", StringComparison.OrdinalIgnoreCase)
                || sampleName.IndexOf("ProfilerWindow", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("ProfileEditor", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static bool IsPresentWaitSample(string sampleName)
        {
            return sampleName.IndexOf("WaitForPresent", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("WaitForTargetFPS", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("PresentFrame", StringComparison.OrdinalIgnoreCase) >= 0
                || sampleName.IndexOf("Semaphore.WaitForSignal", StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private static SpikeReportContext CreateReportContext(IReadOnlyList<int> frameIndices)
        {
            MonoBehaviour presentation = Object.FindObjectsByType<MonoBehaviour>(
                    FindObjectsInactive.Include)
                .FirstOrDefault(component => string.Equals(
                    component.GetType().FullName,
                    "Rustline.Presentation.NativePixelPresentation",
                    StringComparison.Ordinal));
            string logicalResolution = ReadLogicalResolution(presentation);
            string[] qualityNames = QualitySettings.names;
            int qualityIndex = QualitySettings.GetQualityLevel();

            return new SpikeReportContext
            {
                UnityVersion = Application.unityVersion,
                ActiveScene = SceneManager.GetActiveScene().path,
                GraphicsApi = SystemInfo.graphicsDeviceType.ToString(),
                Gpu = SystemInfo.graphicsDeviceName,
                ScreenResolution = $"{Screen.width} x {Screen.height}",
                LogicalResolution = logicalResolution,
                QualityLevel = qualityIndex >= 0 && qualityIndex < qualityNames.Length
                    ? $"{qualityNames[qualityIndex]} (index {qualityIndex})"
                    : qualityIndex.ToString(CultureInfo.InvariantCulture),
                VSyncCount = QualitySettings.vSyncCount,
                TargetFrameRate = Application.targetFrameRate,
                Penumbra = presentation == null
                    ? "Unavailable (no NativePixelPresentation found)"
                    : ReadBooleanProperty(presentation, "PenumbraEnabled", out bool penumbraEnabled)
                        ? penumbraEnabled ? "ON" : "OFF"
                        : "Unavailable (PenumbraEnabled was not readable)",
                ProfilerTarget = DescribeProfilerTarget(),
                FrameRange = frameIndices.Count == 0
                    ? $"No valid frames (Profiler buffer {ProfilerDriver.firstFrameIndex}..{ProfilerDriver.lastFrameIndex})"
                    : $"{frameIndices[0]}..{frameIndices[frameIndices.Count - 1]} from Profiler buffer {ProfilerDriver.firstFrameIndex}..{ProfilerDriver.lastFrameIndex}",
                DeepProfile = ProfilerDriver.deepProfiling
                    ? "Enabled (Profiler setting at analysis time)"
                    : "Disabled (Profiler setting at analysis time)",
                AllocationCallstacks = IsGcAllocationCallstackRecordingEnabled()
                    ? "Enabled for GC.Alloc (Profiler setting at analysis time)"
                    : "Disabled for GC.Alloc (Profiler setting at analysis time)"
            };
        }

        private static bool IsGcAllocationCallstackRecordingEnabled()
        {
            return (((int)ProfilerDriver.memoryRecordMode) & ((int)ProfilerMemoryRecordMode.GCAlloc)) != 0;
        }

        private static string DescribeProfilerTarget()
        {
            try
            {
                string identifier = ProfilerDriver.GetConnectionIdentifier(
                    ProfilerDriver.connectedProfiler);
                return string.IsNullOrEmpty(identifier)
                    ? $"Unknown (connection id {ProfilerDriver.connectedProfiler})"
                    : identifier;
            }
            catch (ArgumentException)
            {
                return $"Unknown (connection id {ProfilerDriver.connectedProfiler})";
            }
        }

        private static string ReadLogicalResolution(MonoBehaviour presentation)
        {
            if (presentation == null)
            {
                return "Unavailable (no NativePixelPresentation found)";
            }

            PropertyInfo viewportProperty = presentation.GetType().GetProperty(
                "Viewport",
                BindingFlags.Instance | BindingFlags.Public);
            object viewport = viewportProperty?.GetValue(presentation);
            if (viewport == null
                || !ReadIntProperty(viewport, "LogicalWidth", out int width)
                || !ReadIntProperty(viewport, "LogicalHeight", out int height)
                || !ReadIntProperty(viewport, "IntegerScale", out int scale)
                || width <= 0
                || height <= 0)
            {
                return "Unavailable (native-pixel presentation was not initialized at analysis time)";
            }

            return $"{width} x {height} ({scale}x integer scale)";
        }

        private static bool ReadIntProperty(object target, string propertyName, out int value)
        {
            value = 0;
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(int))
            {
                return false;
            }

            value = (int)property.GetValue(target);
            return true;
        }

        private static bool ReadBooleanProperty(object target, string propertyName, out bool value)
        {
            value = false;
            PropertyInfo property = target.GetType().GetProperty(
                propertyName,
                BindingFlags.Instance | BindingFlags.Public);
            if (property == null || property.PropertyType != typeof(bool))
            {
                return false;
            }

            value = (bool)property.GetValue(target);
            return true;
        }

        private static string WriteReport(string report)
        {
            string projectRoot = Directory.GetParent(Application.dataPath)?.FullName
                ?? throw new InvalidOperationException("Unable to resolve the Unity project root.");
            string reportDirectory = Path.Combine(projectRoot, "Temp", "RustlineDiagnostics");
            Directory.CreateDirectory(reportDirectory);
            string reportPath = Path.Combine(reportDirectory, "editor-spike-report.txt");
            File.WriteAllText(reportPath, report, new UTF8Encoding(false));
            return reportPath;
        }

        private static string BuildClipboardSummary(
            IReadOnlyList<SpikeFrameSummary> frames,
            string reportPath)
        {
            if (frames.Count == 0)
            {
                return $"Rustline Editor spikes: no valid CPU frames. Report: {reportPath}";
            }

            SpikeDistribution distribution = RustlineEditorSpikeAnalysis.CalculateDistribution(
                frames.Select(frame => frame.MainThreadMs).ToList());
            SpikeFrameSummary worst =
                RustlineEditorSpikeAnalysis.SelectWorstFrames(frames, 1)[0];
            return string.Format(
                CultureInfo.InvariantCulture,
                "Rustline Editor spikes | frames {0} | median {1:F3} ms | p95 {2:F3} ms | p99 {3:F3} ms | max {4:F3} ms frame {5} [{6}] | {7}",
                frames.Count,
                distribution.Median,
                distribution.P95,
                distribution.P99,
                distribution.Maximum,
                worst.FrameIndex,
                RustlineEditorSpikeAnalysis.ClassificationLabel(worst.Classification),
                reportPath);
        }

        private sealed class SpikeSampleNodeWithChildren
        {
            public SpikeSampleNodeWithChildren(
                string name,
                double durationMs,
                IReadOnlyList<SpikeSampleNode> children)
            {
                Node = new SpikeSampleNode(name, durationMs);
                for (int i = 0; i < children.Count; i++)
                {
                    Node.Children.Add(children[i]);
                }
            }

            public SpikeSampleNode Node { get; }
        }
    }
}
