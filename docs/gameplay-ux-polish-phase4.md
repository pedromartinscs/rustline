# Gameplay and UX polish, phase 4

## Baseline and scope

Fetched origin before branching. `codex/gameplay-ux-polish-phase4` starts directly
from approved Phase 3 `bbdae5d6a3bb4f46bb60926f7d92f310cdaece88`, matching the fetched
`origin/codex/visual-direction-phase3`. No environment redesign is included.

The Salvage Intake scene, industrial backgrounds/parallax, machinery, I-beam PNGs,
destructible terrain implementation, enemy health-bar presentation, player motor,
camera, weapon balance, checkpoint data, and extraction rules are preserved.
Changes use the existing production scene and Input System, equipment authority,
enemy state machines, bitmap font, Canonical 28, and frontend modal controller.

## Numeric weapon selection: reproduced root cause

The existing `WeaponSlot0`–`WeaponSlot9` actions were present and subscribed by
`PlayerInputReader`. The bindings used `<Keyboard>/digit0`–`<Keyboard>/digit9`.
These are C# property-style names, rather than control paths in the installed
Input System keyboard layout. Its `Keyboard.cs` declares numeric controls as
`0`–`9` (for example `[InputControl(name = "1", ... bit = (int)Key.Digit1)]`).

A Play Mode test loaded the actual production scene and queued real low-level
`KeyboardState(Key.Digit1)` events through Unity Input System. Before correction
it failed: **expected Latch-9 slot 1, actual Longwatch slot 2**. The same test
passed after correcting just ten binding paths to `<Keyboard>/0`–`<Keyboard>/9`.
Action IDs, interactions, subscriptions, imported asset GUID, input reader,
equipment authority, navigation algorithm, and carousel animation code remain
unchanged. Mouse wheel input still uses the existing `<Mouse>/scroll` action.

Tests also cover 0/1/2, unavailable 9, wheel cycling, multiple requests during
an active visible step (latest valid numeric target wins after committed steps),
reader/equipment re-enabling, unchanged enabled-action counts, pause/resume,
discarded pre-retry edges, restart, and a title-to-play scene round trip.

## Patrol, engagement, and encounter progression

`CombatActive` remains the actor activity switch, so explicit diagnostic freezes
and existing animation policies retain their meaning. A separate `CombatEngaged`
state controls combat permission. Production encounter reset now restores living
actors, enables activity, and clears engagement. They immediately use their
existing patrol speeds, ranges, directional animation, and movement constraints.

Both actors publish a local `PlayerDetected` event. The encounter subscribes once
on enable and unsubscribes on disable. Valid perception activates that encounter;
proximity triggers and early damage still activate it through their original paths.
No actor queries global encounter progress. The director's defeated-prefix rule,
seven objectives, checkpoint snapshot, retry, and extraction logic are unchanged.

Detection requires a live, enabled player collider, the actor's existing bounded
horizontal/vertical window, and a Ground-layer line of sight. Production actors
also require their sensor to be inside the actual world-camera viewport. The
camera is cached; offscreen or distant patrols do not initiate new combat.
Range checks precede raycasts. Already committed attack timing, damage, locked
bomb targets, hit pause, death, and death presentation retain their existing rules.
MovementLab's isolated AI retains its established detection policy.

Sentry movement already checked support and obstacles. Bombardier patrol now
uses the same Ground-layer foot-ahead and wall checks, retaining its authored
horizontal bounds and height. It cannot simply slide over an unsupported breach.
No collision map, collider, speed, detection range, damage, or weapon value changed.

Patrol testing exposed an existing reset synchronization issue: a moved sentry's
Rigidbody could return to spawn while its Transform retained the previous pose.
Both actor reset paths now synchronize Transform and Rigidbody before resetting
health/presentation. This is necessary for moved patrols to reset reliably.

## Checkpoint presentation

The three existing industrial stations and their props remain at their exact
authored locations. The large checkpoint beacon outlines are hidden during normal
play. A small steel-housed status lamp is created beside each station: subdued
while locked, cyan when eligible, bright cyan once saved. Two short pixel-aligned
LineRenderers per station reuse the existing unlit material. There is no new
texture, light, collision, or trigger component.

F3 repurposes the existing beacon lines as exact trigger bounds, with saved/ready/
locked color and indexed coordinate/state rows in the developer HUD. The debug
lines are hidden during frontend modals. Authored labels, areas, required clears,
respawn transforms, healing, and ammunition restoration remain unchanged.

Activation produces a three-second **CHECKPOINT SAVED / HEALTH AND AMMO RESTORED**
notification. This timer holds while paused and expires after active gameplay.
Retry and damage have brief separate feedback; completion retains its restart
instruction. No checkpoint notification becomes a permanent status row.

## Player HUD and F3 behavior

The default HUD contains a compact health panel with exact health and ten small
segments, the current objective, and temporary context feedback. It duplicates no
weapon or ammunition information. `WeaponCarouselHud2D` remains authoritative and
unchanged. Three short entry hints expire after 18 seconds of active play; controls
remain accessible below the existing pause panel at normal supported window sizes.

The HUD uses one reusable Canvas mesh, the existing bitmap glyph atlas and existing
frontend mask shader. Its coordinates follow `NativePixelViewportMath`, centered
viewport offsets, and integer output scaling. The font gains only a period glyph
for exact developer coordinates; existing glyph locations and atlas size remain
unchanged. Opaque surfaces and glyphs retain Canonical 28 values without blended
font edges. No ordinary-play camera, render target, shader, or dependency is added.

F3 is handled by `FrontendController`, alongside its modal-safe keyboard routing:

- Defaults off on every scene load, including Release.
- Works only during unobstructed gameplay; title/options/pause and release-barrier
  input cannot toggle it or leave a deferred edge.
- Preserves the preference across retry and pause/resume; hides developer content
  and bounds while paused.
- Resets off on full run restart and scene transitions.
- Exposes health, equipped slot, encounter progress, elapsed time/retries, current
  checkpoint, and exact indexed checkpoint bounds/state. It changes no gameplay
  data and provides no commands, cheats, or privileged actions.

The normal HUD stays below the existing pause canvas (orders 1000 and 2000).
Meshes/materials/atlas are released on scene teardown. Mesh buffers are reused;
status refresh is event-driven with a bounded 5 Hz refresh for timers/debug text.
Checkpoint renderer properties update only when presentation state changes.

## Rendered evidence

Reviewed actual **640×480** engine renders:

- [Player HUD / developer off](reference/gameplay-ux-polish-phase4/player-hud-off-640x480.png)
- [Developer HUD on](reference/gameplay-ux-polish-phase4/developer-hud-on-640x480.png)
- [Paused / controls reference](reference/gameplay-ux-polish-phase4/paused-640x480.png)

The entry render includes its short, temporary movement hint. Health/objective are
the only persistent player status elements; the existing carousel remains below.
The developer panel fits above the actors at this native viewport. Tests verify
its extra geometry, whole-pixel vertices at 640×480, 800×600 and 1072-square
layouts, actual rendered Canonical 28 colors, a binary point-filtered font mask,
and visible carousel resource pixels at the bottom of the composed frame.

Reviewed actual **1280×720 Windows Release** renders (1072×720 logical viewport):

- [Player HUD / developer off](reference/gameplay-ux-polish-phase4/player-hud-off-release-1280x720.png)
- [Developer HUD on](reference/gameplay-ux-polish-phase4/developer-hud-on-release-1280x720.png)
- [Checkpoint saved / resources restored](reference/gameplay-ux-polish-phase4/checkpoint-saved-release-1280x720.png)
- [Checkpoint developer bounds](reference/gameplay-ux-polish-phase4/checkpoint-developer-release-1280x720.png)
- [Pause menu / controls](reference/gameplay-ux-polish-phase4/paused-release-1280x720.png)

The Release entry captures were taken after tutorial hints expired. A nearby
engaged sentry displays its existing health bar. The checkpoint captures show the
same station with normal presentation and exact developer bounds. Enemies were
defeated by diagnostic staging to reach that checkpoint; this is not human route
traversal evidence. Capture details accompany the images in `release-capture.txt`.

Batch-mode desktop screenshot readback is black on this host. The opt-in review
therefore renders the actual world/carousel presentation to a target, then has
Unity draw the actual HUD/menu canvases over it at their native viewport positions.
These are equivalent in-engine compositions, not desktop screenshots or painted
mockups. The capture helper is installed only with `--gameplay-ux-review` or called
explicitly by the render test, and allocates no targets in normal gameplay.

The initial Release composition omitted the cached carousel camera's contents on
this batch-mode host. Those captures were rejected. The helper now explicitly
renders the existing carousel camera before composition and requires visible
resource text pixels in the final frame. Carousel production code is unchanged.
The affected render fixture passed again, followed by another production Release
build and both standalone checks; the committed images are from that final run.

## Test contracts and results

The stationary dormant-enemy contract was intentionally replaced. Visibility,
hitboxes, death, early hits, and ordered progress remain asserted. New coverage
requires actual pre-engagement movement, distant quiet patrols, local legitimate
detection, wall occlusion, offscreen gating, obstacle/support behavior, death,
pause/resume, and retry restoration. Presentation fixtures now expect actor
activity before engagement; health bars must remain hidden on unengaged patrols,
while legitimate entry detection can reveal the nearby west sentry's bar.

Initial tests caught the invalid numeric paths, a test-local variable naming
compilation error, reset synchronization, and diagnostic render-target orientation.
These were corrected and the affected checks rerun. Existing gameplay assertions
and release collision guards were not weakened.

The final control-resolution assertion was scoped to the scene reader's owned
runtime asset: Unity also enables the existing project-wide definition asset, so
a global name-only `Single` lookup was an invalid test assumption. Re-enabling
the reader retains a stable action count, and only its owned clone has its player
callbacks. No new action map or input subscription path was added.

| Validation | Final result |
| --- | --- |
| Full Edit Mode suite | **439 / 439 passed** |
| Full Play Mode suite | **127 / 127 passed** |
| Production numeric-input reproduction before fix | Failed as reported: Digit1 left slot 2 equipped |
| Corrected numeric input and lifecycle | Passed; all ten owned-clone numeric bindings resolve to the real keyboard controls |
| Patrol / legitimate detection / walls / offscreen | Passed for production actors; existing AI-specific fixtures retained |
| Destructible floor support | Passed for sentry ground patrol and bombardier raised-deck patrol using real production breach hits |
| Death / pause / retry / checkpoint / seven-wave progression | Passed in relevant full-suite fixtures |
| HUD / F3 / actual rendered palette / native layout | Passed; off/on/pause renders inspected |
| Render fixture after capture-only carousel correction | **1 / 1 passed** |
| Scene regeneration and Phase 1–3 art contracts | Passed within Edit Mode |
| Strict preservation against Phase 3 | **130 protected component/map blocks identical**; no authored scene changes |
| Production art audit | **46 PNGs**, zero off-palette opaque pixels or partial-alpha pixels |

The final full Play Mode rerun includes the additional bombardier breach test and
the corrected owned-action lookup, following an earlier 126/126 full-suite pass.
Temporary XML/logs are under `Logs/phase4-*.xml/log`; palette results and temporary
captures are under `Logs/GameplayUxPhase4/`. Only representative images and this
report are versioned, not builds, large temporary outputs, or logs.

## Windows Release

`RustlineFirstPlayableSetup.BuildWindowsRelease` succeeded (Editor exit 0 /
`RUSTLINE_FIRST_PLAYABLE_RELEASE_OK`) using the existing non-development Windows
x64 `BuildOptions.None` pipeline. The actual output is
`Builds/FirstPlayable/Rustline.exe`. The unmodified build collision guard processed
the exported Salvage Intake scene and baked **4 paths / 78 points** for Ground
Collision and **1 path / 4 points** for Maintenance Crouch Collision.

The final standalone Release smoke test exited **0** with
`FIRST_PLAYABLE_SMOKE_PASS`: actual Tilemap collision, real input across the
original/extension seam, all seven hitscan encounters, checkpoints, retry,
extraction, and restart passed.

The additional `--gameplay-ux-review` run exited **0** with
`GAMEPLAY_UX_RELEASE_PASS`. It used real Input System keyboard/mouse events to
verify 0/1/2, unavailable 9, wheel selection, and title-to-play; observed distant
sentry and bombardier patrols before engagement; checked checkpoint health/ammo
restoration and feedback; verified F3, pause/modal suppression, retry preference,
restart defaults, and numeric selection after retry/restart. It produced the
Release captures linked above and asserted visible carousel resource pixels.

Final logs: `Logs/phase4-release-build-final.log`,
`Logs/phase4-release-smoke-final.log`, and `Logs/phase4-release-review-final.log`.
Unused unsupported lens-flare shader warnings and an existing renderer shutdown
ComputeBuffer disposal warning remain non-blocking. The full 127-test Play Mode
run preceded the final diagnostic-only carousel capture correction; the affected
render test, Release build, smoke, and targeted review were rerun afterward.

## Main files changed

- `Assets/InputSystem_Actions.inputactions`: correct numeric control paths.
- `Assets/Scripts/Combat/Sentry2D.cs`, `Bombardier2D.cs`, `FirstPlayableEncounter2D.cs`,
  and new `EnemyPerception2D.cs`: separate patrol/engagement, local perception,
  support checks, and reliable reset.
- `Assets/Scripts/Combat/FirstPlayableCheckpoint2D.cs`: presentation-only lamps and
  optional exact debug bounds, retaining all serialized station data.
- `Assets/Scripts/Presentation/FirstPlayableHud2D.cs`, `FrontendController.cs`,
  `FrontendView.cs`, and `WeaponHudBitmapFont.cs`: player/developer HUD, modal-safe
  F3, controls reference, and numeric coordinate glyph.
- `Assets/Tests/PlayMode/GameplayUxPlayModeTests.cs` plus existing encounter, first
  playable, sentry/bombardier presentation, and health-bar fixtures: real input,
  lifecycle, patrol/obstacle/destruction, and rendered UX coverage.
- `Assets/Scripts/Diagnostics/GameplayUxReleaseReview.cs`: explicit normal-Release
  input/patrol/checkpoint/F3 review and in-engine image capture.

## Remaining manual acceptance

Synthetic device events validate the real Unity Input System and exported game,
but do not replace human checks of physical keyboards/layouts, focus/Alt-Tab,
mouse-wheel feel, and rapidly mixed input. Human full-route combat/patrol pacing,
checkpoint discoverability, and HUD readability on physical displays remain manual
acceptance work. Automated fight staging is not a balance or human-traversal proof.
The existing environment's repetition/terrain texture and existing renderer
shutdown warnings are outside this gameplay/UX iteration.
Actors retain their existing kinematic, horizontal patrol model at their authored
height. Support checks turn them away from missing ground; this phase introduces
no new enemy falling/ledge traversal mechanics.

## Phase 4.1 — Final Bugfixes

### Baseline and confirmed reproduction

Fetched origin and confirmed local HEAD and the existing remote Phase 4 branch
both at `68dadba0da1fbd36f8e24459c51a2b67020786e7`, with a clean working tree.
Continued on `codex/gameplay-ux-polish-phase4`. Read the agent guardrails, collision
contract, environment metrics, this report, enemy/input implementations and tests.
This section supersedes Phase 4's horizontal-only unsupported-movement limitation.

The runtime audit checked all **14 production defenders** against actual Ground
collision. Every spawn was supported, with the authored foot approximately
`.02` units above solid geometry. Both actor types have kinematic Rigidbody2D,
zero gravity, FreezeRotation, discrete collision detection, and a trigger-only
weapon hitbox measuring **1.2×2.5 units**, centered 1.25 units above the root.
They have no solid body collider that would resolve falling automatically.
Machinery sprites are decorative and are not a substitute for terrain support.

Reproduction used the actual Gallery Sentry and Gallery Battery at roughly
`(60,17.02)` and `(82,20.02)`. Four adjacent floor cells were destroyed through
`BreachableTilemap2D.ReceiveHit` with the production Longwatch definition. The
real Composite updated immediately and a current-foot Ground ray no longer hit
within `.7` units. After 2.2 seconds both actors were still at their original
height. Their forward probes prevented horizontal movement, but neither actor
checked current support or integrated vertical motion. Normal movement also
explicitly wrote the stored spawn Y. This confirms a terrain-loss hovering bug;
it does not identify the exact human-reported moment without that screenshot or
a gameplay recording. No unsupported production spawn was found.

The baseline input reproduction also failed: a real Input System
`KeyboardState(Key.Numpad1)` event left slot 2 equipped. The actions had only
top-row bindings. Baseline results are retained locally in
`Logs/phase41-reproduction.xml/log`: spawn audit passed; both terrain-loss cases
and numpad selection failed as expected.

### Minimal support correction

`EnemyGroundSupport2D` is a small shared movement helper, not a new component or
physics framework. It caches the existing hitbox's world footprint once per actor.
One nonallocating BoxCast per active physics step checks only non-trigger Ground
geometry, accepting upward-facing contacts. Missing support produces controlled
downward acceleration using project gravity, capped at 24 units/second. The cast
sweeps the actual intended step and clips descent above the next solid surface,
with a `.02` unit skin. Physics shape-query tolerance is verified within `.04`
units of real support, less than one 16-PPU source pixel.

While descending, horizontal AI and committed attack timers wait. Once grounded,
their existing state machines continue. Patrol probes and MovePosition now use
current body Y, so landing cannot restore the old height. Existing horizontal
bounds, obstacle/edge checks, detection, damage, health and attack parameters are
unchanged. Death still disables the weapon trigger and runs the same visual
finish; the cached footprint lets an unsupported corpse descend as well, even
when encounter engagement has disabled its combat activity.

Landing explicitly clears the kinematic body's residual velocity, including dead
or idle actors. Regression testing also exposed the old mid-height wall ray
missing the side of a one-tile step after a drop. A horizontal footprint sweep
replaces that ray, keeping the same `.8` unit forward limit while checking low
steps too. This prevents walking into the original deck after landing below it.
The helper reuses a fixed eight-hit buffer; no scene discovery, new collider or
per-step collection allocation is introduced.

Reset clears falling speed and restores the original authored spawn and encounter
state. Existing retry does not rebuild breached terrain: a restored actor above
a persistent hole descends again, rather than hovering or being teleported upward.
No enemy body type, gravity setting, collider, scene cell, player physics,
encounter progression, health bar, presentation adapter or environment asset is
changed. Existing projectile cleanup remains authoritative.

### Numpad bindings and lifecycle

Installed Input System **1.19.0**, `InputSystem/Devices/Keyboard.cs` lines 139–148,
declares the exact controls `numpad1` through `numpad9` and `numpad0`. Added exactly
ten bindings: `<Keyboard>/numpad0`–`<Keyboard>/numpad9`, each on the existing
`WeaponSlot0`–`WeaponSlot9` action, using its existing Press interaction and
Keyboard group. All existing action/binding IDs and the asset GUID are preserved;
only the ten additional bindings have new IDs. The input reader, equipment,
navigation, carousel, ammunition and weapon code remain unchanged.

Tests resolve all ten physical numpad controls on the reader's owned runtime
asset, exercise both numeric banks, unavailable slots, wheel, rapid mixed requests,
visible transitions, re-enable/action counts, held pause input, retry/stale edges,
restart and main-menu loading. The existing frontend intentionally keeps gameplay
blocked until held modal keys are released; no pending selection then replays.

These are physical key bindings, independent of text character production. There
is no Num Lock conditional, global hook, navigation-key alias or platform-specific
workaround. Synthetic tests also press/release the separate NumLock control;
KeyboardState has no latched OS Num Lock/LED state. **No physical keyboard or OS
LED state was tested.** Verify actual hardware with Num Lock on and off during
final acceptance; if a backend reports navigation keys with Num Lock off, those
remain navigation keys rather than being rebound to weapons.

### Regression and Release validation

New `EnemySupportPlayModeTests` checks all production spawns, real destructible
floor loss for both types, landing without penetration, stable physical and
native-snapped visual Y, reset over persistent destruction, unsupported placement,
pause/resume, death in midair and checkpoint retry. Existing edge/obstacle, AI,
seven-encounter, checkpoint, health-bar, HUD and frontend fixtures are retained.
`GameplayUxPlayModeTests` gains focused numpad integration/lifecycle tests.
The opt-in Release review extends its existing synthetic input and scene review
with numpad lifecycle and actual production floor-loss/landing assertions.

The new fixtures first caught the two hovering cases and missing numpad binding,
then residual landing velocity and low-step penetration. Their setup was corrected
to distinguish a one-tile drop from a deeper shaft, allow the existing modal
release barrier's extra frame, and destroy support at the actor's reset destination.
No existing test assertions or collision validation rules were relaxed.

Intentional implementation files are `Sentry2D.cs`, `Bombardier2D.cs`, new
`EnemyGroundSupport2D.cs`, and `InputSystem_Actions.inputactions`. Validation changes
are new `EnemySupportPlayModeTests.cs`, added cases in `GameplayUxPlayModeTests.cs`,
and the existing opt-in `GameplayUxReleaseReview.cs`. This report and two small
reference PNGs complete the deliverables; no saved scene changes are required.

| Phase 4.1 validation | Result |
| --- | --- |
| Final focused support suite | **5 / 5 passed** |
| Full Edit Mode suite | **439 / 439 passed** |
| Full Play Mode suite | **134 / 134 passed**, including both new numpad cases |
| Production art audit | **46 PNGs**, zero off-palette or partial-alpha pixels |
| Protected scene/collision blocks against approved Phase 4 | **130 unchanged** |
| Windows x64 non-development Release | **Succeeded**, build exit 0 |
| Existing standalone smoke | **Passed**, exit 0 / `FIRST_PLAYABLE_SMOKE_PASS` |
| Extended standalone UX/support review | **Passed**, exit 0 / `GAMEPLAY_UX_RELEASE_PASS` |

Full-suite logs/results are local `Logs/phase41-editmode.xml/log` and
`Logs/phase41-playmode.xml/log`; final support checks are
`Logs/phase41-support-verified.xml/log`.

Executed `Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease`, producing
the actual non-development `Builds/FirstPlayable/Rustline.exe` with
`RUSTLINE_FIRST_PLAYABLE_RELEASE_OK`. The unchanged export guard baked Ground
**4 paths / 78 points**, Maintenance **1 / 4**, and MovementLab **9 / 48**.
The existing standalone smoke verified real collision/input, all seven hitscan
encounters, checkpoint/retry, extraction and restart.

The extended review ran the exported player at **1280×720**, using the normal
title-to-play route, both numeric banks and wheel, unavailable numpad 9, input
reader re-enable, held modal suppression, retry and restart. It then removed real
Gallery floor cells through the production breaching receiver. The sentry landed
at **Y=16.025 from 17.020** and the bombardier at **Y=19.025 from 20.020**, on
`Ground Collision - Hidden`, retaining kinematic bodies and the landed height
during subsequent patrol. Actual rendered Release frames were inspected locally.
These assertions used synthetic Input System events, not hardware keystrokes.

Final Release logs are `Logs/phase41-release-build.log`,
`Logs/phase41-release-smoke.log`, and `Logs/phase41-release-review.log`.
The previously documented unused lens-flare shader and renderer shutdown
ComputeBuffer warnings remain non-blocking. Temporary outputs and executables
remain unversioned. There are no changes to saved scenes or approved art assets.

### Rendered evidence and remaining manual acceptance

Baseline and corrected production renders use the existing Unity composition
capture described above. The Gallery Battery beside the industrial machinery is
the target actor; the player is held in place for deterministic illumination and
the camera is fixed. These are controlled repro scenes, not human playthroughs.

- [Baseline: hovering after real floor destruction](reference/gameplay-ux-polish-phase4/phase41/gallery-battery-before.png)
- [Corrected: standing on the exposed lower floor](reference/gameplay-ux-polish-phase4/phase41/gallery-battery-after.png)

Final hardware Num Lock/focus checks and a human route run remain acceptance work.
The exact original human incident cannot be matched without its recording;
the supported-spawn audit and deterministic real-terrain-loss reproduction bound
the confirmed diagnosis. No further UX, art, sound or release packaging is included.
