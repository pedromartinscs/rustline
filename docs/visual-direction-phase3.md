# Visual direction, phase 3: Salvage Intake background system

## Starting point and diagnosis

Fetched origin before branching. `codex/visual-direction-phase3` starts directly
from completed Phase 2 `2a39ec48ae3e8c50ee7d862245e9f5ca1ad38fcd`, matching
`origin/codex/visual-refinement-phase2`; main was not used as the new baseline.

Phase 1's 1024×768 service wall established a good industrial language, but its
world bounds (-34..30, -16..32) exposed the older irregular backdrop at its edges.
Elsewhere, large high-contrast abstract shapes interrupted the silhouettes of
otherwise strong machinery. The old decorative structure Tilemap also repeated
the noisy gameplay terrain texture behind the route.

Inspection covered both previous reports, scene/prefab references, the service
wall generator, hardening/refinement tools, sorting, camera follow, native
viewport/penumbra rendering, scene regeneration, collision guardrails, and the
existing parallax implementations. Approved I-beam PNGs/metas, health-bar code,
actors, gameplay geometry, controls, camera behavior, and progression are retained.

## Existing systems and chosen architecture

The old `PixelSnappedParallax2D` repeats three 2160×1080 segments horizontally,
follows 94% of camera X, and locks Y to the screen. The separate
`PixelSnappedVerticalDepthBackdrop2D` uses one opaque 1080×4320 altitude image,
locks X to the screen, and maps camera altitude over a minimum virtual 270u span.
Those behaviors served the earlier backdrop design, but neither is a two-axis
repeatable architectural layer.

`PixelSnappedBackgroundPlane2D` adapts the same centered-wrap and final 1/16u snap
model to both axes. It is a small presentation component, not a camera controller
or general environment framework. Two instances share the existing unlit
material. The renderer repeats a 512×512 source over a 128u square; wrapping the
renderer by a whole 32u source period preserves the image phase.

Local architecture is an editor-authored Tilemap on an 8u grid, using sixteen
128×128 modules from one 512×512 atlas. This provides regional structure without
duplicating room-sized wall pictures. The approved Phase 1 service wall remains
unchanged above those modules. The existing full production command now reapplies
this configuration after rebuilding the managed environment and gameplay rig.

## Assets and art direction

`python Tools/EnvironmentArt/build_industrial_background.py` builds three compact
512×512 textures using the existing Python/Pillow workflow and Canonical 28:

- `far_infrastructure.png`: opaque distant columns, closed machinery masses,
  and trusses in Deep Space / Deep Navy. Matching opposite edges.
- `mid_service_network.png`: transparent interstitial space around remote
  panels, beams, flanged pipes, and service structures. Matching opposite edges.
- `architecture_modules.png`: sixteen local panels, recesses, bulkheads, vents,
  pipe banks, conduits, shaft sides, windows, trusses, relays, ducts, and columns.

All production art remains 16 PPU, point filtered, uncompressed, without mipmaps,
rescaling, partial alpha, or generated physics shapes. Source pixels, transforms,
and cell sizes agree; no fractional art scaling is used. New sprite/tile GUIDs
are retained across reapplication. No third-party assets, dependencies, shaders,
lights, bloom, or additional render targets in ordinary gameplay were introduced.

Deep Navy backing and sparse Shadow/Steel Shadow rims reuse the Phase 1 material
language. Corrosion stays at a few joints. Cyan appears only in a few small
service indicators. Player/threat accents and bright walking faces remain above
the background in contrast and sorting.

## Depth, sorting, and motion

| Plane | Sorting | Camera follow X/Y | Screen-relative scenery travel X/Y |
| --- | ---: | --- | --- |
| Far infrastructure | -40 | 0.94 / 0.96 | 6% / 4% of camera travel |
| Mid service network | -30 | 0.80 / 0.86 | 20% / 14% |
| Local architecture modules | -25 | World anchored | 100% / 100% |
| Approved service wall | -24 | World anchored | 100% / 100% |
| Existing machinery, platforms, actors, effects | Existing orders | Existing behavior | Unchanged |

The camera-follow fractions use the existing 0.94 horizontal far factor as an
anchor. Across the roughly 217u route they produce about 13u of far scenery
travel and 43u of middle scenery travel; a 34u climb produces about 1.4u and 4.8u
respectively. This is enough separation to show depth while keeping distant
infrastructure calm. All final coordinates snap to source pixels. The existing
camera smoothing, player-centered penumbra, and integer presentation are untouched.

The two obsolete roots remain inactive, with their SpriteRenderer references
cleared. Their original MonoBehaviour configuration bytes are retained for the
strict preservation check, but they perform no runtime work and their old PNGs
are absent from Salvage Intake's dependencies. The old noisy decorative Tilemap
renderer is disabled; its stored cells remain intact. No collision component or
gameplay Tilemap was replaced or hidden to achieve the background change.

## Regional architecture and coverage

`RustlineIndustrialBackgroundSetup.ModuleForCell` is the small authoring layout:

- Entry/transfer: quiet panel and conduit continuation around the original wall.
- First shaft: open sided bracing and distant infrastructure through the rise.
- Processing gallery: plant backing, vent/service recesses, alternating windows,
  and high trusses.
- Maintenance: closer panels, relay cabinets, and conduits leading into another
  open vertical shaft.
- Rail transfer: ducts below, sparse columns, broad voids, and high reinforcement.
- Extraction approach: bulkhead/enclosure/recess combinations connected to the
  existing machinery and extraction frame.

The local grid spans x=-64..240 and y=-40..72, beyond the playable route and its
maximum viewport margins. Open cells intentionally expose the two parallax
depths. Each parallax plane covers 128×128u; its camera-relative center drifts by
at most half a 32u repeat plus half a source pixel. A maximum 1072-square viewport
needs only 67u, leaving over 14u of additional total margin after worst-case
drift. The opaque far plane guarantees coverage even outside local architecture.

## Actual rendered comparisons

Seven matching before/after 640×480 logical views were captured from Unity URP.
Camera = held player root + (0,2), with dormant encounters; the overlay status
HUD is excluded. These are actual resolved render targets, not fabricated
composites or normal-window desktop screenshots.

| View | Player root | Before | After |
| --- | --- | --- | --- |
| West entry | (-20,1) | [Before](reference/visual-direction-phase3/west-before.png) | [After](reference/visual-direction-phase3/west-after.png) |
| Transfer bay | (8,1) | [Before](reference/visual-direction-phase3/transfer-before.png) | [After](reference/visual-direction-phase3/transfer-after.png) |
| First shaft | (30,8) | [Before](reference/visual-direction-phase3/shaft-before.png) | [After](reference/visual-direction-phase3/shaft-after.png) |
| Processing gallery | (62,17) | [Before](reference/visual-direction-phase3/gallery-before.png) | [After](reference/visual-direction-phase3/gallery-after.png) |
| Maintenance rise | (124,25) | [Before](reference/visual-direction-phase3/maintenance-before.png) | [After](reference/visual-direction-phase3/maintenance-after.png) |
| Rail/recovery area | (146,24) | [Before](reference/visual-direction-phase3/rail-before.png) | [After](reference/visual-direction-phase3/rail-after.png) |
| Extraction approach | (188,29) | [Before](reference/visual-direction-phase3/extraction-before.png) | [After](reference/visual-direction-phase3/extraction-after.png) |

The reviewed gallery, shaft, maintenance, and extraction renders now read as
different sections of one steel facility. The camouflage backdrop is absent;
machinery silhouettes are clearer. The entry/transfer wall and beam floors retain
their approved appearance. Terrain texture and repeated small modules remain
visible art limitations, especially in broad later views.

The central transfer 640×480 before/after PNGs are byte-identical: this view is
fully inside the approved service wall. Its preservation is intentional; the
new system supplies continuity beyond that wall and at larger viewports.

The normal Release player also captured all seven views at an actual
**1920×1080 window / 1072×1072 logical viewport**, using the unchanged integer
presentation and penumbra. These are after-only larger-view checks; the matching
before/after comparison above is at 640×480. All seven larger views were inspected:
[west](reference/visual-direction-phase3/west-release-1920x1080.png),
[transfer](reference/visual-direction-phase3/transfer-release-1920x1080.png),
[shaft](reference/visual-direction-phase3/shaft-release-1920x1080.png),
[gallery](reference/visual-direction-phase3/gallery-release-1920x1080.png),
[maintenance](reference/visual-direction-phase3/maintenance-release-1920x1080.png),
[rail](reference/visual-direction-phase3/rail-release-1920x1080.png), and
[extraction](reference/visual-direction-phase3/extraction-release-1920x1080.png).
These PNGs contain the final presentation-pass output with its actual centered
viewport and margins; the overlay status HUD is excluded. No old camouflage or
uncovered background edge was seen. The existing circular penumbra still darkens
the outer viewport, and the wider framing makes module repetition more apparent.

## Dynamic validation

The Play Mode route drives the actual unchanged `PixelCameraFollow2D` through
horizontal travel, both shaft rises, the gallery parallax wrap, rail, and terminal
at a 7u/s follow-target pace. Physics/encounters are held for camera validation;
this is not a claim of human traversal. Every sampled frame checks the rendered
camera/layer position, whole-pixel snapping, maximum viewport bounds, and the
unchanged world position of the service wall. The test requires a period wrap.
Actual resolved PNGs are exported during the route, including denser shaft and
wrap sequences. The pure positioning calculation also checks 10,000 calls for
zero managed allocation.

The final traversal passed **2,106 frames**, including **one horizontal period
wrap**, both shaft rises, and every-frame coverage of the maximum 1072-square
logical viewport. The service wall remained anchored. 10,000 positioning calls
allocated **zero managed bytes**. These assertions establish geometry, phase,
and snapping, rather than claiming an automated perceptual test of shimmering.

Actual consecutive rendered frames are retained as compact review clips:
[shaft rise](reference/visual-direction-phase3/shaft-motion.gif) and
[gallery / middle-layer wrap](reference/visual-direction-phase3/gallery-wrap-motion.gif).
Each clip samples every six test frames, presented at approximately 10 fps;
there is no interpolated artwork. Frame inspection shows architecture continuing
through these camera movements without exposed camouflage or coverage gaps.
Remaining manual acceptance covers sustained real combat, subjective parallax
strength, continuous-motion shimmer on physical displays, and hardware-specific
display behavior. Static screenshots alone are not used to certify motion.

## Validation and performance records

Local temporary evidence and XML/CSV results live under
`Logs/IndustrialBackground/` and `Logs/industrial-background-*.log/xml`. The
representative images linked above are versioned; large temporary frame sets,
builds, and logs are not.

Strict preservation uses `python Tools/EnvironmentArt/validate_scene_preservation.py
2a39ec4 --allow-industrial-backgrounds`. Only the two new background presenter
components are allowed; every original gameplay/physics/health-bar component and
both collision maps must remain byte-identical. Unity initially normalized the
empty `m_Name` whitespace on fourteen health-bar records. That formatting is
restored to the Phase 2 bytes before the final strict check; no health-bar logic
or setting is changed and the check is not relaxed for existing components.

The background cost test loads the Phase 2 scene from Git and the current scene
in sequence, restores the current file in a finally block, hides other world
renderers, and samples the same three camera views after warmup. Its CSV reports
active background SpriteRenderers/TilemapRenderers, unique referenced texture
bytes, and available Unity draw/batch/main-thread counters. Shared overlay and
compositor work remains, so these are Editor comparison samples rather than
standalone GPU timings.

| Validation | Final result |
| --- | --- |
| Full Edit Mode suite | **439 / 439 passed** |
| Full Play Mode suite | **116 / 116 passed** |
| Scene regeneration / reapplication | Passed within Edit Mode; repeated scene IDs and atlas sprite GUIDs stable |
| Strict Phase 2 scene preservation | **128 protected component/map blocks identical** |
| Collision footprint | Ground: 890 cells, SHA-256 `682e2f083f1f563383aa4aa3b547519e7f265166d8517fbbe44f6e01354e568e`; maintenance: 6 cells, `328ffd3ad0ddf78bb8d21da54bfdff61d2201acd2161c64b09bd43fbfd5396be` |
| Production environment palette audit | **46 PNGs**, zero off-palette opaque pixels, zero partial-alpha pixels |
| New art/import checks | Passed: canonical palette, binary alpha, 16 PPU, point filtering, no compression/mipmaps/physics shapes, matching repeat edges |
| Traversal / maximum viewport / allocation | Passed as described above |

The initial full Edit Mode run exposed three failures. The base graybox validator
selected the first Grid in the preserved hierarchy, which was now decorative.
It now validates the explicitly named managed gameplay grid with the same 1u
contract. No grid or physics data changed. The other two failures were new test
scene leakage into existing wall-brace fixtures; closing the inspected scene in
TearDown fixed isolation. The wall-brace fixture passed all 14 tests in isolation,
and the complete rerun passed. Existing assertions were not weakened.

The measured [render-cost CSV](reference/visual-direction-phase3/render-cost.csv)
covers transfer, gallery, and extraction:

| Background cost | Phase 2 | Phase 3 |
| --- | ---: | ---: |
| Active SpriteRenderers | 5 | 3 |
| Active TilemapRenderers | 1 | 1 |
| Unique referenced textures | 4 | 4 |
| RGBA32 source texture bytes | 31,188,480 (29.74 MiB) | 6,291,456 (6.00 MiB) |
| Unity runtime texture memory counter | 56,928,544 (54.29 MiB) | 11,537,952 (11.00 MiB) |
| Median main-thread sample | 16.654–16.656 ms | 16.655–16.657 ms |

The active sprites are two repeat planes plus the original local wall. Four old
SpriteRenderer objects still exist, inactive and without sprite references;
the total authored SpriteRenderer count is seven. The replacement textures total
3 MiB, plus the retained 3 MiB service wall. The old decorative terrain atlas is
no longer referenced by an active background renderer. Unity's runtime texture
counter includes its texture-side storage and is not whole-process RAM.

Draw-call/batch counters were unavailable in this batch-mode Editor (`-1` in
the CSV), so draw-call impact is **unmeasured**. The main-thread results sit at
the roughly 60 fps pacing limit and establish no GPU speedup. No new lighting,
per-frame allocation, renderer discovery after camera caching, or runtime art
generation is introduced by the two presenters. GPU timing and overdraw remain
manual profiling work.

## Windows Release and standalone verification

`RustlineFirstPlayableSetup.BuildWindowsRelease` succeeded (Editor exit 0), using
the existing **Windows x64 / BuildOptions.None** production pipeline. The actual
non-development executable is `Builds/FirstPlayable/Rustline.exe`. The exported
Salvage Intake collision guard baked **4 paths / 78 points** for Ground Collision
and **1 path / 4 points** for Maintenance Crouch Collision. The MovementLab guard
also retained **9 paths / 48 points**. The guard and runtime collision initializer
algorithms are unchanged.

With the Editor closed, the existing standalone command
`--first-playable-smoke --smoke-auto-quit` at 1280×720 returned **exit 0 /
FIRST_PLAYABLE_SMOKE_PASS**. It verified title entry, real Tilemap support,
keyboard traversal/jump/landing and extension seam, all seven hitscan encounters,
checkpoints, retry, extraction, and restart. Fight staging is automated; this does
not certify full-route human traversal or combat balance.

The additional opt-in `--industrial-background-review` at 1920×1080 returned
**exit 0 / BACKGROUND_RELEASE_REVIEW_PASS**, with seven actual 1072-square resolved
views and their final presentation output. Camera following remained enabled;
actors/encounters were held for repeatable captures. Capture metadata is retained
in [release-capture.txt](reference/visual-direction-phase3/release-capture.txt).

The existing unsupported unused lens-flare shader messages and a shutdown
ComputeBuffer disposal warning appeared in the standalone logs. Neither run
failed; the disposal warning also appears in Phase 2's report and is outside this
background change. No new post-processing effect is enabled.

## Main implementation files

- `Assets/Editor/RustlineIndustrialBackgroundSetup.cs`: imports, stable module
  IDs, authored sections, coverage/sorting checks, and legacy retirement.
- `Assets/Scripts/Presentation/PixelSnappedBackgroundPlane2D.cs`: cached-camera,
  two-axis wrapped native-pixel positioning.
- `Assets/Art/Environment/IndustrialBackground/`: three PNGs, sixteen Tile assets,
  and Unity metadata; generated by
  `Tools/EnvironmentArt/build_industrial_background.py`.
- `Assets/Scenes/Demo/SalvageIntake.unity`: authored presentation only. All original
  blocks apart from two legacy active flags, four old SpriteRenderers, one old
  background TilemapRenderer, and the preserved art root's child list are exact
  matches to Phase 2.
- `Assets/Editor/RustlineProductionTools.cs` and
  `Assets/Editor/RustlineSalvageIntakeSetup.cs`: production reapplication and
  unambiguous gameplay-grid validation.
- `Assets/Tests/EditMode/IndustrialBackgroundTests.cs`,
  `Assets/Tests/PlayMode/IndustrialBackgroundPlayModeTests.cs`, and
  `Assets/Tests/EditMode/EnvironmentVisualHardeningTests.cs`: focused contracts,
  actual camera/render evidence, comparison measurements, and regeneration.
- `Assets/Scripts/Diagnostics/IndustrialBackgroundReview.cs`: explicit
  `--industrial-background-review` Release-only review entry point, inactive in
  ordinary play. It holds actors for captures and allocates a temporary final-pass
  readback target only during that diagnostic command.
- `Tools/EnvironmentArt/validate_scene_preservation.py`: explicit allowance for
  exactly two new background components, retaining strict existing-record equality.

## Remaining visual problems

The destructible terrain atlas still has dense texture, and actor/machinery rust
accents sometimes overlap. Those approved gameplay materials and sprites were
not changed in Phase 3. The modular wall grammar is intentionally restrained but
still repeats in large views; future art work can add a few authored landmark
modules without changing this system or adding room-sized texture copies.

The main tutorial/status HUD remains a separate refinement. No new atmospheric
lighting or foreground occlusion system is introduced. Hardware GPU/frame-time
profiling and human combat review remain distinct from automated camera and
Release smoke validation.
