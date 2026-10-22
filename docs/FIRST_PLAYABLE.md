# First playable — Salvage Intake

Open this repository with **Unity 6000.4.0f1**. Open `Assets/Scenes/Demo/SalvageIntake.unity` and press Play. This saved scene is also build index **0**; no runtime generation or setup button is required to play the committed content.

Clear west security, defeat both transfer-bay defenders, cross the marked service station on the east staging platform, climb the four-unit service shaft and defeat the upper-deck defenders. Stay in the marked upper exit for **two uninterrupted seconds** to extract. Taking damage or leaving interrupts extraction. The objective text describes the next step.

The checkpoint restores health and magazines. Death/falling automatically retries after a short delay; before the checkpoint, all encounters return; after it, only the upper defense returns. Cleared enemies do not resurrect during a run. F5 starts a completely new run; Enter does the same on the completion screen. Checkpoints are session-local.

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

Existing gamepad movement, jump and crouch bindings remain available. The audited aim/fire configuration is mouse based; a complete gamepad combat/control pass is still pending. Start restarts after completion.

Longwatch starts with 50 loaded rounds and three reserve magazines. Latch-9 is an infinite fallback. Selection keeps ammunition state; the existing weapon carousel remains authoritative for equipment/mode/ammo presentation. The new pixel status display adds health, objective, elapsed time and retry count.

## Authoring and build

- **Tools → Rustline → First Playable → Apply Gameplay** rebuilds only `Gameplay Content - Preserve/First Playable - Managed`, rebinding to the current player rig. Other authored gameplay content is preserved. Do not place hand-authored content under this managed child.
- **Tools → Rustline → Apply Production Setup** still rebuilds production geometry/art; its final step now reapplies gameplay because it replaces the player rig.
- **Tools → Rustline → First Playable → Build Windows Release** builds a normal, non-development Windows x64 player into `Builds/FirstPlayable/Rustline.exe`, with the canonical production entry and existing collision guard.

Command-line equivalents (PowerShell, from repository root; close other Editors for this project):

```powershell
$unityEditor = 'C:/Program Files/Unity/Hub/Editor/6000.4.0f1/Editor/Unity.exe'
Start-Process $unityEditor -Wait -ArgumentList '-batchmode -projectPath C:/Projetos/Github/rustline -executeMethod Rustline.Editor.RustlineFirstPlayableSetup.ApplyGameplay -quit -logFile Logs/first-playable-setup.log'
Start-Process $unityEditor -Wait -ArgumentList '-batchmode -projectPath C:/Projetos/Github/rustline -executeMethod Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease -quit -logFile Logs/first-playable-build.log'
```

Do not use `-nographics` for the full suites: the existing rendering equivalence tests require a real graphics device. Test commands use `-runTests -testPlatform EditMode` or `PlayMode`, with `-testResults` and `-logFile` pointing inside `Logs`. Graphics-enabled validation and the Release smoke are recorded in [first-playable-validation.md](first-playable-validation.md).

With the Editor closed, the produced normal Release also supports a repeatable simulation smoke:

```powershell
Start-Process 'Builds/FirstPlayable/Rustline.exe' -Wait -ArgumentList '-batchmode --first-playable-smoke --smoke-auto-quit -logFile C:/Projetos/Github/rustline/Logs/first-playable-release-smoke.log'
```

Look for `FIRST_PLAYABLE_SMOKE_PASS` and exit code 0. This verifies real collision/input/combat and the run loop with controlled encounter placement; it does not certify human shaft traversal, balance, final screen composition or FPS. Hidden-window screenshots are not valid visual evidence. `FirstPlayablePlayModeTests.Presentation_WorldAndPixelStatusRenderNonEmptyOffscreen` separately verifies and exports actual world/UI render targets for inspection.

## Implementation map and limits

`FirstPlayableProgress` owns deterministic ordered progression/checkpoint/extraction rules. `FirstPlayableDirector2D` owns the run and resets existing player/weapon components. `FirstPlayableEncounter2D` owns authored member activation. `SentryBrain2D` owns timed state transitions; `Sentry2D` owns Ground LOS, wall/edge checks and melee damage. The existing Bombardier, generic health/hitboxes, authored player/weapon animation, Tilemap collision, rendering and performance systems are reused.

This is a compact first playable, with **temporary geometric enemy art**, no combat/ambient audio, and no claim of measured 8–12-minute duration or production balance. Original environment/player/weapon assets are retained. No external assets, packages, lore, inventory or save system were introduced. Human traversal/combat feel, readability at multiple resolutions and sustained standalone performance remain acceptance gates; see the validation record for completed checks and exact remaining steps.
