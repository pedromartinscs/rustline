# Salvage Intake expansion validation

Date: 2026-10-22. Unity 6000.4.0f1. The user reports a successful human playthrough of the previous first playable; this record distinguishes expansion checks from that earlier acceptance.

## Baseline and completed milestones

- Start: clean `feat/salvage-intake-expansion` at updated main merge `27ae1a2`.
- Existing Editor initially blocked batch work; user saved/closed it. Baseline then passed **325/325 EditMode** and **68/68 PlayMode** (`Logs/expansion-baseline-*.xml`).
- Audit/design committed as `69aa9cc`; visibility/damage milestone as `5d65500`.
- Visibility plus previous scene integration: **9/9 passed** (`Logs/expansion-visibility-play.xml`). Dormant living roots/renderers/hitboxes stay active; AI is dormant; early lethal weapon hits wake an encounter without healing on later activation. Restart restores the same actors.
- Expanded environment saved by Unity (`Logs/expansion-environment.log`). Intro/end projection: x=-20..43 → x=-20..193. Main collision bounds x=-28..195, compared with -28..45 before. y=17 gallery connects directly across x=45/46; second shaft exits y=29.
- Compared serialized main collision cells against `27ae1a2`: **444 original cells retained, zero removed, zero new cells inside x<46, 446 added**. Main map now 890 cells; a separate six-cell offset Tilemap supplies the 42 px maintenance ceiling. Both use static Ground/Merge/polygon Composite plus the original initializer/build guard.
- Initial geometry integrity assertions passed. Input tests exposed a test-runtime issue: the isolated `InputTestFixture` did not retain usable control-state buffers in this batch Editor context. No movement/collision tuning was changed. Integration now uses the game's normal Input System with a temporary keyboard, restoring device/focus settings afterward. The former-exit-to-gallery seam walk then **passed** (`Logs/expansion-native-input.xml`).

## Automated gameplay and geometry

The first complete regression run passed **329/329 EditMode** and **80/80 PlayMode** (`Logs/expansion-edit.xml`, `Logs/expansion-play.xml`). This includes existing movement, weapons, ammunition, AI, rendering, native-pixel and scene tests. New coverage includes:

- Configurable encounter counts (6, 7 and 8), ordered clears, multiple checkpoint commits, retry rollback and full restart.
- Dormant enemy roots, renderers and explicit damage hitboxes remain enabled. Early weapon damage wakes AI; early death is not healed by activation. Future defeat cannot skip earlier objectives or block extraction.
- Every saved checkpoint restores its spawn, health, ammunition and the same actor identities, preserves committed defeats, resets future defeats and leaves no bombs. Final extraction requires all seven encounters; the old exit cannot finish the run.
- **Eight real-terrain integration cases** use normal keyboard events and the actual player motor: saved Tilemap/Composite integrity; former-exit/gallery seam walk; processing cover and raised-deck jumps; standing obstruction/crouch/automatic stand at the 42 px underpass; rail-gap jump; wall kicks and ledge exit through the second shaft; wall-kick recovery from the rail catch well; alternate maintenance-roof ledge climb.

Geometry cases place the player only at focused segment starts and isolate combat. They verify actual movement over authored terrain, not an uninterrupted human playthrough or combined traversal/combat balance. An initially overstrict underpass assertion assumed the rounded capsule would stop exactly one half-width before the ceiling; the contact was valid at x=101.647 before the x=102 opening. The assertion now checks standing obstruction at the opening and successful crouched passage, without changing player geometry.

## Production regeneration and Windows Release

- Ran `RustlineProductionTools.ApplyProductionSetup` end to end. An old foundation validator required exactly six input actions, although the existing asset has eighteen (including reload, cycling and ten equipment slots). It initially rejected the setup before scene generation. Updated only its required-action validation to match the existing controls; collision bake and player movement code are unchanged. The production rebuild then passed (`Logs/expansion-production-rebuild-2.log`).
- Compared all **612 serialized scene blocks** before/after full production reapplication, canonicalizing local object IDs through their hierarchy/component/prefab identities: **zero semantic differences** (`Logs/expansion-production-scene-equivalence.json`). Rechecked all original 444 collision cells and all 446 new main-map cells after regeneration. Retained the previously committed scene IDs to avoid a second unrelated serialization diff; the final validation also loads that exact saved scene directly, without running a builder.
- Built a normal Windows x64 Release with `BuildOptions.None`, not a development player (`Logs/expansion-release-build.log`, `Builds/FirstPlayable/Rustline.exe`). Actual Player-build processing baked main terrain **4 paths / 78 points**, maintenance ceiling **1 / 4**, and the unchanged MovementLab floor **9 / 48**. Tilemap shape count zero is expected when shapes belong to the Composite.
- With the Editor closed, the Release smoke passed with exit code 0 (`Logs/expansion-release-smoke.log`, `FIRST_PLAYABLE_SMOKE_PASS`). It checked supported spawn, real keyboard walking/jump/landing without phantom Land, actual keyboard traversal across the original/extension seam, all seven encounters using actual Longwatch hitscan, checkpoint commits, automatic death/retry, final extraction and full restart. Combat arena staging uses placement and is not human-traversal evidence.

Final delivery suites also passed **329/329 EditMode** and **80/80 PlayMode**, after restoring the committed scene IDs and updating the new deprecated API calls (`Logs/expansion-delivery-edit.xml`, `Logs/expansion-delivery-play.xml`). No skipped or failed cases. The final normal Windows Release build passed (`Logs/expansion-delivery-release-build.log`) and its Editor-closed smoke exited **0** with `FIRST_PLAYABLE_SMOKE_PASS` (`Logs/expansion-delivery-release-smoke.log`). Runtime diagnostics explicitly confirmed both Composites: main terrain **4 paths / 78 points**, precision ceiling **1 / 4**. The final build produced no C# compiler warnings; unrelated deprecated API warnings remain in test assemblies as recorded below.

Warnings observed: Unity licensing/native-symbol upload reports an empty cloud access token, but local compilation/build succeeds. Editor startup also reports an unavailable optional VisionOS native module, already present in baseline logs; Windows tests/builds complete. The standalone prints the existing shutdown `GarbageCollector disposing of ComputeBuffer` warning after its successful smoke; no project ComputeBuffer allocation was introduced or changed. New discovery calls were updated to the current Unity overload, and new actor-identity tests now compare references instead of deprecated integer instance IDs. Older unrelated tests/components still use deprecated APIs; this milestone does not rewrite them. These runs are not represented as warning-free.

## Presentation evidence and manual gates

Inspected `Logs/FirstPlayableRenders/expanded-section-3.png` through `expanded-section-6.png`, exported by the real offscreen rendering test. They show gallery machinery/cover/elevation, maintenance housing/shaft, rail gap/catch well/battery and final rail/bulkhead composition. These are world render targets **before penumbra/final screen presentation**, not final-window screenshots. Existing native-pixel/penumbra tests run in the full suites; their architecture and shaders were unchanged. Hidden Release screenshot captures were black and are not used as visual evidence.

Remaining manual acceptance:

1. Open the committed `Assets/Scenes/Demo/SalvageIntake.unity` and press Play; no generation is required. Play from entry to x=193 without positioning/teleport tools. Confirm original introduction, safe intervals, underpass/roof readability, both shafts, rail jump/recovery, combat pacing, ammunition use and final extraction.
2. Die before the service station and after each of the three checkpoints, then F5-restart and Enter-restart after victory. Automated state coverage passed; confirm player-facing timing and feedback during normal play.
3. Launch `Builds/FirstPlayable/Rustline.exe` normally with the Editor closed and repeat the route. Keep the entire build folder together. Confirm native-pixel screen composition, 456 px visibility plus 64 px penumbra, enemy telegraph readability and HUD at 800×600, 1280×720, 1920×1080 and relevant desktop scaling.
4. Measure duration and sustained standalone performance on target hardware. **No measured FPS, play duration, full-route human playability or portfolio-finished art claim is made.** Enemy silhouettes remain temporary, audio is absent, and full gamepad combat remains a previous limitation.

The physical expansion, dormant visibility/damage policy, progression and saved-scene delivery are implemented and have automated evidence. Final human traversal/combat/presentation acceptance remains pending.

## Acceptance status

| Requirement | Evidence / qualification |
| --- | --- |
| Intro preserved | Original collision unchanged; previous full suites retained and passing. Expanded uninterrupted human playthrough pending. |
| Substantial actual environment | Horizontal spawn-to-exit projection 63 → 213 u (3.38×), 446 main terrain cells plus six precision ceiling cells added. |
| Continuous connection | Shared original Tilemap, seam cells verified; real input crosses the former endpoint. |
| No encounter materialization | Living roots/renderers stay active; AI alone sleeps. Presentation distance behavior unchanged. |
| Early interaction coherent | Early damage/kill, out-of-order progression, activation without healing and retry/reset coverage passed. |
| Multiple checkpoints/extraction | Three authored stations; committed-prefix restore and all-seven extraction tested. |
| Direct saved-scene launch | Authored terrain, art, members, stations and references serialized; tests load the scene directly. |
| Regression coverage | Full suites plus actual-input segment tests; results above. |
| Windows Release collision | Normal Release build-time bake and standalone smoke, without Editor running. |
| Honest remaining gates | Full-route human traversal, combat balance, final-window composition, duration and sustained FPS remain manual. |

## Change map

| Files / system | Delivered change |
| --- | --- |
| `Assets/Scenes/Demo/SalvageIntake.unity` | Saved expanded collision, visuals/backgrounds, seven encounter groups, three stations and terminal extraction; directly loadable. |
| `Assets/Editor/RustlineSalvageIntakeExpansionSetup.cs` | Deterministic extension geometry/art and geometry/reference validation; new precision Tilemap/Composite authoring. |
| `RustlineProductionTools`, `RustlineFirstPlayableSetup`, `RustlineM1ASetup` | Production rebuild integration, authored encounter/station data, batch failure propagation and current input-action validation. |
| `FirstPlayableEncounter2D`, `Sentry2D`, `Bombardier2D` | Visual/damage presence independent of AI stepping; cached member subscriptions and early-hit activation. |
| `FirstPlayableProgress`, `FirstPlayableDirector2D`, new `FirstPlayableCheckpoint2D` | Data-driven encounter totals, committed-prefix checkpoints, ordered early defeats, complete retry/restart/extraction. |
| `FirstPlayableHud2D`, `FirstPlayableReleaseSmoke` | Current objective/station feedback, dynamic totals and expanded opt-in Release regression. |
| New `ExpandedProgressTests`, `EncounterVisibilityPlayModeTests`, `SalvageExpansionGeometryPlayModeTests`, `SalvageExpansionProgressPlayModeTests` | Pure progression, visibility/damage, actual-input geometry and saved-scene checkpoint/early-defeat integration tests; existing first-playable tests updated. |
| README, FIRST_PLAYABLE, SALVAGE_INTAKE and expansion design/validation docs | Launch, controls, physical route, ownership, actual evidence and remaining manual gates. Original design/validation retained as historical evidence. |

Existing movement/capsule configuration, native-pixel/penumbra shaders, collision initializer/build guard algorithms, packages and existing art remain unchanged.

## Commits / delivery

- `69aa9cc` — repository audit and physical expansion design.
- `5d65500` — dormant visibility, valid early damage and encounter wake-up.
- `961d508` — continuous galleries, maintenance shaft, rail terminal and actual-input geometry tests.
- `c574474` — seven encounters, three checkpoints, terminal extraction, progression tests and Release smoke expansion.
- `f041a0c` — production input validation, terminal objective priority and explicit per-map Release diagnostics.
- `00bfa26` — current discovery APIs and reference-identity assertions; full suites passed again.
- Final documentation commit — launch/controls, this evidence record and current design/status links.

All work stays on `feat/salvage-intake-expansion`. Build and detailed logs/renders are local ignored artifacts; keep the entire `Builds/FirstPlayable` directory together when running/copying the player. No push, merge or pull request was performed.
