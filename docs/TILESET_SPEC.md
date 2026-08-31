# Rustline Industrial Tileset Specification

This document is the canonical layout and connectivity contract for Rustline's first structural environment atlas.

## Core dimensions

- Final atlas: **128×96 px**
- Tile size: **16×16 px**
- Grid: **8 columns × 6 rows**
- Capacity: **48 slots**
- Filtering: nearest-neighbor / point only
- Production colors: Rustline Canonical 28 only
- Alpha: binary only (`0` or `255`); no partially transparent edge pixels

Planned production file:

`Assets/Art/Environment/Tiles/industrial_surface.png`

## Cardinal connectivity semantics

The first 16 slots are the canonical structural Rule Tile cases.

`N`, `E`, `S`, and `W` mean **a neighboring structural tile exists in that direction**. They do not mean that the corresponding edge is walkable.

For a canonical structural tile:

- Direction present in the rule = connected to another structural tile; no exterior trim on that edge.
- Direction absent from the rule = exposed edge; exterior trim may appear on that edge.
- The tile itself remains a solid 16×16 structural cell in all 16 canonical cases.
- Collider behavior is separate from visual adjacency. Rule Tile selection chooses art; Tilemap/collider configuration determines collision.

Examples:

- `E+S+W`: north is exposed, so this is the canonical top/floor surface case.
- `W+N+E`: south is exposed, so this is the canonical bottom/ceiling case.
- `N+E+S`: west is exposed, so this is the canonical left-facing exterior wall case.
- `S+W+N`: east is exposed, so this is the canonical right-facing exterior wall case.
- `N+E+S+W`: fully surrounded interior; no exterior trim.
- `NONE`: isolated block; all four edges exposed.

## Slot map

Slots are fixed. Do not reorder them once the atlas becomes canonical.

### Row 0 — canonical connectivity A

| Slot | Rule | Exposed edges |
|---:|---|---|
| 00 | `NONE` | N, E, S, W |
| 01 | `N` | E, S, W |
| 02 | `E` | N, S, W |
| 03 | `S` | N, E, W |
| 04 | `W` | N, E, S |
| 05 | `N+E` | S, W |
| 06 | `E+S` | N, W |
| 07 | `S+W` | N, E |

### Row 1 — canonical connectivity B

| Slot | Rule | Exposed edges | Common visual role |
|---:|---|---|---|
| 08 | `W+N` | E, S | corner |
| 09 | `N+S` | E, W | vertical strip |
| 10 | `E+W` | N, S | horizontal strip |
| 11 | `N+E+S` | W | left-facing exterior wall |
| 12 | `E+S+W` | N | top / walkable-looking floor surface |
| 13 | `S+W+N` | E | right-facing exterior wall |
| 14 | `W+N+E` | S | bottom / ceiling underside |
| 15 | `N+E+S+W` | none | interior / fully connected |

## Rows 2–5

Rows after the canonical connectivity set are complementary content. They are not required for the base Rule Tile to function.

### Row 2 — thin / one-way platforms

| Slot | Planned content |
|---:|---|
| 16 | isolated thin platform |
| 17 | thin platform left cap |
| 18 | thin platform middle A |
| 19 | thin platform middle B |
| 20 | thin platform right cap |
| 21 | damaged thin platform left |
| 22 | damaged thin platform middle |
| 23 | damaged thin platform right |

These may contain transparency below the visible platform surface and can later use one-way platform collision behavior.

### Row 3 — structural visual variants

| Slot | Planned content | Canonical geometry reference |
|---:|---|---|
| 24 | top surface variant B | slot 12 |
| 25 | top surface variant C | slot 12 |
| 26 | left wall variant B | slot 11 |
| 27 | right wall variant B | slot 13 |
| 28 | ceiling variant B | slot 14 |
| 29 | interior plate variant B | slot 15 |
| 30 | interior plate variant C | slot 15 |
| 31 | reinforced interior plate | slot 15 |

Variants may change rust, rivets, panel detail, and small wear patterns. Connected edge geometry must remain compatible with the canonical case.

### Row 4 — damaged variants

| Slot | Planned content |
|---:|---|
| 32 | top surface — light damage |
| 33 | top surface — heavy damage |
| 34 | left wall — damaged |
| 35 | right wall — damaged |
| 36 | ceiling — damaged |
| 37 | interior — rusted |
| 38 | interior — dented / cracked |
| 39 | interior — heavy corrosion |

Damage must not break required tile-to-tile connectivity unless the tile is intentionally reclassified as a special piece.

### Row 5 — specials / reserve

| Slot | Planned content |
|---:|---|
| 40 | hazard-marked structural block |
| 41 | reinforced top surface |
| 42 | reinforced wall |
| 43 | reinforced ceiling |
| 44 | structural support / column piece |
| 45 | structural junction / bracket |
| 46 | reserved |
| 47 | reserved |

Reserved slots allow future needs such as a missing corner treatment, broken ledge, slope helper, or another structural special without reordering the atlas.

## Coordinates

For slot index `i`:

- `column = i % 8`
- `row = floor(i / 8)`
- pixel `x = column × 16`
- pixel `y = row × 16`

Example: slot `12` is column `4`, row `1`, occupying `x=64..79`, `y=16..31`.

## Visual consistency rules

- Canonical connectivity cases should be visually neutral and reusable.
- Keep connected edges compatible across cases; do not introduce a trim that creates visible seams where the rule says the material continues.
- Prefer exposed-edge trims of consistent thickness.
- Use rust, bolts, seams, and panel detail selectively; readability at native 16×16 scale is more important than source-image detail.
- Large pipes, vents, lights, cables, and decorative machinery should normally live in detail/overlay assets instead of being baked into every structural tile.
- Do not rely on a generated source image being grid-perfect. Generated imagery may be used as visual reference, then reconstructed/cleaned at final 16×16 resolution.

## Planned related atlases

The structural atlas is intentionally separate from other environmental families:

- `industrial_surface.png` — structural Rule Tile + structural variants
- `industrial_fill.png` — optional interior/fill visual family if needed beyond canonical interior variants
- `industrial_details.png` — pipes, vents, cables, lights, brackets, overlays
- `industrial_hazards.png` — lasers, electrical hazards, damaged hazard pieces, related visual elements

Do not create additional atlases merely to fill this list; add them when the prototype needs them.
