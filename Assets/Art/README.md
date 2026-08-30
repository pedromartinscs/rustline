# Rustline Art Assets

This folder will contain artwork that is safe to redistribute with the Rustline repository.

## Planned structure

When the Unity project is generated, art should be organized approximately as follows:

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
└── Source/
```

`Source/` is intended for editable Rustline-owned source artwork when keeping it in the repository is useful (for example `.xcf` files, palette definitions, or working sprite sheets). Large binary source files may later be moved to Git LFS.

## Rules

- Do not commit third-party reference packs merely because they were used as inspiration.
- Do not commit an asset unless its redistribution rights are known.
- Preserve original editable source files for Rustline-owned artwork when practical.
- Final game sprites should use nearest-neighbor sampling and avoid anti-aliasing.
- Keep sprite dimensions, pivots, and naming predictable so animation/network code never depends on arbitrary art offsets.
- Prefer a small coherent asset library over many inconsistent generated assets.

See [`../../docs/ART_DIRECTION.md`](../../docs/ART_DIRECTION.md) for the visual specification and asset-generation workflow.
