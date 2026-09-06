# Windows Release Tilemap / Composite Collision Contract

This document is a **release-critical guardrail**. Read it before changing MovementLab course generation, Tilemap collision, CompositeCollider2D setup, scene rebuild logic, or build processing.

## Symptom

The recurring failure is specific:

- Editor Play Mode is correct.
- A Windows Release player spawns and falls through the visible floor.
- The respawn system returns it to spawn and the cycle repeats.

## What the original diagnostics actually proved

The 2026-09 investigation used controlled commits, not guesses:

1. **`925a7153` — temporary Ground BoxCollider under spawn.** Release supported the player. This isolated the failure away from the player Rigidbody2D, CapsuleCollider2D, spawn position, Ground layer, and general Physics2D contact path.
2. **`36503b75` — bypass Composite.** Setting the TilemapCollider2D to non-composited collision made the Release player stand/traverse the Tilemap. It also reintroduced tile-seam / phantom-Land behavior, so this was diagnostic only.
3. **`534626af` — restore Merge and force geometry.** The diagnostic explicitly executed:

   ```text
   TilemapCollider2D.ProcessTilemapChanges()
   CompositeCollider2D.GenerateGeometry()
   Physics2D.SyncTransforms()
   ```

   with the Composite restored.
4. **`047c49e7` — permanent repair.** The exact three-step sequence moved into `TilemapCompositeColliderInitializer2D.Awake`, the component was attached to `Ground Collision - Hidden`, and the user manually verified the Windows Release worked.

That original fix is still valid and is deliberately preserved.

## What changed before the regression returned

Repository history is important here because it rules out a false explanation.

The runtime initializer from `047c49e7` remained byte-for-byte unchanged through the crouch hardening and the first gunplay commit. The collision component setup also remained `TilemapCollider2D Merge + CompositeCollider2D`.

The material collision-data change was the gunplay range expansion:

- known pre-range collision Tilemap: **548 tiles**, bounds **142 × 11**;
- post-range collision Tilemap: **793 tiles**, bounds **202 × 13**.

The builder updated those Tilemap cells with `SetTile` / `RefreshAllTiles` and saved the scene, but it did not explicitly run the TilemapCollider2D/Composite generation path before save/export.

Unity documents that TilemapCollider2D normally processes Tilemap collider changes during **LateUpdate**; `ProcessTilemapChanges()` is the API for immediate processing. Therefore Editor Play Mode can naturally hide a stale-build-state problem by giving the Tilemap collider its normal update cycle before inspection. The repository alone cannot prove Unity's internal Player exporter behavior, so do not describe that internal mechanism as established fact. What *is* established is that build output was allowed to depend on unvalidated collider-generation timing after authored Tilemap changes. citeturn428900search9turn228858search1

## Current deterministic contract

Rustline now protects both sides of the pipeline.

### Runtime safety net — keep the proven 047c49e behavior

`TilemapCompositeColliderInitializer2D.Awake` must keep:

```text
ProcessTilemapChanges()
GenerateGeometry()
Physics2D.SyncTransforms()
```

Do not replace the Composite with per-tile collision or BoxColliders.

### Editor builder bake

After creating or synchronizing the collision Tilemap, `RustlineM1ASetup` must:

1. `RefreshAllTiles()`;
2. execute the runtime initializer's geometry sequence immediately;
3. verify `CompositeCollider2D.pathCount > 0`;
4. verify `CompositeCollider2D.pointCount > 0`.

The builder/validator must reject empty geometry.

### Player-build bake and fail-closed guard

`ReleaseCollisionBuildGuard : IProcessSceneWithReport` runs on the actual Scene copy Unity is processing for a Player build. Unity's build API explicitly provides this per-scene callback during Player builds. citeturn415328search0

For every `TilemapCompositeColliderInitializer2D` it:

1. validates the Merge/Composite component contract;
2. refreshes Tile data;
3. executes the same immediate collider/composite generation;
4. **aborts the build** with `BuildFailedException` if path/point counts are zero.

A broken collision scene should therefore fail the build instead of silently producing another floor-through Release.

## Non-negotiable collision setup

`Ground Collision - Hidden` must keep:

- GameObject layer `Ground` = layer 6;
- disabled visual `TilemapRenderer`;
- static `Rigidbody2D`;
- enabled `TilemapCollider2D`;
- `TilemapCollider2D.compositeOperation = Merge`;
- enabled `CompositeCollider2D`;
- `CompositeCollider2D.geometryType = Polygons`;
- enabled `TilemapCompositeColliderInitializer2D`;
- Grid collision tile with no sprite.

The Composite remains required because the controlled no-Composite diagnostic reintroduced seam/phantom-Land behavior.

## Tests and build validation

PlayMode coverage must retain:

- `GroundCompositeGeometry_IsInitializedOnSceneLoad`;
- `Player_SpawnRemainsSupportedAfterCompositeInitialization`.

Builder validation must inspect actual generated `pathCount` / `pointCount`, not merely component presence.

The build guard is mandatory. Editor PlayMode tests alone are insufficient because this failure is specifically build-sensitive.

## Required Windows Release smoke test

Any change touching MovementLab course cells, Tilemap synchronization, collision components, the initializer, or the build guard requires:

1. normal Windows Release build;
2. close Unity;
3. launch the Player;
4. remain supported at spawn;
5. traverse the real Tilemap;
6. verify no fall-through and no phantom Land at seams.

## Forbidden responses to this regression

Do not:

- alter player capsule, gravity, ground probe, spawn, or movement tuning;
- permanently set `compositeOperation = None`;
- disable the Composite;
- replace the Tilemap with diagnostic BoxColliders;
- remove the runtime `047c49e` initialization sequence;
- remove the build-time bake/fail-closed guard;
- claim Editor Play Mode proves the Windows Release path;
- invent a new root cause without first comparing against `047c49e`, `93881eaf`, and the later scene/builder diff.

## Accepted movement invariants remain unrelated

This collision contract does not change:

- standing capsule `1.05 × 2.75`, offset `(0, 1.375)`;
- Backpedal `4 u/s`, exactly 4 authored frames at `7 fps`;
- Land `0.22 s`;
- jump/coyote/buffer/gravity;
- crouch/wall mechanics;
- native-pixel/Penumbra presentation.
