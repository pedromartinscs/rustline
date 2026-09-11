# Environment Art Pipeline

This document captures the production rules established while turning `SalvageIntake` from graybox into the first Rustline demo room.

## Core separation

Rustline environment authoring deliberately separates what the player sees from what gameplay physics uses:

1. **Background dressing** — machinery, pipes, panels, gantries and other non-interactive depth elements. No colliders.
2. **Structural skins** — visible floors, catwalks, ledges, walls and other architecture the player reads as usable structure. The sprites themselves remain non-colliding.
3. **Hidden gameplay collision** — simple Tilemap/Composite geometry remains authoritative for walking, Wall Brace, LedgeClimb and other traversal.
4. **Foreground dressing** — restrained occluding pieces that may render in front of the player, normally without collision.

`visual geometry != gameplay collision` is an explicit production rule. Structural art may contain holes, braces, rust, broken silhouettes and other detail while hidden collision stays simple and safe.

## Native-pixel contract

Production environment sprites follow the same scale contract as the player:

- Canonical 28 palette;
- 16 PPU;
- Transform scale `1,1,1`;
- Point filtering;
- mipmaps off;
- no texture compression for the canonical source presentation;
- no fallback sprite physics shapes;
- no fractional runtime scaling to fix apparent size.

If an asset is the wrong apparent size, re-author/resample the source PNG deliberately rather than applying arbitrary Transform scale.

## Gameplay readability by value

Rustline uses the Canonical 28 not only as a palette restriction but as a gameplay-reading tool.

- Background machinery should generally live lower in the value hierarchy: Deep Space / Deep Navy / Shadow / Steel Shadow / Dark Metal dominate.
- Walkable/collidable structural skins should sit **one restrained readability step above background** through clearer edge contrast and slightly stronger Steel-class values.
- The player and combat information retain visual priority over both.
- Bright warning/status accents remain rare and deliberate.

This is not a rule that gameplay surfaces must be bright. They must simply read faster than non-interactive background dressing.

## Current modular architecture families

### Catwalk

Current reusable production pieces:

- `Assets/Art/Environment/Architecture/catwalk_left.png`
- `Assets/Art/Environment/Architecture/catwalk_mid_short.png`
- `Assets/Art/Environment/Architecture/catwalk_mid_long.png`
- `Assets/Art/Environment/Architecture/catwalk_right.png`

The Salvage Intake catwalk uses authored sprites for presentation and a separate hidden 12×1 collision strip. Gray visual tiles are intentionally absent beneath the production catwalk skin.

### Floor edge

Current floor-edge family:

- `Assets/Art/Environment/Architecture/Floor/floor_edge_left.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_mid_a.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_mid_b.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_right.png`

All four source canvases are `48×32 px` (`3×2 u` at 16 PPU).

`floor_edge_mid_a` is the **common/default** center module. It may be mirrored horizontally for additional repetition control.

`floor_edge_mid_b` carries visibly stronger rust and should be used **less frequently** so corrosion remains localized variation rather than the baseline material language.

The first Salvage Intake floor test uses this six-module sequence across the long lower bay:

`left -> mid_a -> mid_a (flip X) -> mid_b -> mid_a -> right`

That gives three A mids for one B mid. The six modules span exactly 18 u from `x=1` to `x=19`. Their top edge aligns with the lower-bay floor surface at `y=0`; the sprites remain visual-only while the original hidden floor collision is preserved.

Scene-specific tool while this pass is still experimental:

**Tools -> Rustline -> Apply Salvage Intake Floor Dressing**

The tool also removes only the gray visual Tilemap cells directly replaced by the 32 px-tall skin. If the Salvage Intake graybox is rebuilt, re-run the floor-dressing command afterward until this structural-skin knowledge is promoted into the general level-authoring pipeline.

### Wall edge

Current wall-edge family:

- `Assets/Art/Environment/Architecture/Wall/wall_edge_top.png`
- `Assets/Art/Environment/Architecture/Wall/wall_edge_mid.png`
- `Assets/Art/Environment/Architecture/Wall/wall_edge_bottom.png`

All three PNG source canvases are `50×150 px`. The imported `wall_edge_top` Sprite rect is `50×145 px` because five transparent source rows are trimmed from the visible sprite; `wall_edge_mid` and `wall_edge_bottom` retain `50×150 px` Sprite rects.

The Salvage Intake boundary-wall test preserves native scale and aligns each wall skin's **inner visual face** to the authoritative collision inner face. Extra sprite width therefore overhangs outward beyond the playable room rather than intruding into traversal space.

The 19 u boundary height is 304 source pixels. The visible `top + bottom` rects total 295 px, leaving a 9 px seam. Rather than scaling either source asset, one `wall_edge_mid` is placed behind the join as a backing/fill layer; `top` and `bottom` render in front. The east wall mirrors the same family horizontally.

Scene-specific tool while this pass is still experimental:

**Tools -> Rustline -> Apply Salvage Intake Wall Dressing**

Like the floor pass, the tool removes only the generic gray visual Tilemap cells replaced by the production wall skin and explicitly preserves the hidden collision cells.

## Production philosophy

Use **Large -> Medium -> Small**.

Prefer a compact reusable vocabulary over room-specific illustrations. Large hero compositions may be unique, but their constituent machinery/architecture pieces should be reusable whenever practical.

Do not complete every unused structural-atlas slot merely because it exists. New assets should be driven by visible needs in a real production room.
