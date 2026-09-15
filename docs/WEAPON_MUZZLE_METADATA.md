# Weapon muzzle metadata

This document defines the authoring, generation, and serialized-data contract for exact weapon muzzle points.

The offline generator currently supports the **Longwatch DMR** and **Latch-9**. Both weapons consume their generated muzzle corpus for runtime muzzle-flash placement. Latch-9 also uses the same compact runtime metadata as the spawn origin for its visible projectile. Clearance casts, reticle feedback, casing ejection, authored collision particles, and the broader ammo-economy/weapon-switching layer remain separate concerns.

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

Each reference is an **80x96** composite built from Idle frame 0 for all 19 authored right-facing directions. The composite must preserve the exact source-pixel raster of those Idle layers; a reference assembled from another animation state is not coordinate-compatible with the generator seed contract.

Marker contract:

- exactly **19 opaque blue pixels**: `RGBA(0, 0, 255, 255)`;
- exactly **1 opaque red pixel**: `RGBA(255, 0, 0, 255)`;
- each blue pixel identifies the muzzle coordinate for one authored direction;
- the red pixel is a shared offline radial-ordering/orientation anchor;
- the red authoring anchor is **not** the Unity sprite pivot and must never replace the canonical armed pivot `(24,8)`;
- a blue marker may sit in transparent space immediately beyond the barrel **or overwrite the exact production muzzle pixel**. It supplies the coordinate seed used on the corresponding isolated production Idle frame 0.

Longwatch references happen to use transparent-space seeds. Latch-9 includes at least one valid opaque muzzle seed, so seed transparency is not part of the generic metadata contract.

## Direction assignment

The generator never relies on hand-entered direction labels for the blue pixels.

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

Because the seed coordinate is applied directly to that Idle frame, the reference and production Idle art must share the same source-pixel coordinate space. The corrected Latch-9 reference is authored this way; its regenerated 361 points shifted uniformly by `(+1,-3)` source pixels from the earlier Fall-based reference and now resolve from the intended muzzle anchors.

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
- **Latch-9:** `5x5` for all 19 directions after the Idle-reference correction.

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

Longwatch ballistics remain independent of this metadata. Hitscan and the current distal tracer originate from `AimOriginWorld` and use continuous aim. The old collision/impact line is disabled; authored collision particles are deferred.

### Latch-9

`RustlineLatch9MuzzleSetup` validates the generated Latch JSON and converts it Editor-side into a deterministic runtime cache at:

```text
Assets/Resources/Generated/Latch9MuzzleMetadata.asset
```

That cache and the two flash-sheet `.meta` files are intentionally ignored while the current Latch integration remains an Editor-installed harness. The setup rebuilds/validates them automatically in Edit Mode and before builds; no runtime JSON parsing or PNG inspection is performed. When persistent prefab/scene references to these generated subassets are introduced, their import metadata should become versioned rather than locally generated.

The same setup imports both approved `180x9` muzzle-flash sheets as twenty `9x9` sprites each (`10 variants x 2 frames`) with the canonical `0.5 px` left-edge / vertically centered pivot, `16 PPU`, Point filtering, no mipmaps, uncompressed production import, binary source transparency, and sRGB sampling.

`PlayerLatch9AimPresenter2D` exposes the exact rendered state, direction bucket, authored angle, displayed Body frame, and facing for `Idle`, `Run`, `Backpedal`, `Crouch`, and `Fall`. Jump/Land carry deliberately expose no muzzle-capable pose, and traversal states continue to release Latch presentation.

`Latch9MuzzleFlashPresenter2D` consumes that rendered pose plus the compact metadata and follows the established Longwatch two-rendered-frame flash contract. It owns two presentation banks:

- `Conventional` — the approved cyan Latch-9 flash;
- `Bouncing` — the approved violet/electric ricochet flash.

`Latch9MuzzleFlashShotModeBinder2D` synchronizes those banks with `WeaponShotMode2D`. Right mouse switches the Latch between `Conventional` and `Bouncing`; this is intentionally independent of `WeaponFireMode2D`, so the same input continues to cycle Semi/Automatic on Longwatch instead.

The Latch gameplay contract currently implemented is:

- delivery mode: **visible projectile**, currently `30 units/s` in both shot modes;
- projectile visual: `4 px` long by `1 px` wide;
- Conventional projectile color: canonical Neon Cyan (`palette 20`);
- Bouncing projectile color: canonical Violet (`palette 22`);
- conventional damage: **5**;
- bouncing initial damage: **4**;
- after bounce 1 / 2 / 3: **3 / 2 / 1**;
- maximum: **3 ricochets**;
- one total range budget across the full reflected path;
- receiver hits stop the projectile and receive the current stage damage;
- receiverless level/Ground geometry can reflect or stop the bouncing projectile but is never mutated/damaged, so the Latch does not destroy ground tiles;
- Conventional stops on its first valid collision;
- after the third Bouncing reflection, the projectile continues on the final segment until its next collision or range exhaustion, then disappears; production breakup/collision particles are deferred.

The projectile spawns from the exact muzzle point resolved from the currently rendered Latch pose. Its direction remains the exact `ContinuousAimDirection`; the discrete 10-degree authored bucket is used only to locate the visual muzzle and therefore never quantizes ballistics.

`PlayerWeaponController2D.ShotFired` represents launch-time presentation (including the muzzle flash). `ShotResolved` represents the actual end of the shot. For Longwatch those moments are effectively simultaneous because it remains hitscan; for Latch they are separated by projectile travel time, and damage is applied only when the projectile reaches a valid receiver.

The current Editor gameplay harness equips `Assets/Config/Weapons/Latch9.asset`, replaces Longwatch-specific muzzle/recoil presentation, installs the Latch projectile emitter, and re-enables the generic weapon controller after the Latch visual presenter has been installed. Ammo economy and weapon switching remain separate future systems.

## Tooling location

Generator and tests live under:

```text
Tools/WeaponMetadata/
```

See `Tools/WeaponMetadata/README.md` for commands and validation workflow.