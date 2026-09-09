# Weapon Metadata Tools

This directory contains offline weapon-art metadata generation.

The generator produces exact **Longwatch DMR muzzle points** from production PNGs. The design contract is documented in [`docs/WEAPON_MUZZLE_METADATA.md`](../../docs/WEAPON_MUZZLE_METADATA.md).

## Implementation

Use:

- Python 3;
- Pillow for PNG loading and RGBA inspection;
- deterministic exact pixel comparisons;
- no OpenCV unless a future requirement genuinely needs it.

Entry point:

```text
Tools/WeaponMetadata/generate_muzzle_metadata.py
```

Checked-in input reference:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
```

Versioned generated output:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json
```

Install the sole third-party dependency and run the generator from repository root:

```text
python -m pip install -r Tools/WeaponMetadata/requirements.txt
python Tools/WeaponMetadata/generate_muzzle_metadata.py
```

Verify that the checked-in JSON is current without writing it:

```text
python Tools/WeaponMetadata/generate_muzzle_metadata.py --check
```

Run the focused unit and real-art integration tests:

```text
python -m unittest discover -s Tools/WeaponMetadata/tests -p "test_*.py" -v
```

## Scope of the generator

The implementation:

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

The current Longwatch corpus resolves all required frames uniquely with the
smallest `5×5` signature. It emits 324 supported muzzle points plus explicit
unsupported entries for Crouch `m70`, `m80`, and `m90`.

Do not add runtime weapon-clearance logic to this offline phase.

## Failure policy

A required production frame must have exactly one match.

Never silently select the nearest, first, or highest-scoring candidate.

```text
0 matches -> fail
1 match   -> accept
2+        -> fail as ambiguous
```

Transparent RGB must be ignored whenever alpha is zero.

## Cell-boundary policy

Neighborhood extraction uses an explicit out-of-bounds sentinel, so Python
slicing cannot wrap around and searches never leave an individual 80×96 cell.
For exact comparison, out-of-cell samples are equivalent only to normalized
fully transparent pixels. This is deliberate: every frame is an isolated Full
Rect sprite and renders no content outside its cell. The sentinel never matches
pixels whose alpha is greater than zero.

This rule is required by the real p90 Run frames 2 and 3, where the unchanged
barrel-tip signature translates to the top edge. The focused boundary test locks
the policy down. Matching otherwise remains exact RGBA comparison; there is no
fuzzy score, closest-candidate choice, or prior-frame tracking.

## Generated files

Generated JSON is versioned source-of-truth derived from production art. Do not hand-edit it.

Tests should prove deterministic output: identical source art and reference data must produce byte-for-byte identical JSON.

## Runtime boundary

This tool does not implement Unity JSON consumption, runtime muzzle-origin
migration, weapon clearance, muzzle effects, or invalid-crouch-angle reticle/fire
behavior. `AimOriginWorld` remains the current gameplay/tracer origin until a
separate runtime task changes it.
