# Rustline Performance Experiment Log

This log records small Pedro ↔ Echo performance experiments so changes remain attributable and reversible.

## Baseline — stabilized 2.0 s MovementLab HUD

Environment:

- Unity Editor / MovementLab
- Game view: `1086×420`
- `VSync 0`
- `Application.targetFrameRate = -1`
- HUD sample window: `2.0 s`

Idle / standing samples:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 100.0 | 10.00 | 14.69 | 200 |
| 2 | 93.6 | 10.68 | 16.15 | 188 |
| 3 | 114.3 | 8.75 | 14.33 | 229 |

Simple mean: approximately **102.6 FPS / 9.81 ms AVG**.

Active traversal samples:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 77.6 | 12.88 | 22.34 | 156 |
| 2 | 90.6 | 11.04 | 37.96 | 182 |
| 3 | 79.9 | 12.51 | 19.13 | 160 |

Simple mean: approximately **82.7 FPS / 12.14 ms AVG**.

The Editor measurements suggest a repeatable movement-associated increase in frame time, but they do not identify its cause. Rigidbody2D movement, animation, camera following, visible Tilemap traversal, Editor scheduling, and other factors change together during this test. Do not attribute the difference to one subsystem without a focused experiment or profiler capture.

## Experiment 1 — 2D Renderer Depth/Stencil Buffer

Status: **kept provisionally**.

Change:

- `Assets/Settings/Renderer2D.asset`
- `m_UseDepthStencilBuffer: 1` → `0`

Rationale:

- Rustline currently has no `SpriteMask` usage in the repository.
- Unity documents the 2D Renderer Depth/Stencil Buffer as optional when features that require it are not used, and notes that disabling it may improve performance, particularly on mobile hardware.
- This experiment changes one renderer variable only. No gameplay, camera, physics, lighting content, art, or pixel-perfect settings are intentionally changed.

Observed result after disabling the buffer, under the same Editor conditions:

Idle / standing samples:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 112.8 | 8.87 | 13.52 | 226 |
| 2 | 104.4 | 9.57 | 13.28 | 209 |
| 3 | 98.2 | 10.19 | 14.20 | 197 |

Simple mean: approximately **105.1 FPS / 9.54 ms AVG**.

Active traversal samples:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 97.3 | 10.28 | 15.49 | 195 |
| 2 | 80.6 | 12.41 | 24.60 | 163 |
| 3 | 86.7 | 11.53 | 18.49 | 174 |

Simple mean: approximately **88.2 FPS / 11.41 ms AVG**.

Compared with the stabilized baseline, the simple means changed by roughly:

- idle: `102.6 → 105.1 FPS` and `9.81 → 9.54 ms`;
- traversal: `82.7 → 88.2 FPS` and `12.14 → 11.41 ms`.

The apparent improvement is encouraging but not large enough to claim as a proven desktop Editor speedup because sample-to-sample noise remains significant. The setting is nevertheless kept provisionally because the buffer is currently unused, no visual regression was observed, and removing unnecessary depth/stencil allocation is aligned with Rustline's low-overhead rendering goals. Re-enable it later if a feature genuinely requires stencil/depth behavior.

## Diagnostic isolation 1 — camera follow contribution

Status: **complete — no meaningful camera-follow bottleneck identified**.

Question:

Does the movement-associated frame-time increase primarily come from moving the camera / pixel-perfect presentation / newly visible Tilemap regions, or does most of it remain when the player moves under a stationary camera?

Instrumentation:

- Left-clicking the performance HUD copies the last completed sample.
- Right-clicking the HUD toggles only `PixelCameraFollow2D.enabled` at runtime.
- The HUD reports `Camera ON` or `Camera FROZEN` in copied output.
- Toggling resets the current sample window so the transition frame does not contaminate the next completed 2.0-second sample.
- The toggle is diagnostic only, is not saved into the scene, and disappears when Play Mode ends.

Same-session active-movement samples with camera follow enabled:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 104.1 | 9.61 | 23.38 | 209 |
| 2 | 83.3 | 12.01 | 22.17 | 167 |
| 3 | 95.9 | 10.43 | 16.10 | 192 |

Simple mean: approximately **94.4 FPS / 10.68 ms AVG**.

Same-session active-movement samples with camera follow frozen:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 97.8 | 10.22 | 19.50 | 196 |
| 2 | 90.4 | 11.06 | 16.55 | 181 |
| 3 | 92.4 | 10.82 | 19.50 | 185 |

Simple mean: approximately **93.5 FPS / 10.70 ms AVG**.

Conclusion:

- Average frame time was effectively unchanged: approximately `10.68 ms` with camera follow versus `10.70 ms` frozen.
- The small FPS difference is inside normal Unity Editor noise and does not support a causal performance claim.
- Worst-frame values were somewhat lower in the frozen samples, but the average did not improve and worst-frame is highly sensitive to isolated Editor scheduling spikes.
- **Do not optimize or simplify `PixelCameraFollow2D` based on these measurements.** Preserve the accepted camera-follow behavior unless later profiling in a representative standalone build identifies a real issue.
- The session-to-session variation also reinforces that sub-millisecond optimization claims should not be accepted from Editor Game View FPS alone.

## Method decision after early diagnostics

The Pedro ↔ Echo micro-measurement loop remains useful for large regressions and focused A/B checks, but serious frame-time optimization will increasingly use **standalone Development Builds and Unity Profiler captures** as Rustline gains representative rendering/gameplay load.

Obvious unused features may still be removed incrementally when the change is functionally safe and structurally justified. Claims of small speedups, however, should be treated as provisional until measured outside the Editor.

## M1B — native-pixel viewport and palette penumbra prototype

Status: **implemented, consolidated, and instrumented**.

Current implementation boundary:

- MovementLab world rendering is bounded to the computed logical size, never larger than `1072×1072`.
- The gameplay/world camera keeps Renderer2D and renders into a persistent logical world target.
- One lightweight utility/driver camera uses the minimal utility renderer with `cullingMask = 0`; it exists only to drive the custom URP RenderGraph feature and performs no scene-object rendering.
- With Penumbra ON, one logical-resolution RenderGraph pass performs the palette remap and ordered dithering into a persistent resolved target.
- The final RenderGraph pass point-presents either the resolved target or raw world target directly into the physical backbuffer, centered at integer scale over a Deep Space clear.
- The former physical Presentation Camera and both presentation quads were removed from the accepted runtime/setup architecture.
- Source and resolved RenderTextures persist across steady-state frames and are recreated only when logical dimensions change.
- Logical targets use 8-bit sRGB color, Point filtering, no mipmaps, no MSAA, and no HDR. The current target descriptors remain intentionally unchanged while performance variables are isolated.
- `P` toggles Penumbra `ON/OFF` in Editor and Development Builds without target reallocation and resets the 2.0-second sample window.
- Penumbra OFF skips only the logical effect pass and presents the raw world target directly through the same final RenderGraph presentation path.
- HUD/copy output includes physical resolution, logical resolution, integer scale, penumbra state, camera-follow state, FPS, AVG, WORST, frames, VSync, and target frame rate.

### M1B performance recovery 1 — dedicated utility renderer

Status: **accepted, consolidated, and closed**.

Static audit found that the logical penumbra camera and physical presentation camera were both running through Rustline's full `Renderer2D`, even though each rendered only isolated unlit presentation geometry. The world camera must keep the 2D Renderer, but presentation-only work does not need 2D lighting/sprite-renderer machinery.

Change:

- Added `Assets/Settings/RustlineUtilityRenderer.asset`, a minimal Universal Renderer registered at renderer index `1` in `UniversalRP.asset`.
- The utility renderer has no Renderer Features other than the accepted native-pixel RenderGraph presentation feature and performs no normal scene rendering for the driver camera.
- The world camera remains on the default Renderer2D.
- Runtime and PlayMode coverage enforce the renderer split.

Observed Editor samples with **Penumbra ON**, `VSync 0`, `Target -1`, and camera follow ON:

`PHYSICAL 1172×468 / LOGICAL 1072×468 / SCALE 1×`:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 68.3 | 14.65 | 23.57 | 137 |
| 2 | 89.0 | 11.24 | 16.58 | 178 |
| 3 | 72.3 | 13.83 | 23.58 | 145 |
| 4 | 70.7 | 14.15 | 24.63 | 142 |

Simple mean: approximately **75.1 FPS / 13.47 ms AVG**. Median FPS: approximately **71.5 FPS**.

`PHYSICAL 1920×1080 / LOGICAL 1072×1072 / SCALE 1×`:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 71.2 | 14.04 | 23.51 | 143 |
| 2 | 66.6 | 15.02 | 24.95 | 134 |
| 3 | 60.0 | 16.67 | 28.27 | 121 |
| 4 | 68.7 | 14.56 | 21.83 | 138 |

Simple mean: approximately **66.6 FPS / 15.07 ms AVG**. Median FPS: approximately **67.7 FPS**.

Before this change, the same post-M1B editor workflow had been observed around **35–40 FPS** with little subjective difference between Penumbra ON and OFF. That earlier value was not captured as a clean formal sample set, so it is retained only as the motivating regression observation, not as a precise benchmark baseline. The post-change measurements are nevertheless large enough to establish the utility-renderer specialization as a meaningful recovery rather than a micro-optimization.

Final post-consolidation validation at `PHYSICAL 1920×1080 / LOGICAL 1072×1072 / SCALE 1×`, again with Penumbra ON, VSync 0, Target -1, and camera follow ON:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 88.0 | 11.36 | 22.88 | 177 |
| 2 | 65.4 | 15.29 | 22.61 | 132 |
| 3 | 72.1 | 13.88 | 24.32 | 145 |

Simple mean: approximately **75.2 FPS / 13.51 ms AVG**. Median FPS: **72.1 FPS**. The spread remains characteristic of Editor noise, so this final set is used to confirm that consolidation introduced no performance regression; it is not treated as evidence that the cleanup itself produced an additional speedup.

Conclusion:

- **Experiment closed: keep the dedicated utility renderer as part of the accepted M1B presentation architecture.**
- Do not move the world camera away from Renderer2D; it renders the actual 2D scene and lighting.
- A clean standalone Development Build comparison is still required before claiming hardware-tier performance targets.

### M1B performance experiment 2 — direct RenderGraph presentation

Status: **accepted and closed — performance-neutral in Editor measurements; architectural simplification kept**.

Question:

Can Rustline remove the dedicated physical Presentation Camera and its presentation quad, replacing that final camera stage with a supported URP 17 RenderGraph pass that writes the selected logical texture directly to the backbuffer, without changing the accepted visual output?

Change:

- Removed the former Presentation Camera from the accepted runtime architecture.
- Removed the former logical/presentation quads from the accepted runtime/setup architecture.
- The utility Processing Camera was consolidated into a lightweight Native Pixel Driver Camera with `cullingMask = 0`; it drives RenderGraph but renders no scene geometry.
- The custom `RustlineNativePixelPresentFeature` now imports the persistent logical targets with explicit `RenderTargetInfo`, performs the optional logical penumbra resolve, and point-presents the selected source directly to `UniversalResourceData.backBufferColor`.
- The final pass explicitly clears the physical target to canonical Deep Space and uses the computed centered integer viewport.
- No legacy `CommandBuffer.Blit` or `endCameraRendering` callback is used.
- Texture orientation is explicit per transition: the camera-target → logical-resolve transition performs the required platform Y normalization; final presentation performs no extra flip. Repeated Penumbra toggles were manually validated after this correction.

Final Editor measurements at `PHYSICAL 1920×1080 / LOGICAL 1072×1072 / SCALE 1×`, `VSync 0`, `Target -1`, camera follow ON:

Penumbra ON:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 72.1 | 13.87 | 22.63 | 145 |
| 2 | 71.0 | 14.08 | 20.47 | 143 |

Simple mean: approximately **71.6 FPS / 13.98 ms AVG**.

Penumbra OFF:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 75.5 | 13.25 | 17.54 | 151 |
| 2 | 74.0 | 13.51 | 20.76 | 149 |

Simple mean: approximately **74.8 FPS / 13.38 ms AVG**.

The observed same-session Penumbra delta is therefore approximately **0.60 ms/frame** at the maximum logical viewport in this Editor workflow.

Compared with the Experiment 1 final Penumbra-ON mean (`13.51 ms`), Experiment 2's Penumbra-ON mean (`13.98 ms`) does not demonstrate a speedup, but the approximately `0.47 ms` difference is also too small relative to the already-observed Editor variance to establish a meaningful regression.

Conclusion:

- **Keep Experiment 2.** The measurable Editor result is best classified as performance-neutral, not as a demonstrated speedup.
- The accepted architecture is simpler: one gameplay World Camera plus one no-culling Driver Camera and explicit RenderGraph passes, with no physical Presentation Camera or presentation quads.
- The new structure provides direct control over the logical effect and final presentation stages and removes unnecessary scene presentation objects without sacrificing visual correctness.
- The next focused performance experiment is the palette-penumbra shader itself. Its current same-session incremental cost is approximately `0.60 ms/frame` in this Editor measurement, providing a concrete ON-vs-OFF comparison for the next experiment.
- Do not claim sub-millisecond gains from Editor Game View alone; use the immediate ON/OFF comparison as a directional experiment and validate serious claims later in a standalone Development Build / Unity Profiler.

### M1B performance experiment 3A — palette-penumbra region rejection

Status: **kept — visual/functional validation passed; measured speedup inconclusive**.

Hypothesis:

- The palette-penumbra fragment shader previously sampled the logical world texture and calculated a true Euclidean distance for every logical pixel before classifying that pixel as fully visible, in the 64 px transition annulus, or fully dark.
- At the maximum `1072×1072` logical viewport, large coherent inner and outer regions do not require the annulus-only square root, ordered dithering, nearest-palette search, or darkness-LUT lookup. The fully dark outer region does not require a world-texture sample either.

Change:

- The shader still derives the logical pixel from `floor(input.uv * _LogicalSize)` and evaluates the player-relative position at the existing half-pixel center, `logicalPixel + 0.5`.
- It now classifies pixels with `dot(deltaFromPlayer, deltaFromPlayer)` and squared versions of the existing 456 px and 520 px radii.
- Pixels at or beyond radius 520 return canonical Deep Space immediately, before sampling `_MainTex` and without calculating a square root, Bayer threshold, nearest palette color, or darkness-LUT result.
- Remaining pixels sample `_MainTex` once. Pixels at or inside radius 456 return that source sample immediately, without calculating a square root, Bayer threshold, nearest palette color, or darkness-LUT result.
- Only pixels strictly inside the existing 456..520 annulus calculate `sqrt(distanceSquared)` and continue through the unchanged continuous band progression, world-anchored deterministic 4×4 Bayer threshold, 28-color nearest-palette search, and 5-level darkness LUT.
- The shader's disabled guard still returns the raw source sample. In the accepted runtime, Penumbra OFF remains structurally unchanged: RenderGraph skips the logical effect pass and directly presents the raw world target.

Deliberately unchanged:

- The fullscreen vertex path, RenderGraph architecture, source orientation handling, point sampling, logical/physical sizing, cameras, gameplay, movement, and camera follow.
- The canonical 28-color palette, Deep Space value, 456/520 px boundaries, 64 px annulus, half-pixel positioning, world anchoring, Bayer matrix/threshold math, band progression, 28-entry linear palette search, and 5-level darkness LUT.
- No palette-search optimization, texture LUT, compute shader, RenderTexture change, fallback path, presentation camera/quad, `CommandBuffer.Blit`, or `endCameraRendering` callback is introduced.

Validation status:

- Unity `6000.4.0f1` imported and compiled the shader for Direct3D 11 without shader errors.
- The full EditMode suite passed: **32/32**.
- The full PlayMode suite completed with **2/3 passing**. `MovementLab_RendersWorldPixelsThroughRawAndPenumbraRenderGraphPaths` reported no visible pixels in the resolved target near the player during its stage-B readback. The same isolated test failed identically after temporarily restoring the pre-Experiment-3A fragment body, so this batch-mode result does not identify a 3A regression. The test remains unchanged and its failed status is not being hidden or reclassified as a pass.
- Pedro's human validation passed: Penumbra ON and OFF look correct, repeated `P` toggles preserve orientation, and movement/camera behavior remains correct.

Approximate Penumbra-ON Editor samples from the validation session:

| Sample | FPS | AVG ms | WORST ms | Frames |
|---:|---:|---:|---:|---:|
| 1 | 82.0 | 12.19 | 17.34 | 165 |
| 2 | 76.8 | 13.03 | 22.75 | 154 |
| 3 | 79.8 | 12.53 | 47.29 | 160 |

Arithmetic means: approximately **79.5 FPS / 12.58 ms AVG**.

These samples do **not** establish a performance improvement. Instantaneous/runtime Editor behavior drifted severely during the session, from roughly 150 FPS at one point to roughly 20 FPS later. Pedro therefore stopped the Editor micro-benchmark and did not collect a formal comparable Penumbra-OFF set. Comparing the `12.58 ms` mean with older sessions would confound the shader change with large session drift.

Conclusion:

- Experiment 3A visual/functional validation: **passed**.
- Experiment 3A structural optimization: **keep**.
- Experiment 3A measured speedup: **inconclusive / not established**.
- The Editor Game View HUD remains useful for obvious regressions, visual validation, and coarse diagnostics, but not for accepting sub-millisecond changes.
- Further micro-optimization decisions require the balanced standalone benchmark harness documented in `docs/BENCHMARKING.md`. No Experiment 3B work begins before a same-session A/A control and real A/B standalone run.

## Benchmark Stabilization 1 — allocation visibility and balanced report semantics

Status: **implemented; one final full A/A control pending**.

Motivation:

- The first full standalone `control-off` A/A at commit `83b0bfa` was extremely unstable. Identical Penumbra-OFF block means ranged from approximately `1.76 ms` to `116.34 ms`; signed false deltas ranged from approximately `-10.86 ms` to `+77.50 ms`.
- A nearby Penumbra A/B run also changed behavior sharply between identical-time blocks. Its old aggregate `9.41 ms` / `264.69%` output is not accepted as penumbra-cost evidence.
- GC collection deltas appeared at roughly one reported collection per 245–251 rendered frames across conditions. Matching `CollectionCount(0/1/2)` values are treated only as a clue under Unity's managed-GC model, not as proof of desktop-style generation-2 pressure.

Static managed-allocation audit:

- Active movement, ground checking, respawn, camera follow, native-pixel parameter updates, and animation selection use value types, persistent buffers, or event callbacks; no clear unnecessary per-frame user-code heap allocation was found.
- `PlayerAnimator2D` calls `ToString()` only when animation state changes, not every frame. Changing that was unnecessary for the static benchmark and could not explain the regular collection cadence.
- The development-only MovementLab HUD periodically formats display strings and uses IMGUI, but the benchmark disables the component before warm-up.
- The valid benchmark loop has no per-frame logging, formatting, LINQ, collections growth, or object creation. Failure descriptions allocate only after invalid state is detected.
- RenderGraph may perform Unity-internal managed/native work not visible from source inspection; accepted rendering architecture was not changed. Runtime allocation recording is used to observe the complete player frame.
- No behavior-preserving allocation fix was made because no source-level per-frame culprit was established.

Change:

- Every benchmark attempts Unity's `ProfilerRecorder` memory counter `GC Allocated In Frame` once before warm-up. Each measured value goes into a preallocated `long[]`; every block reports availability, total, mean, median, p95, maximum, and non-zero frames. `GC.GetTotalMemory(false)` before/after remains an explicitly non-cumulative heap snapshot.
- Statistics scratch buffers are allocated once and reused, removing large post-block sample-sort allocations that could leave avoidable garbage for a later block.
- Optional `--benchmark-diagnostics` enables preallocated `FrameTimingManager` CPU/main-thread/render-thread/present-wait/GPU observations. Default wall-clock timing remains canonical and does not call FrameTimingManager.
- Schema version 2 labels pooled frame distributions as `pooledFrameTime` and adds equal-weight `blockBalancedFrameTime` summaries. Global chronological block stability now includes min/max, ratio, mean, standard deviation, and coefficient of variation.
- A/A paired output adds mean/median/max absolute false delta. Penumbra relative percentages are now calculated per pair using that pair's OFF block mean, then summarized; absolute ON-minus-OFF milliseconds remain primary.

Windows D3D11 smoke validation:

- `GC Allocated In Frame` was available in the Unity `6000.4.0f1` Windows x86_64 Development player.
- A one-pair diagnostic smoke serialized all schema-v2 fields, reported FrameTimingManager available, returned positive CPU/main-thread/render-thread/present-wait/GPU samples, and exited cleanly.
- The diagnostic smoke reported a `4400 B/frame` allocation median in both blocks. A matching smoke without `--benchmark-diagnostics` reported the same `4400 B/frame` median, so optional FrameTimingManager capture was not the source of that regular allocation.
- This proves that the complete Development-player frame reports managed allocation; it does not attribute the bytes to Rustline gameplay, the benchmark coroutine, URP/RenderGraph, Input System, or Unity Development-player internals. The recorder observes the whole frame and does not provide allocation call stacks. The pre-instrumentation collection cadence is consistent with continuing allocation pressure, but causation remains unproven.
- The smoke's frame times and its apparently tight A/A delta are non-evidence because blocks were only one second long.

Decision gate:

- Pedro will run one full diagnostic `control-off` A/A after the implementation is validated.
- If that A/A is reasonably stable, Penumbra A/B may follow. If it remains extremely unstable, benchmark engineering stops for now; the harness remains a coarse diagnostic, Experiment 3A remains structurally justified without a measured-speedup claim, and project focus returns to gameplay/content/assets.
- No Experiment 3B or automatic Benchmark Stabilization 2 begins from this work.

## Focused performance pass — depth, steady-state CPU work, and URP capabilities

Status: **implemented and validated in Unity 6000.4.0f1**.

Hypotheses:

- the resolved logical texture was paying for an unused 16-bit depth attachment;
- stationary presentation, aim, Animator facing, and Longwatch paths repeated native calls or scans despite unchanged inputs;
- the shared URP asset enabled generic 3D/HDR/Volume capabilities that neither current scene can use.

Retained changes:

- Added static scoped markers: `Rustline.Player.Aim`, `Rustline.Player.Motor`, `Rustline.Player.GroundProbe`, `Rustline.Presentation.NativePixelUpdate`, `Rustline.Presentation.Longwatch`, and `Rustline.Presentation.Animator`.
- Changed only the resolved/penumbra RenderTexture depth request from 16 to 0. At `1072×1072`, the removed 16-bit attachment is 2,298,368 bytes (approximately 2.19 MiB) before platform-specific alignment. ARGB32 sRGB color, Point/Clamp, AA 1, mipmap-off, and anisotropy 0 are explicitly tested.
- Kept World Camera target depth at 16. Renderer2D depth/stencil is disabled and no feature samples scene depth, but this pass did not complete the separate depthless-camera experiment plus manual pixel validation across both authored scenes required to accept that less-certain change.
- Moved `_LogicalSize` updates out of the per-frame path. Exact player/camera/projection dirty state bypasses stationary penumbra coordinate conversion; exact cached vectors suppress redundant material updates.
- Cached the 5-degree facing threshold once. `PlayerAim2D` now skips camera conversion only when pointer, AimOrigin, World Camera identity/position/rotation/projection values, presentation identity, and every native viewport field are exactly equal to the previous resolved frame. No epsilon is used. A revision changes only when the resolved continuous direction or facing changes.
- Cached Animator state hashes and facing. Longwatch validates immutable serialized configuration once per enable, uses the aim revision to avoid redundant `Atan2`/quantization, checks Body sprite plus direction before scanning the 2/6/4 Body frame arrays, and retains the sole Body Animator as its clock.
- Disabled unused URP HDR, Terrain Holes, LOD Cross Fade, 3D main/additional light rendering and shadows, mixed lighting, 3D light cookies, data-driven/screen-space lens flare, and Adaptive Performance. Set Volume Update Mode to `Via Scripting`. Repository audit found only `Light2D`, no 3D `Light`, Terrain, LODGroup, lens flare, runtime Volume, post-processing, or 3D light cookie use; both authored scenes retain their global 2D light.

Quality-profile decision:

- Standalone remains mapped to `Ultra`; the benchmark continues to force `Very Low` by name.
- The mismatch is documented rather than changed because `Ultra` and `Very Low` also encode different VSync policy and Rustline has no shipping frame-pacing requirement yet.
- The shared URP asset now makes unused expensive renderer capabilities deterministic across those profiles without changing resolution, native viewport rules, VSync, or target frame rate.

Correctness coverage added:

- final World/resolved depth contracts and all retained RenderTexture sampling/color properties;
- URP capability policy;
- aim revision stability and immediate pointer/player/camera-position/camera-rotation/projection/viewport invalidation;
- existing vertical hysteresis, Longwatch 360-degree selection, Body synchronization, locomotion, jump, dust, respawn, palette, raw/penumbra orientation, and toggle tests remain in place.

Validation evidence and limits:

- The pre-edit benchmark build was attempted with Unity `6000.4.0f1`, but the local Unity Licensing Client repeatedly timed out and reported the headless entitlement unavailable before project compilation. No baseline report was produced.
- After allowing Unity to reach its local licensing service, the Editor compiled cleanly and the full EditMode suite passed **126/126**.
- The batch PlayMode suite passed **15/16**. Its sole failure was the already documented environment-sensitive stage-B RenderTexture readback (`MovementLab_RendersWorldPixelsThroughRawAndPenumbraRenderGraphPaths` saw zero resolved pixels). The same complete suite then passed **16/16** in the graphical D3D11 Editor, including the raw and Penumbra RenderGraph readbacks. This establishes that the batch result is not a new presentation regression.
- The Windows Development benchmark built successfully. The short `control-off` smoke succeeded and auto-quit with block stability max/min `1.003537`.
- The full six-pair `control-off` A/A succeeded at physical `1920×1080`, logical `1072×1072`, Very Low, VSync 0. Its 12 chronological block means stayed between `10.235874 ms` and `10.284769 ms` (max/min `1.004777`, CV `0.001501`). Mean absolute false A/B delta was `0.017725 ms`, with a maximum of `0.041016 ms`. The harness is stable enough for later comparisons, but no before/after percentage is claimed because licensing prevented a pre-edit baseline.
- A generated-solution MSBuild attempt was not a valid substitute for Unity compilation: its package reference paths were stale/missing. The later Unity test and player-build results supersede that diagnostic.
- The available benchmark allocation counter is whole-frame only and provides no call stacks. Across the full run, block means were `4553.63–4583.18 B/frame`, p95 was consistently `4388 B`, and isolated maxima were `39040–41616 B`. Without reliable attribution to Rustline code, no speculative allocation rewrite was made and the historical approximately `4400 B/frame` observation remains engine/user-code unattributed.

Expected impact:

- guaranteed logical-target GPU memory reduction from removing the resolved depth attachment;
- fewer main-thread managed-to-native camera/material/renderer calls during idle and steady aim;
- fewer Longwatch trigonometric calculations and Body-array comparisons between actual aim/frame changes;
- less URP feature setup/variant surface and no per-frame Volume stack update for the current no-Volume presentation;
- profiler attribution in Development captures with no marker Begin/End overhead in non-Development Release builds.
