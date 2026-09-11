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

## Current spatial contract — Graybox v2.6

The room remains **56 canonical tiles wide** at the outer boundary. All current gameplay geometry is aligned to the normal **16×16 px / 1×1 unit** structural grid.

The current pass intentionally contains no sub-cell collision. The accepted **24 source pixel / 1.5 unit Minimum Traversable Gap** remains the hard authoring floor, while ordinary production openings should normally use at least **2 whole tiles / 32 px** when practical.

The current room contains:

- a continuous safety floor and left/right bulkhead boundaries;
- a simple west-side entry deck;
- a small service step leading toward the lower central bay;
- a compact **4×2 tile** central collision plinth for the salvage machine;
- a **12×1 tile** upper transfer catwalk represented physically only by hidden collision and visually by the production catwalk sprites;
- **no east catwalk support wall** — the space below/right of the catwalk remains open;
- a simple east-side staging / future exit deck;
- a **16×1 tile** overhead service deck at `y=18`, providing a real structural mounting surface above the salvage handler;
- left/right boundary walls extending to `y=19`, matching the raised overhead envelope;
- player spawn centered on the west production bulkhead at **x = -20** and serialized at **y = 1.75**, giving the player root **0.75 u / 12 source pixels** of initial clearance above the entry-deck surface at `y=1` before gravity settles it;
- exit-staging marker on the east side.

The original v2 left-approach geometry used a floating ledge beginning one tile after the service step. Human playtesting exposed a real wedge/stuck case there: the combination produced a **16 px horizontal notch followed by a 16 px undercut**, both below the intended traversal-safe envelope. Graybox v2.1 closed that geometry by making the left approach a solid `4×2` block beginning exactly where the service step ends. This removes the sub-minimum notch and inaccessible undercut rather than changing the player capsule.

A second playtest exposed the same class of problem at the transition from the central plinth toward the catwalk. The original catwalk began only **1 tile / 16 px** beyond the plinth edge, creating a diagonal micro-gap where the capsule could partially enter and wedge. Graybox v2.2 moved the catwalk start one tile east and shortened it correspondingly, leaving a real **2 tile / 32 px** opening.

Graybox v2.3 added the authoritative overhead service deck and corrected the original west spawn from the room baseline to the entry-deck level. Graybox v2.4 gave the serialized spawn deliberate clearance instead of authoring it at an almost-touching epsilon. The canonical standing collider has its bottom exactly at the player root, so the raised authoring position is a spawn-presentation/safety choice; normal gravity remains responsible for final grounding.

Graybox v2.5 raised the overhead service deck from `y=15` to `y=18` and extended the side boundary walls to the same `y=19` top envelope. This gives the large central machinery more vertical breathing room. The handler moved only **8 source pixels / 0.5 u** upward.

Graybox v2.6 is the first deliberate production split between visual structure and gameplay collision. The catwalk's gray `Industrial Surface` cells are no longer rendered at all; `catwalk_left + catwalk_right` are now the sole visible platform. A simple hidden `12×1` collision strip remains authoritative for standing/traversal. The former one-tile east support wall was removed from both visual and collision geometry, leaving a true open span beneath the catwalk.

The critical route does not require a diagnostic sequence of every movement ability. The geometry should read as an industrial place first. Jump, LedgeClimb, Wall Brace/Kick, crouch, Fall aim, and future combat positioning should emerge from useful architecture rather than isolated ability-test stations.

## Visual geometry versus gameplay collision

The scene retains three graybox Tilemaps:

1. `Background Structure - Visual` — currently intentionally **empty** and reserved for future tile-based background needs;
2. `Industrial Surface - Visual` — visible generic structural surfaces where production structural skins have not replaced them;
3. `Ground Collision - Hidden` — the authoritative gameplay geometry.

The original tile-built machinery/background placeholders have been retired. Production background identity comes from non-colliding sprites under `Art Dressing - Preserve`.

Production structural skins such as the catwalk are also non-colliding `SpriteRenderer`s. They may visually represent a walkable surface while the hidden Tilemap remains the sole gameplay collision authority. The catwalk is the first canonical example: the sprite is visible, the gray structural tiles underneath are absent, and only the hidden collision strip remains.

This means **visual geometry and gameplay collision are intentionally allowed to diverge**. Visual wear, holes, brackets, diagonals, railing, rust, transparent negative space, pipes, foreground pieces, and machinery silhouettes do not need to generate matching physical shapes. Gameplay collision should remain simple, continuous, readable, and traversal-safe.

`Ground Collision - Hidden` keeps the release-hardened contract from `RELEASE_COLLISION.md`:

- Ground layer 6;
- renderer disabled;
- static `Rigidbody2D`;
- `TilemapCollider2D` with `Merge`;
- polygon `CompositeCollider2D`;
- `TilemapCompositeColliderInitializer2D`;
- immediate geometry generation/validation before the scene is accepted.

## Visual readability hierarchy

The Canonical 28 restriction is also a gameplay-readability tool, not only an art limitation.

Production environment art should follow a value/contrast hierarchy:

1. **Player / combat information** gets the strongest local readability and should remain the fastest silhouette to parse.
2. **Gameplay structure** — surfaces the player can stand on, collide with, brace against, climb, or otherwise use — should generally sit one restrained value/contrast step above background dressing.
3. **Background machinery / dressing** should favor the darker palette families and lower local contrast so it contributes atmosphere and mechanical plausibility without competing with traversal geometry.
4. **Foreground dressing** may be stronger locally when useful for depth, but must not obscure gameplay-critical information for sustained periods.

This does **not** mean walkable surfaces should become bright or saturated. Rustline remains dark industrial pixel art. The intent is relative separation inside the existing Canonical 28: gameplay structure should read a little more explicitly than non-interactive machinery behind it.

For current environment families, that implies:

- background gantries / housing / handler: predominantly Deep Space, Deep Navy, Shadow, Steel Shadow, Dark Metal, with restrained rust;
- structural catwalk / floor / wall edges: more frequent Steel / readable edge values and slightly clearer silhouettes;
- bright Light Metal, strong warning colors, and high-value accents remain scarce and purposeful.

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

The current **12 u** gameplay catwalk is visually dressed with `catwalk_left + catwalk_right`. Together those source sprites measure **200 px / 12.5 u**, so centering them over the authoritative 12 u collision creates only a restrained **4 source pixel** visual overhang at each outer end. The modules render at sorting order `2`: above generic structural visual tiles but below the player renderers. There are intentionally no generic gray tiles behind this production catwalk skin.

`catwalk_mid_short` (**100×128 px**) and `catwalk_mid_long` (**200×128 px**) are accepted reusable modules but are not required by this first 12 u assembly. They remain available for longer future spans rather than being forced into the current room.

`gantry_overhead_rail.png` is accepted as Canonical-28 production art and retains native scale, but it is **not yet placed** in Salvage Intake. Its source canvas is **870×160 px = 54.375×10 u** at 16 PPU, almost the room's entire width. Placing it now would make the asset dictate the room architecture. Prefer extracting/re-authoring a modular rail family or deliberately designing a later room-wide crane span before using this full-size source asset.

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

The central visual idea is a **salvage transfer / sorting machine** whose production art occupies much more visual space than its simple collision plinth. The first macro kit provides two tall gantry uprights, suspended salvage handler, central machinery housing, west bulkhead, and the first production catwalk skin.

The intended visual hierarchy is:

1. readable player and combat silhouettes;
2. clearly readable gameplay structural edges;
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
- structural support / bracket pieces where the machinery composition visibly asks for them;
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
- `catwalk_left + catwalk_right` are the sole visible catwalk surface — no gray structural tiles remain behind them;
- the hidden `12×1` catwalk collision remains stable and correctly aligned to the sprite skin;
- the former east catwalk support wall is absent, leaving the underside/right side open;
- the catwalk remains visually behind the player while reading more clearly than non-interactive background machinery;
- the raised handler reads as suspended from the overhead service deck rather than floating;
- the taller overhead deck/side-wall envelope gives the hero machinery enough visual breathing room;
- player and environment assets retain coherent relative scale at native presentation;
- the west entry / lower bay / catwalk relationship feels like architecture rather than a movement tutorial;
- upper and lower routes feel meaningfully distinct even before enemies are added;
- the west-to-east route is readable under the existing penumbra;
- no new collision seam, phantom Land, or Release-only floor regression appears;
- the player/Longwatch presentation remains unchanged.
