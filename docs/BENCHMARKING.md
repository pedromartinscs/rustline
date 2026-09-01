# Rustline Standalone Performance Benchmark

Rustline uses a dedicated Windows standalone harness for performance differences small enough to be obscured by Unity Editor scheduling. The existing MovementLab 2-second HUD remains useful for visual smoke testing, coarse diagnostics, and obvious regressions, but it is not an acceptance tool for sub-millisecond optimizations.

The first scenario is the **MovementLab static presentation benchmark**. It compares palette penumbra ON and OFF while the player and camera are settled and idle. It does not simulate traversal, combat, or input and must not be interpreted as a complete gameplay benchmark.

## Activation and build

The benchmark runner is compiled only into benchmark builds with the additional `RUSTLINE_BENCHMARK` scripting define. `BuildPlayerOptions.extraScriptingDefines` supplies that define for the build without changing project-wide Player settings. Even in that build, the runner activates only when `--rustline-benchmark` is present.

The build contains only `Assets/Scenes/MovementLab.unity` and targets Windows x86_64 as a Development Build. Auto Connect Profiler and Script Debugging are not enabled. Output is ignored by Git:

```text
Builds/Performance/RustlineBenchmark.exe
```

Unity menu commands:

- `Tools > Rustline > Performance Benchmark > Build Windows Benchmark`
- `Tools > Rustline > Performance Benchmark > Build & Run Penumbra A/B`
- `Tools > Rustline > Performance Benchmark > Build & Run A/A Control (Penumbra OFF)`
- `Tools > Rustline > Performance Benchmark > Build & Run Short Smoke`

The builder records `git rev-parse HEAD` and `git status --porcelain` in the ignored build output. If Git is unavailable, the report uses `unknown`; building never changes source files to stamp metadata.

## Normalized runtime configuration

Before warm-up, the harness requests and verifies:

- physical resolution: `1920×1080`;
- full-screen mode: `Windowed`;
- quality: `Very Low`, resolved by exact name;
- VSync: `0`;
- `Application.targetFrameRate`: `-1`;
- `Time.captureFramerate`: `0`;
- logical viewport: `1072×1072`;
- integer scale: `1×`;
- output: `1072×1072`, offset `(424, 4)`.

This quality override is essential: the repository's current Editor quality is `Very Low` at index 0, while the Standalone platform default is `Ultra` at index 5. The harness deliberately resolves `Very Low` by name, applies it only at runtime, and records both its name and resulting index. It does not change the project's Standalone default.

If the required quality name is missing or the exact physical/logical configuration is not reached within 30 seconds, the run fails and writes an explicit error report instead of benchmarking a different configuration.

The harness also aborts rather than silently accepting contamination if focus is lost or if resolution, viewport, quality index, VSync, target frame rate, capture frame rate, presentation component, or window mode changes. A measured block that detects contamination remains in the report as invalid; it is not included in aggregates.

## Default protocol

The default protocol is:

- global warm-up: 15 seconds;
- warm-up paths: 5 s ON, 5 s OFF, 5 s ON;
- settling after every condition change: 2 seconds, excluded;
- measured block duration: 15 seconds;
- pairs: 6;
- order: `ON>OFF | OFF>ON | ON>OFF | OFF>ON | ON>OFF | OFF>ON`.

This is 12 measured blocks. Excluding startup/configuration time, the default run is approximately:

```text
15 s warm-up + 12 × (2 s settle + 15 s measure) = 219 s
```

That is about 3 minutes 39 seconds, plus process startup, configuration, report writing, and human handling.

The balanced fixed order prevents a simple monotonic drift from always favoring the same condition. It does not eliminate OS scheduling, thermal, clock, driver, or background-process noise.

## A/A control before A/B

Run `control-off` before the real comparison. Both A and B slots use Penumbra OFF, but chronological A/B slots and paired `A - B` deltas remain in the report. This estimates the false delta and time-order bias produced by the machine and protocol.

Do not invent a universal A/A pass threshold. A small real ON-minus-OFF delta is credible only when it is more consistent and meaningfully larger than the same-session A/A noise.

The September 1 control run at commit `83b0bfa` was not stable enough to serve this purpose. Identical Penumbra-OFF block means ranged from about `1.76 ms` to `116.34 ms`, with false paired deltas from about `-10.86 ms` to `+77.50 ms`. The nearby Penumbra A/B run was similarly unstable, so its reported aggregate difference is not shader-performance evidence. Benchmark Stabilization 1 keeps those blocks rather than filtering them and makes the instability explicit in the report.

The real `penumbra-ab` mode calculates every pair as `ON - OFF`, regardless of whether ON ran first or second.

## Command-line usage

From the repository root after building:

### A/A control-off

```powershell
.\Builds\Performance\RustlineBenchmark.exe --rustline-benchmark --benchmark-mode control-off
```

### Penumbra ON/OFF A/B

```powershell
.\Builds\Performance\RustlineBenchmark.exe --rustline-benchmark --benchmark-mode penumbra-ab
```

### Short smoke

```powershell
.\Builds\Performance\RustlineBenchmark.exe --rustline-benchmark --benchmark-mode control-off --benchmark-warmup-seconds 1 --benchmark-settle-seconds 0.25 --benchmark-block-seconds 1 --benchmark-pairs 1 --benchmark-diagnostics --benchmark-auto-quit
```

The smoke proves activation, normalization, deterministic state selection, report output, and clean exit only. Its timing values are not benchmark evidence.

Supported overrides:

- `--benchmark-warmup-seconds <0..600>`
- `--benchmark-settle-seconds <0..60>`
- `--benchmark-block-seconds <greater than 0, up to 600>`
- `--benchmark-pairs <1..100>`
- `--benchmark-diagnostics`
- `--benchmark-auto-quit`

After a non-auto-quit run, press `C` to copy the final summary or `Q`/Escape to quit. Detailed UI is created only after measurement. The ordinary MovementLab performance HUD is disabled before benchmark warm-up and remains unchanged in normal Editor/Development play.

## Timing and statistics

The primary metric is the end-to-end interval between successive frames at the same coroutine phase, measured with monotonic `System.Diagnostics.Stopwatch.GetTimestamp`. Samples are stored in a preallocated array during a block. The harness performs no per-frame logging or formatting and does not silently discard slow frames or statistical outliers.

Every block reports:

- measured wall duration and frame count;
- mean and median frame time;
- population standard deviation;
- minimum;
- p90, p95, and p99;
- maximum;
- equivalent FPS (`1000 / mean ms`);
- `GC Allocated In Frame` counter availability, total bytes, mean/median/p95/max bytes per frame, and non-zero frame count/percentage;
- `GC.GetTotalMemory(false)` before and after the block as heap snapshots, not as an allocation total;
- generation 0/1/2 GC collection deltas;
- validity and any invalidation reason.

Percentiles use linear interpolation at `(sampleCount - 1) × percentile` in the sorted sample distribution.

Mean reflects total frame-time cost and is the basis of the primary paired comparison. Median describes the typical frame with less sensitivity to isolated hitches. p95 and p99 expose tail behavior without reducing the run to one extreme observation. Maximum/WORST is retained but is not a useful optimization decision by itself because one unrelated scheduling or OS hitch can dominate it.

The allocation counter is opened once before warm-up with `Unity.Profiling.ProfilerRecorder`, category `Memory`, marker `GC Allocated In Frame`, and capacity 1. Its value is copied once per measured frame into a preallocated `long[]`. If Unity does not expose the marker on a player/platform, the run continues and records it as unavailable. `GC.CollectionCount(0/1/2)` remains supplementary context; equal values must not be interpreted as ordinary desktop .NET generational-GC pressure.

The Windows D3D11 validation player exposed the counter. Matching one-second smokes with diagnostics ON and OFF both reported a `4400 B/frame` allocation median, so optional FrameTimingManager capture did not cause that regular allocation. The counter covers the complete frame and supplies no call stacks; this observation does not by itself identify Rustline scripts, URP/RenderGraph, Input System, the benchmark coroutine, or Development-player internals as the owner. No source-level allocation was changed without that evidence.

The valid measured loop itself performs a coroutine resume (`yield return null`), `Stopwatch.GetTimestamp`, preallocated array writes, cheap scalar configuration checks, and optional recorder reads. It does not log, format strings, use LINQ, or allocate a result object per frame. Failure strings are constructed only after contamination is detected. Large statistic-sorting buffers are also preallocated and reused between blocks, avoiding temporary per-block sample arrays that could seed a later measured block with avoidable garbage.

### Pooled and block-balanced statistics

The report keeps two distinct estimands:

- `pooledFrameTime` describes the distribution of every captured frame. A faster fixed-duration block contributes more frames and therefore more weight.
- `blockBalancedFrameTime` gives every valid block equal weight by summarizing block means. It reports count, mean, median, population standard deviation, minimum, and maximum of those block means.

Neither is substituted for the other. Absolute paired block-mean deltas remain the primary comparison. The report also preserves chronological block order and exposes minimum/maximum block mean, max/min ratio, mean, standard deviation, and coefficient of variation without automatically rejecting a block.

For A/A, paired output includes signed `A - B` deltas plus mean, median, and maximum absolute false delta. For Penumbra A/B, every valid pair computes:

```text
relativeCostPercent = (ON block mean - OFF block mean) / that pair's OFF block mean × 100
```

The report summarizes those pair-relative percentages only when the individual OFF denominator is finite and positive. It no longer divides an equal-weight paired numerator by a differently weighted pooled OFF mean. Absolute `ON - OFF` milliseconds remain primary; volatile pair denominators do not support a confident conclusion.

### Optional CPU/GPU diagnostics

`--benchmark-diagnostics` additionally calls `FrameTimingManager.CaptureFrameTimings` and reads the latest timing into one preallocated `FrameTiming` slot. Positive reported values are summarized separately for CPU frame, CPU main thread, CPU render thread, CPU present wait, and GPU frame timing. The report records both feature availability and per-field sample availability; an empty metric means unsupported/unreported, not a measured zero.

Default runs do not call `FrameTimingManager`, so wall-clock timing remains canonical and its optional capture overhead/platform variation cannot silently change the normal protocol. The managed-allocation recorder remains active in both default and diagnostic runs because its one-value read is the focus of this stabilization pass.

## Reports

Reports are written after measurement under:

```text
Application.persistentDataPath/RustlineBenchmarks/
```

With the current Windows company/product settings this normally resolves to:

```text
%USERPROFILE%\AppData\LocalLow\DefaultCompany\rustline\RustlineBenchmarks\
```

UTC timestamped output includes:

- `.json`: schema-versioned full metadata, chronological blocks, aggregates, and paired deltas;
- `.csv`: one block-summary row per chronological block;
- `.txt`: concise copy/paste summary.

Schema version 2 records Unity/build state, system/graphics information, requested and actual viewport configuration, quality name/index, protocol, diagnostic availability, sequence, every block, managed-allocation metrics, GC deltas, explicitly pooled and block-balanced aggregates, block stability, paired absolute-noise metrics, and pair-consistent relative percentages.

## Laptop pre-run checklist

- Connect AC power and select Windows **Best performance** (or the equivalent vendor power mode).
- Close Unity before the full standalone run.
- Close browser video, OBS/recording, overlays, and game launchers or background tools doing active work.
- Give the machine a short idle period before starting.
- Keep the benchmark focused and do not interact with the machine during the run.
- Run A/A before A/B.
- Do not change power, thermal, display, driver, or background-process conditions between the two runs.
- Do not attach the Unity Profiler, enable deep profiling, Auto Connect Profiler, or Script Debugging for the canonical run.

## Limitations and interpretation

- Wall-clock frame intervals are end-to-end behavior, not isolated GPU duration.
- Development Builds carry overhead and should be compared only with matching benchmark builds.
- OS tasks, overlays, recording software, power policy, driver behavior, hardware clocks, and thermal state still affect results.
- Optional recorder/FrameTimingManager diagnostics add some instrumentation overhead. Compare like with like and keep diagnostics settings identical within a decision sequence.
- Compare an A/B result only after a nearby A/A control from the same build and session has passed human review.
- Preserve chronological blocks; drift across the run is diagnostic information.
- Do not accept or reject Experiment 3A from the short smoke or from the old Editor HUD.

## Final decision gate after Benchmark Stabilization 1

Run exactly one full `control-off` A/A first. Review chronological block means, signed and absolute A-B deltas, allocation metrics, GC counts, and any optional CPU/GPU diagnostics.

If A/A is reasonably stable, run the Penumbra A/B and retain this harness for future optimization work. If identical OFF blocks still change by many milliseconds or large multiples comparable to the previous run, stop benchmark-engineering work for now. Keep the harness as a coarse diagnostic, document the environment limitation, keep Experiment 3A because it is structurally cheaper and visually correct without claiming a measured speedup, and return project focus to gameplay/content/assets.

There is no automatic Benchmark Stabilization 2 and no universal pass/fail threshold. Pedro and Echo make this one human methodological decision before any A/B run or further optimization.
