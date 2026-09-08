# September 2026 repository performance audit

Scope: Unity 6000.4.0f1 / URP 17.4, starting at `930c50a`. The repository, including runtime systems, shaders, configuration/prefabs/scenes, Editor/build tooling, both test suites, and project documentation/history, was reviewed before editing. No gameplay tuning, source art, shader math, rendering passes, physics query counts, or frame-pacing policy was intentionally changed.

## Findings and decisions

| Area | Evidence and decision |
| --- | --- |
| Movement / environment probes | Rigidbody2D simulation, latched jump edges, coyote/buffer timing, nonalloc casts and cached contact filters are already appropriate. Ground ascent and impossible wall-brace queries are already gated. Preserve these, clearance behavior, and wall-kick lock semantics. |
| Aim / animation / armed layers | Exact camera/viewport/input caches and aim revisions already avoid redundant conversions and quantization. Body remains the sole Animator clock; sprite changes drive the small overlay mappings. Keep continuous gameplay aim independent from visual quantization. |
| Camera follow | Repeated target position reads and identical snapped Transform writes remained. Read target/own positions once and skip exact unchanged writes while still advancing continuous smoothing and applying presentation offsets. |
| Weapons / feedback | Raycast buffers, cooldown and authoritative result reuse already avoid per-shot array allocation and duplicate impact queries. Reject farther candidates before collider/Transform access and reuse the selected collider reference. Inactive trace/impact timers now return on managed flags instead of querying both renderers every frame. |
| Health / enemy / encounter | Value-type damage, explicit child-hitbox routing, health events, and reset of the same enemy are retained. Patrol still simulates each fixed step; one enemy does not justify a scheduler or ECS conversion. |
| Native-pixel / RenderGraph | Persistent color targets, depthless resolved target, explicit imported dependencies, `WriteAll`, backbuffer switch and utility camera are retained. Cache the six import metadata fields per target at configuration time. Penumbra OFF no longer polls player/camera parameters; enabling refreshes immediately, even after LateUpdate. Cache the constant linear clear color. |
| Shaders / palette | Retain the 5-bit RGB darkness lookup, canonical ramps, squared radius rejection, world-anchored Bayer pattern and exact sRGB/orientation rules. No shader rewrite or texture memory reduction. Unity's installed Color32 has typed Equals overloads, so the palette equality loop was not treated as a boxing bug. |
| HUD / benchmark | A hidden `OnGUI` method still registers GUI processing. A separate fixed-rectangle view exists only after the HUD is shown or the benchmark finishes, and is disabled when the HUD/owner is hidden/disabled. Sampling, hotkeys, protocol, recorder and statistics are retained. This also changes benchmark instrumentation overhead; old timing results are not interchangeable with new builds. |
| Diagnostics / Editor | Custom markers, spike analyzer self/inclusive-time distinctions and bounded capture analysis remain intact. Editor-only LINQ, source-pixel validation, rebuild/discovery operations and report strings are outside gameplay hot paths. No fresh spike capture is used to infer a frame-time bottleneck in this pass. |
| Configuration / content | Current URP already prunes unused 3D lights, shadows, depth/opaque textures and post effects; ordinary world renderers are Unlit. The camera-output target's 16-bit depth requirement remains. Keep the explicit 60 FPS shipping cap and benchmark's separate uncapped normalization. Rare jump-dust Instantiate/Destroy is not a justification for a pool. |

## Correctness findings

- Baseline EditMode: 190/191 passed. The lone stale assertion expected crouch height 1.75; both defaults and saved config now explicitly test the accepted 1.05 × 2.375 size, offset (0, 1.1875), and common foot anchor.
- Three existing PlayMode failures reproduced with the original presentation runtime. They were not masked by changing product behavior or weakening assertions.
- Commit `2bf22f9` updated the precision crouch builder/config/docs without updating `MovementLab.unity`. Its saved whole-cell ceiling blocked the taller accepted crouch collider. The existing M1A builder synchronized the scene, moved those nine visual cells into the approved static Ground BoxCollider ceiling at +10 source pixels, and baked/validated the real floor Composite. No builder, runtime collision initializer, or build guard logic was changed.
- The crouch animation fixture did not establish aim-facing before testing reverse playback and its extended traversal could enter the low ceiling before attempting to stand. It now establishes right-facing aim and uses open floor, retaining the exact 5→0 sequence, speed, layer synchronization, collider and stand assertions.
- Batch PlayMode renders the world camera's RT but had no visible Game View for the on-screen utility camera. The graphics fixture now redirects that camera to its existing physical probe RT before testing the logical Penumbra stage too. Product rendering is unchanged; assertions still require actual world, resolved and physical pixels across repeated toggles.
- README's M1-only/fallback status, the movement input list, the weapon presenter/clip documentation and the historical benchmark gate were corrected. Previous inconclusive measurements remain historical evidence, not claimed wins.

## Evidence classification

The cache/dirty-state changes structurally remove known operations. They are not an FPS percentage claim. No GPU bandwidth, pass-count, texture-footprint or physics-query-count reduction is claimed. Ordinary presentation changes introduce no new per-frame managed allocations; diagnostic view/delegate allocation happens on first display, outside benchmark measurement.

Benchmark results and exact validation outcomes are recorded in the dated entry in [PERFORMANCE_LOG.md](PERFORMANCE_LOG.md). Full machine reports, test XML, build logs and temporary release-smoke source remain under `Logs/` for local inspection. Short smokes establish functionality/allocation observations, not small timing wins. Whole-frame allocation counters do not identify allocation call stacks.

## High-impact architecture opportunities

1. **Bounded enemy simulation when encounters grow.** Current patrol executes every fixed step even far outside the playable camera area. For workloads with dozens of enemies across distant rooms, explicit encounter activation/sleep ranges and staggered low-frequency decision ticks could eliminate irrelevant simulation and script dispatch. Keep active nearby physics at the accepted fixed cadence and make simulation range independent from the Penumbra artwork. Complexity medium; regression risk medium/high (wake timing, patrol/reset and damage continuity). Defer until a 1/10/50-enemy Player profile shows this cost. Measure Physics2D and script time, p95/p99, allocations, and deterministic wake/damage/reset behavior.
2. **Remove a URP camera context only if profiling justifies it.** The utility camera has no scene culling, but still enters URP as a second camera. A world-camera-driven final presentation pass could reduce pipeline setup/CPU work if that is a material part of PlayerLoop/render-thread cost. Complexity high; regression risk high for output targets, backbuffer ownership, orientation, resize and integer viewport rules. Defer: the current two-camera path is small, explicit and tested. Require independent CPU/render-thread evidence, normalized A/A then comparable builds, exact image comparisons at crop/1×/2× sizes, toggles, resize/re-enable, and a non-development Windows smoke before adoption.

No ECS, Jobs/Burst, custom allocator, broad pooling or renderer replacement is justified by the current one-player/one-enemy workload.
