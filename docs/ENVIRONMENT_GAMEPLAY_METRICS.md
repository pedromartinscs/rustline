# Rustline Environment Gameplay Metrics

This document defines the current level-geometry measurements that environment art and collision authoring must respect. It is a design/authoring contract, not a replacement for the release-critical collision setup in [`RELEASE_COLLISION.md`](RELEASE_COLLISION.md).

Rustline deliberately keeps **visual environment geometry separate from gameplay collision geometry**. Visual cracks, recesses, trim, damage, and silhouette variation may be narrower or more irregular than the playable collision envelope. Hidden collision should remain simple and traversal-safe.

## Canonical scale

- Production density: **16 source pixels per Unity unit**.
- Environment grid: **16×16 px = 1×1 Unity unit** per canonical structural tile.
- Player visual Body cell: **48×64 px**.
- Standing physical capsule: **1.05 × 2.75 units = 16.8 × 44 source pixels**, offset `(0, 1.375)`.
- Crouched physical capsule: **1.05 × 2.375 units = 16.8 × 38 source pixels**, offset `(0, 1.1875)`.
- The player Visual child is offset downward by **4 source pixels** relative to the physical root.

These are physical/controller facts. Level-design comfort margins may intentionally be larger than the exact collider dimensions.

## Minimum traversable horizontal gap

**Accepted level-design rule:** any horizontal opening intended to be physically traversable must be at least:

```text
24 source pixels = 1.5 Unity units
```

This applies to the narrowest horizontal cross-section of pits, shafts, slots, doorway-like openings, and gaps between collision surfaces that the player could attempt to enter.

The standing/crouched capsule is only **16.8 px** wide mathematically. Using that exact value as a level-design minimum leaves too little tolerance around capsule curvature, side contacts, incoming horizontal velocity, and pixel-scale collision geometry. A sub-24-pixel opening can allow the rounded lower capsule to begin entering while preventing the body from passing cleanly, producing rare wedge/stuck states.

Therefore:

- `0 px` opening: ordinary continuous collision surface.
- `>0 and <24 px`: **not a valid traversable gap**. Close/bridge it in gameplay collision.
- `>=24 px`: valid starting minimum for a traversable horizontal opening.

This is currently a **documented authoring rule only**. No automatic builder/test rejection is implemented yet.

### Visual cracks versus collision cracks

A visual asset may still show a narrow crack or damaged separation smaller than 24 px. If it is not intended as a route, the hidden collision must bridge across it.

```text
Visual art:       ███████   ███████
                         \ /
                          V

Gameplay collision:
                  █████████████████
```

Do not resize the player capsule merely to accommodate decorative micro-gaps.

## Vertical clearances

The exact physical thresholds are:

- standing capsule height: **44 px**;
- crouched capsule height: **38 px**.

MovementLab's precision crouch tunnel uses a deliberate **42 px** opening. It is a diagnostic proof case: crouch fits while standing does not. It should not be interpreted as a universal production corridor height.

For production environment work, choose deliberate clearances based on the intended posture and readability rather than placing ceilings accidentally close to the exact collider threshold.

## Wall Brace surface coverage

The authored Wall Brace pose requires continuous, aligned near-vertical `Ground` coverage across the following physical-root band:

```text
+14.5 px
+19.5 px
+24.5 px
+29.5 px
+34.5 px
+39.5 px
+44.5 px
```

All seven samples must hit an aligned wall plane within **1 source pixel** of one another. The authored wall plane is approximately `±9.5 px` from the player root; the unchanged standing half-width plus `WallCheckDistance` reaches approximately `9.6 px`.

Short, broken, or stepped surfaces that do not cover this authored contact band must not be designed as reliable Wall Brace anchors. See [`MOVEMENT.md`](MOVEMENT.md) for the runtime rule.

## LedgeClimb geometry

LedgeClimb is a committed traversal, not a generic auto-grab. Environment geometry intended to support it must provide:

- a near-vertical side wall;
- a valid upward-facing platform top;
- wall/top corner agreement within the current **0.25-unit / 4-px capture tolerance**;
- clear standing destination geometry;
- valid support below the destination.

The final standing root is derived from the platform geometry and standing capsule, so environment art should be built around the collision-authoritative ledge rather than moving collision to match decorative pixels after the fact.

## Collision-authoring discipline

When building production rooms:

1. establish gameplay collision and traversal intent first;
2. verify openings against these metrics;
3. layer structural visual tiles over that geometry;
4. add architectural detail, damage, cracks, props, foreground, and background independently;
5. keep decorative geometry from silently creating unintended physical routes or wedge traps.

The release-critical Tilemap/Composite pipeline remains unchanged. Read [`RELEASE_COLLISION.md`](RELEASE_COLLISION.md) before modifying generated collision, Composite setup, scene build processing, or Release validation.

## Future validation

A future environment-validation pass may automate selected rules from this document, especially the **24 px Minimum Traversable Gap**. Until such tooling is deliberately added, this document is the authority for manual level authoring and review.
