# Windows Release Tilemap / Composite Collision Contract

This document is a **release-critical guardrail**. Read it before changing MovementLab course generation, Tilemap collision, CompositeCollider2D setup, scene rebuild logic, or startup execution order.

## Symptom

The recurring failure is specific and severe:

- Editor Play Mode appears correct.
- A Windows Release player spawns normally, then falls through the visible floor.
- The respawn system returns the player to the spawn point and the cycle repeats.

This has happened more than once after otherwise unrelated MovementLab work.

## What was proven by diagnostics

The original investigation isolated the fault with controlled A/B tests:

1. A temporary build-only Ground BoxCollider under spawn supported the player. This proved the player Rigidbody2D, CapsuleCollider2D, spawn position, Ground layer, and general Physics2D collision path were working.
2. Temporarily bypassing the Composite and using the individual TilemapCollider2D cells made the Release player stand and traverse the Tilemap. That introduced seam-related/phantom Land behavior, so it was diagnostic only and **not an acceptable fix**.
3. Restoring `TilemapCollider2D.compositeOperation = Merge` and explicitly executing:

   ```text
   TilemapCollider2D.ProcessTilemapChanges()
   CompositeCollider2D.GenerateGeometry()
   Physics2D.SyncTransforms()
   ```

   restored Release collision and removed the seam/phantom-Land behavior.

The failure is therefore in **runtime Composite geometry readiness/initialization**, not in player movement, ground probing, capsule dimensions, spawn position, or the Ground layer matrix.

## Why Awake-only is not enough

The first permanent repair ran the three-step regeneration once from `Awake` and initially passed human Windows Release testing.

After MovementLab later grew with crouch/wall/gun-range geometry, the Release-only fall-through returned even though:

- the initializer component was still present;
- `TilemapCollider2D` was still enabled and set to `Merge`;
- `CompositeCollider2D` was still enabled;
- the Editor tests still saw generated geometry.

That means the original repair was still timing-sensitive: in a Player build, the native Tilemap/Collider data can become fully ready later in startup than the initializer's early `Awake`.

The hardened contract therefore deliberately regenerates in multiple startup phases:

1. `Awake`
2. `Start`
3. first `FixedUpdate`
4. second `FixedUpdate`

`TilemapCompositeColliderInitializer2D` has execution order `-1000`, so its defensive FixedUpdate passes run before the normal player motor FixedUpdate and before the corresponding physics simulation consumes the geometry.

The extra startup calls are intentional, bounded, and cheap. **Do not "optimize" them back to Awake-only.**

## Non-negotiable collision setup

`Ground Collision - Hidden` must keep all of the following:

- GameObject layer: `Ground` = layer 6
- hidden/disabled `TilemapRenderer`
- static `Rigidbody2D`
- enabled `TilemapCollider2D`
- `TilemapCollider2D.compositeOperation = Merge`
- enabled `CompositeCollider2D`
- `CompositeCollider2D.geometryType = Polygons`
- enabled `TilemapCompositeColliderInitializer2D`

The collision tile remains an unsmoothed Grid collider tile with no sprite.

The Composite is required because the individual tile colliders created visible seam/ground-probe artifacts during the diagnostic bypass.

## Serialized Composite paths are not the source of truth

The committed MovementLab YAML may show empty serialized Composite path arrays. Do not treat that as evidence that the scene should work without runtime regeneration, and do not attempt to "fix" the problem by hand-editing serialized Composite paths.

Runtime generation through the initializer is the source of truth.

## Builder and test guardrails

`RustlineM1ASetup` must:

- recreate/repair the static Rigidbody2D + TilemapCollider2D Merge + polygon CompositeCollider2D contract;
- recreate the initializer if it is missing;
- re-enable required collision components if they were disabled;
- validate the complete contract.

PlayMode coverage must retain:

- `GroundCompositeGeometry_IsInitializedOnSceneLoad`;
- `Player_SpawnRemainsSupportedThroughCompositeStartupPasses`.

These Editor tests are useful regression gates, but they **do not replace a Windows Release smoke test** because the bug has repeatedly been Player-build-specific.

## Required Windows Release smoke test

Any change touching any of the following requires a Windows Release smoke test before the milestone is accepted:

- MovementLab Tilemap cells/course geometry;
- `RustlineM1ASetup` scene synchronization;
- TilemapCollider2D / CompositeCollider2D / Rigidbody2D configuration;
- `TilemapCompositeColliderInitializer2D`;
- scene startup/execution-order behavior involving physics.

Minimum smoke test:

1. Build normal Windows Release.
2. Close Unity.
3. Launch the player.
4. Confirm the player remains supported at spawn.
5. Walk across the real Tilemap floor.
6. Confirm no fall-through and no phantom Land events across seams.

## Forbidden "fixes" and regressions

Do not:

- remove or disable `TilemapCompositeColliderInitializer2D`;
- reduce it to an Awake-only regeneration;
- set `TilemapCollider2D.compositeOperation` to `None` as a permanent solution;
- disable the Composite to use individual tile colliders;
- replace the real Tilemap collision with diagnostic BoxColliders;
- alter player capsule, spawn, ground probe, gravity, or movement tuning to compensate for this failure;
- assume Editor Play Mode proves the Windows Release path is healthy.

If the Release player falls through again, investigate this contract first.

## Historical accepted movement invariants

This collision fix must not change the accepted movement contract, including:

- standing capsule `1.05 × 2.75`, offset `(0, 1.375)`;
- Backpedal `4 u/s`, exactly 4 authored frames at `7 fps`;
- Land presentation `0.22 s`;
- jump/coyote/buffer/gravity tuning;
- crouch and wall mechanics;
- native-pixel/Penumbra presentation.
