# Longwatch DMR muzzle reference

`longwatch_dmr_muzzle_reference.png` is the authoring reference used to seed automatic muzzle-point discovery.

It is an **80×96** flattened composite made from **Idle frame 0** of all 19 authored right-facing Longwatch angles.

## Marker colors

The image intentionally contains:

- 19 pixels of exact opaque blue `#0000FFFF`, one muzzle marker per authored direction;
- 1 pixel of exact opaque red `#FF0000FF`, the shared shoulder/reference anchor.

Markers live in otherwise transparent space and are tooling data, not production artwork.

The red point exists only so tooling can order the 19 blue points radially. It is **not** the Unity sprite pivot. Production armed sprites continue to use the canonical `(24,8)` pixel pivot.

Do not add left-facing markers. Runtime mirroring supplies the opposite hemisphere.

## Canonical mapping

Tooling assigns the blue markers by polar order around the red anchor:

```text
p90 p80 p70 p60 p50 p40 p30 p20 p10 0
m10 m20 m30 m40 m50 m60 m70 m80 m90
```

Because this is pixel art, the measured red-to-blue polar angle is only a sanity check; exact 10-degree geometry is not required.

## Important signature rule

Do **not** copy a neighborhood directly from this flattened composite. Other overlaid weapon angles may contaminate neighboring pixels.

For each direction, the blue marker is only a seed. The actual matching signature is extracted from the corresponding isolated production **Idle frame 0** sprite.

## Maintenance

This reference is expected to remain stable across normal animation edits.

Update the marker reference only if the canonical Longwatch Idle frame-0 muzzle geometry itself changes enough that the existing seed no longer identifies the correct local muzzle region.

Longwatch Crouch `m70`, `m80`, and `m90` are intentionally allowed to have no generated muzzle point because the muzzle leaves the 80×96 cell in those poses.

See [`docs/WEAPON_MUZZLE_METADATA.md`](../../../../../docs/WEAPON_MUZZLE_METADATA.md) for the full generator contract.
