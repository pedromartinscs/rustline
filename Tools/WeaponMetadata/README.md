# Weapon Metadata Tools

This directory contains deterministic offline generation of exact weapon-art metadata.

The muzzle generator currently supports:

- **Longwatch DMR** (`longwatch_dmr`)
- **Latch-9** (`latch_9`)

The design/runtime contract is documented in [`docs/WEAPON_MUZZLE_METADATA.md`](../../docs/WEAPON_MUZZLE_METADATA.md).

## Implementation

Use:

- Python 3;
- Pillow for PNG loading and RGBA inspection;
- deterministic exact pixel comparisons;
- no OpenCV or fuzzy image matching.

Entry point:

```text
Tools/WeaponMetadata/generate_muzzle_metadata.py
```

## Checked-in references

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
ArtSource/Metadata/Weapons/latch_9/Muzzle/latch_9_muzzle_reference.png
```

Each reference is an `80x96` composite with exactly 19 opaque blue muzzle markers and one opaque red radial-ordering anchor.

The red pixel is an **authoring anchor**, not the Unity sprite pivot. Runtime offsets remain relative to the canonical armed pivot `(24,8)`.

A blue marker may be placed in transparent space immediately beyond the muzzle or may overwrite the exact production muzzle pixel. The marker supplies only a coordinate seed. The signature used for propagation is always extracted from the matching isolated production Idle frame 0, so either authoring convention is valid.

## Generated outputs

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json
ArtSource/Metadata/Weapons/latch_9/Generated/latch_9_muzzle_metadata.json
```

Generated JSON is derived data and must not be hand-edited.

## Commands

Install the dependency and generate both configured weapons from repository root:

```text
python -m pip install -r Tools/WeaponMetadata/requirements.txt
python Tools/WeaponMetadata/generate_muzzle_metadata.py
```

Generate one weapon only:

```text
python Tools/WeaponMetadata/generate_muzzle_metadata.py --weapon longwatch_dmr
python Tools/WeaponMetadata/generate_muzzle_metadata.py --weapon latch_9
```

Check that selected checked-in JSON is byte-identical to fresh in-memory generation:

```text
python Tools/WeaponMetadata/generate_muzzle_metadata.py --check
python Tools/WeaponMetadata/generate_muzzle_metadata.py --weapon latch_9 --check
```

When more than one weapon is selected, every corpus is fully resolved before any output file is written. A failure in one weapon therefore cannot leave another weapon partially regenerated.

Run unit and real-art integration tests:

```text
python -m unittest discover -s Tools/WeaponMetadata/tests -p "test_*.py" -v
```

## Scope of the generator

For each configured weapon the implementation:

1. validates the `80x96` reference image;
2. requires exactly 19 opaque blue muzzle markers and one opaque red anchor;
3. assigns markers to `p90 ... 0 ... m90` by radial order around the red anchor;
4. uses each marker only as a seed into the matching isolated production Idle frame 0;
5. extracts a clean production signature around that seed;
6. finds a unique exact match in every supported Idle, Run, Backpedal, Crouch, and Fall frame;
7. grows the odd signature size through `5x5`, `7x7`, `9x9`, `11x11`, and `13x13` only when needed to obtain uniqueness;
8. applies weapon-specific declared unsupported poses instead of inventing coordinates;
9. writes deterministic schema-versioned offsets relative to the canonical `(24,8)` armed pivot.

Current validated corpora:

- **Longwatch DMR:** 343 supported points — Idle 38, Run 114, Backpedal 76, Crouch 96, Fall 19. Crouch `m70`, `m80`, and `m90` remain explicitly unsupported. Every direction resolves with a `5x5` signature.
- **Latch-9:** 361 supported points — Idle 38, Run 114, Backpedal 76, Crouch 114, Fall 19. No direction is currently unsupported. `p90/p80/p70` require `9x9`, `p60/p50/p40/p30` require `7x7`, and all remaining directions resolve with `5x5`.

Jump and Land carry, Wall Brace, Wall Kick, and LedgeClimb are not muzzle-metadata states because they are not currently firing poses.

## Failure policy

A required production frame must have exactly one exact signature match:

```text
0 matches -> fail
1 match   -> accept
2+        -> fail as ambiguous
```

Never select the nearest, first, or highest-scoring candidate.

Fully transparent pixels are normalized by alpha, so hidden RGB data is ignored when `alpha == 0`.

## Cell-boundary policy

Neighborhood extraction uses an explicit out-of-bounds sentinel. Searches never wrap across an individual `80x96` cell.

For exact comparison, out-of-cell samples are equivalent only to normalized fully transparent space. This models an isolated Full Rect sprite outside its authored cell while never allowing padding to match an alpha-greater-than-zero pixel.

## Runtime boundary

This tool is offline authoring infrastructure. Runtime gameplay must not scan PNGs, perform signature matching, invoke Python/Pillow, or repeatedly parse generated JSON.

Longwatch currently imports its generated JSON into compact Unity runtime presentation metadata for muzzle-flash placement. Ballistics/tracer origin still uses `AimOriginWorld` until a separate runtime task explicitly migrates it.

Latch-9 runtime metadata consumption and projectile-origin integration are separate follow-up work; this generator only establishes and validates the exact authored muzzle coordinates.
