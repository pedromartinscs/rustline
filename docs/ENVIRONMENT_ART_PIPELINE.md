# Environment Art Pipeline

This document captures the production rules established while turning `SalvageIntake` from graybox into the first Rustline demo route.

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
- no texture compression for canonical source presentation;
- no fallback sprite physics shapes;
- no fractional runtime scaling to fix apparent size.

If an asset is the wrong apparent size, re-author/resample the source PNG rather than applying arbitrary Transform scale.

## Gameplay readability by value

- Background machinery should generally live lower in the value hierarchy: Deep Space / Deep Navy / Shadow / Steel Shadow / Dark Metal dominate.
- Walkable/collidable structural skins should sit **one restrained readability step above background** through clearer edge contrast and slightly stronger Steel-class values.
- Player and combat information retain visual priority over both.
- Bright warning/status accents remain rare and deliberate.

Gameplay surfaces do not need to be bright. They need to read faster than non-interactive background dressing.

## Structural Tilemap

`industrial_surface.png` remains the canonical structural atlas. Slots `00–15` keep their fixed N/E/S/W connectivity semantics.

The fully-connected interior slot `15` is allowed deterministic RuleTile rotation in `0/90/180/270°` steps. This breaks obvious fill repetition without changing connectivity. Canonical slots `00–14` remain fixed-orientation rules.

The Tilemap is structural grammar and fill, not the entire final art layer. Production skins may replace visible Tilemap cells at important edges while hidden collision stays authoritative.

## Current modular architecture families

### Catwalk

Reusable production pieces:

- `Assets/Art/Environment/Architecture/catwalk_left.png`
- `Assets/Art/Environment/Architecture/catwalk_mid_short.png`
- `Assets/Art/Environment/Architecture/catwalk_mid_long.png`
- `Assets/Art/Environment/Architecture/catwalk_right.png`

The Salvage Intake catwalk uses authored sprites for presentation and a separate hidden 12×1 collision strip. Generic structural visual tiles are intentionally absent beneath the production catwalk skin.

### Floor edge

Current family:

- `Assets/Art/Environment/Architecture/Floor/floor_edge_left.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_mid_a.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_mid_b.png`
- `Assets/Art/Environment/Architecture/Floor/floor_edge_right.png`

All four source canvases are `48×32 px` (`3×2 u` at 16 PPU).

`floor_edge_mid_a` is the common/default center module and may be mirrored horizontally for repetition control. `floor_edge_mid_b` carries stronger rust and should appear less frequently.

Scene-specific application tool while this family is still being integrated:

**Tools -> Rustline -> Apply Salvage Intake Floor Dressing**

The tool removes only generic visual Tilemap cells directly replaced by the skin. Hidden collision is preserved.

### Wall edge

Current family:

- `Assets/Art/Environment/Architecture/Wall/wall_edge_top.png`
- `Assets/Art/Environment/Architecture/Wall/wall_edge_mid.png`
- `Assets/Art/Environment/Architecture/Wall/wall_edge_bottom.png`

All source canvases are `50×150 px`. The imported `wall_edge_top` visible Sprite rect is `50×145 px`; `mid` and `bottom` remain `50×150 px`.

This family now dresses the **west outer boundary only** in Salvage Intake. The former mirrored east boundary skin was retired when the east side became the permanent service-shaft entrance. Do not use the old full-height east-wall composition there again.

Scene-specific application tool:

**Tools -> Rustline -> Apply Salvage Intake Wall Dressing**

The current tool replaces only the west generic wall cells and preserves the west hidden collision.

### East service shaft

The approved east service shaft is generated directly by `RustlineSalvageIntakeSetup` and is gameplay architecture, not decorative dressing.

Its fixed traversal envelope is:

- 4 u / 64 px lower entry height;
- 4 u / 64 px open shaft width;
- continuous left/right wall coverage suitable for Wall Brace;
- right-wall top and upper deck at walkable `y=15`.

The shaft currently uses `Industrial Surface - Visual` directly. Future shaft-specific wall/floor skins should be visual-only and must preserve the accepted hidden collision geometry unless traversal is deliberately re-tested.

## Production philosophy

Use **Large -> Medium -> Small**.

Prefer a compact reusable vocabulary over room-specific illustrations. Large hero compositions may be unique, but their constituent machinery/architecture pieces should be reusable whenever practical.

Do not complete every unused structural-atlas slot merely because it exists. New assets should be driven by visible needs in a real production room.