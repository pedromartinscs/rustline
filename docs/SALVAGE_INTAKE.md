# Salvage Intake — First Production Area

`SalvageIntake` is Rustline's first production-area scene and the first step away from the diagnostic `MovementLab` toward the itch.io vertical slice.

The current goal remains deliberately narrow: build one coherent industrial route that is enjoyable to traverse and aim through with the frozen player/Longwatch foundation before expanding into a broader environment library.

## Scene and production tooling

Target scene:

`Assets/Scenes/Demo/SalvageIntake.unity`

Canonical geometry builder:

`Assets/Editor/RustlineSalvageIntakeSetup.cs`

Specialized production passes remain implemented as separate deterministic editor classes, but they are no longer exposed individually in `Tools/Rustline`.

The normal authoring entry point is now intentionally a single command:

**Tools → Rustline → Apply Production Setup**

`RustlineProductionTools` orchestrates the current production chain in order:

1. rebuild canonical Salvage Intake geometry;
2. enforce industrial-surface variation;
3. apply macro environment dressing;
4. apply lower-bay floor dressing;
5. apply west wall dressing;
6. apply service-shaft wall dressing;
7. apply horizontal far parallax;
8. apply the vertical-depth backdrop;
9. enforce and validate canonical release build-scene order.

The specialized setup classes remain callable internally and from command-line workflows. The collapsed menu is a UI cleanup, not a removal of deterministic tooling.

The builder owns and regenerates only:

- `Environment - Managed Graybox`
- `Player Rig - Managed`

Preserved authoring roots:

- `Art Dressing - Preserve`
- `Gameplay Content - Preserve`

Production setup classes may own named managed children under `Art Dressing - Preserve`; unrelated hand-authored content must not be placed inside those managed children.

## Current spatial contract

Gameplay geometry uses the normal **16×16 px / 1×1 unit** structural grid. The accepted **24 source pixel / 1.5 u Minimum Traversable Gap** remains the hard authoring floor.

Current route:

- west production bulkhead and entry deck;
- small service step and solid left approach into the hero room;
- compact **4×2 tile** central collision plinth for the salvage machine;
- **12×1 tile** transfer catwalk represented physically by hidden collision and visually by production catwalk sprites;
- no east catwalk support wall;
- east staging platform leading into the permanent service shaft;
- **4 u / 64 px**-wide service shaft using the accepted MovementLab wall-kick spacing;
- lower shaft entry with **4 u / 64 px** of clear standing height;
- left shaft wall at `x=26..27`, `y=4..18`;
- open shaft interior at `x=28..31`;
- right shaft wall at `x=32..33`, `y=0..14`;
- upper continuation deck at `x=34..45`, `y=14`, walkable surface `y=15`;
- `Exit Staging` at `(42, 15.08, 0)`;
- handler overhead support at `x=-8..24`, cell row `y=19`, with upper surface at `y=20`;
- one full cell at `x=25` intentionally left between the overhead support and the shaft wall for a future authored transition;
- player spawn centered on the west production bulkhead at `x=-20`, serialized at `y=1.75`.

The service shaft and handler-overhead support are now part of the canonical builder and are reproduced by a normal production rebuild.

See [`SALVAGE_INTAKE_SERVICE_SHAFT.md`](SALVAGE_INTAKE_SERVICE_SHAFT.md) for the traversal contract.

## Traversal intent

The critical route should read as industrial architecture first, not as a sequence of movement tutorials.

The east shaft turns Wall Brace / Wall Kick into required spatial progression without reducing geometry to the 24 px technical minimum. Its four-cell width matches the already human-tested MovementLab shaft spacing. Continuous wall coverage supports the accepted seven-probe Wall Brace presentation rule.

The player crosses Salvage Intake at floor level, enters the shaft through the lower opening, climbs between the walls, reaches the right-wall top/ledge at `y=15`, then exits onto the upper deck toward the next production section.

## Visual geometry versus gameplay collision

Three structural Tilemaps remain authoritative:

1. `Background Structure - Visual` — currently intentionally empty;
2. `Industrial Surface - Visual` — generic visible structure where production skins have not replaced it;
3. `Ground Collision - Hidden` — authoritative gameplay collision.

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
- deterministic geometry generation and validation.

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

These sprites align their straight interior faces to the hidden collision planes while growing outward from the 64 px playable opening. They contain no colliders.

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
- current Salvage Intake altitude span `0..20 u`;
- canonical minimum virtual phase span **4320 source px / 270 u**.

The new 1080×4320 backdrop is procedurally authored using exact Canonical 28 colors and irregular multi-scale masses rather than the previous uniform ordered-dot field. It intentionally provides much more visible Deep Navy / Shadow / Steel Shadow structure while keeping warmer corrosion concentrated toward the surface direction.

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

The handler overhead support now extends east toward the service shaft but deliberately stops before it. The final authored visual connection between overhead architecture and the shaft is deferred until the surrounding composition is approved.

## Immediate acceptance gate

When rebuilding or extending Salvage Intake, verify:

- the complete route is traversable without getting stuck;
- no unintended opening below the accepted authoring envelope invites entry;
- player spawn remains clear before gravity settles it;
- all managed production sprites remain collider-free;
- catwalk visual skin and hidden collision remain aligned;
- shaft lower entrance stays 4 u high;
- shaft interior stays exactly 4 u / 64 px wide;
- Wall Brace and alternating Wall Kicks remain comfortable;
- shaft wall art never visually intrudes into the playable opening in a misleading way;
- the right-wall top leads naturally into the upper continuation deck;
- the old full-height east boundary never returns;
- the handler overhead support is at `y=19`, extends through `x=24`, and keeps `x=25` reserved;
- far parallax remains quieter than normal background machinery and exposes no seam;
- Vertical Depth stays behind the horizontal far layer and does not expose its source edges;
- climbing reveals the Vertical Depth gradually from camera altitude rather than raw jumps;
- the 4320 px vertical source is imported without 4096 px downscaling;
- no shimmer, subpixel crawl, wrap pop or Release-only collision regression appears;
- player and Longwatch presentation remain unchanged.
