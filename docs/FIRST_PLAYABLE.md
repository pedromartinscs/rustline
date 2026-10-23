# First playable — Salvage Intake

Open this repository with **Unity 6000.4.0f1**. Open `Assets/Scenes/Demo/SalvageIntake.unity` and press Play for direct development gameplay. Normal builds now start at `Assets/Scenes/UI/MainMenu.unity` (index **0**); Start Game loads SalvageIntake (index **1**). No setup button is required. See [frontend-menu-system.md](frontend-menu-system.md).

Both enemy archetypes now use original Canonical-28 pixel-art sprites. M5 retains the approved Sentry artwork, shortens its melee preparation to 0.48 seconds and adds a pixel-disintegration finish after collapse. The Bombardier has its own animated demolition-automaton presentation with unchanged ranged timing, locked target and bomb trajectory. See [bombardier-art-pass.md](bombardier-art-pass.md) for assets, authoring and validation.

M5.1 adds a six-frame pixel-art bomb explosion and a Bombardier wreck-dissolution finish, preserving damage/occlusion and the approved character sprites. See [combat-vfx-polish.md](combat-vfx-polish.md).

M7 reinforces the traversal-support/wall/floor presentation and replaces repeated late cranes with original pressure, exhaust and conduit infrastructure. Saved collision, breaching policy and route geometry are preserved. See [environment-visual-hardening.md](environment-visual-hardening.md) for authoring, assets, validation and remaining human review.

Clear west security and the transfer bay, climb the first service shaft and clear the upper deck. The former extraction at x=43 is now the **Upper Relay checkpoint**. Continue east through the processing gallery, maintenance underpass/roof and second shaft, then cross the rail bridge and defeat the terminal defense. The final extraction is at **x=193, y=29**, after **seven encounters / fourteen defenders**. Hold the exit for two uninterrupted seconds; leaving or taking damage interrupts it. Follow the current objective in the HUD.

Three session-local stations restore health/magazines and clear transient projectiles: **Service Station (x=22, after 2 encounters)**, **Upper Relay (x=43, after 3)** and **Service Refuge (x=135, after 5)**. Death/falling retries after a short delay. The latest station's committed encounters stay defeated; every later encounter resets in place. F5 restarts the entire level; Enter does the same after completion.

Alive enemies remain visible and damageable before activation. Proximity or an early hit wakes combat. Early kills stay dead, and objectives advance only when every preceding encounter is also defeated. Retry resets uncommitted early kills; activation never resurrects them. The existing penumbra alone controls distance visibility.

## Controls

| Action | Keyboard / mouse |
| --- | --- |
| Move | A/D or left/right arrows |
| Jump / wall kick | Space |
| Crouch | S / down arrow |
| Wall brace / ledge climb | Move toward an eligible wall/ledge in the existing traversal states |
| Aim | Mouse |
| Fire | Left mouse |
| Fire/shot mode | Right mouse (Longwatch semi/auto; Latch conventional/bouncing) |
| Equipment | 0 hands, 1 Latch-9, 2 Longwatch; mouse wheel cycles |
| Reload Longwatch | R (discards the partial magazine) |
| Restart whole run | F5; Enter after extraction |
| Pause / resume / menu Back | Escape |

Existing gamepad movement, jump and crouch bindings remain available. The audited aim/fire configuration is mouse based; a complete gamepad combat/control pass is still pending. Start restarts after completion.

Longwatch starts with 50 loaded rounds and three reserve magazines. Latch-9 is an infinite fallback. Selection keeps ammunition state; the existing weapon carousel remains authoritative for equipment/mode/ammo presentation. The new pixel status display adds health, objective, elapsed time and retry count.

## Authoring and build

- **Tools → Rustline → First Playable → Apply Gameplay** rebuilds only `Gameplay Content - Preserve/First Playable - Managed`, rebinding to the current player rig. Other authored gameplay content is preserved. Do not place hand-authored content under this managed child.
- **Tools → Rustline → First Playable → Apply Expanded Environment** deterministically authors extension cells and its named managed art root. The saved scene already contains them; use this for authoring, not normal launch.
- **Tools → Rustline → Apply Production Setup** rebuilds the original geometry/art, then reapplies the expanded environment and gameplay, rebinding the replaced player rig. Expansion authoring survives this workflow.
- **Tools → Rustline → First Playable → Build Windows Release** builds a normal, non-development Windows x64 player into `Builds/FirstPlayable/Rustline.exe`, with the canonical production entry and existing collision guard.

Command-line equivalents (PowerShell, from repository root; close other Editors for this project):

```powershell
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe'
Start-Process $unityEditor -WindowStyle Hidden -Wait -ArgumentList '-batchmode -projectPath C:/Projetos/Github/rustline -executeMethod Rustline.Editor.RustlineFirstPlayableSetup.ApplyGameplay -quit -logFile Logs/first-playable-setup.log'
Start-Process $unityEditor -WindowStyle Hidden -Wait -ArgumentList '-batchmode -projectPath C:/Projetos/Github/rustline -executeMethod Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease -quit -logFile Logs/first-playable-build.log'
```

Do not use `-nographics` for the full suites: the existing rendering equivalence tests require a real graphics device. Test commands use `-runTests -testPlatform EditMode` or `PlayMode`, with `-testResults` and `-logFile` pointing inside `Logs`. Current expansion results and manual gates are in [salvage-intake-expansion-validation.md](salvage-intake-expansion-validation.md); the earlier milestone's evidence is retained in [first-playable-validation.md](first-playable-validation.md).

With the Editor closed, the produced normal Release also supports a repeatable simulation smoke:

```powershell
Start-Process 'Builds/FirstPlayable/Rustline.exe' -WindowStyle Hidden -Wait -ArgumentList '-batchmode --first-playable-smoke --smoke-auto-quit -logFile C:/Projetos/Github/rustline/Logs/first-playable-release-smoke.log'
```

Look for `FIRST_PLAYABLE_SMOKE_PASS` and exit code 0. This verifies real collision/input/combat and the run loop with controlled encounter placement; it does not certify human shaft traversal, balance, final screen composition or FPS. Hidden-window screenshots are not valid visual evidence. `FirstPlayablePlayModeTests.Presentation_WorldAndPixelStatusRenderNonEmptyOffscreen` separately verifies and exports actual world/UI render targets for inspection.

## Implementation map and limits

`FirstPlayableProgress` owns deterministic ordered progression/checkpoint/extraction rules. `FirstPlayableDirector2D` owns the run and resets existing player/weapon components. `FirstPlayableEncounter2D` owns authored member activation. `SentryBrain2D` owns timed state transitions; `Sentry2D` owns Ground LOS, wall/edge checks and melee damage. The existing Bombardier, generic health/hitboxes, authored player/weapon animation, Tilemap collision, rendering and performance systems are reused.

Sentry uses original Canonical-28 sprites and `SentryPresenter2D`, driven by the existing combat state/timer. Idle/walk, alert, windup/strike/recovery, hit and death are integrated in the saved scene and gameplay builder. See [Sentry art pass](sentry-art-pass.md). Bombardier also uses its approved original sprites, explosion and death finish; see [Bombardier art pass](bombardier-art-pass.md) and [combat VFX polish](combat-vfx-polish.md).

The [expanded physical route/design](salvage-intake-expansion.md) increases the spawn-to-exit horizontal projection from 63 to 213 units, preserving all introductory collision cells. Gallery elevation changes, a 42 px crouch passage with an alternate ledge-climb roof route, a second four-unit shaft and a rail gap/recovery well add actual terrain. M6 adds title/pause menus and independent persistent Music/SFX routing. **Production audio clips remain absent, and duration/FPS/balance are unmeasured.** Original environment/player/weapon assets and rendering are retained; no external assets or packages were introduced. Human traversal/combat feel, menu artistic acceptance and sustained standalone performance remain explicit acceptance gates.
