# Destructible Environment Contract

Rustline's destructible-environment system is intentionally constrained to the modular industrial structural Tilemap. The goal is controlled breaching, not a general-purpose terrain simulation.

## Material and weapon authority

The gameplay rule is:

- visible cells on `Industrial Surface - Visual` are the only cells initially eligible for structural breaching;
- `Ground Collision - Hidden` owns the authored structural collision;
- authored production architecture rendered as Floor, Wall, ServiceShaft, catwalk, machinery, support pillars, and similar sprites is not breachable;
- one valid Longwatch DMR hit destroys one eligible **16x16 px / 1x1 u visual structural cell** immediately;
- there is no per-tile HP, damaged intermediate state, crack progression, debris physics, collapse propagation, or structural-stability simulation in this pass.

`BreachableTilemap2D` lives on `Ground Collision - Hidden` and implements the existing `IWeaponHitReceiver2D` contract. The current hitscan path therefore requires no special-case firing code for structural damage: a hit on the CompositeCollider2D is delivered directly to the breaching authority.

The component holds an explicit whitelist of `WeaponDefinition2D` assets. Salvage Intake's deterministic setup currently authorizes exactly `Assets/Config/Weapons/LongwatchDMR.asset`. The Longwatch asset itself is not mutated with scene-specific breaching state, so rebuilding other weapon content cannot silently disable this contract.

Initial eligibility is deliberately strict: a cell must have **both** an `Industrial Surface - Visual` tile and an authored hidden collision tile when it is first breached. A cell that was authored with collision but no generic structural visual is therefore indestructible.

## Runtime breach authority

Visual absence alone never means "destroyed". `BreachableTilemap2D` keeps explicit runtime state for every cell it actually breaches.

A valid breach now always removes both representations of the structural cell immediately:

- the `Industrial Surface - Visual` tile is removed;
- the matching `Ground Collision - Hidden` tile is removed.

If that destruction would create a genuinely enclosed micro-aperture below the safe traversal threshold, movement protection is supplied by a **separate runtime-only `BoxCollider2D` safety seal**. The seal is not a Tilemap cell and is not structural material.

This separation is intentional:

- Tilemap presentation and Tilemap collision always agree about whether the structural tile still exists;
- safety seals exist only to prevent the player from entering invalid sub-body-width openings;
- safety seals use the same Ground layer as the authored floor so normal player collision/probes continue to see them;
- every safety-seal collider carries `WeaponRaycastPassthrough2D`, so weapon hitscan ignores that collider and continues to the next real hit along the ray;
- skipping a safety seal never skips the farther `CompositeCollider2D`, enemy, prop, or other collider because the seal is a separate Collider2D object.

A corner, exposed ledge, step, protrusion, platform edge, or any other run that opens directly to air never receives a safety seal.

Every hit reconciles all existing breached cells, not only the newest one. Destroying a neighboring tile can therefore invalidate an earlier seal; if one of its supporting ends becomes breached or exposed, that runtime-only collider is disabled and destroyed immediately.

Authored `visual absent + collision present` cells remain distinct from runtime breach state. Floor armor, production wall skins, catwalk collision, and similar architecture must never become destructible merely because their generic RuleTile visual was removed during authoring.

The runtime state is session-local. Reloading/rebuilding the scene restores its authored structure, which is the intended behavior for the current single-player vertical slice.

## Minimum physical aperture

A bounded visual hole does not become a player-traversable opening until its contiguous destroyed run is large enough to avoid trapping the player. Unbounded exposed geometry does not use protection at all.

### Horizontal floor / ceiling surfaces

Hits whose contact normal is predominantly vertical are classified as horizontal-surface breaches.

For a run that is still bounded by intact structural collision on both left and right ends:

- 1 destroyed cell = **16 px visual/structural hole + one runtime safety-seal collider**;
- 2 contiguous destroyed cells = **32 px real aperture, no safety seals**.

If either horizontal end is already open to air or becomes breached, the run is no longer a protected aperture and any existing safety seal is removed immediately. This prevents shots on platform corners and one-tile steps from creating invisible stair blocks.

### Vertical wall surfaces

Hits whose contact normal is predominantly horizontal are classified as vertical-surface breaches.

For a run that is still bounded by intact structural collision above and below:

- 1 or 2 contiguous destroyed cells vertically = runtime safety seals protect the non-traversable opening;
- 3 contiguous destroyed cells = **48 px real aperture, no safety seals**.

If either vertical end is exposed or becomes breached, the safety seals are removed immediately instead of creating floating invisible wall segments.

The 48 px threshold intentionally exceeds both the standing 44 px and crouched 38 px player heights. A 32 px-high enclosed slit must never become physical just because two wall cells were destroyed.

Each breached cell records the surface axis through which it was damaged. Horizontal and vertical runs are evaluated separately so, for example, two adjacent wall cells cannot accidentally invoke the two-cell floor threshold. A breached cell itself never counts as a solid support for another pending aperture; runtime safety seals are traversal aids, not structural boundaries.

## Weapon-raycast passthrough

`PlayerWeaponController2D` still performs its normal multi-hit physics query and chooses the nearest valid collider. The candidate filter now ignores only colliders carrying `WeaponRaycastPassthrough2D`.

This is deliberately narrower than excluding a layer or ignoring `Ground Collision - Hidden`:

- normal floor/wall/catwalk/architecture collision still stops shots normally;
- only traversal-only safety seals are transparent to hitscan;
- a shot through a protected 16 px visual hole can continue to a legitimate farther target or structural surface;
- player collision remains unchanged because movement physics does not apply the weapon-only candidate filter.

## Impact-to-cell resolution

`BreachableTilemap2D` samples the hit point **1/32 u / half a source pixel inward** along the collider normal before converting world position to a Tilemap cell. This avoids boundary ambiguity when a raycast lands exactly on a CompositeCollider2D edge.

Every valid first-time breach changes the hidden Tilemap collision immediately, so the runtime path performs:

`Tilemap.RefreshAllTiles -> TilemapCollider2D.ProcessTilemapChanges -> CompositeCollider2D.GenerateGeometry -> Physics2D.SyncTransforms`

Creating or removing runtime safety seals is reconciled before the final physics sync.

## Salvage Intake protected floor envelope

Salvage Intake protects the bottom of its safety floor so the player cannot tunnel out of the authored level envelope.

The safety floor spans `x=-28..33` and `y=-4..-1` in structural cells. `RustlineSalvageIntakeSafetyFloorArmorSetup` removes the visual RuleTiles from the two lowest rows only:

- protected rows: `y=-4..-3`;
- protected horizontal span: `x=-28..33`;
- hidden collision is preserved in every protected cell;
- the upper safety-floor rows remain structurally independent and are breachable wherever their generic visual RuleTiles are still present.

The protected rows are covered by the existing Floor family at native 16 PPU. The armor renderers contain no Collider2D. Because those protected cells start without `Industrial Surface - Visual`, the runtime breaching authority rejects them automatically.

## Retired micro-aperture fixture

The temporary authored one-tile diagnostic fixture has been retired. Human testing already established that a real **16 px physical opening can trap the player**, and the runtime safety-seal logic now owns that requirement directly.

`RustlineSalvageIntakeMicroGapTestSetup` is no longer part of the repository or **Apply Production Setup**. Salvage Intake therefore keeps its normal authored geometry, and protected 16 px apertures are created only by actual runtime Longwatch breaches when the surrounding topology qualifies for temporary protection.

This avoids permanently authored `visual absent + collision present` test cells being mistaken for gameplay content or indestructible invisible blocks.

## Production setup contract

`RustlineSalvageIntakeBreachableSurfaceSetup` runs from **Tools -> Rustline -> Apply Production Setup** after the environment's structural skins are finalized. It:

- finds `Industrial Surface - Visual` and `Ground Collision - Hidden`;
- adds exactly one `BreachableTilemap2D` to the hidden collision Tilemap;
- links the structural visual Tilemap;
- authorizes exactly the Longwatch DMR definition;
- validates that the existing TilemapCollider2D + CompositeCollider2D stack is preserved;
- does not alter gameplay geometry itself.

Runtime safety seals are created only during play after a qualifying breach and are never serialized into Salvage Intake.

## Human acceptance gate

Before this feature is considered frozen, runtime testing in Salvage Intake must verify:

- a Longwatch hit in the middle of a bounded floor span removes the real Tilemap collision but creates exactly one traversal safety seal for the 16 px aperture;
- the player can stand/walk over that protected 16 px opening without getting trapped;
- firing through that protected opening does **not** stop on the safety seal and can hit a legitimate farther collider;
- the second adjacent floor hit opens the complete 32 px run, removes the seal, and the player falls/passes cleanly;
- one or two vertically contiguous hits inside a bounded wall span remain physically blocked for the player while shots pass through their safety seals;
- the third contiguous vertical wall hit opens the complete 48 px run;
- shooting a platform corner, ledge, one-tile step, protrusion, or exposed edge removes both visual and matching structural collision without creating a safety seal;
- destroying a neighbor of an existing protected aperture can invalidate that protection and removes stale safety colliders immediately;
- Floor/Wall/ServiceShaft/catwalk/armor architecture remains indestructible and still blocks weapon hits normally;
- rebuilding production setup does not introduce any pre-authored invisible micro-aperture collider;
- RuleTile neighbors refresh cleanly around destroyed cells;
- repeated breaching does not produce CompositeCollider2D jitter, stale collision, stale runtime seal objects, or Release-only divergence;
- player movement, Wall Brace, Wall Kick, LedgeClimb, crouch, and Longwatch presentation remain unchanged.
