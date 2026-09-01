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
