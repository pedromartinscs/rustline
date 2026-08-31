# Rustline

**Rustline** is a Unity 6 2D multiplayer extraction-shooter prototype focused on responsive platforming, 360° gunplay, loot, extraction, and server-authoritative networking.

The project is being built as a compact technical showcase: prove that movement and combat feel good first, then layer multiplayer, persistence, and extraction systems on top of a clean gameplay architecture.

## Core loop

**Deploy → Traverse → Fight → Loot → Extract**

## Goals

- Responsive 2D platformer movement
- 360° mouse/gamepad aiming
- Satisfying projectile and weapon systems
- PvE combat and lightweight enemy AI
- Loot, inventory, and extraction mechanics
- Multiplayer with server-authoritative gameplay
- Prediction/reconciliation where latency-sensitive systems require it
- Clean separation between gameplay simulation, presentation, networking, and persistence
- A small, polished vertical slice rather than a content-heavy game

## Technology

- Unity 6
- C#
- Unity 2D / Tilemaps
- Multiplayer stack: to be selected after the local movement/combat prototype is proven
- Backend/persistence: to be selected when the multiplayer milestone begins

## Art direction

Rustline uses an original pixel-art visual identity centered on a derelict orbital salvage facility: dark industrial metals, corrosion, exposed machinery, warning markings, and cold electronic accents.

Environment art uses a **16×16 modular tile grid**. The first structural atlas is specified as **128×96 px** with an 8×6 grid of 48 fixed slots; the first 16 slots cover the canonical N/E/S/W structural connectivity cases for Rule Tile-style selection.

All final pixel art uses the fixed **Rustline Canonical 28** palette plus transparency, nearest-neighbor sampling, and binary alpha.

All redistributable game artwork committed to this repository will be created specifically for Rustline or come from dependencies whose licenses explicitly permit repository redistribution. Third-party reference packs are not included.

See:

- [`docs/ART_DIRECTION.md`](docs/ART_DIRECTION.md) — visual specification and asset workflow
- [`docs/PALETTE.md`](docs/PALETTE.md) — canonical production palette
- [`docs/TILESET_SPEC.md`](docs/TILESET_SPEC.md) — structural atlas layout and connectivity contract

## Development approach

The prototype is intentionally milestone-driven:

1. **Movement** — responsive controller, jump feel, coyote time, buffering, acceleration, air control
2. **Gunplay** — 360° aim, weapon pivot, projectiles/hitscan, recoil, hit feedback
3. **Combat** — health, damage, enemies, death/restart
4. **Extraction loop** — loot, inventory, extraction objective
5. **Multiplayer** — authoritative combat/state, prediction/reconciliation, reconnect/error handling
6. **Polish** — presentation, audio, performance, automated tests, portfolio build

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the current milestone plan.

## Status

**M0 — Visual foundation in progress.**

The canonical player sprite and initial idle/run animation sheets are versioned, the Rustline Canonical 28 palette is locked, and the first modular industrial structural tile family is currently being authored before the Unity project is generated.

## License

Licensing will be finalized before the first public release. Code and original artwork may use separate licenses.
