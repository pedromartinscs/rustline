# Active Asset Sprints

This document tracks the current production-art push for Rustline. The order below is the working sequence as of **2026-09-10** and should change when the playable vertical slice demonstrates a better dependency order.

The project repository remains authoritative for implementation state. Asset approval is not complete until the result has been inspected in-engine at native gameplay scale.

## Production rules

All production art in these sprints must continue to follow the existing Rustline contracts:

- Rustline Canonical 28 plus transparency only;
- binary alpha;
- no antialiasing;
- 16 PPU;
- Point filtering;
- Full Rect meshes;
- no importer rescaling;
- canonical player Body / Unarmed Arms cells remain 48×64 with the shared bottom-center body reference;
- Longwatch aim-capable overlays remain 80×96 with armed pivot `(24,8)`;
- smaller fixed carry cells are allowed when the complete silhouette fits the canonical Body cell; Longwatch Jump/Land use 48×64;
- continuous gameplay aim remains independent from 10-degree authored visual quantization;
- left-facing presentation uses the accepted mirrored runtime path unless a state explicitly requires different authored treatment;
- visual environment detail must remain separable from gameplay collision and obey [`ENVIRONMENT_GAMEPLAY_METRICS.md`](ENVIRONMENT_GAMEPLAY_METRICS.md).

Do not multiply art before the representative implementation has proved its import/runtime contract. The Longwatch first-weapon locomotion package has now reached that point; the immediate production priority is environment rather than more player locomotion states.

## Asset Sprint 1 — Crouch, wall, and ledge locomotion foundation

Goal: remove the major player-movement presentation fallbacks before expanding armed presentation.

- [x] 1. Crouch Body — six authored frames integrated from `player_salvager_body_crouch.png`
- [x] 2. Crouch Unarmed Arms — six matching frames integrated from `player_salvager_arms_crouch.png`
- [x] 3. Approve crouch in-engine at native gameplay scale
- [x] 4. Wall Brace Body / Unarmed Arms — two matching frames integrated from `player_salvager_body_wall_brace.png` / `player_salvager_arms_wall_brace.png`
- [x] 5. Wall Kick uses the accepted Jump/Fall fallback; dedicated art is not currently required
- [x] 6. Approve wall presentation in-engine at native gameplay scale
- [x] LedgeClimb Body / Unarmed Arms — six matching 48×64 frames at 10 fps, integrated and human-approved as an immediate committed traversal

Acceptance notes:

- Crouch preserves grounded-only mechanics, 3 units/s crouch speed, collider behavior, stand-clearance logic, and aim-facing semantics.
- Wall art preserves the Wall Brace / Wall Kick mechanics and timing: 4 units/s brace descent cap, 8 / 11.5 kick velocity, and 0.12 s lock.
- Wall Brace loops its two authored Body / Unarmed Arms frames at 6 fps. The art is right-wall-authored, so `WallSide == +1` is unflipped and `WallSide == -1` is flipped; it does not consume or alter aim-facing.
- Wall Brace now also requires the authored seven-sample vertical wall-coverage band documented in `MOVEMENT.md`; the correction was human-tested successfully.
- Crouch Idle statically reuses frame 0 and intentionally has no breathing animation; Crouch Move loops frames 0..5 at 7 fps. Standing Idle remains the only breathing idle.
- Longwatch crouch presentation uses the completed six-frame directional package. Its presenter owns the shared overlay and firing is enabled in both crouch states.
- Wall Brace intentionally has no Longwatch carry package: both hands and one leg are committed to wall contact, so the unarmed overlay owns its matching Arms frames, the weapon is not rendered, and firing remains blocked.
- LedgeClimb has no grab/hang state and no Longwatch carry package. The accepted six-frame traversal commits once valid, mirrors by captured ledge side, blocks firing, keeps frame 4 pinned at its calibrated ledge contact for `0.40–0.50 s`, uses frame 5 as recovery, and ends at exactly `0.60 s` without a hidden settle/easing phase.

## Asset Sprint 2 — Complete Longwatch locomotion presentation

Goal: complete the first weapon's locomotion art/import/runtime contract.

- [x] 7. Longwatch Fall aim — 19 right-facing authored sprites, integrated as one Body-clocked frame per direction
- [x] 8. Longwatch Jump carry — 3 authored/integrated 48×64 sprites, human-tested in-engine
- [x] 9. Longwatch Land carry — 2 authored/integrated 48×64 sprites, human-tested in-engine
- [x] 10. Longwatch Crouch Idle — frame 0 of the shared 19 × 6 package
- [x] 11. Longwatch Crouch Move — shared 19 × 6 package following displayed Body frames one-to-one
- [x] 12. Longwatch Wall Brace presentation — no weapon carry by design; unarmed Wall Brace Arms own presentation and firing remains blocked

Presentation policy:

- Idle, Run, Backpedal, Crouch Idle / Crouch Move, and Fall are aim/fire-capable and use authored directional Longwatch presentation.
- Jump and Land retain Longwatch through non-firing Body-clocked 48×64 carry frames. They expose no muzzle-capable rendered pose and require no muzzle metadata.
- Active recoil returns to baseline and an already-active muzzle flash is canceled as soon as presentation no longer exposes a muzzle-capable Longwatch pose, preventing state-transition bleed into carry/traversal.
- Wall Brace permanently shows no Longwatch. LedgeClimb also releases to its unarmed six-frame traversal overlay. Dedicated Wall Kick art is not required; the accepted Jump/Fall fallback remains non-firing.
- Body animation remains the authoritative frame clock; weapon presentation never adds a second locomotion timer.
- No Longwatch locomotion carry assets remain pending for the current movement set.

## Asset Sprint 3 — First production environment slice — ACTIVE

Goal: turn the movement/combat laboratory into the first piece of a place that visibly belongs to Rustline, while keeping gameplay collision simple and authoritative.

- [x] Establish the first real area concept and deterministic `SalvageIntake` graybox builder
- [ ] Generate and inspect `Assets/Scenes/Demo/SalvageIntake.unity` in Unity at native scale
- [ ] Lock the first-room traversal/combat graybox after human playtesting
- [ ] Complete the useful structural tile family beyond canonical slots 00–15
- [ ] Establish floor / wall / ceiling / inside-outside corner variants needed by the first real area
- [ ] Establish beams, columns, brackets, and structural support language
- [ ] Add the first large industrial props / machinery silhouettes
- [ ] Add pipes, conduits, vents, warning markings, corrosion, and restrained dressing
- [ ] Establish foreground/background depth without compromising gameplay readability
- [ ] Produce one polished playable environment slice / hero room at native scale
- [ ] Expand the accepted visual language into the demo's single playable level

The first production-area contract is documented in [`SALVAGE_INTAKE.md`](SALVAGE_INTAKE.md). Its builder owns only the deterministic graybox/player-rig roots and deliberately preserves separate `Art Dressing` and `Gameplay Content` roots across rebuilds.

Do **not** build a giant generic asset library before a real scene needs it. Prefer a concrete room/sequence and author the minimum reusable pieces required to make that sequence look finished.

Environment collision remains independent from decorative art. In particular, any horizontal opening intended to be traversable must be at least **24 source pixels / 1.5 units** wide at its narrowest point; narrower visual cracks must be bridged by hidden collision. See [`ENVIRONMENT_GAMEPLAY_METRICS.md`](ENVIRONMENT_GAMEPLAY_METRICS.md).

## Asset Sprint 4 — Production ground enemy

Goal: replace the existing programmer-art M3A ground enemy without changing the reusable combat foundation.

- [ ] 13. Ground Enemy concept
- [ ] 14. Ground Enemy Patrol / Move
- [ ] 15. Hit reaction
- [ ] 16. Death

The current prototype already provides the combat behavior to skin: 100 HP, deterministic horizontal patrol, 0.12 s nonlethal hit pause, death after the third normal 40-damage Longwatch hit, disabled hitbox on death, and MovementLab reset. Production art should initially fit that proven behavior rather than expanding AI scope during the art sprint.

## Asset Sprint 5 — Remaining production gun-feel presentation

Goal: replace the remaining prototype combat presentation without rewriting authoritative shot/damage logic.

- [x] 17. Muzzle metadata
- [x] 18. Muzzle flash
- [ ] 19. Production impact FX
- [ ] 20. Combat audio

Integration notes:

- Longwatch muzzle metadata already supplies exact visual attachment data for supported authored poses. Hitscan/tracer gameplay still intentionally originates from `AimOriginWorld`; do not silently migrate ballistics while doing environment or FX work.
- Muzzle flash is presentation-only and does not affect hit resolution. It is canceled immediately when no muzzle-capable Longwatch pose remains active.
- Production impact FX should consume the existing authoritative resolved obstruction/hit result rather than performing a second physics query.
- Combat audio should be driven from existing combat/shot outcomes rather than coupled into health, weapon-hitbox routing, or enemy simulation.

## Working discipline

For each asset group:

1. establish or reuse an approved Rustline reference;
2. author/generate the candidate within the Canonical 28 constraints;
3. clean and palette-validate the final pixels;
4. pack/export using the documented fixed-cell geometry;
5. integrate without changing accepted simulation semantics;
6. validate automatically where possible;
7. inspect in-engine at native gameplay scale;
8. only then mark the checklist item complete and update the relevant Rustline documentation.
