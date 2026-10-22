# Salvage Intake expansion — design and authoring audit

## Baseline / ownership

Start: clean `feat/salvage-intake-expansion`, `27ae1a2` (updated main merge). Unity 6000.4.0f1, unchanged URP/Input packages. Read AGENTS, first-playable design/validation/launch docs, environment metrics, release collision, presentation, movement config, director/progress/encounters/AI, production/dressing/parallax builders and tests. The user reports a successful human playthrough of the preceding milestone; the expansion still requires its own human acceptance.

The introductory playable route projects from **x=-20 to x=43**, with the first shaft climbing to **y=17**. Horizontal span is **63 u**; horizontal plus required ascent is about **80 u**, a design proxy rather than measured player travel. Original collision occupies x=-28..45 (west boundary, safety floor, platforms, shaft and deck). Original encounters: west sentry, transfer-bay pair, upper-deck pair. Service checkpoint x=22 commits two encounters; extraction was x=43 after three.

`RustlineSalvageIntakeSetup` rebuilds `Environment - Managed Graybox` and `Player Rig - Managed`. Production passes subsequently refine terrain and regenerate named dressing/parallax children. `Art Dressing - Preserve` and `Gameplay Content - Preserve` preserve unrelated authoring. FirstPlayable setup owns only `First Playable - Managed`. Scene content is mostly deterministically authored; preserved roots are extension points for hand authoring, not permission to destroy their contents.

The expansion is a deterministic **final terrain/dressing pass before gameplay setup**, not runtime generation. It owns extension cells x=46..195, y=16..40 on existing structural/collision Tilemaps and its own named managed child. Original collision cells x<46 stay unchanged. New precision maintenance ceiling is a separate offset Tilemap/Composite, not a replacement floor. Both bake through the existing initializer sequence and actual-build guard. No movement tuning, capsule changes, penumbra changes, package upgrades or automated gap enforcement.

## Continuous physical route

| Space | Physical route / purpose | Encounter |
| --- | --- | --- |
| Existing introduction | x=-20..43, y=0..17; preserved traversal and combat | 01–03 unchanged |
| Upper relay refuge | Former extraction x=43, y=17; now a checkpoint and seamless passage east | Commits 3 |
| Processing gallery | x=46..95, floor y=17; cover, jump steps, elevated firing platform and machinery landmarks | 04 gallery pair |
| Maintenance machinery | x=96..127, floor y=17; 42 px crouch underpass and optional elevated roof route, then a four-unit wall-kick shaft from y=17 to y=29 | 05 maintenance pair |
| Upper service refuge | x=135, y=29; respite after the second shaft | Commits 5 |
| Rail transfer | x=140..168, y=29; four-unit bridge gap, lower catch/recovery stairs, elevated ranged position | 06 rail pair |
| Terminal defense | x=174..195, y=29; larger final fighting deck with low cover | 07 final trio |
| Final extraction | x=193, y=29; all seven encounters plus uninterrupted two-second hold | Completion |

New endpoint projects **213 u** east of spawn (3.38× previous horizontal span), with 29 u total required ascent (~242 u horizontal-plus-ascent proxy, ~3×). Actual travel time/distance and FPS remain unmeasured. Three new differentiated sections add real collision, visual structure, original production sprites and background composition, not teleports. The bridge gap has a traversable recovery route. No mandatory jump exceeds the existing envelope by design; input-driven geometry tests and human playtests must verify this.

Use original native-scale floor, gantry, handler, machine, rail and shaft art. Far horizontal backdrop already wraps; vertical depth backdrop is screen-locked on X with a 270 u minimum reveal span, sufficient for the new y=29 route. Keep camera follow, Canonical 28 and radial penumbra (456 px visible +64 px transition) unchanged. Landmark signs may use the existing bitmap font. Enemy art remains temporary.

## Visibility, early interaction and progression policy

Alive enemy roots, silhouettes, health and explicit weapon hitboxes remain active from scene load. Only AI stepping is dormant. Presentation and damage/death subscriptions keep running. A hit on a dormant enemy wakes its encounter immediately, including out-of-order encounters; lethal hits remain lethal. Proximity can wake any encounter the player reaches. Progress only clears the contiguous defeated prefix. Later activation never heals/recreates early-defeated enemies.

Cache enemy AI references and event subscriptions once. Do not search the scene per enemy per frame, disable whole roots, use invisible immunity, or touch the darkness shader. Retry resets every encounter after the checkpoint's committed prefix (including early kills in uncommitted later encounters); committed clears remain dead. Full restart resets every encounter in place. Clear owned bombs and player projectile/transient state on retry.

Seven encounters / fourteen defenders and three checkpoints are authored scene data. Progress receives its encounter count; checkpoint data supplies required clears, area, spawn and label. HUD and extraction use these data. No campaign framework or per-level hard-coded count/checkpoint branching.

## Acceptance / verification plan

- Baseline full EditMode/PlayMode, then focused visibility/early-death tests before geometry work.
- Saved scene, continuous tile seam at x=45/46, release-hardened static Ground/Merge/polygon Composite and non-empty paths/points on every collision Tilemap.
- Real keyboard-driven tests over extended terrain, jump obstacles, crouch underpass, bridge/recovery and a wall-kick shaft. Placement allowed only to establish focused test starts, not as proof of whole-route traversal.
- Test all three checkpoints, committed/uncommitted early kills, full restart, ordered seven-encounter extraction and serialized references.
- Run the full available suites, production rebuild/reapply validation, normal Windows Release and smoke with Editor closed. Inspect offscreen renders while identifying them accurately.
- Human gates: uninterrupted complete route, alternate-route readability, both shafts/ledge transitions, combat balance and final screen composition at several resolutions. No measured duration/FPS claims without measurements.

Validation results and final manual gates: `salvage-intake-expansion-validation.md`.
