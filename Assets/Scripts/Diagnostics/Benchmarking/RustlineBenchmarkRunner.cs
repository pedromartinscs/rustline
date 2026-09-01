#if RUSTLINE_BENCHMARK
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Rustline.Presentation;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rustline.Diagnostics.Benchmarking
{
    [DefaultExecutionOrder(-10000)]
    public sealed class RustlineBenchmarkRunner : MonoBehaviour
    {
        private const int RequestedWidth = 1920;
        private const int RequestedHeight = 1080;
        private const int RequiredLogicalWidth = 1072;
        private const int RequiredLogicalHeight = 1072;
        private const int RequiredScale = 1;
        private const double ConfigurationTimeoutSeconds = 30.0;
        private const int MaximumExpectedFramesPerSecond = 4096;
        private const string GcAllocatedInFrameMarker = "GC Allocated In Frame";

        private static readonly double TimestampToSeconds = 1.0 / Stopwatch.Frequency;
        private static readonly Rect SummaryRect = new Rect(20f, 20f, 1200f, 680f);

        private BenchmarkOptions _options;
        private BenchmarkRunReport _report;
        private NativePixelPresentation _presentation;
        private int _requiredQualityIndex;
        private long _benchmarkStartTimestamp;
        private bool _focusLostDuringBlock;
        private bool _completed;
        private string _summary;
        private GUIStyle _summaryStyle;
        private string _abortReason;
        private ProfilerRecorder _gcAllocatedInFrameRecorder;
        private bool _gcAllocationRecorderAvailable;
        private string _gcAllocationRecorderStatus;
        private bool _frameTimingFeatureAvailable;
        private readonly FrameTiming[] _latestFrameTiming = new FrameTiming[1];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void InstallWhenRequested()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            if (!ContainsActivationFlag(arguments))
            {
                return;
            }

            GameObject runnerObject = new GameObject("Rustline Standalone Benchmark")
            {
                hideFlags = HideFlags.DontSave
            };
            DontDestroyOnLoad(runnerObject);
            runnerObject.AddComponent<RustlineBenchmarkRunner>();
        }

        private static bool ContainsActivationFlag(string[] arguments)
        {
            for (int index = 0; index < arguments.Length; index++)
            {
                if (string.Equals(
                        arguments[index],
                        BenchmarkOptions.ActivationFlag,
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }

        private IEnumerator Start()
        {
            if (!BenchmarkOptions.TryParse(
                    Environment.GetCommandLineArgs(),
                    out _options,
                    out string parseError))
            {
                InitializeReport(new BenchmarkOptions());
                yield return Finish("FAILED", parseError);
                yield break;
            }

            InitializeReport(_options);
            if (!_options.requested)
            {
                yield break;
            }

            if (!TryApplyNormalizedConfiguration(out string configurationError))
            {
                yield return Finish("FAILED", configurationError);
                yield break;
            }

            yield return WaitForExactPresentationConfiguration();
            if (!string.IsNullOrEmpty(_abortReason))
            {
                yield return Finish("FAILED", _abortReason);
                yield break;
            }

            DisableLegacyPerformanceHud();
            InitializeMeasurementDiagnostics();
            CaptureRuntimeMetadata();
            BenchmarkBlockPlan[] plans = BenchmarkProtocol.CreateBlockPlans(
                _options.mode,
                _options.pairCount);
            _report.protocol.sequence = BenchmarkProtocol.DescribeSequence(plans);

            int sampleCapacity = Math.Max(
                64,
                (int)Math.Ceiling(_options.blockSeconds * MaximumExpectedFramesPerSecond) + 16);
            double[] sampleBuffer = new double[sampleCapacity];
            long[] allocationBuffer = new long[sampleCapacity];
            double[] statisticsScratchBuffer = new double[sampleCapacity];
            long[] allocationScratchBuffer = new long[sampleCapacity];
            BenchmarkDiagnosticBuffers diagnosticBuffers =
                new BenchmarkDiagnosticBuffers(sampleCapacity, _options.diagnostics);
            int aggregateCapacity = Math.Max(
                64,
                Math.Min(
                    1000000,
                    (int)Math.Ceiling(
                        _options.blockSeconds * _options.pairCount * 512.0)));
            List<double> firstConditionSamples = new List<double>(aggregateCapacity);
            List<double> secondConditionSamples = new List<double>(aggregateCapacity);
            AggregateCounters firstCounters = new AggregateCounters();
            AggregateCounters secondCounters = new AggregateCounters();

            _benchmarkStartTimestamp = Stopwatch.GetTimestamp();
            yield return RunWarmup();
            if (!string.IsNullOrEmpty(_abortReason))
            {
                yield return Finish("FAILED", _abortReason);
                yield break;
            }

            for (int index = 0; index < plans.Length; index++)
            {
                BenchmarkBlockPlan plan = plans[index];
                _presentation.SetPenumbraEnabled(plan.penumbraEnabled);
                yield return WaitUnmeasured(_options.settleSeconds, "settling");
                if (!string.IsNullOrEmpty(_abortReason))
                {
                    yield return Finish("FAILED", _abortReason);
                    yield break;
                }

                BenchmarkBlockMeasurement measurement = new BenchmarkBlockMeasurement(
                    plan,
                    _options.blockSeconds,
                    ElapsedBenchmarkSeconds());
                yield return MeasureBlock(
                    measurement,
                    sampleBuffer,
                    allocationBuffer,
                    diagnosticBuffers);
                BenchmarkBlockResult block = measurement.CreateResult(
                    sampleBuffer,
                    allocationBuffer,
                    diagnosticBuffers,
                    statisticsScratchBuffer,
                    allocationScratchBuffer);
                _report.blocks.Add(block);

                if (block.valid)
                {
                    bool useFirstCondition = _options.mode == BenchmarkMode.PenumbraAb
                        ? block.penumbraEnabled
                        : string.Equals(block.slot, "A", StringComparison.Ordinal);
                    List<double> destination = useFirstCondition
                        ? firstConditionSamples
                        : secondConditionSamples;
                    for (int sampleIndex = 0; sampleIndex < measurement.sampleCount; sampleIndex++)
                    {
                        destination.Add(sampleBuffer[sampleIndex]);
                    }

                    AggregateCounters counters = useFirstCondition
                        ? firstCounters
                        : secondCounters;
                    counters.Add(block);
                }
                else
                {
                    _abortReason = block.invalidReason;
                    break;
                }
            }

            BuildAggregates(firstConditionSamples, secondConditionSamples, firstCounters, secondCounters);
            _report.pairDeltas = BenchmarkAnalysis.CalculatePairDeltas(
                _report.blocks,
                _options.mode,
                _options.pairCount);
            _report.paired = BenchmarkAnalysis.SummarizePairDeltas(_report.pairDeltas);
            BuildBlockStability();
            bool completedAllBlocks =
                _report.blocks.Count == plans.Length && string.IsNullOrEmpty(_abortReason);
            yield return Finish(
                completedAllBlocks ? "SUCCEEDED" : "FAILED",
                completedAllBlocks ? null : _abortReason);
        }

        private void InitializeReport(BenchmarkOptions options)
        {
            _report = new BenchmarkRunReport
            {
                utcTimestamp = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                mode = BenchmarkOptions.ToModeName(options.mode),
                unityVersion = Application.unityVersion,
                developmentBuild = UnityEngine.Debug.isDebugBuild
            };
            _report.system = CaptureSystemMetadata();
            _report.build = LoadBuildMetadata(options);
            _report.protocol = new BenchmarkProtocolMetadata
            {
                warmupSeconds = options.warmupSeconds,
                settleSeconds = options.settleSeconds,
                blockSeconds = options.blockSeconds,
                pairCount = options.pairCount,
                timingSource = "System.Diagnostics.Stopwatch.GetTimestamp (monotonic)",
                percentileMethod = "Linear interpolation at (sampleCount - 1) * percentile",
                scenario = "MovementLab static presentation benchmark"
            };
            _report.diagnostics = new BenchmarkDiagnosticsMetadata
            {
                diagnosticsRequested = options.diagnostics,
                allocationCounterName = GcAllocatedInFrameMarker,
                allocationCounterStatus = "Not initialized.",
                frameTimingStatus = options.diagnostics
                    ? "Not initialized."
                    : "Not requested. Use --benchmark-diagnostics for supplementary CPU/GPU frame timing."
            };
        }

        private bool TryApplyNormalizedConfiguration(out string error)
        {
            string[] qualityNames = QualitySettings.names;
            _requiredQualityIndex = BenchmarkQuality.FindLevelIndex(
                qualityNames,
                BenchmarkOptions.DefaultQualityName);
            if (_requiredQualityIndex < 0)
            {
                error =
                    $"Required benchmark quality '{BenchmarkOptions.DefaultQualityName}' does not exist.";
                return false;
            }

            QualitySettings.SetQualityLevel(_requiredQualityIndex, applyExpensiveChanges: true);
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = -1;
            Time.captureFramerate = 0;
            Screen.SetResolution(RequestedWidth, RequestedHeight, FullScreenMode.Windowed);
            error = null;
            return true;
        }

        private IEnumerator WaitForExactPresentationConfiguration()
        {
            long start = Stopwatch.GetTimestamp();
            while ((Stopwatch.GetTimestamp() - start) * TimestampToSeconds <
                   ConfigurationTimeoutSeconds)
            {
                if (_presentation == null)
                {
                    _presentation = FindAnyObjectByType<NativePixelPresentation>();
                }

                if (_presentation != null &&
                    Application.isFocused &&
                    IsExactPresentationConfiguration())
                {
                    yield break;
                }

                yield return null;
            }

            NativePixelViewport viewport = _presentation != null
                ? _presentation.Viewport
                : default;
            _abortReason =
                $"Timed out waiting for PHYSICAL {RequestedWidth}x{RequestedHeight}, " +
                $"LOGICAL {RequiredLogicalWidth}x{RequiredLogicalHeight}, SCALE {RequiredScale}x. " +
                $"Observed PHYSICAL {Screen.width}x{Screen.height}, " +
                $"LOGICAL {viewport.LogicalWidth}x{viewport.LogicalHeight}, " +
                $"SCALE {viewport.IntegerScale}x, focus {Application.isFocused}.";
        }

        private IEnumerator RunWarmup()
        {
            double segmentSeconds = _options.warmupSeconds / 3.0;
            bool[] states = { true, false, true };
            for (int index = 0; index < states.Length; index++)
            {
                _presentation.SetPenumbraEnabled(states[index]);
                yield return WaitUnmeasured(segmentSeconds, "warm-up");
                if (!string.IsNullOrEmpty(_abortReason))
                {
                    yield break;
                }
            }
        }

        private IEnumerator WaitUnmeasured(double seconds, string phase)
        {
            _focusLostDuringBlock = false;
            long start = Stopwatch.GetTimestamp();
            while ((Stopwatch.GetTimestamp() - start) * TimestampToSeconds < seconds)
            {
                if (_focusLostDuringBlock)
                {
                    _abortReason = $"Benchmark invalid during {phase}: application focus was lost.";
                    yield break;
                }

                if (!TryValidateStableConfiguration(out string invalidReason))
                {
                    _abortReason = $"Benchmark invalid during {phase}: {invalidReason}";
                    yield break;
                }

                yield return null;
            }
        }

        private IEnumerator MeasureBlock(
            BenchmarkBlockMeasurement measurement,
            double[] sampleBuffer,
            long[] allocationBuffer,
            BenchmarkDiagnosticBuffers diagnosticBuffers)
        {
            _focusLostDuringBlock = false;
            measurement.managedAllocationAvailable = _gcAllocationRecorderAvailable;
            measurement.managedAllocationStatus = _gcAllocationRecorderStatus;
            measurement.frameTimingRequested = _options.diagnostics;
            measurement.frameTimingFeatureAvailable = _frameTimingFeatureAvailable;
            measurement.managedHeapBytesBefore = GC.GetTotalMemory(false);
            int gc0Before = GC.CollectionCount(0);
            int gc1Before = GC.CollectionCount(1);
            int gc2Before = GC.CollectionCount(2);
            long start = Stopwatch.GetTimestamp();
            long previous = start;

            while ((previous - start) * TimestampToSeconds < measurement.requestedDurationSeconds)
            {
                if (_options.diagnostics && _frameTimingFeatureAvailable)
                {
                    FrameTimingManager.CaptureFrameTimings();
                }

                yield return null;
                long current = Stopwatch.GetTimestamp();
                if (measurement.sampleCount >= sampleBuffer.Length)
                {
                    measurement.invalidReason =
                        $"Frame sample capacity {sampleBuffer.Length} was exceeded; no samples were discarded.";
                    break;
                }

                int sampleIndex = measurement.sampleCount;
                sampleBuffer[sampleIndex] =
                    (current - previous) * TimestampToSeconds * 1000.0;
                RecordManagedAllocation(measurement, allocationBuffer, sampleIndex);
                RecordDiagnosticFrameTiming(measurement, diagnosticBuffers);
                measurement.sampleCount++;
                previous = current;

                if (_focusLostDuringBlock)
                {
                    measurement.invalidReason = "Application focus was lost during the measured block.";
                    break;
                }

                if (!TryValidateStableConfiguration(out string invalidReason))
                {
                    measurement.invalidReason = invalidReason;
                    break;
                }
            }

            measurement.measuredDurationSeconds = (previous - start) * TimestampToSeconds;
            measurement.managedHeapBytesAfter = GC.GetTotalMemory(false);
            measurement.gcGen0Collections = GC.CollectionCount(0) - gc0Before;
            measurement.gcGen1Collections = GC.CollectionCount(1) - gc1Before;
            measurement.gcGen2Collections = GC.CollectionCount(2) - gc2Before;
        }

        private void RecordManagedAllocation(
            BenchmarkBlockMeasurement measurement,
            long[] allocationBuffer,
            int sampleIndex)
        {
            if (!measurement.managedAllocationAvailable)
            {
                return;
            }

            if (!_gcAllocatedInFrameRecorder.Valid)
            {
                measurement.managedAllocationAvailable = false;
                measurement.managedAllocationStatus =
                    "ProfilerRecorder became invalid during the measured block.";
                return;
            }

            allocationBuffer[sampleIndex] = Math.Max(0L, _gcAllocatedInFrameRecorder.LastValue);
        }

        private void RecordDiagnosticFrameTiming(
            BenchmarkBlockMeasurement measurement,
            BenchmarkDiagnosticBuffers buffers)
        {
            if (!measurement.frameTimingRequested || !measurement.frameTimingFeatureAvailable)
            {
                return;
            }

            uint timingCount = FrameTimingManager.GetLatestTimings(1, _latestFrameTiming);
            if (timingCount == 0)
            {
                return;
            }

            FrameTiming timing = _latestFrameTiming[0];
            buffers.AddCpuFrameTime(timing.cpuFrameTime, ref measurement.cpuFrameTimingCount);
            buffers.AddCpuMainThreadFrameTime(
                timing.cpuMainThreadFrameTime,
                ref measurement.cpuMainThreadTimingCount);
            buffers.AddCpuRenderThreadFrameTime(
                timing.cpuRenderThreadFrameTime,
                ref measurement.cpuRenderThreadTimingCount);
            buffers.AddCpuMainThreadPresentWaitTime(
                timing.cpuMainThreadPresentWaitTime,
                ref measurement.cpuMainThreadPresentWaitTimingCount);
            buffers.AddGpuFrameTime(timing.gpuFrameTime, ref measurement.gpuFrameTimingCount);
        }

        private bool TryValidateStableConfiguration(out string reason)
        {
            if (_presentation == null)
            {
                reason = "NativePixelPresentation disappeared.";
                return false;
            }

            if (!Application.isFocused)
            {
                reason = "Application does not have focus.";
                return false;
            }

            if (!IsExactPresentationConfiguration())
            {
                reason =
                    $"Viewport changed to PHYSICAL {Screen.width}x{Screen.height}, " +
                    $"LOGICAL {_presentation.Viewport.LogicalWidth}x{_presentation.Viewport.LogicalHeight}, " +
                    $"SCALE {_presentation.Viewport.IntegerScale}x.";
                return false;
            }

            if (QualitySettings.GetQualityLevel() != _requiredQualityIndex)
            {
                reason =
                    $"Quality changed to index {QualitySettings.GetQualityLevel()}; " +
                    $"required {_requiredQualityIndex} ({BenchmarkOptions.DefaultQualityName}).";
                return false;
            }

            if (QualitySettings.vSyncCount != 0)
            {
                reason = $"VSync changed to {QualitySettings.vSyncCount}; required 0.";
                return false;
            }

            if (Application.targetFrameRate != -1)
            {
                reason =
                    $"Application.targetFrameRate changed to {Application.targetFrameRate}; required -1.";
                return false;
            }

            if (Time.captureFramerate != 0)
            {
                reason = $"Time.captureFramerate changed to {Time.captureFramerate}; required 0.";
                return false;
            }

            reason = null;
            return true;
        }

        private bool IsExactPresentationConfiguration()
        {
            NativePixelViewport viewport = _presentation.Viewport;
            return Screen.width == RequestedWidth &&
                   Screen.height == RequestedHeight &&
                   Screen.fullScreenMode == FullScreenMode.Windowed &&
                   viewport.PhysicalWidth == RequestedWidth &&
                   viewport.PhysicalHeight == RequestedHeight &&
                   viewport.LogicalWidth == RequiredLogicalWidth &&
                   viewport.LogicalHeight == RequiredLogicalHeight &&
                   viewport.IntegerScale == RequiredScale &&
                   viewport.OutputOffsetX == 424 &&
                   viewport.OutputOffsetY == 4 &&
                   viewport.OutputWidth == RequiredLogicalWidth &&
                   viewport.OutputHeight == RequiredLogicalHeight;
        }

        private void BuildAggregates(
            List<double> firstSamples,
            List<double> secondSamples,
            AggregateCounters firstCounters,
            AggregateCounters secondCounters)
        {
            string firstName = _options.mode == BenchmarkMode.PenumbraAb ? "ON" : "A";
            string secondName = _options.mode == BenchmarkMode.PenumbraAb ? "OFF" : "B";
            _report.aggregates.Add(CreateAggregate(firstName, firstSamples, firstCounters));
            _report.aggregates.Add(CreateAggregate(secondName, secondSamples, secondCounters));
        }

        private static BenchmarkConditionAggregate CreateAggregate(
            string condition,
            List<double> samples,
            AggregateCounters counters)
        {
            double[] values = samples.ToArray();
            double[] blockMeans = counters.blockMeans.ToArray();
            return new BenchmarkConditionAggregate
            {
                condition = condition,
                blockCount = counters.blockCount,
                pooledFrameTime = BenchmarkStatistics.Calculate(values, values.Length),
                blockBalancedFrameTime = BenchmarkStatistics.CalculateBlockBalanced(
                    blockMeans,
                    blockMeans.Length),
                gcGen0Collections = counters.gc0,
                gcGen1Collections = counters.gc1,
                gcGen2Collections = counters.gc2
            };
        }

        private void BuildBlockStability()
        {
            double[] chronologicalBlockMeans = new double[_report.blocks.Count];
            int validCount = 0;
            for (int index = 0; index < _report.blocks.Count; index++)
            {
                BenchmarkBlockResult block = _report.blocks[index];
                if (!block.valid || !block.frameTime.hasSamples)
                {
                    continue;
                }

                chronologicalBlockMeans[validCount] = block.frameTime.meanMs;
                validCount++;
            }

            _report.blockStability = BenchmarkStatistics.CalculateBlockStability(
                chronologicalBlockMeans,
                validCount);
        }

        private void InitializeMeasurementDiagnostics()
        {
            try
            {
                _gcAllocatedInFrameRecorder = ProfilerRecorder.StartNew(
                    ProfilerCategory.Memory,
                    GcAllocatedInFrameMarker,
                    1);
                _gcAllocationRecorderAvailable = _gcAllocatedInFrameRecorder.Valid;
                _gcAllocationRecorderStatus = _gcAllocationRecorderAvailable
                    ? "Available through Unity.Profiling.ProfilerRecorder."
                    : "ProfilerRecorder marker is unavailable on this player/platform.";
            }
            catch (Exception exception)
            {
                _gcAllocationRecorderAvailable = false;
                _gcAllocationRecorderStatus =
                    "ProfilerRecorder initialization failed: " + exception.GetType().Name;
            }

            _report.diagnostics.allocationCounterAvailable = _gcAllocationRecorderAvailable;
            _report.diagnostics.allocationCounterStatus = _gcAllocationRecorderStatus;

            if (!_options.diagnostics)
            {
                return;
            }

            try
            {
                _frameTimingFeatureAvailable = FrameTimingManager.IsFeatureEnabled();
                _report.diagnostics.frameTimingFeatureAvailable = _frameTimingFeatureAvailable;
                _report.diagnostics.frameTimingStatus = _frameTimingFeatureAvailable
                    ? "FrameTimingManager enabled; supplementary timings collected with preallocated buffers."
                    : "FrameTimingManager feature is unavailable on this player/platform.";
            }
            catch (Exception exception)
            {
                _frameTimingFeatureAvailable = false;
                _report.diagnostics.frameTimingFeatureAvailable = false;
                _report.diagnostics.frameTimingStatus =
                    "FrameTimingManager availability check failed: " + exception.GetType().Name;
            }
        }

        private void DisposeMeasurementDiagnostics()
        {
            if (_gcAllocatedInFrameRecorder.Valid)
            {
                _gcAllocatedInFrameRecorder.Dispose();
            }

            _gcAllocationRecorderAvailable = false;
        }

        private void CaptureRuntimeMetadata()
        {
            NativePixelViewport viewport = _presentation.Viewport;
            _report.runtime = new BenchmarkRuntimeMetadata
            {
                requestedPhysicalWidth = RequestedWidth,
                requestedPhysicalHeight = RequestedHeight,
                actualPhysicalWidth = Screen.width,
                actualPhysicalHeight = Screen.height,
                logicalWidth = viewport.LogicalWidth,
                logicalHeight = viewport.LogicalHeight,
                integerScale = viewport.IntegerScale,
                outputOffsetX = viewport.OutputOffsetX,
                outputOffsetY = viewport.OutputOffsetY,
                outputWidth = viewport.OutputWidth,
                outputHeight = viewport.OutputHeight,
                qualityName = QualitySettings.names[QualitySettings.GetQualityLevel()],
                qualityIndex = QualitySettings.GetQualityLevel(),
                vSyncCount = QualitySettings.vSyncCount,
                targetFrameRate = Application.targetFrameRate,
                captureFrameRate = Time.captureFramerate,
                fullScreenMode = Screen.fullScreenMode.ToString()
            };
        }

        private static BenchmarkSystemMetadata CaptureSystemMetadata()
        {
            return new BenchmarkSystemMetadata
            {
                operatingSystem = SystemInfo.operatingSystem,
                processorType = SystemInfo.processorType,
                processorCount = SystemInfo.processorCount,
                systemMemoryMb = SystemInfo.systemMemorySize,
                graphicsDeviceName = SystemInfo.graphicsDeviceName,
                graphicsDeviceVendor = SystemInfo.graphicsDeviceVendor,
                graphicsDeviceType = SystemInfo.graphicsDeviceType.ToString(),
                graphicsDeviceVersion = SystemInfo.graphicsDeviceVersion,
                graphicsMemoryMb = SystemInfo.graphicsMemorySize
            };
        }

        private static BenchmarkBuildMetadata LoadBuildMetadata(BenchmarkOptions options)
        {
            BenchmarkBuildMetadata metadata = new BenchmarkBuildMetadata();
            try
            {
                string playerDirectory = Directory.GetParent(Application.dataPath)?.FullName;
                if (!string.IsNullOrEmpty(playerDirectory))
                {
                    string path = Path.Combine(
                        playerDirectory,
                        BenchmarkReportWriter.BuildMetadataFileName);
                    if (File.Exists(path))
                    {
                        BenchmarkBuildMetadata loaded =
                            JsonUtility.FromJson<BenchmarkBuildMetadata>(File.ReadAllText(path));
                        if (loaded != null)
                        {
                            metadata = loaded;
                        }
                    }
                }
            }
            catch (Exception)
            {
                metadata = new BenchmarkBuildMetadata();
            }

            if (!string.Equals(options.gitCommit, "unknown", StringComparison.Ordinal))
            {
                metadata.gitCommit = options.gitCommit;
            }

            if (!string.Equals(options.gitDirtyState, "unknown", StringComparison.Ordinal))
            {
                metadata.gitDirtyState = options.gitDirtyState;
            }

            return metadata;
        }

        private void DisableLegacyPerformanceHud()
        {
            MovementLabPerformanceHud hud = FindAnyObjectByType<MovementLabPerformanceHud>();
            if (hud != null)
            {
                hud.enabled = false;
            }
        }

        private IEnumerator Finish(string status, string failureReason)
        {
            DisposeMeasurementDiagnostics();
            _report.status = status;
            _report.failureReason = failureReason;
            if (_presentation != null)
            {
                CaptureRuntimeMetadata();
            }

            string reportDirectory = BenchmarkReportWriter.WriteAll(_report, out _summary);
            _completed = true;
            UnityEngine.Debug.Log(_summary);
            UnityEngine.Debug.Log($"Rustline benchmark reports written to: {reportDirectory}");

            yield return null;
            if (_options != null && _options.autoQuit)
            {
                Application.Quit(string.Equals(status, "SUCCEEDED", StringComparison.Ordinal) ? 0 : 2);
            }
        }

        private double ElapsedBenchmarkSeconds()
        {
            return (Stopwatch.GetTimestamp() - _benchmarkStartTimestamp) * TimestampToSeconds;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                _focusLostDuringBlock = true;
            }
        }

        private void OnDestroy()
        {
            DisposeMeasurementDiagnostics();
        }

        private void Update()
        {
            if (!_completed || Keyboard.current == null)
            {
                return;
            }

            if (Keyboard.current.cKey.wasPressedThisFrame)
            {
                GUIUtility.systemCopyBuffer = _summary;
            }

            if (Keyboard.current.escapeKey.wasPressedThisFrame ||
                Keyboard.current.qKey.wasPressedThisFrame)
            {
                Application.Quit();
            }
        }

        private void OnGUI()
        {
            if (!_completed)
            {
                return;
            }

            if (_summaryStyle == null)
            {
                _summaryStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 16,
                    alignment = TextAnchor.UpperLeft,
                    wordWrap = false
                };
                _summaryStyle.normal.textColor = Color.white;
            }

            GUI.Label(SummaryRect, _summary + Environment.NewLine + "C COPY | Q/ESC QUIT", _summaryStyle);
        }

        private sealed class BenchmarkBlockMeasurement
        {
            public readonly BenchmarkBlockPlan plan;
            public readonly double requestedDurationSeconds;
            public readonly double elapsedBenchmarkSecondsAtStart;
            public int sampleCount;
            public double measuredDurationSeconds;
            public string invalidReason;
            public int gcGen0Collections;
            public int gcGen1Collections;
            public int gcGen2Collections;
            public bool managedAllocationAvailable;
            public string managedAllocationStatus;
            public long managedHeapBytesBefore;
            public long managedHeapBytesAfter;
            public bool frameTimingRequested;
            public bool frameTimingFeatureAvailable;
            public int cpuFrameTimingCount;
            public int cpuMainThreadTimingCount;
            public int cpuRenderThreadTimingCount;
            public int cpuMainThreadPresentWaitTimingCount;
            public int gpuFrameTimingCount;

            public BenchmarkBlockMeasurement(
                BenchmarkBlockPlan plan,
                double requestedDurationSeconds,
                double elapsedBenchmarkSecondsAtStart)
            {
                this.plan = plan;
                this.requestedDurationSeconds = requestedDurationSeconds;
                this.elapsedBenchmarkSecondsAtStart = elapsedBenchmarkSecondsAtStart;
            }

            public BenchmarkBlockResult CreateResult(
                double[] samples,
                long[] allocationSamples,
                BenchmarkDiagnosticBuffers diagnosticBuffers,
                double[] statisticsScratch,
                long[] allocationScratch)
            {
                return new BenchmarkBlockResult
                {
                    pairIndex = plan.pairIndex,
                    slot = plan.slot,
                    orderIndex = plan.orderIndex,
                    penumbraEnabled = plan.penumbraEnabled,
                    elapsedBenchmarkSecondsAtStart = elapsedBenchmarkSecondsAtStart,
                    requestedDurationSeconds = requestedDurationSeconds,
                    measuredDurationSeconds = measuredDurationSeconds,
                    valid = string.IsNullOrEmpty(invalidReason),
                    invalidReason = invalidReason,
                    frameTime = BenchmarkStatistics.Calculate(
                        samples,
                        sampleCount,
                        statisticsScratch),
                    managedAllocation = BenchmarkStatistics.CalculateAllocation(
                        allocationSamples,
                        sampleCount,
                        managedAllocationAvailable,
                        managedAllocationStatus,
                        allocationScratch),
                    managedHeapBytesBefore = managedHeapBytesBefore,
                    managedHeapBytesAfter = managedHeapBytesAfter,
                    diagnosticFrameTiming = diagnosticBuffers.CreateSummary(
                        this,
                        statisticsScratch),
                    gcGen0Collections = gcGen0Collections,
                    gcGen1Collections = gcGen1Collections,
                    gcGen2Collections = gcGen2Collections
                };
            }
        }

        private sealed class AggregateCounters
        {
            public int blockCount;
            public int gc0;
            public int gc1;
            public int gc2;
            public readonly List<double> blockMeans = new List<double>();

            public void Add(BenchmarkBlockResult block)
            {
                blockCount++;
                gc0 += block.gcGen0Collections;
                gc1 += block.gcGen1Collections;
                gc2 += block.gcGen2Collections;
                blockMeans.Add(block.frameTime.meanMs);
            }
        }

        private sealed class BenchmarkDiagnosticBuffers
        {
            private readonly double[] _cpuFrameTime;
            private readonly double[] _cpuMainThreadFrameTime;
            private readonly double[] _cpuRenderThreadFrameTime;
            private readonly double[] _cpuMainThreadPresentWaitTime;
            private readonly double[] _gpuFrameTime;

            public BenchmarkDiagnosticBuffers(int capacity, bool allocate)
            {
                int length = allocate ? capacity : 0;
                _cpuFrameTime = new double[length];
                _cpuMainThreadFrameTime = new double[length];
                _cpuRenderThreadFrameTime = new double[length];
                _cpuMainThreadPresentWaitTime = new double[length];
                _gpuFrameTime = new double[length];
            }

            public void AddCpuFrameTime(double value, ref int count)
            {
                AddPositiveFinite(_cpuFrameTime, value, ref count);
            }

            public void AddCpuMainThreadFrameTime(double value, ref int count)
            {
                AddPositiveFinite(_cpuMainThreadFrameTime, value, ref count);
            }

            public void AddCpuRenderThreadFrameTime(double value, ref int count)
            {
                AddPositiveFinite(_cpuRenderThreadFrameTime, value, ref count);
            }

            public void AddCpuMainThreadPresentWaitTime(double value, ref int count)
            {
                AddPositiveFinite(_cpuMainThreadPresentWaitTime, value, ref count);
            }

            public void AddGpuFrameTime(double value, ref int count)
            {
                AddPositiveFinite(_gpuFrameTime, value, ref count);
            }

            public BenchmarkFrameTimingSummary CreateSummary(
                BenchmarkBlockMeasurement measurement,
                double[] statisticsScratch)
            {
                string status;
                if (!measurement.frameTimingRequested)
                {
                    status = "Not requested.";
                }
                else if (!measurement.frameTimingFeatureAvailable)
                {
                    status = "FrameTimingManager unavailable.";
                }
                else
                {
                    status = "Available fields contain positive samples; empty fields were unsupported or unreported.";
                }

                return new BenchmarkFrameTimingSummary
                {
                    requested = measurement.frameTimingRequested,
                    featureAvailable = measurement.frameTimingFeatureAvailable,
                    status = status,
                    cpuFrameTime = BenchmarkStatistics.Calculate(
                        _cpuFrameTime,
                        measurement.cpuFrameTimingCount,
                        statisticsScratch),
                    cpuMainThreadFrameTime = BenchmarkStatistics.Calculate(
                        _cpuMainThreadFrameTime,
                        measurement.cpuMainThreadTimingCount,
                        statisticsScratch),
                    cpuRenderThreadFrameTime = BenchmarkStatistics.Calculate(
                        _cpuRenderThreadFrameTime,
                        measurement.cpuRenderThreadTimingCount,
                        statisticsScratch),
                    cpuMainThreadPresentWaitTime = BenchmarkStatistics.Calculate(
                        _cpuMainThreadPresentWaitTime,
                        measurement.cpuMainThreadPresentWaitTimingCount,
                        statisticsScratch),
                    gpuFrameTime = BenchmarkStatistics.Calculate(
                        _gpuFrameTime,
                        measurement.gpuFrameTimingCount,
                        statisticsScratch)
                };
            }

            private static void AddPositiveFinite(double[] destination, double value, ref int count)
            {
                if (destination.Length == 0 || value <= 0.0 ||
                    double.IsNaN(value) || double.IsInfinity(value))
                {
                    return;
                }

                if (count < destination.Length)
                {
                    destination[count] = value;
                    count++;
                }
            }
        }
    }
}
#endif
