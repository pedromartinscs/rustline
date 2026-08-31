# Rustline Environment Tiles

This folder contains redistributable Rustline-owned environment tile atlases.

## Primary atlas

The first structural atlas is:

`industrial_surface.png`

Canonical specification:

- **128×96 px** final size
- **16×16 px** per tile
- **8 columns × 6 rows**
- **48 fixed slots**
- slots `00–15` are the canonical N/E/S/W structural connectivity cases
- slots `16–47` are thin platforms, visual variants, damaged variants, specials, and reserve

See [`../../../../docs/TILESET_SPEC.md`](../../../../docs/TILESET_SPEC.md) for the full slot map and Rule Tile semantics.

## Production requirements

- Rustline Canonical 28 palette only
- binary alpha only (`0` or `255`)
- nearest-neighbor / point filtering
- no anti-aliased edge colors
- exact 16×16 grid alignment
- connected structural edges must remain visually compatible across Rule Tile cases

## Unity integration

Unity imports the complete atlas at 16 PPU with Point filtering, no mipmaps or compression, and Full Rect sprite meshes. It is sliced into all 48 fixed 16×16 cells, including transparent unfinished slots. Logical slot row 0 is the top source row (`y=80` in Unity sprite coordinates), and logical row 1 is immediately below it (`y=64`), preserving the documented top-to-bottom slot order.

The generated `IndustrialSurfaceRuleTile` uses only the four cardinal neighbors. A present direction requires `This`; an absent direction requires `NotThis`; diagonal cells are ignored. Rebuild or validate it through the Rustline tools in Unity's **Tools** menu.

## Editing workflow

Generated environment images may be used as Rustline-owned source/reference material, but final structural tiles are intentionally cleaned and reconstructed at 16×16 where required. Do not assume a generated image is geometrically tileable merely because it visually resembles pixel art.

Preserve the fixed slot order defined in `docs/TILESET_SPEC.md`. If the atlas needs additional cases, use reserved slots or update the specification deliberately rather than silently reordering existing sprites.
