# Rustline Performance Strategy

Performance is a first-class design constraint for Rustline. The target is not merely to make the game run acceptably on the developer machine; the project should remain easy to run on a wide range of devices while preserving deterministic pixel-art presentation.

## Performance goals

- **60 FPS is the minimum acceptable gameplay target on weak supported hardware.**
- **120 FPS should be comfortable on reasonable modern hardware** when the display/runtime is allowed to run that fast.
- On stronger hardware, prefer low frame time, low power use, and stable pacing over consuming all available GPU/CPU budget unnecessarily.
- Gameplay feel and visual correctness must not be sacrificed for theoretical micro-optimizations.

These are design goals, not claims about current measured performance. Device tiers and concrete minimum specifications will be established later from real profiling.

## Optimization method

Rustline uses a measured, incremental optimization workflow:

1. Establish a repeatable baseline.
2. Change **one meaningful variable at a time** where practical.
3. Re-run the same test.
4. Keep a change only when it improves the intended metric without damaging gameplay, visual quality, maintainability, or correctness.
5. Revert or defer changes whose benefit is not measurable or whose complexity is not justified.

Avoid optimization by superstition. Unity/URP settings that look expensive are candidates for measurement, not automatically proven bottlenecks.

## Current baseline instrumentation

`MovementLab` has a development-only diagnostic HUD implemented by `MovementLabPerformanceHud`.
It is hidden by default so ordinary play does not continuously sample, format strings, or run
IMGUI labels. Press `H` or `F3` to show/hide it; `P` continues to toggle Penumbra while hidden.

It now samples in **2.0-second windows** and displays:

- frames per second;
- average frame time in milliseconds;
- worst frame time inside the current sample window;
- number of frames included in the completed sample;
- current screen resolution;
- current VSync count;
- `Application.targetFrameRate`.

The original 0.5-second window proved too sensitive to Unity Editor scheduling and other short-lived disturbances for trustworthy before/after comparisons, so it was deliberately increased to 2.0 seconds before any real optimization was attempted.

Clicking the performance readout copies the current metrics directly to the system clipboard. This is the preferred quick-sharing workflow during Pedro ↔ Echo performance iteration because taking screenshots or invoking screen-capture software can perturb frame timing and contaminate the sample being measured.

The HUD is created only when `MovementLab` starts and is compiled only for the Unity Editor or Development Builds. It is not intended to ship in normal release builds.

Editor FPS is useful for quick comparisons but is not a final benchmark because Editor overhead can distort results. Later milestone/performance gates should also use standalone Development Builds and Unity Profiler captures on representative hardware.

### Early instrumented observations

An initial Editor observation at `1086×420` showed approximately `104.5 FPS` / `9.56 ms AVG`, with `VSync 0` and `Target -1`. That reading was captured using screenshot software and included a `117.45 ms WORST` sample, so it is **not accepted as a clean worst-frame baseline**.

Clipboard-based 0.5-second samples then produced the following ranges:

- idle/standing: roughly `67.8–205.4 FPS`, `4.87–14.75 ms AVG`, `7.43–23.43 ms WORST`;
- active traversal: roughly `35.6–78.7 FPS`, `12.70–28.06 ms AVG`, `17.29–39.46 ms WORST`;
- all samples: `VSync 0`, `Target -1`, `1086×420`.

That spread is too large to use as a reliable micro-optimization baseline. It is treated as evidence that the short Editor sample window is noisy, not as evidence that movement itself necessarily costs the full difference between the idle and traversal numbers. The instrumentation was therefore stabilized before changing rendering or gameplay settings.

## Rendering budget and native-pixel presentation

The approved camera prototype starts from a canonical maximum viewport of **1072×1072 production pixels**, or **67×67 16 px tiles**. That is approximately **1.15 million logical pixels**.

The performance intent is to render gameplay at the logical/native-pixel presentation size and use nearest-neighbor **integer upscaling** when a larger physical display permits 2×, 3×, 4×, etc. A 4K monitor should not force Rustline to shade the gameplay world at full 3840×2160 merely because those physical pixels exist.

Smaller displays crop the native-pixel world rather than shrinking production art below 1×. Larger displays do not reveal additional gameplay world beyond the canonical viewport solely because they have more pixels; unused presentation area resolves to canonical Deep Space `#01020B`.

## Runtime architecture principles

The following principles are approved directions for future systems:

- Keep distant/inactive gameplay entities asleep when they do not need simulation.
- Treat visual range, simulation range, and audible range as independently tunable distances.
- Use Tilemap/chunk-oriented world construction so large facilities do not require every region to remain active simultaneously.
- Prefer object pooling for high-churn combat objects such as projectiles, impact FX, and other repeated transient entities.
- Avoid repeated `Instantiate`/`Destroy` churn during active combat when pooling is appropriate.
- Avoid hundreds of independent `Update()` loops when a simpler centralized/ticked architecture can provide the same behavior clearly.
- Keep 2D lighting selective and purposeful rather than filling scenes with overlapping lights by default.
- Minimize unnecessary materials, shader variants, renderer features, and full-screen passes.
- Avoid very large transparent sprites/layers when a smaller bounded representation can produce the same result.
- Keep pixel-art textures small, point-filtered, uncompressed where required for exact production pixels, and free of unnecessary mipmaps.
- Preserve the existing separation between visual Tilemaps and collision Tilemaps.
- Profile before replacing clear code with lower-level or more complex alternatives.

## Penumbra performance direction

The player-centered penumbra should be implemented as a small number of cheap presentation operations, ideally a single bounded screen-space/presentation pass rather than many overlapping lights or scene objects.

The approved visual geometry for the initial prototype is documented in `ART_DIRECTION.md`:

- fully visible circle: 57 tiles / 912 px diameter;
- penumbra thickness: 4 tiles / 64 px radially;
- full darkness reached at 65 tiles / 1040 px diameter;
- canonical viewport: 67×67 tiles / 1072×1072 px;
- full darkness color: Deep Space `#01020B`.

### Palette lookup table

Rustline has only 28 legal production colors. The penumbra annulus uses a runtime-generated,
point-sampled `1024×160` RGBA32 sRGB lookup rather than searching all 28 colors per fragment.
Each linear RGB channel is quantized to 5 bits; RGB occupies `1024×32` cells and the five
darkness levels are stacked vertically. The 655,360-byte (640 KiB) texture stores the final
Canonical 28 output directly.

For a small number of discrete shadow levels, a lookup can map each canonical source color to another canonical darker color. Pixel-pattern dithering can then transition spatially between those palette-safe levels and ultimately Deep Space.

The table is generated once when presentation is enabled and its CPU copy is discarded after
upload. Tests prove that all canonical colors occupy distinct quantized cells, resolve to
themselves at level 0, retain the authored 28×5 darkness mapping, never leave Canonical 28,
and always reach Deep Space at level 4. This remains a presentation-only lookup; source art is
not converted to indexed textures.

## Focused runtime optimization pass

The September 2026 pass retained a small set of structurally verifiable changes:

- static `ProfilerMarker` scopes identify player aim, motor, ground probe, native-pixel update, Longwatch presentation, and animation work in Editor/Development captures; Unity compiles marker Begin/End calls out of non-Development Release builds;
- the persistent resolved/penumbra RenderTexture is depthless; the World Camera target retains
  16-bit depth because Unity 6.4 RenderGraph rejects a depthless camera output texture even
  though Renderer2D depth/stencil is disabled and no current feature samples scene depth;
- stable source texture and orientation bindings are set only when materials, targets, or the
  Penumbra selection change; RenderGraph callbacks retain texture dependencies but only clear,
  set viewports, and draw;
- native-pixel material parameters are exact-dirty-checked, and the player/camera-derived penumbra values are not recomputed in a stationary frame;
- aim conversion skips its two camera-space conversions only when every current conversion input is exactly unchanged; an exact aim revision prevents Longwatch from repeating angle quantization for an unchanged resolved aim;
- Animator state names are cached as hashes, renderer facing writes happen only on an actual facing change, and Longwatch validates immutable configuration once per enable and scans Body frames only after its Body/direction cache misses;
- the active URP asset disables unused HDR, Terrain Holes, LOD Cross Fade, 3D main/additional lights and shadows, mixed lighting, 3D light cookies, both lens-flare systems, and Adaptive Performance. Volume updates are `Via Scripting`; current cameras have post-processing off and the repository contains no authored runtime Volume.
- ordinary SpriteRenderers and TilemapRenderers in MovementLab and ArtShowcase use URP
  `Sprite-Unlit-Default`; their redundant white, intensity-1 Global Light2D objects were removed
  after an exact D3D11 Color32 comparison found zero differing pixels. Renderer2D defaults new
  sprite renderers to Unlit while retaining the Lit material reference for future intentional
  2D lighting.

Depth Texture, Opaque Texture, MSAA, Renderer2D depth/stencil, and SRP Batcher retain their accepted settings. This current-scene optimization does not prohibit a future authored Lit renderer plus purposeful Light2D where lighting changes the accepted image.

### Shipping quality policy

The benchmark still resolves and forces `Very Low` by exact name, while Standalone still defaults to `Ultra`. The profiles differ in VSync and other generic quality fields, and the repository does not define a shipping frame-pacing policy. The default mapping therefore remains unchanged rather than inventing one. Expensive rendering capabilities Rustline cannot currently display are disabled in the shared URP asset, making that part of the runtime policy deterministic across quality levels. A future shipping-settings milestone must choose VSync/target-frame-rate policy explicitly before changing the Standalone default.

## Baseline test discipline

For quick Pedro ↔ Echo iteration in `MovementLab`:

1. Use the same Unity version and the same MovementLab scene, then press `H` or `F3` to show the HUD.
2. Let the scene run for several seconds before reading values.
3. Record whether VSync or a target frame-rate cap is active.
4. Compare idle/standing measurements under the same conditions first.
5. Click the HUD to copy the current completed sample rather than taking a screenshot.
6. When useful, repeat while continuously traversing the course to include animation, Rigidbody2D movement, camera following, and Tilemap rendering.
7. Change one thing, repeat the same measurement, then decide whether to keep it.

As the game grows, synthetic stress scenes and standalone profiling will replace subjective FPS checks for serious performance decisions.
