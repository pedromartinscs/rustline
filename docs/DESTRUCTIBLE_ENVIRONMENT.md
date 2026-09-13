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

## Runtime breach authority

Visual absence alone never means "destroyed". `BreachableTilemap2D` keeps explicit runtime state for every cell it actually breaches. A breached cell can be in one of two physical outcomes:

1. **Temporary bounded safety seal** — visual tile removed, collision temporarily retained because the destroyed run is a genuinely enclosed micro-aperture below the safe traversal threshold;
2. **Open breach** — visual tile removed and collision removed.

The important constraint is that retained collision is **not** a generic third state for every first hit. It is only legal when the destroyed run is physically bounded by solid collision at both ends of its relevant axis. A corner, exposed ledge, step, protrusion, platform edge, or any other run that opens directly to air cannot keep ghost collision after its visual disappears.

Every hit reconciles all still-pending breached cells, not only the newest one. Destroying a neighboring tile can therefore invalidate an earlier safety seal; if one of its supporting ends becomes breached or exposed, that old retained collision is removed immediately.

Authored `visual absent + collision present` cells remain distinct from runtime breach state. Floor armor, production wall skins, catwalk collision, and similar architecture must never become destructible merely because their generic RuleTile visual was removed during authoring.

The runtime state is session-local. Reloading/rebuilding the scene restores its authored structure, which is the intended behavior for the current single-player vertical slice.

## Minimum physical aperture

A bounded visual hole does not become a physical opening until its contiguous destroyed run is large enough to avoid trapping the player. Unbounded exposed geometry does not use this protection and loses its collision immediately.

### Horizontal floor / ceiling surfaces

Hits whose contact normal is predominantly vertical are classified as horizontal-surface breaches.

For a run that is still bounded by solid collision on both left and right ends:

- 1 destroyed cell = **16 px visual hole, temporary collision retained**;
- 2 contiguous destroyed cells = **32 px aperture, collision released for the run**.

If either horizontal end is already open to air or becomes breached, the run is no longer a protected aperture and any retained collision is released immediately. This is what prevents shots on platform corners and one-tile steps from creating invisible stair blocks.

### Vertical wall surfaces

Hits whose contact normal is predominantly horizontal are classified as vertical-surface breaches.

For a run that is still bounded by solid collision above and below:

- 1 or 2 contiguous destroyed cells vertically = temporary collision retained;
- 3 contiguous destroyed cells = **48 px vertical aperture, collision released for the run**.

If either vertical end is exposed or becomes breached, the retained collision is released immediately instead of creating a floating invisible wall segment.

The 48 px threshold intentionally exceeds both the standing 44 px and crouched 38 px player heights. A 32 px-high enclosed slit must never become physical just because two wall cells were destroyed.

Each breached cell records the surface axis through which it was damaged. Horizontal and vertical runs are evaluated separately so, for example, two adjacent wall cells cannot accidentally invoke the two-cell floor threshold. A breached cell itself never counts as a solid support for another pending aperture, even while its temporary collision is still present.

## Impact-to-cell resolution

`BreachableTilemap2D` samples the hit point **1/32 u / half a source pixel inward** along the collider normal before converting world position to a Tilemap cell. This avoids boundary ambiguity when a raycast lands exactly on a CompositeCollider2D edge.

Whenever reconciliation removes one or more hidden collision cells, the runtime path performs:

`Tilemap.RefreshAllTiles -> TilemapCollider2D.ProcessTilemapChanges -> CompositeCollider2D.GenerateGeometry -> Physics2D.SyncTransforms`

Hits whose collision remains as a valid bounded safety seal do not unnecessarily regenerate collision geometry.

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

Human testing proved that a real **16 px physical opening can trap the player**. Salvage Intake therefore keeps the earlier diagnostic location but models the protected micro-aperture state deliberately.

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

- a Longwatch hit in the middle of a bounded floor span can leave exactly one protected 16 px visual aperture;
- the second adjacent floor hit opens the complete 32 px run and the player falls/passes cleanly;
- one or two vertically contiguous hits inside a bounded wall span remain physically blocked;
- the third contiguous vertical wall hit opens the complete 48 px run;
- shooting a platform corner, ledge, one-tile step, protrusion, or exposed edge removes both visual and matching collision rather than leaving a ghost stair/block;
- destroying a neighbor of an existing protected aperture can invalidate that protection and removes stale retained collision immediately;
- two visually absent runtime-breached cells cannot remain side-by-side as stale ghost collision after their enclosing topology has opened;
- Floor/Wall/ServiceShaft/catwalk/armor architecture remains indestructible;
- the authored protected diagnostic micro-aperture remains physically solid despite being visually empty;
- RuleTile neighbors refresh cleanly around destroyed cells;
- repeated breaching does not produce CompositeCollider2D jitter, stale collision, or Release-only divergence;
- player movement, Wall Brace, Wall Kick, LedgeClimb, crouch, and Longwatch presentation remain unchanged.
