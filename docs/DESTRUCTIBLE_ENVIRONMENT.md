# Destructible Environment Contract

Rustline's destructible-environment work is intentionally constrained to the modular industrial structural Tilemap. The goal is breaching, not a general-purpose terrain simulation.

## Material rule

The intended gameplay rule is:

- visible cells on `Industrial Surface - Visual` are breachable structural material;
- matching cells on `Ground Collision - Hidden` are removed when a valid breaching shot destroys that structure;
- production architecture rendered as authored sprites (Floor, Wall, ServiceShaft, catwalk, machinery, support pillars, etc.) is not breachable;
- no per-tile health or damaged intermediate state is planned for the first pass;
- one valid Longwatch DMR hit is intended to destroy one breachable 16x16 px / 1x1 u structural cell.

This keeps presentation and gameplay authority aligned with the existing production pipeline: when generic RuleTile visuals are replaced by production architecture, the preserved collision automatically belongs to an indestructible structure rather than to breachable material.

## Salvage Intake protected floor envelope

Before runtime breaching is enabled, Salvage Intake protects the bottom of its safety floor so the player cannot tunnel out of the authored level envelope.

The safety floor spans `x=-28..33` and `y=-4..-1` in structural cells. `RustlineSalvageIntakeSafetyFloorArmorSetup` removes the visual RuleTiles from the two lowest rows only:

- protected rows: `y=-4..-3`;
- protected horizontal span: `x=-28..33`;
- hidden collision is preserved in every protected cell;
- the upper safety-floor rows remain structurally independent and may later remain breachable wherever their visual RuleTiles are still present.

The protected rows are covered by the existing 48x32 px Floor family at native 16 PPU:

- `floor_edge_left.png`: 3 u left cap;
- `floor_edge_mid_a.png`: tiled continuously across the 56 u middle span;
- `floor_edge_right.png`: 3 u right cap.

Together they cover exactly `x=-28..34` and `y=-4..-2` with no Transform scaling and no visual overhang beyond the collision envelope. The armor renderers contain no Collider2D; `Ground Collision - Hidden` remains authoritative.

The managed scene root is:

`Art Dressing - Preserve/Safety Floor Armor v0 - Managed`

This pass is part of **Tools -> Rustline -> Apply Production Setup** and runs after the existing lower-bay floor dressing.

## One-tile micro-gap traversal test

Before runtime breaching is implemented, Salvage Intake carries a temporary deterministic stress test for the smallest possible structural opening.

The base graybox naturally leaves a two-cell gap at `x=-4..-3` between the raised left approach and the central machinery plinth. `RustlineSalvageIntakeMicroGapTestSetup` fills only `x=-4`, leaving exactly one empty structural column at:

- gap column: `x=-3`;
- empty vertical cells: `y=0..1`;
- width: **1 u / 16 px**;
- depth from the raised surface to the preserved base floor: **2 u / 32 px**;
- the protected floor collision at `(-3,-1)` remains present beneath the slot.

Both `Industrial Surface - Visual` and `Ground Collision - Hidden` use the same one-cell opening, and the CompositeCollider2D is explicitly regenerated after the mutation. This makes the specimen representative of the collision topology future runtime breaching will create rather than a presentation-only mockup.

This gap is diagnostic, not an accepted minimum traversal opening. Human testing should cover walking slowly across it, running across it, stopping directly over it, crouching over it, jumping onto it, and falling onto it from above. The result determines whether a separate micro-gap bridging rule is necessary before Longwatch breaching is enabled.

The test is part of **Tools -> Rustline -> Apply Production Setup** while the destructible-environment behavior is being established. Once the one-cell safety policy is decided, the diagnostic geometry may be removed or repurposed.

## Future breaching constraints

The first runtime implementation should remain deliberately small:

1. only the industrial structural RuleTile layer is eligible;
2. a breaching shot resolves exactly one impacted cell;
3. removing a cell removes both its structural visual and its matching hidden collision cell;
4. the CompositeCollider2D is refreshed/regenerated after the edit;
5. authored production sprites remain untouched;
6. no debris physics, structural-stability simulation, collapse propagation, or tile HP is required.

A separate movement-safety rule is still required before enabling the feature: a one-cell / 16 px opening must not be able to trap the player. The Salvage Intake micro-gap specimen above is the current acceptance test for that requirement.
