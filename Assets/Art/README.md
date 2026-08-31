# Rustline Art Assets

This folder contains artwork that is safe to redistribute with the Rustline repository.

## Structure

Art is organized approximately as follows:

```text
Assets/Art/
├── Characters/
│   ├── Player/
│   └── Enemies/
├── Environment/
│   ├── Tiles/
│   ├── Backgrounds/
│   └── Props/
├── Weapons/
├── Pickups/
├── FX/
├── UI/
├── Palette/
└── Source/
```

`Source/` is intended for editable Rustline-owned source artwork when keeping it in the repository is useful (for example `.xcf` files or working sprite sheets). Large binary source files may later be moved to Git LFS.

The canonical GIMP palette lives in `Palette/rustline.gpl`.

## Production rules

- Do not commit third-party reference packs merely because they were used as inspiration.
- Do not commit an asset unless its redistribution rights are known.
- Preserve original editable source files for Rustline-owned artwork when practical.
- Final pixel art uses Rustline Canonical 28 plus transparency only.
- Final alpha is binary (`0` or `255`); remove partially transparent edge pixels.
- Use nearest-neighbor / point filtering and no anti-aliasing.
- Keep sprite dimensions, pivots, cell sizes, and naming predictable so gameplay code never depends on arbitrary art offsets.
- Prefer a small coherent asset library over many inconsistent generated assets.

## Unity import contract

Production PNGs under `Assets/Art/` use 16 Pixels Per Unit, Point filtering, no mipmaps, no texture/crunch compression, Full Rect sprite meshes, clamp wrapping, and source alpha as transparency. Unity must preserve the source pixels and complete fixed cells without content-based trimming.

`Assets/Editor/RustlinePixelArtPostprocessor.cs` applies the common baseline automatically. Use **Tools → Rustline → Rebuild M0 Art Showcase** to reapply the known fixed-grid player/industrial-atlas slices and regenerate the M0 preview assets safely.

Player animation sheets use complete 48×64 cells with a consistent normalized `(0.5, 0.0)` pivot. `industrial_surface.png` uses all 48 fixed 16×16 cells; its logical top-to-bottom rows are mapped explicitly to Unity's bottom-origin coordinates.

## Environment tiles

The first structural environment atlas is defined in [`../../docs/TILESET_SPEC.md`](../../docs/TILESET_SPEC.md). The tile workspace contains its own README at [`Environment/Tiles/README.md`](Environment/Tiles/README.md).

The canonical structural atlas uses:

- 16×16 tiles
- 128×96 atlas size
- 8×6 grid / 48 slots
- fixed N/E/S/W connectivity mapping in slots 00–15

Do not silently reorder fixed atlas slots after they become canonical.

See [`../../docs/ART_DIRECTION.md`](../../docs/ART_DIRECTION.md) for the broader visual specification and asset-generation workflow.
