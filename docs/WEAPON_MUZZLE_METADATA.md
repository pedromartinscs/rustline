# Weapon muzzle metadata

This document defines the authoring, generation, and serialized presentation-data contract for exact weapon muzzle points. The Longwatch muzzle-flash presenter consumes that data for visual placement only. Clearance casts, reticle feedback, casing ejection, and gameplay/tracer-origin migration remain separate follow-up work.

## Current scope

The first supported weapon is the **Longwatch DMR**.

Phase 1 is implemented as an offline Python/Pillow generator. The checked-in schema-versioned JSON is derived from production PNGs and the authoring reference. `RustlineM1ASetup` validates that JSON Editor-side and deterministically serializes its exact values into a compact runtime `LongwatchMuzzleMetadata2D` asset for presentation use.

Production aim-capable art remains authored only for the canonical right-facing hemisphere:

```text
p90 p80 p70 p60 p50 p40 p30 p20 p10 0
m10 m20 m30 m40 m50 m60 m70 m80 m90
```

Left-facing runtime presentation continues to use the accepted horizontal mirroring path. Do not author a second set of muzzle markers for the mirrored hemisphere.

Current Longwatch aim-capable source packages:

| State | Frames per direction |
|---|---:|
| Idle | 2 |
| Run | 6 |
| Backpedal | 4 |
| Crouch | 6 |
| Fall | 1 |

Jump and Land are deliberately **carry-only, non-firing states**. Their 48×64 carry sprites do not expose a `LongwatchRenderedPose2D`, require no muzzle metadata, and cannot produce a muzzle flash. Wall Brace and LedgeClimb also expose no Longwatch muzzle pose.

Future aim-capable weapon/state packages may join this pipeline only when their production art and gameplay semantics actually require muzzle attachment data.

## Canonical Longwatch reference

Authoring reference:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
```

The reference is an **80×96** composite built by overlaying **Idle frame 0** for all 19 right-facing authored directions.

Marker contract:

- exactly **19 opaque blue pixels**: `RGBA(0, 0, 255, 255)`;
- exactly **1 opaque red pixel**: `RGBA(255, 0, 0, 255)`;
- blue pixels mark the muzzle for the 19 authored directions;
- the red pixel is an offline authoring anchor near the shoulder, shared by all directions;
- marker pixels are intentionally placed in otherwise transparent space;
- the red authoring anchor is **not** the Unity sprite pivot and must never replace the canonical armed pivot `(24,8)`.

The checked-in reference currently validates as 19 blue markers and one red marker.

## Direction assignment

The generator must not rely on hand-entered blue-pixel coordinates.

Instead:

1. locate the unique red anchor;
2. locate all 19 blue markers;
3. compute each `red -> blue` vector using image X to the right and image Y inverted so positive Y is up;
4. sort markers by polar angle from highest to lowest;
5. assign them in canonical order `p90 ... p10, 0, m10 ... m90`;
6. sanity-check that the radial ordering is monotonic and broadly agrees with the expected 10-degree progression.

Pixel-art geometry means measured radial angles need not equal the authored angle numerically. The ordering is authoritative; angle checks should use a tolerant sanity range rather than exact equality.

## Composite-reference contamination rule

**Never use the flattened composite reference itself as the neighborhood signature.**

The reference contains all 19 weapon layers at once, so pixels from neighboring angles can contaminate the local neighborhood around a blue marker.

For each direction, the reference marker only supplies the seed location. The clean signature must be extracted from the matching isolated production sprite:

```text
Longwatch Idle / same direction / frame 0
```

This is the canonical signature source because the reference itself was authored from those Idle frame-0 poses.

## Pixel normalization

When comparing signatures:

- exact opaque production colors may be compared directly;
- transparent pixels must be normalized by alpha;
- if `alpha == 0`, hidden RGB data must be ignored and treated as canonical transparent;
- reference marker colors are authoring-only and are not production palette colors.

Production sprites should contain zero opaque blue marker pixels and zero opaque red marker pixels.

## Adaptive exact matching

Use deterministic exact pixel matching, not image-recognition heuristics.

For each direction:

1. derive a clean signature around the seeded muzzle in isolated Idle frame 0;
2. start with a small odd neighborhood, for example `5×5`;
3. search every applicable production frame of that direction;
4. if any frame has more than one candidate, enlarge the signature (`7×7`, `9×9`, `11×11`, `13×13`, within a documented maximum);
5. select the smallest signature size that produces exactly one candidate in every required frame.

Allowed outcomes for a required frame are deliberately strict:

```text
1 match   -> success
0 matches -> error
>1 match  -> ambiguous error
```

The generator must never silently choose a "best" candidate.

If no supported signature radius is unique across the corpus, fail with weapon/state/direction/frame diagnostics.

### Cell-edge behavior

The implementation extracts neighborhoods with a distinct out-of-bounds sentinel and confines every search to one 80×96 cell. During exact comparison, an out-of-cell sample is equivalent only to a normalized fully transparent pixel. This models the empty rendered space beyond an isolated Full Rect sprite; it never permits an out-of-cell sample to match an alpha-greater-than-zero pixel.

This explicit transparent-padding rule is exercised by Longwatch p90 Run frames 2 and 3: the unchanged muzzle signature reaches the top cell boundary. It avoids Python slice wraparound while preserving exact normalized RGBA matching.

The current production corpus resolves uniquely at `5×5` for every direction; adaptive sizes through `13×13` remain available for future art revisions.

## Intentional Crouch exception

Longwatch Crouch angles:

```text
m70
m80
m90
```

are intentionally **unsupported muzzle poses**. In those downward crouch poses the muzzle falls outside the authored 80×96 cell, matching the visual floor-intersection problem observed in-engine.

The generator treats these as a declared exception, not as extraction failures. Generated metadata represents them explicitly as unsupported rather than inventing coordinates.

Approved future presentation behavior:

- Crouch requests that would select `m70`, `m80`, or `m90` render the `m60` pose instead;
- exact continuous pointer aim remains available to gameplay code;
- firing/reticle behavior for this invalid crouch sector is follow-up runtime work;
- the intended UI direction is a normal yellow crosshair for valid aim and a red crosshair for an impossible/blocked weapon angle.

Do not implement that runtime behavior as part of metadata regeneration unless explicitly requested.

## Coordinate contract

Generated muzzle points are stored in **source-pixel space relative to the canonical armed sprite pivot**, not as absolute sheet coordinates.

Longwatch canonical aim-capable geometry:

```text
cell        = 80×96 px
pivot       = (24,8) px
PPU         = 16
```

Use pixel-center coordinates consistently and document the conversion.

Conceptually:

```text
muzzleOffsetPixels = muzzlePixelCenter - armedPivot
```

The generated coordinate system is:

```text
+x = forward/right in authored right-facing art
+y = up
```

Runtime left-facing mirroring can therefore negate X while preserving Y.

## Generated artifact

The versioned generated output is:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Generated/longwatch_dmr_muzzle_metadata.json
```

The JSON is derived data and must not be edited by hand. Re-running the generator after an art change is expected to update the file, producing a useful Git diff of changed muzzle points.

The current schema is version 1 with generator version 2. The validated Longwatch corpus contains **343 supported frame points**: 38 Idle, 114 Run, 76 Backpedal, 96 Crouch, and 19 Fall. Crouch `m70`, `m80`, and `m90` are present as explicit unsupported direction entries with empty frame arrays.

The schema is versioned and includes enough information to validate:

- weapon id;
- cell size;
- canonical pivot;
- state;
- authored direction/suffix;
- frame index;
- supported/unsupported status;
- muzzle offset for supported frames;
- generator/schema version.

## Unity/runtime boundary

Python is responsible for image inspection and metadata generation.

Unity Editor C# consumes the generated JSON through `RustlineM1ASetup` and serializes compact, strongly typed runtime data into:

```text
Assets/Config/Weapons/Generated/LongwatchDMRMuzzleMetadata.asset
```

The asset contains all 343 supported points, including all 19 one-frame Fall directions, and preserves Crouch `m70`, `m80`, and `m90` as unsupported direction records with no frame coordinates. `PlayerLongwatchAimPresenter2D` exposes the state, direction, authored angle, displayed Body frame, and facing of the weapon sprite actually rendered. Jump/Land carry deliberately return no rendered muzzle pose. The muzzle-flash presenter performs a direct indexed lookup after an aim-capable visual pose has been selected, and an already-active flash is hidden immediately if the presenter transitions to a state without a muzzle-capable pose.

Runtime gameplay must not:

- scan PNG files;
- perform signature matching;
- depend on Pillow/Python;
- repeatedly parse JSON;
- discover assets from disk.

Runtime muzzle-flash placement is implemented without changing ballistics. The hitscan result origin and the existing distal tracer still use `AimOriginWorld`. Runtime clearance, the Crouch invalid-angle visual clamp and red reticle, hitscan/tracer-origin migration, and casing ejection remain separate work.

## Tooling location

The generator belongs under:

```text
Tools/WeaponMetadata/
```

See `Tools/WeaponMetadata/README.md` for implementation boundaries.
