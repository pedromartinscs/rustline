# Active Asset Sprints

This document tracks the current production-art push for Rustline. The order below is the working sequence as of 2026-09-07; it may be adjusted when native-scale, in-engine validation exposes a better dependency order.

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
- Longwatch aim-capable overlays remain 80×96 with armed pivot (24,8);
- continuous gameplay aim remains independent from 10-degree authored visual quantization;
- left-facing presentation is produced through the accepted mirrored runtime path unless a state explicitly requires different authored treatment.

Do not multiply an unapproved locomotion pose across weapon directions. Crouch and wall presentation therefore have explicit visual approval gates before the corresponding Longwatch packages are authored.

## Asset Sprint 1 — Crouch and wall locomotion foundation

Goal: remove the remaining player movement fallbacks before expanding armed presentation.

- [ ] 1. Crouch Body
- [ ] 2. Crouch Unarmed Arms
- [ ] 3. Approve crouch in-engine at native gameplay scale
- [ ] 4. Wall Brace Body / Unarmed Arms
- [ ] 5. Wall Kick Body / Unarmed Arms
- [ ] 6. Approve wall presentation in-engine at native gameplay scale

Acceptance notes:

- Crouch must preserve the existing grounded-only mechanics, 3 units/s crouch speed, collider behavior, stand-clearance logic, and aim-facing semantics.
- Wall art must preserve the existing Wall Brace / Wall Kick mechanics and timing. Presentation must not change the 4 units/s brace descent cap, 8 / 11.5 kick velocity, or 0.12 s lock.
- Crouch frame counts become the authoritative N used by the armed crouch packages in Sprint 2.
- Wall frame counts become the authoritative carry-frame counts used by the armed wall package in Sprint 2.

## Asset Sprint 2 — Complete Longwatch locomotion presentation

Goal: remove unsupported-state Longwatch presentation fallbacks and complete the first weapon's art pipeline.

- [ ] 7. Longwatch Fall aim — 19 right-facing authored sprites
- [ ] 8. Longwatch Jump carry — 3 sprites
- [ ] 9. Longwatch Land carry — 2 sprites
- [ ] 10. Longwatch Crouch Idle — 19 × N, where N is the approved Crouch Idle frame count
- [ ] 11. Longwatch Crouch Move — 19 × N, where N is the approved Crouch Move frame count
- [ ] 12. Longwatch Wall carry — one carried/locked weapon presentation per approved wall animation frame

Presentation policy:

- Fall and Crouch Idle / Crouch Move are aim/fire-capable states and therefore use the 19-direction authored set.
- Jump, Land, Wall Brace, and Wall Kick keep the weapon visible through carry-only presentation and remain non-firing states.
- Body animation remains the authoritative frame clock; weapon art follows the approved locomotion frame one-to-one.
- The reusable armed import/presenter contract should be considered ready to scale to the remaining arsenal only after these packages are imported, validated, and human-approved in-engine.

## Asset Sprint 3 — Production ground enemy

Goal: replace the existing programmer-art M3A ground enemy without changing the reusable combat foundation.

- [ ] 13. Ground Enemy concept
- [ ] 14. Ground Enemy Patrol / Move
- [ ] 15. Hit reaction
- [ ] 16. Death

The current prototype already provides the combat behavior to skin: 100 HP, deterministic horizontal patrol, 0.12 s nonlethal hit pause, death after the third normal 40-damage Longwatch hit, disabled hitbox on death, and MovementLab reset. Production art should initially fit that proven behavior rather than expanding AI scope during the art sprint.

## Asset Sprint 4 — Production gun-feel presentation

Goal: replace prototype firing presentation with authored production metadata, FX, and audio.

- [ ] 17. Muzzle metadata
- [ ] 18. Muzzle flash
- [ ] 19. Production impact FX
- [ ] 20. Combat audio

Integration notes:

- Authored muzzle metadata should replace the current AimOrigin-derived approximation for the visible muzzle/tracer origin while preserving continuous shot direction.
- Muzzle flash must remain presentation-only and must not affect hit resolution.
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
