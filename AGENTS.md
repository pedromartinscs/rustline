# Rustline coding-agent guardrails

## Release-critical Tilemap / Composite collision

Before changing MovementLab course generation, physics startup, Tilemap collision, or `RustlineM1ASetup`, read `docs/RELEASE_COLLISION.md`.

A recurring Windows Release-only bug makes the player fall through the real Tilemap while Editor Play Mode remains correct. The accepted collision contract is non-negotiable:

- `Ground Collision - Hidden` stays on Ground layer 6.
- It uses a static `Rigidbody2D`.
- `TilemapCollider2D` stays enabled with `compositeOperation = Merge`.
- `CompositeCollider2D` stays enabled with polygon geometry.
- `TilemapCompositeColliderInitializer2D` stays enabled.
- The initializer must keep the explicit sequence `ProcessTilemapChanges -> GenerateGeometry -> Physics2D.SyncTransforms`.
- The initializer must keep its multi-stage startup regeneration in Awake, Start, and the first two FixedUpdate ticks. Do not simplify it back to Awake-only.
- Do not permanently bypass the Composite, substitute BoxColliders, or change player movement/collider/spawn to mask this bug.
- Builder synchronization and validation must preserve/repair this contract.
- Any relevant change requires a Windows Release smoke test; Editor tests alone are insufficient.

If a proposed refactor conflicts with this contract, preserve the contract and document why the refactor was constrained.
