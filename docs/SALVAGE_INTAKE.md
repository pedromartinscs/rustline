# Salvage Intake — First Production Area

`SalvageIntake` is Rustline's first production-area scene and the first step away from the diagnostic `MovementLab` toward the itch.io vertical slice.

The current goal remains deliberately narrow: build one coherent industrial route that is enjoyable to traverse and aim through with the frozen player/Longwatch foundation before expanding into a broader environment library.

## Scene and production tooling

Target scene:

`Assets/Scenes/Demo/SalvageIntake.unity`

Base geometry builder:

`Assets/Editor/RustlineSalvageIntakeSetup.cs`

Specialized production passes remain implemented as separate deterministic editor classes, but they are no longer exposed individually in `Tools/Rustline`.

The normal authoring entry point is intentionally a single command:

**Tools → Rustline → Apply Production Setup**

`RustlineProductionTools` orchestrates the current production chain in order:

1. rebuild base Salvage Intake geometry;
2. enforce industrial-surface variation;
3. connect and raise the handler-overhead support into the left shaft wall;
4. apply route refinements: raise the shaft exit and add west-entry visual support columns;
5. apply macro environment dressing;
6. apply lower-bay floor dressing;
7. apply west wall dressing;
8. apply service-shaft wall dressing;
9. apply horizontal far parallax;
10. apply the vertical-depth backdrop;
11. enforce and validate canonical release build-scene order.

The specialized setup classes remain callable internally and from command-line workflows. The collapsed menu is a UI cleanup, not a removal of deterministic tooling.

The base builder owns and regenerates only:

- `Environment - Managed Graybox`
- `Player Rig - Managed`

Preserved authoring roots:

- `Art Dressing - Preserve`
- `Gameplay Content - Preserve`

Production setup classes may own named managed children under `Art Dressing - Preserve`; unrelated hand-authored content must not be placed inside those managed children.

While Salvage Intake is still being composed, accepted geometry refinements that have not yet been folded back into the large base builder are applied immediately after rebuild by deterministic production passes. Running **Apply Production Setup** is therefore the current source-of-truth authoring workflow.

## Current spatial contract

Gameplay geometry uses the normal **16×16 px / 1×1 unit** structural grid. The accepted **24 source pixel / 1.5 u Minimum Traversable Gap** remains the hard authoring floor.

Current production route:

- west production bulkhead and entry deck;
- two visual-only background support columns beneath the starting area at `x=-23..-22` and `x=-19..-18`, descending through `y=-12..-5` so the opening platform does not read as suspended;
- small service step and solid left approach into the hero room;
- compact **4×2 tile** central collision plinth for the salvage machine;
- **12×1 tile** transfer catwalk represented physically by hidden collision and visually by production catwalk sprites;
- no east catwalk support wall;
- east staging platform leading into the permanent service shaft;
- **4 u / 64 px**-wide service shaft using the accepted MovementLab wall-kick spacing;
- lower shaft entry with **4 u / 64 px** of clear standing height;
- left shaft wall at `x=26..27`, `y=4..18`;
- open shaft interior at `x=28..31`;
- raised right shaft wall at `x=32..33`, `y=0..16`;
- raised upper continuation deck at `x=34..45`, `y=16`, walkable surface `y=17`;
- `Exit Staging` at `(42, 17.08, 0)`;
- handler-overhead support main row at `x=-8..27`, `y=20`, upper surface `y=21`;
- structural joint at `x=26..27`, `y=19`, tying the overhead support directly into the left shaft wall while preserving `x=28..31` as the playable opening;
- player spawn centered on the west production bulkhead at `x=-20`, serialized at `y=1.75`.

The raised shaft exit reduces the vertical difference to the handler support from the former 6 u to **4 u**, while retaining a clear visual hierarchy and the accepted 64 px shaft width.

See [`SALVAGE_INTAKE_SERVICE_SHAFT.md`](SALVAGE_INTAKE_SERVICE_SHAFT.md) for the traversal contract.

## Traversal intent

The critical route should read as industrial architecture first, not as a sequence of movement tutorials.

The east shaft turns Wall Brace / Wall Kick into required spatial progression without reducing geometry to the 24 px technical minimum. Its four-cell width matches the already human-tested MovementLab shaft spacing. Continuous wall coverage supports the accepted seven-probe Wall Brace presentation rule.

The player crosses Salvage Intake at floor level, enters the shaft through the lower opening, climbs between the walls, reaches the raised right-wall top/ledge at `y=17`, then exits onto the upper deck toward the next production section.

Because the exit height changed while the shaft width and movement tuning stayed frozen, the raised landing requires a quick human traversal re-test before it is considered fully accepted.

## Visual geometry versus gameplay collision

Three structural Tilemaps remain authoritative for their respective roles:

1. `Background Structure - Visual` — presentation-only distant/support structure; currently includes the two west-entry support columns;
2. `Industrial Surface - Visual` — generic visible structure where production skins have not replaced it;
3. `Ground Collision - Hidden` — authoritative gameplay collision.

The west-entry columns exist only on `Background Structure - Visual`. They do **not** create collision or alter the safety-floor gameplay envelope.

Production structural skins are non-colliding `SpriteRenderer`s. The visual/collision rule remains explicit:

`visual geometry != gameplay collision`

Visual wear, holes, brackets, diagonals, rust, pipes and transparent negative space do not need matching physical shapes. Hidden collision should remain simple, continuous, readable and traversal-safe.

`Ground Collision - Hidden` keeps the release-hardened contract:

- Ground layer 6;
- renderer disabled;
- static `Rigidbody2D`;
- `TilemapCollider2D` using `Merge`;
- polygon `CompositeCollider2D`;
- `TilemapCompositeColliderInitializer2D`;
- deterministic geometry generation and validation after route refinement.

## Current production art

Accepted families currently include:

- gantry uprights;
- suspended salvage handler;
- machine housing;
- small west bulkhead;
- modular catwalk family;
- floor-edge family;
- west wall-edge family;
- service-shaft wall A/B family;
- horizontal far-industrial parallax;
- vertical-depth backdrop;
- accepted but currently unplaced full-size `gantry_overhead_rail.png`.

The west entry uses the **105×100 px** bulkhead at native scale. The current catwalk uses `catwalk_left + catwalk_right` over a separate hidden `12×1` collision strip. Gray structural tiles are intentionally absent behind that production catwalk skin.

The floor-edge family uses 48×32 px modules. `floor_edge_mid_a` is the common center module; `floor_edge_mid_b` carries stronger rust and remains less frequent.

The west wall-edge family dresses only the west outer boundary. The old mirrored east boundary skin must not return because the east side is the approved service-shaft route.

The service shaft currently uses:

- `Assets/Art/Environment/Architecture/ServiceShaft/shaft_wall_a.png`
- `Assets/Art/Environment/Architecture/ServiceShaft/shaft_wall_b.png`

These sprites align their straight interior faces to the hidden collision planes while growing outward from the 64 px playable opening. They contain no colliders. The raised right-wall rows at `y=15..16` currently remain generic structural cap geometry above the 240 px right-wall sprite; a dedicated upper cap/transition may replace that visual later without altering collision.

## Parallax / depth contract

Horizontal production asset:

`Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png`

- **2160×1080 px**;
- 16 PPU;
- sorting order `-30`;
- three adjacent seamless copies at exact **135 u** intervals;
- World Camera X follow `0.94`;
- camera Y screen-lock;
- final `1/16 u` snap.

Vertical production asset:

`Assets/Art/Environment/Parallax/VerticalDepth/vertical_depth_backdrop_a.png`

- **1080×4320 px**;
- 16 PPU;
- Point filtering, no mipmaps, uncompressed;
- importer `maxTextureSize >= 8192` so Unity cannot downscale the 4320 px source;
- sorting order `-40`;
- non-repeating;
- X screen-locked to the World Camera;
- vertical reveal driven by base camera altitude rather than raw player jumps or recoil;
- current Salvage Intake altitude span `0..21 u`;
- canonical minimum virtual phase span **4320 source px / 270 u**.

The 1080×4320 backdrop is procedurally authored using exact Canonical 28 colors and irregular multi-scale masses rather than the previous uniform ordered-dot field. It intentionally provides much more visible Deep Navy / Shadow / Steel Shadow structure while keeping warmer corrosion concentrated toward the surface direction.

See [`PARALLAX_BACKDROPS.md`](PARALLAX_BACKDROPS.md) for the exact motion and art contract.

## Visual readability hierarchy

Canonical 28 remains a gameplay-readability tool, not only a palette restriction.

1. player / combat information;
2. gameplay structure;
3. hero transfer machinery;
4. normal background machinery;
5. horizontal far parallax;
6. vertical-depth field;
7. restrained foreground dressing where useful.

The vertical depth layer should communicate atmosphere and altitude, not become a high-frequency texture competing with the foreground.

## Native-pixel invariants

- 16 PPU production art;
- Transform scale `1,1,1` for pixel-authored environment sprites;
- Canonical 28 only;
- `Sprite-Unlit-Default` for current environment rendering;
- native-pixel logical rendering;
- integer presentation scaling;
- no fractional Transform scaling as an asset-sizing technique;
- horizontal parallax wraps by whole source-tile intervals;
- vertical-depth final placement snaps to `1/16 u`;
- no gameplay colliders on dressing/parallax objects;
- palette-constrained penumbra;
- no decorative `Light2D` identity layer in the current pass;
- existing runtime performance policy unchanged.

If a production environment asset is the wrong apparent size, re-author/resample the source rather than scaling its Transform fractionally.

## Hero-room composition

The central visual idea remains a **salvage transfer / sorting machine** whose art occupies much more visual space than its simple collision plinth.

Current hierarchy:

1. readable player/combat silhouette;
2. clearly readable gameplay structural edges;
3. hero transfer machine;
4. normal background machinery/structure;
5. horizontal distant industrial silhouette;
6. vertical altitude/depth field.

The handler-overhead support now reaches the left shaft-wall mass and is connected through the `x=26..27, y=19` structural joint. A dedicated production-art skin for that joint remains optional; the gameplay opening at `x=28..31` must never be narrowed to make the connection prettier.

The west starting platform now receives two deep background columns so the first screen reads as part of a larger industrial installation instead of a platform suspended in empty space.

## Immediate acceptance gate

When rebuilding or extending Salvage Intake, verify:

- the complete route is traversable without getting stuck;
- no unintended opening below the accepted authoring envelope invites entry;
- player spawn remains clear before gravity settles it;
- all managed production sprites remain collider-free;
- west-entry support columns remain visual-only and visually continue below the initial camera frame;
- catwalk visual skin and hidden collision remain aligned;
- shaft lower entrance stays 4 u high;
- shaft interior stays exactly 4 u / 64 px wide;
- Wall Brace and alternating Wall Kicks remain comfortable;
- the raised `y=17` shaft exit is reachable consistently without precision input;
- shaft wall art never visually intrudes into the playable opening in a misleading way;
- the raised right-wall top leads naturally into the continuation deck;
- the old full-height east boundary never returns;
- handler support remains at `y=20`, extends through `x=27`, joins the left shaft wall at `x=26..27, y=19`, and leaves `x=28..31` untouched;
- far parallax remains quieter than normal background machinery and exposes no seam;
- Vertical Depth stays behind the horizontal far layer and does not expose its source edges;
- climbing reveals the Vertical Depth gradually from camera altitude rather than raw jumps;
- the 4320 px vertical source is imported without 4096 px downscaling;
- no shimmer, subpixel crawl, wrap pop or Release-only collision regression appears;
- player and Longwatch presentation remain unchanged.
