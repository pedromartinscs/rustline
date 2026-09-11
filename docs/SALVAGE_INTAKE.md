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

## Current spatial contract — Graybox v2.5

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
- a **16×1 tile** overhead service deck at `y=18`, providing a real structural mounting surface above the salvage handler;
- left/right boundary walls extending to `y=19`, matching the raised overhead envelope;
- player spawn centered on the west production bulkhead at **x = -20** and serialized at **y = 1.75**, giving the player root **0.75 u / 12 source pixels** of initial clearance above the entry-deck surface at `y=1` before gravity settles it;
- exit-staging marker on the east side.

The original v2 left-approach geometry used a floating ledge beginning one tile after the service step. Human playtesting exposed a real wedge/stuck case there: the combination produced a **16 px horizontal notch followed by a 16 px undercut**, both below the intended traversal-safe envelope. Graybox v2.1 closed that geometry by making the left approach a solid `4×2` block beginning exactly where the service step ends. This removes the sub-minimum notch and inaccessible undercut rather than changing the player capsule.

A second playtest exposed the same class of problem at the transition from the central plinth toward the catwalk. The original catwalk began only **1 tile / 16 px** beyond the plinth edge, creating a diagonal micro-gap where the capsule could partially enter and wedge. Graybox v2.2 moves the catwalk start one tile east and shortens it correspondingly, leaving a real **2 tile / 32 px** opening. That gap is now intentionally traversable rather than an ambiguous near-fit, while the east support and overall room envelope remain unchanged.

Graybox v2.3 added the authoritative overhead service deck and corrected the original west spawn from the room baseline to the entry-deck level. Graybox v2.4 gave the serialized spawn deliberate clearance instead of authoring it at an almost-touching epsilon. The canonical standing collider has its bottom exactly at the player root, so the raised authoring position is a spawn-presentation/safety choice; normal gravity remains responsible for final grounding.

Graybox v2.5 raises the overhead service deck from `y=15` to `y=18` and extends the side boundary walls to the same `y=19` top envelope. This gives the large central machinery more vertical breathing room and makes the room read as a credible industrial volume rather than machinery compressed under a low ceiling. The handler moves only **8 source pixels / 0.5 u** upward, preserving the established machine composition while reducing crowding around the housing/player line.

The critical route does not require a diagnostic sequence of every movement ability. The geometry should read as an industrial place first. Jump, LedgeClimb, Wall Brace/Kick, crouch, Fall aim, and future combat positioning should emerge from useful architecture rather than isolated ability-test stations.

## Visual geometry versus gameplay collision

The scene retains three graybox Tilemaps:

1. `Background Structure - Visual` — currently intentionally **empty** and reserved for future tile-based background needs;
2. `Industrial Surface - Visual` — the visible structural graybox corresponding to the current traversable solids, including the overhead service deck;
3. `Ground Collision - Hidden` — the authoritative gameplay geometry.

The original tile-built machinery/background placeholders have been retired. Production background identity now comes from non-colliding sprites under `Art Dressing - Preserve`, beginning with Macro Environment Kit v0.

Production structural skins such as the catwalk are also non-colliding SpriteRenderers. They may render in front of the graybox visual Tilemap to replace its appearance, but the simple hidden Tilemap remains the sole gameplay collision authority. The current catwalk skin therefore does **not** add sprite colliders even though the player physically walks on the platform it depicts.

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

## Environment production assets

The accepted Canonical-28 macro/architecture assets currently include:

- `Environment/Machinery/gantry_upright_a.png`;
- `Environment/Machinery/gantry_upright_b.png`;
- `Environment/Machinery/salvage_handler.png`;
- `Environment/Machinery/machine_housing.png`;
- `Environment/Machinery/gantry_overhead_rail.png`;
- `Environment/Architecture/bulkhead_door_frame.png`;
- `Environment/Architecture/bulkhead_door_frame_small.png` — current west-entry variant;
- `Environment/Architecture/catwalk_left.png`;
- `Environment/Architecture/catwalk_mid_short.png`;
- `Environment/Architecture/catwalk_mid_long.png`;
- `Environment/Architecture/catwalk_right.png`;
- `Environment/Architecture/catwalk_assembly_large.png` — reference/full assembly; prefer modular pieces for production placement when practical.

The current west entry uses the **105×100 px** source-authored small bulkhead at native 16 PPU / Transform scale 1.0 rather than scaling the original 210×200 sprite in Unity. Its bottom is anchored to the entry-deck surface at `y=1`, giving a center position of `(-20, 4.125)`. The player spawn shares its horizontal center. The salvage handler is centered at `y=13.0`, **8 source pixels** above the prior pass, while its long suspension continues upward behind the structural service deck at `y=18..19`.

The current **12 u** gameplay catwalk is visually dressed with `catwalk_left + catwalk_right`. Together those source sprites measure **200 px / 12.5 u**, so centering them over the authoritative 12 u collision creates only a restrained **4 source pixel** visual overhang at each outer end. The modules render at sorting order `2`: in front of the gray structural Tilemap but behind the player renderers.

`catwalk_mid_short` (**100×128 px**) and `catwalk_mid_long` (**200×128 px**) are accepted reusable modules but are not required by this first 12 u assembly. They remain available for longer future spans rather than being forced into the current room.

`gantry_overhead_rail.png` is accepted as Canonical-28 production art and retains native scale, but it is **not yet placed** in Salvage Intake. Its source canvas is **870×160 px = 54.375×10 u** at 16 PPU, almost the room's entire width. Placing it now would make the asset dictate the room architecture and could visually imply collision across a mostly non-colliding span. Prefer extracting/re-authoring a modular rail family or deliberately designing a later room-wide crane span before using this full-size source asset.

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

The central visual idea is a **salvage transfer / sorting machine** whose production art occupies much more visual space than its simple collision plinth. The first macro kit now provides the two tall gantry uprights, suspended salvage handler, central machinery housing, west bulkhead, and first production catwalk skin that begin defining this silhouette.

The intended visual hierarchy is:

1. readable player and combat silhouettes;
2. collision-authoritative structural floor/catwalk edges;
3. hero transfer machine;
4. large background structure;
5. pipes, conduits, vents, warning markings, corrosion, and restrained foreground dressing.

Do not solve the room by adding many overlapping lights, full-screen effects, or large transparent illustrations. Prefer composition, modular pieces, bounded sprites/overlays, and the existing penumbra presentation.

## Next environment-asset gate

Do **not** fill every unused slot in the structural atlas merely because they are available.

The next useful production targets should be selected from what the dressed room visibly needs. Current high-value candidates are:

- modularize or source-resize the accepted `gantry_overhead_rail` if a production top rail is still desired for this room;
- floor-edge / deck-trim family to replace the largest remaining graybox masses;
- vertical wall-edge / bulkhead strip family;
- top-surface variants (`industrial_surface` slots 24/25) only where the room actually benefits;
- one minimal pipe/conduit family;
- structural support / bracket pieces where the catwalk or machinery composition visibly asks for them;
- later, vents, warning markings, restrained status lights, cables, rust overlays, and other small-detail dressing.

This list is a starting expectation, not a requirement to manufacture unused assets.

## Immediate acceptance gate

Inspect the current scene in Unity and verify:

- the room is traversable without getting stuck;
- no physical opening narrower than the accepted authoring envelope invites entry;
- the player visibly enters from the center of the west bulkhead and starts with clear separation from the deck before normal gravity settles it;
- the smaller source-authored bulkhead has a better gameplay-scale relationship to the player than the original 210×200 variant;
- the old tile-built background placeholders are gone;
- all managed environment sprites introduce **no colliders**;
- `catwalk_left + catwalk_right` visually cover the existing 12 u catwalk without misleadingly changing its gameplay span;
- the catwalk skin remains behind the player while hiding the gray structural block beneath it;
- the raised handler reads as suspended from the overhead service deck rather than floating;
- the taller overhead deck/side-wall envelope gives the hero machinery enough visual breathing room;
- the overhead deck remains collision-coherent if reached;
- player and environment assets retain coherent relative scale at native presentation;
- the west entry / lower bay / catwalk relationship feels like architecture rather than a movement tutorial;
- the compact central plinth and catwalk create useful sightline/positioning changes with the Longwatch;
- upper and lower routes feel meaningfully distinct even before enemies are added;
- the west-to-east route is readable under the existing penumbra;
- no new collision seam, phantom Land, or Release-only floor regression appears;
- the player/Longwatch presentation remains unchanged.
