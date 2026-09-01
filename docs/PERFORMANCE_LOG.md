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

Status: **testing**.

Change:

- `Assets/Settings/Renderer2D.asset`
- `m_UseDepthStencilBuffer: 1` → `0`

Rationale:

- Rustline currently has no `SpriteMask` usage in the repository.
- Unity documents the 2D Renderer Depth/Stencil Buffer as optional when features that require it are not used, and notes that disabling it may improve performance, particularly on mobile hardware.
- This experiment changes one renderer variable only. No gameplay, camera, physics, lighting content, art, or pixel-perfect settings are intentionally changed.

Acceptance rule:

- First confirm no visible regression in MovementLab.
- Repeat the same 3 idle + 3 traversal samples under the same Game view size, VSync state, target frame rate, and 2.0 s HUD window.
- Keep the setting disabled if it is visually safe and either improves performance measurably or removes an unnecessary buffer at no observed cost.
- Re-enable it if it causes rendering regressions or future required features genuinely depend on depth/stencil.
