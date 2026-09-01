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

The next major performance-relevant system is the native-pixel viewport + palette-constrained penumbra. It should ship with a development-only runtime toggle so its incremental cost can be measured directly with the same scene and later in a standalone Development Build.
