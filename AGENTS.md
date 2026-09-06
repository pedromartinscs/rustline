# Rustline coding-agent guardrails

## Release-critical Tilemap / Composite collision

Before changing MovementLab course generation, Tilemap collision, `RustlineM1ASetup`, or build processing, read `docs/RELEASE_COLLISION.md`.

A recurring Windows Release-only bug makes the player fall through the real Tilemap while Editor Play Mode remains correct. The original fix was human-verified in commit `047c49e7`; do not replace it with a new theory without comparing repository history.

Non-negotiable contract:

- `Ground Collision - Hidden` stays on Ground layer 6 with a static `Rigidbody2D`.
- `TilemapCollider2D` stays enabled with `compositeOperation = Merge`.
- `CompositeCollider2D` stays enabled with polygon geometry.
- `TilemapCompositeColliderInitializer2D.Awake` keeps `ProcessTilemapChanges -> GenerateGeometry -> Physics2D.SyncTransforms`.
- `RustlineM1ASetup` must bake and validate non-empty Composite geometry after collision Tilemap synchronization.
- `ReleaseCollisionBuildGuard` must process the actual Player-build scene and fail the build if Composite paths/points are empty.
- Do not permanently bypass the Composite, substitute BoxColliders, or change player movement/collider/spawn to mask the bug.
- Relevant changes require a Windows Release smoke test; Editor tests alone are insufficient.

If a refactor conflicts with this contract, preserve the contract and document why.
