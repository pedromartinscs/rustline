# Weapon muzzle metadata

This document defines the authoring, generation, and serialized-data contract for exact weapon muzzle points.

The offline generator currently supports the **Longwatch DMR** and **Latch-9**. Both weapons now consume their generated muzzle corpus for runtime muzzle-flash placement. Latch-9 keeps its generated JSON as the authoring/source-of-truth artifact and converts it Editor-side into compact runtime metadata. Clearance casts, reticle feedback, casing ejection, projectile/tracer-origin migration, and Latch-9 gameplay/ammunition behavior remain separate concerns.

## Current aim-capable source contract

Both supported weapons use the canonical right-facing aim hemisphere:

```text
p90 p80 p70 p60 p50 p40 p30 p20 p10 0
m10 m20 m30 m40 m50 m60 m70 m80 m90
```

Left-facing presentation continues to use horizontal mirroring. Do not author a second muzzle-reference set for the mirrored hemisphere.

Current aim-capable packages for both weapons are:

| State | Frames per direction |
|---|---:|
| Idle | 2 |
| Run | 6 |
| Backpedal | 4 |
| Crouch | 6 |
| Fall | 1 |

Jump and Land are fixed carry states and remain non-firing. Wall Brace and LedgeClimb render no weapon; Wall Kick also remains non-firing. These states therefore require no muzzle metadata.

## Authoring references

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
ArtSource/Metadata/Weapons/latch_9/Muzzle/latch_9_muzzle_reference.png
```

Each reference is an **80x96** composite built from Idle frame 0 for all 19 authored right-facing directions.

Marker contract:

- exactly **19 opaque blue pixels**: `RGBA(0, 0, 255, 255)`;
- exactly **1 opaque red pixel**: `RGBA(255, 0, 0, 255)`;
- each blue pixel identifies the muzzle coordinate for one authored direction;
- the red pixel is a shared offline radial-ordering/orientation anchor;
- the red authoring anchor is **not** the Unity sprite pivot and must never replace the canonical armed pivot `(24,8)`;
- a blue marker may sit in transparent space immediately beyond the barrel **or overwrite the exact production muzzle pixel**. It is only a coordinate seed; the clean matching signature always comes from isolated production art.

This last rule is intentional. Longwatch references happen to use transparent-space seeds. Latch-9 includes at least one valid opaque muzzle seed, so seed transparency is not part of the generic metadata contract.

## Direction assignment

The generator never relies on hand-entered blue-pixel coordinates.

For each weapon it:

1. locates the unique red anchor;
2. locates all 19 blue markers;
3. computes each `red -> blue` vector using image X to the right and inverted image Y so positive Y is up;
4. sorts markers by polar angle from highest to lowest;
5. assigns them in canonical order `p90 ... p10, 0, m10 ... m90`;
6. sanity-checks that radial ordering is unique and broadly agrees with the expected 10-degree progression.

Pixel-art geometry means measured radial angles need not numerically equal the authored bucket angle. Ordering is authoritative; the angular check is intentionally tolerant.

## Composite-reference contamination rule

**Never use the flattened composite reference itself as the matching signature.**

A reference contains all 19 weapon layers at once, so neighboring angles can contaminate pixels around a marker. For each direction, the blue marker supplies only a seed coordinate. The clean signature is extracted from:

```text
<weapon> Idle / same direction / frame 0
```

The generator then searches every supported production frame for an exact copy of that signature.

## Pixel normalization

When comparing signatures:

- exact opaque production colors are compared directly;
- fully transparent pixels are normalized by alpha;
- when `alpha == 0`, hidden RGB data is ignored and treated as canonical transparent;
- reference marker colors are authoring-only and must not appear as opaque marker pixels in production sprites.

## Adaptive exact matching

Matching is deterministic and exact, not heuristic image recognition.

For each direction:

1. derive a clean signature around the seeded muzzle from isolated Idle frame 0;
2. begin at `5x5`;
3. search every supported production frame for that direction;
4. if any frame has more than one candidate, retry the whole direction at `7x7`, then `9x9`, `11x11`, and `13x13` as necessary;
5. accept the smallest size that yields exactly one match in every required frame.

Allowed outcomes for every required frame are strict:

```text
1 match   -> success
0 matches -> error
>1 match  -> ambiguous error
```

The generator must never silently choose a closest or best candidate.

Current validated signature sizes are:

- **Longwatch DMR:** `5x5` for all 19 directions;
- **Latch-9:** `9x9` for `p90`, `p80`, `p70`; `7x7` for `p60`, `p50`, `p40`, `p30`; `5x5` for `p20` through `m90`.

## Cell-edge behavior

Neighborhood extraction uses a distinct out-of-bounds sentinel and confines every search to one `80x96` cell. Python slicing therefore cannot wrap across cell edges.

During exact comparison, an out-of-cell sample is equivalent only to normalized fully transparent space. This models the empty rendered region beyond an isolated Full Rect sprite; it never allows padding to match an alpha-greater-than-zero pixel.

Longwatch `p90` Run frames 2 and 3 exercise this policy because their muzzle signature reaches the top cell boundary.

## Weapon-specific unsupported poses

Unsupported muzzle poses are declared by weapon/state/direction configuration. They are not inferred by choosing a nearby coordinate and are never filled with invented data.

### Longwatch DMR

Crouch directions:

```text
m70
m80
m90
```

remain intentionally unsupported because the muzzle leaves the authored `80x96` cell in those downward poses. Generated metadata contains explicit unsupported direction records with empty frame arrays.

The previously approved future presentation behavior remains: requests in this invalid crouch sector may visually clamp to `m60`, while continuous pointer aim stays available to gameplay. Reticle/fire blocking for that sector is separate runtime work.

### Latch-9

The current production corpus validates **all 19 directions in all five aim-capable states**. No Latch-9 muzzle pose is currently declared unsupported.

If future art changes make a Latch pose impossible, it must be explicitly reviewed and declared unsupported rather than hidden by a generator fallback.

## Coordinate contract

Generated muzzle points are stored in **source-pixel space relative to the canonical armed sprite pivot**, not relative to the red authoring anchor and not as absolute sheet coordinates.

Current shared aim-capable geometry:

```text
cell        = 80x96 px
pivot       = (24,8) px
PPU         = 16
```

Pixel-center coordinates are used consistently:

```text
muzzleOffsetPixels = muzzlePixelCenter - armedPivot
```

Generated coordinates use:

```text
+x = forward/right in authored right-facing art
+y = up
```

Runtime left-facing mirroring can therefore negate X while preserving Y.

The red reference pixel serves only to assign/orient authored directions. It is not serialized as the sprite pivot.

## Generated artifacts

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json
ArtSource/Metadata/Weapons/latch_9/Generated/latch_9_muzzle_metadata.json
```

Generated JSON is versioned derived data and must not be edited by hand. Re-running the generator after art changes should produce a meaningful Git diff.

The current schema remains **version 1** and generator version remains **2**. Generalizing the tool to multiple configured weapons did not change the serialized schema or exact matching semantics, and the approved Longwatch output remains byte-identical.

Validated corpora:

- **Longwatch DMR — 343 supported frame points:** 38 Idle, 114 Run, 76 Backpedal, 96 Crouch, 19 Fall; Crouch `m70/m80/m90` are explicit unsupported entries.
- **Latch-9 — 361 supported frame points:** 38 Idle, 114 Run, 76 Backpedal, 114 Crouch, 19 Fall; no unsupported entries.

Each JSON includes enough information to validate:

- weapon id;
- cell size;
- canonical pivot;
- state;
- authored direction/suffix;
- frame index;
- supported/unsupported status;
- muzzle offset for each supported frame;
- generator/schema version;
- reference path.

When multiple weapons are selected, the generator resolves every corpus completely in memory before writing any output. Failure in one weapon therefore cannot leave another weapon half-regenerated.

## Unity/runtime boundary

Python owns image inspection and metadata generation. Runtime gameplay must not:

- scan PNG files;
- perform signature matching;
- depend on Pillow/Python;
- repeatedly parse JSON;
- discover art assets from disk.

### Longwatch DMR

`RustlineM1ASetup` imports the generated Longwatch JSON Editor-side into:

```text
Assets/Config/Weapons/Generated/LongwatchDMRMuzzleMetadata.asset
```

`PlayerLongwatchAimPresenter2D` exposes the rendered state, direction, authored angle, displayed Body frame, and facing; the muzzle-flash presenter performs a direct indexed lookup against the compact runtime metadata. Jump/Land carry expose no muzzle-capable rendered pose.

Longwatch ballistics are deliberately unchanged by this metadata phase. Hitscan and the current tracer still originate from `AimOriginWorld`.

### Latch-9

`RustlineLatch9MuzzleSetup` validates the generated Latch JSON and converts it Editor-side into:

```text
Assets/Config/Weapons/Generated/Latch9MuzzleMetadata.asset
```

The same setup deterministically imports both approved `180x9` muzzle-flash sheets as twenty `9x9` sprites each (`10 variants x 2 frames`) with the canonical `0.5 px` left-edge / vertically centered pivot, `16 PPU`, Point filtering, no mipmaps, uncompressed production import, binary source transparency, and sRGB sampling.

`PlayerLatch9AimPresenter2D` now exposes the exact rendered state, direction bucket, authored angle, displayed Body frame, and facing for `Idle`, `Run`, `Backpedal`, `Crouch`, and `Fall`. Jump/Land carry deliberately expose no muzzle-capable pose, and traversal states continue to release Latch presentation.

`Latch9MuzzleFlashPresenter2D` consumes that rendered pose plus the compact metadata and follows the established Longwatch two-rendered-frame flash contract. It owns two explicit presentation banks:

- `Conventional` — the approved cyan Latch-9 flash and the default profile;
- `Bouncing` — the approved violet/electric flash, reserved for a future ricochet-ammunition gameplay contract.

The presenter never infers `Bouncing` from semi/automatic fire mode. Until ricochet ammunition exists in gameplay, the purple bank remains an explicitly selectable presentation profile rather than an invented weapon rule.

Latch-9 ballistics are likewise **not** migrated by this presentation integration. `PlayerWeaponController2D` continues to resolve shots from `AimOriginWorld` using the continuous `ContinuousAimDirection`; the discrete 10-degree art bucket is presentation-only.

The current Editor Latch harness is still presentation-only and disables weapon gameplay. This muzzle integration therefore establishes the production import/metadata/pose/flash contract without inventing Latch damage, fire rate, range, ammunition economy, or ricochet behavior.

## Tooling location

Generator and tests live under:

```text
Tools/WeaponMetadata/
```

See `Tools/WeaponMetadata/README.md` for commands and validation workflow.
