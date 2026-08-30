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

Environment art is planned around a 16×16 modular tile grid, with larger character and enemy sprites where readability benefits from the extra resolution.

All redistributable game artwork committed to this repository will be created specifically for Rustline or come from dependencies whose licenses explicitly permit repository redistribution. Third-party reference packs are not included.

See [`docs/ART_DIRECTION.md`](docs/ART_DIRECTION.md) for the working visual specification.

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

**Pre-production / visual foundation.**

The repository is being established before the Unity project itself so art direction, architecture decisions, and generated original assets can be versioned from the beginning.

## License

Licensing will be finalized before the first public release. Code and original artwork may use separate licenses.
