# Salvage Intake — East Service Shaft Test Pass

This document records the first traversal-driven extension beyond the accepted Salvage Intake hero room. The shaft is intentionally implemented as a scene-specific test pass before promotion into the canonical `RustlineSalvageIntakeSetup` graybox builder.

## Tool

Apply or re-apply the current test geometry with:

**Tools -> Rustline -> Apply Salvage Intake Service Shaft**

Validate the saved result with:

**Tools -> Rustline -> Validate Salvage Intake Service Shaft**

If `Rebuild Salvage Intake Graybox` is run while this pass remains experimental, re-run the service-shaft tool afterward. The service-shaft tool also removes the obsolete full-height east boundary wall-skin children while preserving the approved west wall skin.

## Geometry contract

All geometry remains on the canonical `16 px = 1 u` grid. Visual structure and hidden collision use the same coarse shape for this first traversal test; later production skins may diverge visually while preserving the hidden collision envelope.

The east extension owns the region `x=26..45`, `y=-4..18` and rebuilds only that reserved region. The accepted hero-room composition to the west remains untouched.

Current service-shaft geometry:

- safety floor under the entry/shaft: `x=26..33`, `y=-4..-1`;
- lower shaft entry: `x=26..27`, `y=0..3` remains completely open, giving **4 u / 64 px** of standing clearance;
- left shaft wall: `x=26..27`, `y=4..18`;
- open shaft interior: **exactly four cells**, `x=28..31` = **4 u / 64 px**;
- right shaft wall: `x=32..33`, `y=0..14`;
- the right-wall top is therefore a walkable/ledge-climbable surface at `y=15`;
- upper continuation deck: `x=34..45`, `y=14`, with its walkable surface at `y=15`;
- `Exit Staging` moves to `(42, 15.08, 0)` on the upper-right deck.

The four-cell interior width deliberately reuses the human-tested MovementLab shaft spacing rather than treating the 24 px Minimum Traversable Gap as a target. The shaft walls are continuous where Wall Brace is expected, satisfying the authored wall-coverage intent instead of using broken decorative collision.

## Intended route

The player crosses the east side of Salvage Intake at floor level, walks through the 4 u-high lower opening, then climbs the 4 u-wide service shaft with Wall Brace / Wall Kick. The lower right wall provides the first reliable brace surface; alternating kicks then climb between the two walls. The top of the right wall becomes a natural ledge/landing at `y=15`, leading directly onto the new east continuation deck.

This is intended to make traversal emerge from plausible industrial architecture rather than reading as a MovementLab ability station.

## Collision / presentation invariants

The pass keeps the accepted release-critical collision pipeline:

- hidden `Ground Collision - Hidden` Tilemap remains authoritative;
- Ground layer and existing TilemapCollider2D -> CompositeCollider2D pipeline are unchanged;
- collision is explicitly refreshed and baked after the shaft cells change;
- the production player, Longwatch, native-pixel presentation, penumbra, and movement tuning are untouched;
- no new collider is attached to environment dressing sprites.

The current shaft uses `Industrial Surface - Visual` directly while traversal is being evaluated. Do not create shaft-specific wall/floor skins until human playtesting approves width, entry height, wall heights, and exit placement.

## Human acceptance gate

Before promoting this geometry into the canonical Salvage Intake builder, verify in Play mode:

- walking through the lower entry never clips or wedges the standing player;
- first contact with the right wall can enter a valid Wall Brace naturally;
- repeated alternating Wall Kicks feel comfortable at 4 u / 64 px width;
- neither wall creates false brace poses with hands/feet in empty space;
- the player can reach the top-right ledge consistently without pixel-perfect input;
- LedgeClimb at the right-wall top behaves naturally when its normal intent requirements are met;
- exiting right onto the `y=15` deck feels like continuation of a level, not the end of a diagnostic shaft;
- the camera transition upward/right remains readable under native-pixel presentation;
- no new Composite-collider seam, phantom Land, or Release-only collision regression appears.

After approval, fold the exact geometry into `RustlineSalvageIntakeSetup` as the next canonical graybox revision and retire the temporary scene-specific overlay workflow.
