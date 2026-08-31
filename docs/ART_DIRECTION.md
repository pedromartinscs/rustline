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
- Canonical production cell: **48×64 px**
- Target visible character height: approximately **42–48 px**
- Weapon rendered as a separate object from the body
- Body animation must remain readable when horizontally flipped
- Stable weapon-hand / weapon-pivot location is more important than ornamental detail
- Initial canonical pose: neutral, unarmed, facing right

### Enemies

Prefer designs that produce strong gameplay silhouettes and modest animation requirements:

- Small ground crawler / melee unit
- Flying drone
- Turret or heavy stationary/slow unit

## Weapon presentation

Weapons are independent sprites attached to an aim pivot. This allows continuous 360° aiming without requiring directional firing animations for the player body.

Initial weapon family:

- Sidearm
- Rifle
- Shotgun

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
- Idle
- Run
- Jump
- Fall
- Land
- Roll / dodge
- Hit
- Death

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
