# Destructible Environment Contract

Rustline's destructible-environment system is intentionally constrained to the modular industrial structural Tilemap. The goal is controlled breaching, not a general-purpose terrain simulation.

## Material and weapon authority

The gameplay rule is:

- visible cells on `Industrial Surface - Visual` are the only cells initially eligible for structural breaching;
- the matching `Ground Collision - Hidden` Tilemap remains the sole gameplay-collision authority;
- authored production architecture rendered as Floor, Wall, ServiceShaft, catwalk, machinery, support pillars, and similar sprites is not breachable;
- one valid Longwatch DMR hit destroys one eligible **16x16 px / 1x1 u visual structural cell** immediately;
- there is no per-tile HP, damaged intermediate state, crack progression, debris physics, collapse propagation, or structural-stability simulation in this pass.

`BreachableTilemap2D` lives on `Ground Collision - Hidden` and implements the existing `IWeaponHitReceiver2D` contract. The current hitscan path therefore requires no special-case firing code: a hit on the CompositeCollider2D is delivered directly to the breaching authority.

The component holds an explicit whitelist of `WeaponDefinition2D` assets. Salvage Intake's deterministic setup currently authorizes exactly `Assets/Config/Weapons/LongwatchDMR.asset`. The Longwatch asset itself is not mutated with scene-specific breaching state, so rebuilding other weapon content cannot silently disable this contract.

Initial eligibility is deliberately strict: a cell must have **both** an `Industrial Surface - Visual` tile and a hidden collision tile when it is first breached. A cell that was authored with collision but no generic structural visual is therefore indestructible.

## Runtime three-state authority

Visual absence alone never means "destroyed". `BreachableTilemap2D` keeps explicit runtime state for every cell it actually breaches:

1. **Intact** — visual tile present, collision present, no runtime breach state;
2. **Protected micro-aperture** — visual tile removed, collision still present, runtime breach state recorded;
3. **Open breach** — visual tile removed, collision removed, runtime breach state records that collision has been released.

This distinction is critical because Rustline deliberately contains authored cells with no RuleTile visual but preserved collision. Floor armor, production wall skins, catwalk collision, and similar architecture must never become destructible merely because their generic visual was removed during authoring.

The runtime state is session-local. Reloading/rebuilding the scene restores its authored structure, which is the intended behavior for the current single-player vertical slice.

## Minimum physical aperture

A visual hole does not become a physical opening until its contiguous destroyed run is large enough to avoid trapping the player.

### Horizontal floor / ceiling surfaces

Hits whose contact normal is predominantly vertical are classified as horizontal-surface breaches.

- 1 destroyed cell = **16 px visual hole, collision retained**;
- 2 contiguous destroyed cells = **32 px aperture, collision released for the run**;
- additional contiguous destroyed cells join the same open run.

This prevents the known 16 px floor-slot trap without touching the frozen player collider or movement code.

### Vertical wall surfaces

Hits whose contact normal is predominantly horizontal are classified as vertical-surface breaches.

- 1 or 2 contiguous destroyed cells vertically = collision retained;
- 3 contiguous destroyed cells = **48 px vertical aperture, collision released for the run**;
- additional contiguous destroyed cells join the same open run.

The 48 px threshold intentionally exceeds both the standing 44 px and crouched 38 px player heights. A 32 px-high slit must never become physical just because two wall cells were destroyed.

Each breached cell records the surface axis through which it was damaged. Horizontal and vertical runs are evaluated separately so, for example, two adjacent wall cells cannot accidentally invoke the two-cell floor threshold.

## Impact-to-cell resolution

`BreachableTilemap2D` samples the hit point **1/32 u / half a source pixel inward** along the collider normal before converting world position to a Tilemap cell. This avoids boundary ambiguity when a raycast lands exactly on a CompositeCollider2D edge.

After a qualified aperture removes collision cells, the runtime path performs:

`Tilemap.RefreshAllTiles -> TilemapCollider2D.ProcessTilemapChanges -> CompositeCollider2D.GenerateGeometry -> Physics2D.SyncTransforms`

Visual-only first hits do not unnecessarily regenerate collision geometry.

## Salvage Intake protected floor envelope

Salvage Intake protects the bottom of its safety floor so the player cannot tunnel out of the authored level envelope.

The safety floor spans `x=-28..33` and `y=-4..-1` in structural cells. `RustlineSalvageIntakeSafetyFloorArmorSetup` removes the visual RuleTiles from the two lowest rows only:

- protected rows: `y=-4..-3`;
- protected horizontal span: `x=-28..33`;
- hidden collision is preserved in every protected cell;
- the upper safety-floor rows remain structurally independent and are breachable wherever their generic visual RuleTiles are still present.

The protected rows are covered by the existing 48x32 px Floor family at native 16 PPU:

- `floor_edge_left.png`: left cap;
- `floor_edge_mid_a.png`: tiled continuous middle;
- `floor_edge_right.png`: right cap.

Together they cover exactly `x=-28..34` and `y=-4..-2` with no Transform scaling. The armor renderers contain no Collider2D; `Ground Collision - Hidden` remains authoritative. Because those protected cells start without `Industrial Surface - Visual`, the runtime breaching authority rejects them automatically.

## Protected one-tile diagnostic specimen

Human testing proved that a real **16 px physical opening can trap the player**. Salvage Intake therefore keeps the earlier diagnostic location but now models the corrected first-shot state.

The base graybox naturally leaves a two-cell gap at `x=-4..-3` between the raised left approach and the central machinery plinth. `RustlineSalvageIntakeMicroGapTestSetup` fills the west half and leaves the `x=-3, y=0..1` column:

- visually empty on `Industrial Surface - Visual`;
- physically solid on `Ground Collision - Hidden`;
- one cell / **16 px** wide;
- explicitly outside `BreachableTilemap2D`'s runtime state because it is authored by the diagnostic setup rather than destroyed during play.

This specimen verifies two invariants at once: the player can no longer fall into the invalid slot, and authored `visual absent + collision present` geometry is not mistaken for runtime-destroyed material.

The test is temporary diagnostic geometry and may be retired after the runtime breaching behavior is human-approved.

## Production setup contract

`RustlineSalvageIntakeBreachableSurfaceSetup` runs from **Tools -> Rustline -> Apply Production Setup** after the environment's structural skins are finalized. It:

- finds `Industrial Surface - Visual` and `Ground Collision - Hidden`;
- adds exactly one `BreachableTilemap2D` to the hidden collision Tilemap;
- links the structural visual Tilemap;
- authorizes exactly the Longwatch DMR definition;
- validates that the existing TilemapCollider2D + CompositeCollider2D stack is preserved;
- does not alter gameplay geometry itself.

## Human acceptance gate

Before this feature is considered frozen, runtime testing in Salvage Intake must verify:

- a single Longwatch hit removes exactly one eligible visual tile but does **not** open a 16 px floor hole;
- the second adjacent floor hit opens the complete 32 px run and the player falls/passes cleanly;
- one or two vertically contiguous wall hits remain physically blocked;
- the third contiguous vertical wall hit opens the complete 48 px run;
- Floor/Wall/ServiceShaft/catwalk/armor architecture remains indestructible;
- the protected diagnostic micro-aperture remains physically solid despite being visually empty;
- RuleTile neighbors refresh cleanly around destroyed cells;
- repeated breaching does not produce CompositeCollider2D jitter, stale collision, or Release-only divergence;
- player movement, Wall Brace, Wall Kick, LedgeClimb, crouch, and Longwatch presentation remain unchanged.
