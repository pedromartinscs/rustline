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
.\Builds\Performance\RustlineBenchmark.exe --rustline-benchmark --benchmark-mode control-off --benchmark-warmup-seconds 1 --benchmark-settle-seconds 0.25 --benchmark-block-seconds 1 --benchmark-pairs 1 --benchmark-auto-quit
```

The smoke proves activation, normalization, deterministic state selection, report output, and clean exit only. Its timing values are not benchmark evidence.

Supported overrides:

- `--benchmark-warmup-seconds <0..600>`
- `--benchmark-settle-seconds <0..60>`
- `--benchmark-block-seconds <greater than 0, up to 600>`
- `--benchmark-pairs <1..100>`
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
- generation 0/1/2 GC collection deltas;
- validity and any invalidation reason.

Percentiles use linear interpolation at `(sampleCount - 1) × percentile` in the sorted sample distribution.

Mean reflects total frame-time cost and is the basis of the primary paired comparison. Median describes the typical frame with less sensitivity to isolated hitches. p95 and p99 expose tail behavior without reducing the run to one extreme observation. Maximum/WORST is retained but is not a useful optimization decision by itself because one unrelated scheduling or OS hitch can dominate it.

For a real A/B run, the report includes aggregate ON and OFF distributions plus chronological block data. More importantly, it calculates each pair's mean and median delta as `ON - OFF`, then reports the list, mean, median, and spread of paired deltas. The relative percentage uses the aggregate OFF mean as its denominator.

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

Schema version 1 records Unity/build state, system/graphics information, requested and actual viewport configuration, quality name/index, protocol, sequence, every block, GC deltas, aggregates, and paired comparisons.

Unity `FrameTimingManager` CPU/GPU timing is intentionally not captured in the default harness. Support and validity vary by graphics API/platform, and `CaptureFrameTimings` would add a measurement behavior not required by the primary wall-clock comparison. Reports state this explicitly and never substitute zero CPU/GPU values.

## Limitations and interpretation

- Wall-clock frame intervals are end-to-end behavior, not isolated GPU duration.
- Development Builds carry overhead and should be compared only with matching benchmark builds.
- OS tasks, overlays, recording software, power policy, driver behavior, hardware clocks, and thermal state still affect results.
- Keep the benchmark window focused and avoid interacting with the machine during a run.
- Compare an A/B result with a nearby A/A control from the same build and session.
- Preserve chronological blocks; drift across the run is diagnostic information.
- Do not accept or reject Experiment 3A from the short smoke or from the old Editor HUD.

The next optimization should begin only after a full A/A control and full Penumbra A/B run establish that this harness's noise floor is small enough for the intended decision.
