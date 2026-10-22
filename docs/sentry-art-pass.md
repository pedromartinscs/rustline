# M4 — Sentry visual production pass

M5 follow-up: the original art below is preserved. Windup is now 0.48 seconds; a separate three-frame subset-only dissolution follows the collapse and wreck hold, then hides only the renderer. See [bombardier-art-pass.md](bombardier-art-pass.md) for current timing, reset behavior and validation. The M4 timing/permanent-wreck descriptions below remain the historical milestone record.

## Audit and scope

Start: clean `feat/sentry-art-pass` at `6be6f85`, current main merge containing the validated expansion. Read AGENTS, FIRST_PLAYABLE, PRESENTATION, ART_DIRECTION, PALETTE, SALVAGE_INTAKE and the original/expanded design and validation records. Unity remains 6000.4.0f1, URP 2D, existing native-pixel presentation. This pass owns Sentry presentation only; no route, Bombardier, collision, health or combat-policy redesign.

`RustlineFirstPlayableSetup.Actor` creates the shared kinematic enemy root and explicit 1.2×2.5 u trigger hitbox, then a seven-point LineRenderer. `Sentry` modifies that outline and stores it in `Sentry2D.silhouette`. Sentry2D currently mixes gameplay with outline recoloring/hit flash and a scaled disappearing death. Eight sentries are serialized in SalvageIntake. Encounters keep living actors/renderers/damage enabled while AI sleeps, wake on valid early damage, and reset pending members in place. `SentryBrain2D` owns patrol/alert/pursuit/windup/recovery/death; its 0.55 s windup and single strike edge must remain authoritative.

References inspected: native player Body/Unarmed idle sheets (48×64 cells, bottom-center pivot, approximately 42–48 visible pixels), original industrial gantry/handler/floor composition in the expanded level, RustlinePalette and rustline.gpl. The global production contract is **exact Canonical 28 plus alpha 0/255**, not a broader authored-sprite allowance. Source/reference work may live under Art/Source; final sprites must be verified/remapped before acceptance. Existing import postprocessor provides point/clamp/16 PPU/uncompressed/no mipmaps/FullRect/no fallback physics shapes. Player uses authored frame swaps; Bombardier has its own independent presenter but still draws lines.

## Replacement strategy

Original Sentry art: compact industrial security automaton, dark worn steel, pale metal edge groups, selective yellow safety plates and a red visor; asymmetric hydraulic impact arm. Right-facing fixed 48×64 cells with feet anchored at the same root as the unchanged hitbox. Target approximately 40 visible pixels standing height, matching the existing enemy damage volume and remaining distinguishable from the taller player. No runtime sprite rotation, fractional scaling, tint fades, extra lights or shader changes.

Use built-in image generation for original art, inspect its output, then deterministically convert accepted source cells to the production grid/palette. A dedicated Sentry presenter reads existing brain state/timing/facing and health events. Authored idle/walk, alert, windup/strike/recovery, hit and collapse frames replace the outline. Visual clocks never apply damage or advance AI. Health reset restores visible idle presentation immediately; living dormant actors remain drawn and damageable.

Builder integration must serialize sprite/frame references in the saved scene and recreate them during Apply Gameplay/Apply Production Setup. Preserve all existing enemy member/health/hitbox references and progression semantics. Existing visibility tests must check the new renderer rather than retain a fake LineRenderer solely to satisfy the old assertion.

## Risks and validation plan

- Generated art is not automatically game ready: audit exact colors/alpha, alignment, silhouette, per-cell content and native-size readability. Keep original source and processing recipe; inspect final assets and rendered world output.
- Pose drift and tool extension must not suggest extra melee reach; visuals fit the existing 1.8 u committed strike. Lock attack-facing to the gameplay direction, including dodging behind it.
- Hit pause freezes the brain timer; animation must read that timer rather than independently predict the strike. Death/reset must not leave hidden/scaled sprites or stale animation clocks.
- Preserve point sampling, Canonical 28 and the unchanged 456+64 px penumbra. Test actual rendered sprite pixels and palette-safe shadow behavior where feasible.
- Run baseline and final full EditMode/PlayMode; add focused state selection, saved-reference/import/palette and death/reset/dormant integration tests. Reapply deterministic gameplay setup, build Windows Release and run the existing smoke. Human animation/combat readability remains separate from automated state/render evidence.

## Delivered art and integration

- Production asset: `Assets/Art/Characters/Enemies/Sentry/sentry_security_sheet.png`, **192×256**, sixteen **48×64** cells, **16 PPU**, bottom-center `(24,0)` pixel pivots. Exactly **22 of Canonical 28** colors plus binary alpha; standing idle is 39 visible pixels tall, raised-tool windup 45, settled wreck 14. No automatic sprite physics shapes.
- Unmodified original image-generation source and exact prompt/recipe: [`Assets/Art/Source/Enemies/Sentry/`](../Assets/Art/Source/Enemies/Sentry/generation-prompt.md). Built-in image_gen was used; no external sprites or packs. Source is an intermediate reference excluded from production import rules. Reviewed conversion preserves a single 7:1 scale, foot alignment and nominal body center, resolves generated column overlap, and quantizes every accepted pixel. The final imported sheet and stable subasset IDs are committed.
- `SentryPresenter2D` owns sprite swaps, facing and a pixel-snapped visual child. `SentryAnimationFrames` selects frames from the existing state and remaining brain timer. Hit/death/reset remain health-driven; the presenter never applies damage, advances AI or moves the enemy root. Sprite/facing/transform writes are avoided when unchanged; actors share one sheet and the existing URP unlit material, without per-actor materials or per-frame discovery/allocation.
- `Sentry2D` no longer contains outline animation. Its read-only facing/timer/hit-pause/movement metadata supports presentation. A successful `MovePosition` command retains movement intent until the next AI step, since Unity consumes its generated velocity before rendering. Health, hitbox, movement bounds/speeds, detection, attack lock, damage and state timings are unchanged.
- All eight authored Sentries have real sprite presenters and no placeholder LineRenderer. Bombardier is unchanged. Living dormant Sentries idle visibly and accept early hits; defeated actors hold their final wreck sprite, with their original damage hitbox disabled. Pending resets restore idle, facing and visibility immediately; committed defeats retain their wrecks.

| Frames (top-to-bottom, left-to-right) | Presentation |
| --- | --- |
| 0–1 | Idle, 2 fps; also used while AI is dormant or stopped |
| 2–5 | Four-frame patrol at 8 fps / pursuit at 12 fps |
| 6 | Alert stance |
| 7–8 | Two windup poses selected by the existing 0.55 s brain timer |
| 9 | Strike during the first 0.12 s of the existing recovery state, after the single damage edge |
| 10 | Remaining recovery |
| 11 | Hit flinch during the existing hit pause, without advancing the brain timer |
| 12–15 | Collapse at 0.12 s per frame, then hold the final wreck; death overrides hit/patrol |

### Launch / authoring

Open the committed `Assets/Scenes/Demo/SalvageIntake.unity` and press Play. No regeneration or manual hookup is required. Existing controls and gameplay remain as documented in FIRST_PLAYABLE.

**Tools → Rustline → Sentry → Rebuild Art and Apply to Salvage Intake** runs `RustlineSentryArtSetup.RebuildAndApply`: converts the retained source, reuses sprite subasset IDs, and updates only the eight Sentry visual children/references. **Apply Gameplay** and the full **Apply Production Setup** recreate this presentation from the committed sheet automatically. Source conversion is an Editor authoring operation, never runtime generation.

## Validation record — 2026-10-22

| Check | Result / evidence |
| --- | --- |
| Clean main-based baseline | `6be6f85`, `feat/sentry-art-pass`; **329/329 EditMode**, **80/80 PlayMode**, `Logs/sentry-baseline-*.xml` |
| Focused art/selection | **8/8 passed**, `Logs/sentry-focused-edit.xml`: exact PNG colors/alpha, fixed cells/pivots, import settings, no generated physics shapes, authoritative timing/precedence |
| Focused real presentation | **4/4 passed**, `Logs/sentry-focused-play-3.xml`: all saved Sentries visible/dormant, early lethal hit/death/reset, actual patrol frame progression/pixel snap observed at camera rendering, paused windup/actual strike/hit sprite names and unchanged 20 damage, real penumbra rendering |
| Deterministic art reapplication | PNG, PNG `.meta` and saved scene all **byte-identical** before/after rebuild; `Logs/sentry-art-idempotence.json` |
| Full production setup | Passed, `Logs/sentry-production-reapply.log`. All **620 scene blocks** semantically identical after canonicalizing regenerated local IDs; `Logs/sentry-scene-preservation.json`. Retained earlier scene IDs to avoid unrelated serialization churn |
| Scene scope | Compared against main: **72 canonical block changes, all within Sentry roots/visual children**; no camera, player, terrain, Bombardier, encounter or checkpoint block changes |
| Full Unity suites | **337/337 EditMode**, **84/84 PlayMode**, no failures or skips; `Logs/sentry-delivery-edit.xml`, `Logs/sentry-final-play.xml` |
| Source-reference import | Explicit Default texture with no Sprite subassets; removed automatic source slicing. Rechecked all 337 EditMode cases. Production PNG, production `.meta`, saved scene and original source PNG remained byte-identical; `Logs/sentry-reference-import-preservation.json`. Runtime/game content and the validated Release are unchanged |
| Windows x64 normal Release | Passed with `BuildOptions.None`; `Logs/sentry-release-build.log`, `Builds/FirstPlayable/Rustline.exe` |
| Release collision guard | Actual build scenes: main terrain **4 paths / 78 points**, precision ceiling **1 / 4**, unchanged MovementLab **9 / 48**; initializer and guard code unchanged |
| Standalone smoke, Editor closed | Exit **0**, `FIRST_PLAYABLE_SMOKE_PASS`, `Logs/sentry-release-smoke.log`; actual hitscan fights, committed checkpoint retry, extraction/restart and real input/Composite support preserved |

Initial focused tests exposed two issues that were corrected rather than bypassed: consumed kinematic velocity selected idle during actual patrol, and a snap assertion sampled before LateUpdate. Movement now exposes its committed step; the test observes the actual camera-render callback. An initial Editor setup also rejected a missing renderer because C# null coalescing does not honor Unity's missing-component wrapper; explicit Unity null checks now create it correctly.

Inspected `Logs/SentryArt/contact-sheet-4x.png` (nearest-neighbor inspection enlargement of final cells), and `world-visible.png`, `world-penumbra.png`, `world-dark.png` from the actual resolved native-pixel target. The latter use a held camera/disabled player physics to isolate foreground presence at 4, 30.5 and 35 world units. Foreground-on/off comparisons require visible pixels inside the clear radius and penumbra, canonical colors in every changed pixel, and zero Sentry contribution in full darkness. These are real offscreen rendering tests, **not normal-window gameplay screenshots or a human art approval**. Existing native-pixel/penumbra code and shaders are unchanged.

The final Release compilation produced no C# warnings. The environment still reports the existing empty Unity Cloud access token/native-symbol upload warning and optional VisionOS module load diagnostic. The successful standalone smoke still ends with the previously recorded `GarbageCollector disposing of ComputeBuffer` warning. This pass creates no gameplay ComputeBuffers and does not claim to resolve that unrelated rendering shutdown diagnostic. Existing test assemblies also retain unrelated deprecated-API warnings.

### Remaining human review / limits

1. Play normally from the west entry and approach the first Sentry. Review idle/walk/pursuit legibility against the refinery backdrop, the complete windup/strike/recovery, hit flinch and collapse. Dodge behind its locked attack and confirm facing/feedback read correctly.
2. Shoot a visible dormant Sentry early, retry before/after a station and restart with F5. Verify restored live presentation and retained committed wrecks in ordinary play; automated state/combat coverage is separate from this feel review.
3. Inspect native 1× and integer 2× presentation, near radius 456, within the 64 px penumbra and in full darkness, in Editor and a normally launched Windows player. Assess silhouette contrast, foot alignment, pose transitions and final-window readability; no human visual sign-off is claimed.

This is one coherent first production pass, with four reused walk poses and mirrored side-view art rather than a universal animation framework. Further frame retouching/cadence tuning should follow human visual review. No enemy audio, Bombardier redesign, level expansion, new dependencies or measured FPS claims are part of this milestone.

Changed systems: the production PNG/source/provenance and metas; new Sentry presenter/frame selector/art setup; narrow Sentry gameplay metadata and builder hookup; saved Sentry visual children; focused EditMode/PlayMode tests and the existing dormant-visibility assertion; launch/status docs. Work remains on `feat/sentry-art-pass`; no push, merge or pull request was performed. Builds, detailed test logs and inspection renders are local ignored artifacts.
