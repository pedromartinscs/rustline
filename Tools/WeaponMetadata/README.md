# Weapon Metadata Tools

This directory is reserved for offline weapon-art metadata generation.

The first tool should generate exact **Longwatch DMR muzzle points** from production PNGs. The design contract is documented in [`docs/WEAPON_MUZZLE_METADATA.md`](../../docs/WEAPON_MUZZLE_METADATA.md).

## Intended implementation

Use:

- Python 3;
- Pillow for PNG loading and RGBA inspection;
- deterministic exact pixel comparisons;
- no OpenCV unless a future requirement genuinely needs it.

Expected entry point:

```text
Tools/WeaponMetadata/generate_muzzle_metadata.py
```

Expected checked-in input reference:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
```

Expected generated output:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json
```

## Scope of the first generator

The first implementation should:

1. validate the 80×96 reference image;
2. require exactly 19 opaque blue muzzle markers and one opaque red anchor;
3. assign blue markers to `p90 ... m90` by radial order around the red anchor;
4. use each marker only as a seed into the matching isolated Longwatch Idle frame 0;
5. extract a clean production signature from that isolated frame;
6. find a unique exact match in every supported Idle, Run, Backpedal, and Crouch frame;
7. use adaptive odd signature sizes rather than guessing when a small neighborhood is ambiguous;
8. explicitly allow Crouch `m70`, `m80`, and `m90` to be unsupported;
9. write deterministic, schema-versioned JSON relative to the canonical `(24,8)` armed pivot;
10. provide clear diagnostics and automated tests.

Do not add runtime weapon-clearance logic in this first phase.

## Failure policy

A required production frame must have exactly one match.

Never silently select the nearest, first, or highest-scoring candidate.

```text
0 matches -> fail
1 match   -> accept
2+        -> fail as ambiguous
```

Transparent RGB must be ignored whenever alpha is zero.

## Generated files

Generated JSON is versioned source-of-truth derived from production art. Do not hand-edit it.

Tests should prove deterministic output: identical source art and reference data must produce byte-for-byte identical JSON.
