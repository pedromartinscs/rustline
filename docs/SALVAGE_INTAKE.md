# Salvage Intake — First Production Area

`SalvageIntake` is Rustline's first production-area graybox and the first step away from the diagnostic `MovementLab` toward the itch.io vertical slice.

The current goal is deliberately narrow: build one coherent industrial room that is enjoyable to traverse and aim through with the frozen player/Longwatch foundation before creating a broad environment asset library.

## Scene and builders

Target scene:

`Assets/Scenes/Demo/SalvageIntake.unity`

Deterministic graybox builder:

`Assets/Editor/RustlineSalvageIntakeSetup.cs`

Macro art-dressing setup:

`Assets/Editor/RustlineSalvageIntakeArtSetup.cs`

Unity menus:

- **Tools → Rustline → Rebuild Salvage Intake Graybox**
- **Tools → Rustline → Apply Salvage Intake Macro Dressing**

The graybox builder first runs the existing M1 validation, then creates/synchronizes the production-area collision and player rig without modifying `MovementLab`.

The builder owns and regenerates only these roots:

- `Environment - Managed Graybox`
- `Player Rig - Managed`

These roots are deliberately preserved across graybox rebuilds and must be used for hand-authored work that should survive regeneration:

- `Art Dressing - Preserve`
- `Gameplay Content - Preserve`

The macro-dressing tool owns only `Art Dressing - Preserve/Macro Environment Kit v0 - Managed`. Do not place unrelated permanent hand-authored dressing inside that managed child.

## Current spatial contract — Graybox v2.3

The room remains **56 canonical tiles wide** at the outer boundary. All current gameplay geometry is aligned to the normal **16×16 px / 1×1 unit** structural grid.

The current pass intentionally contains no sub-cell collision. The accepted **24 source pixel / 1.5 unit Minimum Traversable Gap** remains the hard authoring floor, while ordinary production openings should normally use at least **2 whole tiles / 32 px** when practical.

Graybox v2 keeps the room envelope from the first pass but replaces the movement-course-like central staircase/mass with a clearer industrial composition:

- a continuous safety floor and left/right bulkhead boundaries;
- a simple west-side entry deck;
- a small service step leading toward the lower central bay;
- a compact **4×2 tile** central collision plinth for the salvage machine;
- a **12×1 tile** upper transfer catwalk crossing part of the bay;
- a one-tile-wide east catwalk support;
- a simple east-side staging / future exit deck;
- a **16×1 tile** overhead service deck at `y=15`, providing a real structural mounting surface above the salvage handler;
- player spawn centered on the west production bulkhead at **x = -20** and positioned at **y = 1.08**, preserving the accepted `0.08 u` spawn clearance above the actual entry-deck surface at `y=1`;
- exit-staging marker on the east side.

The original v2 left-approach geometry used a floating ledge beginning one tile after the service step. Human playtesting exposed a real wedge/stuck case there: the combination produced a **16 px horizontal notch followed by a 16 px undercut**, both below the intended traversal-safe envelope. Graybox v2.1 closed that geometry by making the left approach a solid `4×2` block beginning exactly where the service step ends. This removes the sub-minimum notch and inaccessible undercut rather than changing the player capsule.

A second playtest exposed the same class of problem at the transition from the central plinth toward the catwalk. The original catwalk began only **1 tile / 16 px** beyond the plinth edge, creating a diagonal micro-gap where the capsule could partially enter and wedge. Graybox v2.2 moves the catwalk start one tile east and shortens it correspondingly, leaving a real **2 tile / 32 px** opening. That gap is now intentionally traversable rather than an ambiguous near-fit, while the east support and overall room envelope remain unchanged.

Graybox v2.3 corrects two composition/authoring issues revealed by the first production-dressing screenshot. The west spawn now uses the entry deck's actual top surface rather than the room baseline, avoiding the visible one-tile embed before physics resolution. The new overhead service deck is authoritative structural/collision geometry, not a decorative background strip, so the suspended machinery has a coherent physical mounting surface if traversal ever reaches it.

The critical route does not require a diagnostic sequence of every movement ability. The geometry should read as an industrial place first. Jump, LedgeClimb, Wall Brace/Kick, crouch, Fall aim, and future combat positioning should emerge from useful architecture rather than isolated ability-test stations.

## Visual geometry versus gameplay collision

The scene retains three graybox Tilemaps:

1. `Background Structure - Visual` — currently intentionally **empty** and reserved for future tile-based background needs;
2. `Industrial Surface - Visual` — the visible structural graybox corresponding to the current traversable solids, including the overhead service deck;
3. `Ground Collision - Hidden` — the authoritative gameplay geometry.

The original tile-built machinery/background placeholders have been retired. Production background identity now comes from non-colliding sprites under `Art Dressing - Preserve`, beginning with Macro Environment Kit v0.

`Ground Collision - Hidden` keeps the release-hardened contract from `RELEASE_COLLISION.md`:

- Ground layer 6;
- renderer disabled;
- static `Rigidbody2D`;
- `TilemapCollider2D` with `Merge`;
- polygon `CompositeCollider2D`;
- `TilemapCompositeColliderInitializer2D`;
- immediate geometry generation/validation before the scene is accepted.

The current visible structural graybox mirrors the collision cells because that is useful during blockout. This is **not** a permanent requirement. As dressing develops, visual wear, cracks, recesses, machine silhouettes, pipes, foreground pieces, and background structure may diverge freely from collision while gameplay collision remains simple and traversal-safe.

## Native-pixel environment scale

Environment sprites follow the same source-pixel contract as the player:

- production PNGs import at **16 PPU**;
- macro environment SpriteRenderers remain at **Transform scale 1.0**;
- at the base presentation scale, one source-art pixel maps to one display pixel;
- integer presentation scaling (`2×`, `3×`, `4×`, ...) scales the complete logical frame together, preserving player/environment proportions;
- fractional Transform scaling is not a production technique for correcting an environment asset's apparent size.

If a production environment asset is judged too large or too small, prefer reauthoring/resampling it deliberately at source pixel resolution rather than applying arbitrary runtime Transform scale.

## Macro Environment Kit v0

The first accepted Canonical-28 macro assets are:

- `Environment/Machinery/gantry_upright_a.png`;
- `Environment/Machinery/gantry_upright_b.png`;
- `Environment/Machinery/salvage_handler.png`;
- `Environment/Machinery/machine_housing.png`;
- `Environment/Architecture/bulkhead_door_frame.png`.

They are currently placed as non-colliding background/architectural dressing at native scale. The bulkhead is centered at `x=-20`, and the player spawn uses the same horizontal center so the opening reads as the actual west-side entry point. The salvage handler is centered at `y=12.5`, one canonical tile above the first macro pass, so its suspension intersects the structural service deck at `y=15..16` and reads as mounted machinery rather than a free-floating object.

## Presentation/performance invariants

Salvage Intake reuses the accepted player prefab and the same scene-level presentation architecture proven in MovementLab:

- 16 PPU;
- Rustline Canonical 28 production art contract;
- `Sprite-Unlit-Default` for current graybox and macro dressing;
- native-pixel logical rendering;
- integer presentation scaling;
- palette-constrained penumbra;
- no identity/decorative `Light2D` objects in the current pass;
- existing 60 FPS runtime policy unchanged.

The builder wires `PlayerAim2D` to the scene's `NativePixelPresentation` and preserves the accepted Longwatch camera-impulse integration without changing player, movement, weapon, muzzle, recoil, or carry behavior.

## Hero-room composition

The central visual idea is a **salvage transfer / sorting machine** whose production art occupies much more visual space than its simple collision plinth. The first macro kit now provides the two tall gantry uprights, suspended salvage handler, central machinery housing, and west bulkhead that begin defining this silhouette.

The intended visual hierarchy is:

1. readable player and combat silhouettes;
2. collision-authoritative structural floor/catwalk edges;
3. hero transfer machine;
4. large background structure;
5. pipes, conduits, vents, warning markings, corrosion, and restrained foreground dressing.

Do not solve the room by adding many overlapping lights, full-screen effects, or large transparent illustrations. Prefer composition, modular pieces, bounded sprites/overlays, and the existing penumbra presentation.

## Next environment-asset gate

Do **not** fill every unused slot in the structural atlas merely because they are available.

After the current macro dressing is inspected in-engine at native scale, choose the next art batch from what the room visibly needs. Likely candidates include:

- a reusable gantry/rail beam if the current silhouette needs an authored top connection;
- catwalk visual skin/support language;
- top-surface variants (`industrial_surface` slots 24/25);
- left/right wall variants (26/27);
- ceiling variant (28);
- interior plate variants (29/30/31);
- structural support / bracket pieces (44/45);
- one minimal pipe/conduit family;
- vent, warning marking, and restrained status-light treatments.

This list is a starting expectation, not a requirement to manufacture unused assets.

## Immediate acceptance gate

Inspect the current scene in Unity and verify:

- the room is traversable without getting stuck;
- no physical opening narrower than the accepted authoring envelope invites entry;
- the player visibly enters from the center of the west bulkhead and begins above the entry-deck surface without an initial physics correction;
- the old tile-built background placeholders are gone;
- the five macro sprites remain visually behind gameplay structure and introduce no colliders;
- the raised handler reads as suspended from the overhead service deck rather than floating;
- the overhead deck remains collision-coherent if reached;
- player and macro assets retain coherent relative scale at native presentation;
- the west entry / lower bay / catwalk relationship feels like architecture rather than a movement tutorial;
- the compact central plinth and catwalk create useful sightline/positioning changes with the Longwatch;
- upper and lower routes feel meaningfully distinct even before enemies are added;
- the west-to-east route is readable under the existing penumbra;
- no new collision seam, phantom Land, or Release-only floor regression appears;
- the player/Longwatch presentation remains unchanged.
