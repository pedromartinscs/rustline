# Salvage Intake — East Service Shaft

The east service shaft is now a **canonical part of Salvage Intake** and is generated directly by `Assets/Editor/RustlineSalvageIntakeSetup.cs`.

The temporary scene-specific service-shaft overlay tool has been retired. Running **Tools -> Rustline -> Rebuild Salvage Intake Graybox** now reproduces the approved shaft and upper-right continuation deck automatically.

## Geometry contract

All geometry remains on the canonical `16 px = 1 u` grid. Visual structure and hidden collision currently use the same coarse shape in the shaft; later production skins may diverge visually while preserving this collision envelope.

Canonical service-shaft geometry:

- safety floor extends beneath the entry/shaft through `x=33`, `y=-4..-1`;
- lower shaft entry: `x=26..27`, `y=0..3` remains completely open, giving **4 u / 64 px** of standing clearance;
- left shaft wall: `x=26..27`, `y=4..18`;
- open shaft interior: **exactly four cells**, `x=28..31` = **4 u / 64 px**;
- right shaft wall: `x=32..33`, `y=0..14`;
- the right-wall top is a walkable/ledge-climbable surface at `y=15`;
- upper continuation deck: `x=34..45`, `y=14`, with its walkable surface at `y=15`;
- `Exit Staging` is at `(42, 15.08, 0)` on the upper-right deck.

The four-cell interior width deliberately reuses the human-tested MovementLab wall-kick spacing rather than treating the 24 px Minimum Traversable Gap as a target. The shaft walls remain continuous where Wall Brace is expected, satisfying the authored wall-coverage contract.

## Intended route

The player crosses the east side of Salvage Intake at floor level, walks through the 4 u-high lower opening, then climbs the 4 u-wide service shaft with Wall Brace / Wall Kick. The lower right wall provides the first reliable brace surface; alternating kicks climb between the two walls. The top of the right wall becomes a natural ledge/landing at `y=15`, leading directly onto the east continuation deck.

The route is intended to feel like plausible industrial architecture rather than a diagnostic movement station.

## Collision / presentation invariants

The canonical builder keeps the accepted release-critical collision pipeline:

- hidden `Ground Collision - Hidden` Tilemap remains authoritative;
- Ground layer and existing TilemapCollider2D -> CompositeCollider2D pipeline are unchanged;
- generated collision is refreshed and baked before validation;
- player, Longwatch, native-pixel presentation, penumbra, and movement tuning remain untouched;
- environment dressing sprites do not gain colliders.

The shaft currently uses `Industrial Surface - Visual` directly. Shaft-specific production skins may be added later, but must not change the approved gameplay envelope without a deliberate traversal review.

## Accepted human validation

The current geometry was approved after Play-mode testing. The accepted qualities are:

- lower entry is comfortable and does not wedge the standing player;
- first right-wall contact supports Wall Brace naturally;
- repeated alternating Wall Kicks are comfortable at 4 u / 64 px width;
- the player can consistently reach the upper-right ledge without pixel-perfect input;
- the exit deck reads as continuation of the level rather than the end of a test course.

Any future geometry change to width, entry height, wall heights, or exit level should be treated as a traversal-contract change and re-tested accordingly.