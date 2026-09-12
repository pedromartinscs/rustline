# Salvage Intake — First Production Area

`SalvageIntake` is Rustline's first production-area scene and the first step away from the diagnostic `MovementLab` toward the itch.io vertical slice.

The current goal is deliberately narrow: build one coherent industrial route that is enjoyable to traverse and aim through with the frozen player/Longwatch foundation before expanding into a broad environment asset library.

## Scene and builders

Target scene:

`Assets/Scenes/Demo/SalvageIntake.unity`

Deterministic layout builder:

`Assets/Editor/RustlineSalvageIntakeSetup.cs`

Macro art-dressing setup:

`Assets/Editor/RustlineSalvageIntakeArtSetup.cs`

Far-parallax setup:

`Assets/Editor/RustlineSalvageIntakeParallaxSetup.cs`

Primary Unity menus:

- **Tools → Rustline → Rebuild Salvage Intake Graybox**
- **Tools → Rustline → Apply Salvage Intake Macro Dressing**
- **Tools → Rustline → Apply Salvage Intake Floor Dressing**
- **Tools → Rustline → Apply Salvage Intake Wall Dressing**
- **Tools → Rustline → Apply Salvage Intake Far Parallax**

The layout builder first runs the existing M1 validation, then recreates the production-area collision and player rig without modifying `MovementLab`.

The builder owns and regenerates only these roots:

- `Environment - Managed Graybox`
- `Player Rig - Managed`

These roots are preserved across graybox rebuilds and are used for hand-authored work that should survive regeneration:

- `Art Dressing - Preserve`
- `Gameplay Content - Preserve`

The macro-dressing tool owns only `Art Dressing - Preserve/Macro Environment Kit v0 - Managed`. The far-parallax tool owns only `Art Dressing - Preserve/Far Parallax v0 - Managed`. Do not place unrelated permanent hand-authored dressing inside either managed child.

## Current spatial contract

All gameplay geometry is aligned to the normal **16×16 px / 1×1 unit** structural grid. The accepted **24 source pixel / 1.5 unit Minimum Traversable Gap** remains the hard authoring floor, while ordinary production openings should normally be more generous when movement readability benefits.

The current route contains:

- west production bulkhead and entry deck;
- small service step and solid left approach into the hero room;
- compact **4×2 tile** central collision plinth for the salvage machine;
- **12×1 tile** transfer catwalk represented physically only by hidden collision and visually by production catwalk sprites;
- no east catwalk support wall — the space below/right of the catwalk remains open;
- east staging platform leading into the permanent service shaft;
- **4 u / 64 px**-wide vertical service shaft using the human-tested MovementLab wall-kick spacing;
- lower shaft entry with **4 u / 64 px** of clear standing height;
- left shaft wall at `x=26..27`, `y=4..18`;
- open shaft interior at `x=28..31`;
- right shaft wall at `x=32..33`, `y=0..14`;
- upper continuation deck at `x=34..45`, `y=14`, with walkable surface at `y=15`;
- `Exit Staging` at `(42, 15.08, 0)` on that upper-right deck;
- **16×1 tile** overhead service deck at `y=18`, used as the structural mounting surface above the salvage handler;
- player spawn centered on the west production bulkhead at `x=-20`, serialized at `y=1.75` for deliberate initial ground clearance.

The service shaft is no longer an experimental overlay. It is part of `RustlineSalvageIntakeSetup` and is reproduced by every normal Salvage Intake rebuild. See [`SALVAGE_INTAKE_SERVICE_SHAFT.md`](SALVAGE_INTAKE_SERVICE_SHAFT.md) for the exact traversal contract.

Historical graybox labels such as `v2.1` through `v2.6` were only internal iteration markers while the hero room was changing rapidly; they are not game or content-version numbers. The current documentation uses the canonical spatial contract directly instead of assigning a new arbitrary graybox version to each accepted change.

## Traversal intent

The critical route should read as industrial architecture first, not as a sequence of movement tutorials.

The east shaft deliberately turns Wall Brace / Wall Kick into required spatial progression without reducing the geometry to the technical 24 px minimum. Its four-cell width matches the already human-tested MovementLab shaft spacing. Continuous wall coverage supports the accepted seven-probe Wall Brace presentation rule.

The player crosses Salvage Intake at floor level, enters the shaft through the lower opening, climbs between the walls, reaches the right-wall top/ledge at `y=15`, then exits onto the upper deck toward the next section of the level.

Jump, LedgeClimb, Wall Brace/Kick, crouch, Fall aim, and future combat positioning should continue to emerge from useful architecture rather than isolated ability stations.

## Visual geometry versus gameplay collision

The scene retains three structural Tilemaps:

1. `Background Structure - Visual` — currently intentionally empty and reserved for future tile-based background needs;
2. `Industrial Surface - Visual` — visible generic structural surfaces where production structural skins have not replaced them;
3. `Ground Collision - Hidden` — authoritative gameplay geometry.

Production background identity comes primarily from non-colliding sprites under `Art Dressing - Preserve`.

Production structural skins such as catwalk, floor edges, and wall edges are also non-colliding `SpriteRenderer`s. They may visually represent walkable or braceable structure while the hidden Tilemap remains the sole gameplay collision authority.

`visual geometry != gameplay collision` is an explicit production rule. Visual wear, holes, brackets, diagonals, railing, rust, transparent negative space, pipes, foreground pieces, and machinery silhouettes do not need to generate matching physical shapes. Gameplay collision should remain simple, continuous, readable, and traversal-safe.

`Ground Collision - Hidden` keeps the release-hardened contract from `RELEASE_COLLISION.md`:

- Ground layer 6;
- renderer disabled;
- static `Rigidbody2D`;
- `TilemapCollider2D` with `Merge`;
- polygon `CompositeCollider2D`;
- `TilemapCompositeColliderInitializer2D`;
- immediate geometry generation/validation before the scene is accepted.

## Visual readability hierarchy

Canonical 28 is a gameplay-readability tool, not only an art restriction.

1. **Player / combat information** gets the strongest local readability.
2. **Gameplay structure** — floors, walls, catwalks, ledges and other usable surfaces — sits one restrained value/contrast step above background dressing.
3. **Background machinery / dressing** favors darker palette families and lower local contrast.
4. **Far parallax** is quieter still: large distant silhouettes with minimal internal information.
5. **Foreground dressing** may be stronger locally when useful for depth, but must not obscure gameplay-critical information for sustained periods.

This does not mean walkable surfaces should become bright or saturated. Rustline remains dark industrial pixel art; the requirement is relative separation inside the Canonical 28.

## Native-pixel environment scale

Environment sprites follow the same source-pixel contract as the player:

- production PNGs import at **16 PPU**;
- environment SpriteRenderers remain at **Transform scale 1.0**;
- at base presentation scale, one source-art pixel maps to one display pixel;
- integer presentation scaling scales the complete logical frame together, preserving player/environment proportions;
- fractional Transform scaling is not a production technique for fixing an asset's apparent size.

If a production environment asset is the wrong apparent size, re-author/resample the source PNG rather than arbitrarily scaling it in Unity.

## Current environment production assets

Accepted macro/architecture families include:

- gantry uprights;
- suspended salvage handler;
- machine housing;
- source-authored small west bulkhead;
- modular catwalk family;
- floor-edge family;
- wall-edge family;
- far-industrial parallax silhouette family;
- accepted but currently unplaced full-size `gantry_overhead_rail.png`.

The west entry uses the **105×100 px** small bulkhead at native scale. The current catwalk uses `catwalk_left + catwalk_right` over a separate hidden `12×1` collision strip. Gray structural tiles are intentionally absent behind that production catwalk skin.

The accepted floor-edge family uses 48×32 px modules. `floor_edge_mid_a` is the common/default center module; `floor_edge_mid_b` carries stronger rust and should remain less frequent.

The wall-edge family currently dresses the **west outer boundary only**. The former mirrored east outer-wall skin was retired when that boundary became the service-shaft entrance. Shaft-specific skins may be authored later, but the generic old east wall must never visually seal the approved route.

The first far-parallax asset is `Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png`, authored at **1296×410 px** and native **16 PPU**. It intentionally uses a sparse, extremely dark industrial silhouette language rather than the lighter blue/steel vocabulary of ordinary background machinery. In Salvage Intake it renders at sorting order `-30`, follows the World Camera at `0.94x / 0.96y`, and snaps its final transform to `1/16 u` through `PixelSnappedParallax2D`.

`gantry_overhead_rail.png` remains valid production art, but its 870×160 px source canvas is effectively room-wide at native scale. Do not force it into Salvage Intake; modularize or deliberately design a later crane span before using it.

## Presentation / performance invariants

Salvage Intake reuses the accepted player prefab and presentation architecture proven in MovementLab:

- 16 PPU;
- Canonical 28 production art contract;
- `Sprite-Unlit-Default` for current environment rendering;
- native-pixel logical rendering;
- integer presentation scaling;
- far parallax motion quantized to the same `1/16 u` source-pixel grid;
- palette-constrained penumbra;
- no identity/decorative `Light2D` objects in the current pass;
- existing 60 FPS runtime policy unchanged.

The builder preserves the accepted Longwatch camera-impulse integration without changing player, movement, weapon, muzzle, recoil, carry, Wall Brace, LedgeClimb, or animation behavior. Far parallax resolves the active `MainCamera` dynamically, so preserved art does not retain a stale reference when the managed player/camera rig is rebuilt.

## Hero-room composition

The central visual idea remains a **salvage transfer / sorting machine** whose production art occupies much more visual space than its simple collision plinth.

The intended hierarchy is:

1. readable player and combat silhouettes;
2. clearly readable gameplay structural edges;
3. hero transfer machine;
4. normal background machinery/structure;
5. far industrial silhouettes;
6. pipes, conduits, vents, warning markings, corrosion, and restrained foreground dressing.

Do not solve the room with many overlapping lights, full-screen effects, or giant transparent illustrations. Prefer composition, modular pieces, bounded sprites/overlays, and the existing penumbra presentation.

## Immediate acceptance gate

When rebuilding or extending Salvage Intake, verify:

- the room is traversable without getting stuck;
- no unintended physical opening below the accepted authoring envelope invites entry;
- player spawn remains clear of the west deck before gravity settles it;
- all managed environment sprites remain collider-free;
- catwalk visual skin and hidden collision remain aligned;
- the service-shaft lower entrance stays 4 u high;
- the service-shaft interior stays exactly 4 u / 64 px wide;
- Wall Brace engages naturally on the shaft walls;
- alternating Wall Kicks remain comfortable rather than precision-gated;
- the right-wall top leads naturally into the upper continuation deck;
- the old full-height east boundary wall skin never returns;
- far parallax remains visually quieter than normal background machinery and never reads as reachable gameplay space;
- far parallax moves subtly with the camera without shimmer, subpixel crawl, or abrupt re-anchoring;
- the route remains readable under native-pixel presentation and penumbra;
- no new collision seam, phantom Land, or Release-only floor regression appears;
- player and Longwatch presentation remain unchanged.
