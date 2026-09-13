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

## Future breaching constraints

The first runtime implementation should remain deliberately small:

1. only the industrial structural RuleTile layer is eligible;
2. a breaching shot resolves exactly one impacted cell;
3. removing a cell removes both its structural visual and its matching hidden collision cell;
4. the CompositeCollider2D is refreshed/regenerated after the edit;
5. authored production sprites remain untouched;
6. no debris physics, structural-stability simulation, collapse propagation, or tile HP is required.

A separate movement-safety rule is still required before enabling the feature: a one-cell / 16 px opening must not be able to trap the player. That problem is intentionally handled after the level envelope protection and before runtime breaching ships.
