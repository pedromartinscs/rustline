# Rustline Art Direction

This document is the working visual specification for Rustline. It is intentionally small and should evolve only when a change improves consistency or gameplay readability.

## Visual identity

**Theme:** derelict orbital salvage refinery / industrial extraction site.

The environment should feel functional, worn, and dangerous rather than sleek or pristine. Machinery should read as serviceable industrial equipment that has survived years of neglect, patchwork repair, heat, and corrosion.

### Core motifs

- Dark steel and layered industrial plating
- Oxidized / heat-stained metal
- Exposed pipes, conduits, vents, and structural beams
- Warning markings and maintenance labels
- Cold electronic displays and status lights
- Strong silhouettes and readable interactables
- Selective bright accents for gameplay-relevant objects

## Pixel-art constraints

### Global production rules

- Nearest-neighbor / point filtering only
- No anti-aliasing in final pixel art
- Final transparency is binary: alpha must be either `0` or `255`
- Final production pixels must use Rustline Canonical 28 plus transparency only
- Judge assets at native gameplay scale, not only while zoomed in

### Unity import contract

Production PNGs under `Assets/Art/` (excluding editable/reference files under `Assets/Art/Source/`) use these Unity settings:

- Sprite texture type at **16 Pixels Per Unit**
- Point filtering, clamp wrapping, mipmaps off
- Texture compression and crunch compression off
- Full Rect sprite meshes; transparent pixels remain part of fixed cells
- Source alpha imported as transparency
- No importer rescaling of the production source pixels
- No generated fallback physics shape; visual adjacency and collision remain separate concerns

The baseline is enforced by `Assets/Editor/RustlinePixelArtPostprocessor.cs`. Known fixed sheets and the M0 showcase are rebuilt with **Tools → Rustline → Rebuild M0 Art Showcase**.

Body and Unarmed Arms player sheets use complete **48×64** cells with a normalized bottom-center pivot of `(0.5, 0.0)`. Armed overlays may use a larger fixed cell when the weapon silhouette needs authored space outside the Body cell, provided the armed pivot represents the exact same body reference point. The first approved armed geometry is the Longwatch DMR **80×96** cell with Body reference rectangle `x=0, y=8, w=48, h=64` and armed pivot `(24,8)` px / normalized `(0.30, 0.083333333...)`. The complete contract is documented in [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md).

The industrial structural atlas is always sliced into all **48** fixed **16×16** cells. Its documented logical rows run top-to-bottom, so the editor setup deliberately converts them to Unity's bottom-origin sprite coordinates without changing slot names or semantics.

### Environment

- Base tile grid: **16×16 px**
- Modular construction preferred over large baked room illustrations
- Avoid noisy single-pixel detail that disappears during gameplay
- Primary structural atlas: **128×96 px**, 8×6 cells, 48 fixed 16×16 slots
- Slots `00–15` implement the canonical cardinal N/E/S/W connectivity cases
- Structural adjacency controls sprite selection; collision remains a separate Tilemap/collider concern
- Connected edges must remain visually compatible across canonical tiles and their visual variants

The complete structural atlas contract and slot mapping are documented in [`TILESET_SPEC.md`](TILESET_SPEC.md).

### Player

- Side-view humanoid
- Canonical Body / Unarmed Arms production cell: **48×64 px**
- Target visible character height: approximately **42–48 px**
- Player presentation is decomposed into aligned Body and Arms/Weapon layers
- Unarmed Body + Unarmed Arms must produce the accepted coherent layered appearance; deliberate decomposition retouching means historical composites are diagnostic references, not pixel-equality targets
- Armed aiming uses authored fixed-cell directional overlays rather than free runtime rotation of the final pixel art
- The first Longwatch DMR armed aim overlay uses **80×96 px** cells around the unchanged 48×64 Body reference, with 32 px additional forward space, 24 px above, and 8 px below
- Body animation must remain readable when horizontally flipped
- The three-frame Jump takeoff uses explicit keys at 0.00, 0.10, and 0.26 seconds: 100 ms of Y-anchored compression, 160 ms of cubic eased catch-up, then a held layered ascent pose; X and the camera continue following the physical root, and Fall remains velocity-selected
- Grounded Run and Backpedal are selected from actual velocity relative to aim-facing; Backpedal uses exactly four authored frames and a 5 units/s grounded cap versus 7 units/s forward
- `AimOrigin` is an explicit Visual child 38 source pixels above the renderer pivot; generic aim-facing uses a 5° hysteresis zone around vertical
- Grounded takeoff dust is a separate three-frame, 48×64 world-space one-shot at the player pivot; it snapshots facing, stays on the floor, and is omitted for coyote jumps
- Initial canonical pose: neutral, unarmed, facing right

The complete Body/Arms decomposition, armed-cell geometry, directional-aim sprite, naming, folder, mirroring, and locomotion-state contract is documented in [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md).

### Enemies

Prefer designs that produce strong gameplay silhouettes and modest animation requirements:

- Small ground crawler / melee unit
- Flying drone
- Turret or heavy stationary/slow unit

## Native-pixel viewport and penumbra

Rustline should treat display resolution as part of its visual language rather than freely scaling the world to fill every screen.

The target presentation model is:

- At the base presentation scale, **one production-art pixel maps to one display pixel**. A 64 px player cell therefore remains 64 physical pixels tall.
- The canonical maximum logical viewport at **1×** is **1072×1072 px**, exactly **67×67** environment tiles at 16 px per tile.
- Displays smaller than that canonical viewport should show a smaller crop of the same native-pixel world rather than shrinking the art below 1×.
- Displays larger than the canonical viewport should not reveal additional gameplay world merely because more physical pixels are available. Unused space is filled with the canonical darkness color.
- When a display can contain an exact integer multiple of the canonical viewport, the presentation may upscale by **2×, 3×, 4×, ...** using nearest-neighbor integer scaling only.
- Fractional world scaling and smoothing are not part of the target presentation model.

A player-centered atmospheric mask defines the readable visual region. The central area remains fully visible; outward from it, the scene enters a pixel-art penumbra and eventually reaches complete canonical darkness.

### Canonical prototype geometry

The first implementation should use a **perfect circular** visibility mask centered on the player, inside the square 1072×1072 viewport.

- Fully visible circle: **57 tiles in diameter = 912 px**, radius **28.5 tiles = 456 px**.
- Penumbra thickness: **4 tiles = 64 px radially**.
- Outer edge of the penumbra / start of full darkness: **65 tiles in diameter = 1040 px**, radius **32.5 tiles = 520 px**.
- Absolute viewport radius: **33.5 tiles = 536 px**.
- Therefore at least **1 complete tile = 16 px** of solid canonical darkness remains between the outer visibility circle and each cardinal edge of the logical viewport.
- The corners of the square viewport naturally contain substantially more full darkness because the mask is circular.
- Outside the canonical viewport, any unused physical display area is also filled with the same canonical darkness color.

These dimensions are the approved starting specification for the camera/penumbra prototype. They may be tuned later from playtesting, but the native-pixel viewport model, circular-mask concept, and integer-scaling rules should remain stable unless deliberately revisited.

### Palette-constrained darkness

The penumbra must preserve the fixed Rustline palette. It must **not** generate arbitrary interpolated RGB shades.

Darkening should happen by remapping visible colors through existing canonical colors of progressively lower value. For example, a bright warm/red pixel may step through darker canonical warm tones before reaching the darkest canonical color. The exact ramp used depends on the source material and should preserve hue/material identity where practical.

The final darkness color is **Deep Space `#01020B`**, not an additional `#000000` color.

Controlled pixel-pattern dithering is explicitly allowed for this atmospheric transition, provided that:

- every resulting pixel is still one of Rustline Canonical 28;
- no alpha gradients, anti-aliasing, blur, bilinear filtering, or synthesized intermediate colors are introduced;
- dithering is deliberate, sparse, and readable at native scale rather than noisy texture;
- the effect transitions from fully readable color, through palette-constrained shadow/remapping and pixel-pattern penumbra, into solid Deep Space;
- production sprites and tiles themselves remain unchanged; the penumbra is a presentation/lighting effect layered over the rendered world.

The penumbra is intended to become a gameplay-readable boundary as well as an aesthetic device. Future rendering, simulation, and spatial-audio ranges may use related but independently tunable distances; they should not be hard-coupled merely because the initial prototype centers all three around the player.

## Weapon presentation

Rustline separates **continuous gameplay aim** from **authored visual aim**.

The gameplay direction may be mathematically continuous for mouse/gamepad targeting, projectiles, and hitscan. The player artwork uses discrete authored arm/weapon overlays at 10-degree intervals. For right-facing artwork, `0°` is horizontal, positive angles aim upward, and negative angles aim downward.

The canonical right-facing aim-capable set contains **19 directions** from `+90°` through `0°` to `-90°`; horizontal mirroring covers the opposite hemisphere. Idle, Run, Backpedal, and Fall are aim/fire-capable presentation states. Jump, Land, and Roll/Dodge keep the equipped weapon visible through authored carry poses but do not use the 19-angle firing set and do not permit firing.

The **Longwatch DMR** is the first pipeline-validation weapon. Its right-facing Idle, Run, and Backpedal sets are authored and integrated for all 19 directions: Idle uses two `80×96` cells per `160×96` sheet, Run uses six per `480×96`, and Backpedal uses exactly four per `320×96`. All use the common armed pivot and explicit AimOrigin 38 source pixels above the renderer pivot. Mouse-driven 360° mirroring, generic aim-facing, and Body-clock synchronization are automated. Run and the corrected origin are human-approved; Backpedal approval remains pending.

This is an intentional artistic/gameplay choice. It avoids free-angle rotation artifacts in small pixel art and gives each weapon authored hand placement and silhouette control.

See [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md) for the complete production contract and [`WEAPONS.md`](WEAPONS.md) for the versioned initial 20-weapon roster.

## Canonical palette

Rustline uses a **fixed 28-color production palette** documented in [`PALETTE.md`](PALETTE.md) and provided as a GIMP palette at [`Assets/Art/Palette/rustline.gpl`](../Assets/Art/Palette/rustline.gpl).

**Hard rule:** final production pixel art may use only those 28 colors plus full transparency where applicable.

Generated candidates should always be prompted with the canonical palette, but prompt compliance alone is not considered sufficient. Exact hexadecimal compliance must be verified before an image becomes a production asset; remap/quantize colors when necessary.

Do not introduce near-duplicate shades, anti-aliased edge colors, or one-off colors outside the canonical palette.

## Asset-generation workflow

1. Generate an original Rustline canonical asset from the written brief.
2. Include the fixed Rustline 28-color palette in every production-oriented generation request.
3. Select and clean the candidate manually where necessary.
4. Validate/remap the candidate so all opaque pixels use only canonical palette colors.
5. Remove partial-alpha edge pixels before approving production assets.
6. Treat the approved Rustline asset as the visual reference for subsequent Rustline generations.
7. Keep silhouettes, proportions, light direction, pixel density, and palette consistent.
8. Use GIMP/manual pixel editing for alignment, frame cleanup, palette correction, and sprite-sheet packing.
9. Commit only assets whose redistribution rights are clear.

For highly constrained modular assets such as structural tiles, generative output is primarily a visual/reference source. Final 16×16 geometry should be reconstructed or cleaned deterministically when needed so connectivity is not dependent on generative precision.

### Reference rule

Third-party packs may be studied for broad design principles, genre conventions, modularity, and readability, but Rustline assets should not be produced by modifying or redistributing those third-party source files.

Once an original Rustline visual baseline exists, prefer Rustline's own artwork as the reference source for future generations.

## Initial asset checklist

### Player

- Canonical neutral side-view
- Layered Body / Unarmed Arms decomposition
- Idle
- Run
- Jump
- Fall
- Land
- Roll / dodge
- Hit
- Death

### Weapons

- Longwatch DMR standalone/reference concept
- Longwatch DMR Idle: 19 authored right-facing aim directions × 2 Idle frames
- Longwatch DMR Run: 19 authored right-facing aim directions × 6 Run frames
- Longwatch DMR Backpedal: 19 authored right-facing aim directions × exactly 4 Backpedal frames
- Unity import + 360° mirrored Idle/Run/Backpedal aim validation
- Longwatch Fall aim remains deferred
- Longwatch carry-only armed poses for non-firing movement states after the current visual gate
- Remaining initial arsenal from [`WEAPONS.md`](WEAPONS.md) after pipeline validation

### Environment

- Canonical structural Rule Tile family
- Floor/platform center + edges
- Walls
- Inside/outside corners
- Structural beams
- Thin / one-way platforms
- Pipes/conduits
- Vents
- Background panels/machinery
- Warning-stripe variants
- Damaged/corroded variants
- Parallax background layers

### Gameplay props

- Door
- Extraction terminal
- Loot container
- Scrap crate
- Medkit
- Ammo pickup
- Scrap/resource pickup

### Enemies

- Ground crawler
- Drone
- Turret/heavy unit

### FX

- Muzzle flash
- Projectile/tracer
- Impact spark
- Small explosion
- Damage feedback
- Extraction beacon/effect

## Quality bar

The art does not need to be content-rich. It does need to look like one coherent game.

A small, consistent set of excellent reusable assets is preferable to a large set with inconsistent proportions, palette, or pixel density.
