# First playable validation — 2026-10-22

All runs use the installed **Unity 6000.4.0f1**. Evidence is in ignored `Logs/`; no packaged build or performance result should be inferred from source compilation alone.

## Baseline and implementation checks

| Check | Result / evidence |
| --- | --- |
| Initial Git | Clean `feat/first-playable-phase-01`, `ae4eb0f`; experimental branch untouched |
| Existing EditMode baseline | **310 passed**, 0 failed; `Logs/first-playable-baseline-edit-gfx.xml` |
| New gameplay scene generation | Passed; `Logs/first-playable-setup.log`, `RUSTLINE_FIRST_PLAYABLE_SETUP_OK` |
| EditMode after integration | **325 passed**, 0 failed; `Logs/first-playable-edit.xml` |
| Initial full PlayMode | 64 passed, 1 failed; found real retry Rigidbody reactivation/teleport ordering bug, subsequently corrected; `Logs/first-playable-play.xml` |
| Focused PlayMode after fix | **6 passed**, 0 failed; `Logs/first-playable-focused-play-2.xml`. First fix (simulation before Rigidbody position only) still failed; final fix sets Transform and Rigidbody before collider synchronization |
| Final full Unity regressions | **67/67 PlayMode**, **325/325 EditMode** passed; `Logs/first-playable-final-play.xml`, `Logs/first-playable-final-edit.xml` |
| Offscreen rendering integration | **7/7 focused tests passed**, including expanded non-empty world/status capture assertions; `Logs/first-playable-render-play-final.xml`. This adds one rendering test after the 67-test full suite; all 68 current PlayMode cases were covered across the full/focused runs |
| Windows x64 Release | Passed, non-development; `Logs/first-playable-build-2.log`, `Builds/FirstPlayable/Rustline.exe` |
| Actual build-scene collision guard | Salvage Intake **3 paths / 38 points**; MovementLab **9 / 48**; original initializer and guard unchanged |
| Standalone Release smoke | Passed with Editor closed; `Logs/first-playable-release-smoke-3.log`, `FIRST_PLAYABLE_SMOKE_PASS` |
| Scene preservation | Compared original YAML object blocks: existing terrain, cameras, collision and art unchanged; only new managed child added to existing preserve root |
| Python metadata suite | **18 passed, 1 baseline failure** after rerun with workspace-local temp; Latch checked-in JSON uses CRLF whereas deterministic generator emits LF. No content-generation failure; 361 muzzle points resolve. Left unrelated assets/test conventions unchanged |
| Whitespace | Source/docs checked separately; Unity's generated scene uses its standard empty-field trailing spaces. Git also reports normal checkout CRLF conversion notices |

The first Null-graphics baseline crashed in existing rendering-equivalence tests. Full suites require graphics; this is not a gameplay-source failure. A stale Editor backup was preserved under `Logs/pre-first-playable-scene-backups`; sandbox licensing IPC also required running Unity outside the sandbox. Initial Python temp-file permission error disappeared with `tempfile.tempdir` set to `Logs/metadata-test-temp`.

## Runtime / Release and remaining gates

The normal non-development Windows Release was built and executed with the Editor closed. Its opt-in smoke verified supported spawn, non-empty Composite geometry, Input System keyboard movement across entry Tilemap seams without an extra Land event, jump/landing, actual Longwatch hitscan kills against both enemy types, automatic retry before/after checkpoint, committed clears, extraction freeze and full restart. Fight staging uses controlled placement; player deaths use controlled damage. This is not a human playthrough of the shaft or combat balance. Real Latch projectiles and sentry damage/LOS/dodging are additionally exercised in PlayMode.

`--first-playable-smoke` enables the harness only for that process: background progression, ignore-focus input and a virtual keyboard when batch mode exposes no physical keyboard. Ordinary play does not enable any of these overrides. `--smoke-auto-quit` exits with 0 on success; errors/exceptions exit with 1. Screen captures made while the player window was hidden were black and are **not visual evidence**. The attempt to open the player through Computer Use timed out at desktop app authorization; final on-screen composition is unverified.

Instead, the graphics-enabled PlayMode rendering test explicitly rendered the existing world camera target and the actual pixel-status Canvas through an offscreen camera, asserted non-empty pixels, and saved separate PNGs in `Logs/FirstPlayableRenders`. Inspected all six images: entry world/status, transfer bay, checkpoint, upper defense and completion status. Original industrial art/player, temporary enemy silhouettes and colored beacons are visible; status/completion glyphs are legible at the 1280×720 UI capture size. These separate render targets are not a fabricated composite or a final Player screenshot. The world captures use the existing raw world target before penumbra/final presentation.

The standalone shutdown log contains `GarbageCollector disposing of ComputeBuffer. Please use ComputeBuffer.Release() or .Dispose() to manually release the buffer.` No project gameplay source allocates a ComputeBuffer; the origin has not been isolated. No gameplay exception occurred in the passing smoke. This warning remains an explicit investigation item; do not describe the build as warning-free or change the rendering pipeline speculatively.

Required manual acceptance before calling this portfolio-ready:

1. Open Salvage Intake in Unity 6000.4.0f1 and play from the west bulkhead without teleports. Confirm movement, crouch, backpedal, both weapons/modes/reload, and legible telegraphs/HUD.
2. Clear west security and both bay defenders. Cross the marked station at x=22; verify health/ammunition feedback. Die before and after checkpoint, then use F5 to verify a new run.
3. Wall brace/kick through the four-unit shaft and reach the y=17 deck consistently. Human-test the previously documented raised-catwalk/shaft-exit traversal, which this phase preserves.
4. Defeat upper defenders, leave/reenter extraction to interrupt its hold, then finish. Confirm stable success and Enter restart.
5. Launch the produced normal Windows Release `Builds/FirstPlayable/Rustline.exe` with Unity closed and complete the route manually. Spawn support, entry Tilemap seams and jump/landing passed automation; full-route collision/feel remains a human gate.
6. Inspect 800×600, 1280×720, 1920×1080 and a higher-DPI window for readable UI/native pixels. Profile representative combat in standalone; **stable 60 FPS and 8–12-minute duration are unmeasured**.

Art/audio limits: enemy geometry remains temporary programmer art; combat/ambient audio is absent. This is a compact integrated first playable, not a finished portfolio release.
