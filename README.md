# Rustline

**Rustline** is a Unity 6 2D single-player extraction-shooter prototype focused on responsive platforming, 360° gunplay, PvE combat, loot, and extraction.

The project is being built as a compact technical showcase: prove that movement and combat feel good first, then build a small, polished extraction loop on top of a clean gameplay architecture.

## Core loop

**Deploy → Traverse → Fight → Loot → Extract**

## Goals

- Responsive 2D platformer movement
- 360° mouse/gamepad aiming
- Satisfying projectile and weapon systems
- PvE combat and lightweight enemy AI
- Loot, inventory, and extraction mechanics
- Clean separation between gameplay simulation, presentation, input, and content/configuration
- A small, polished vertical slice rather than a content-heavy game

## Scope

Rustline is currently a **single-player project**.

Multiplayer, networking, server-authoritative simulation, prediction/reconciliation, matchmaking, network persistence, and multiplayer backend architecture are intentionally outside the project scope unless this document is explicitly changed in the future.

## Technology

- Unity 6
- C#
- Universal Render Pipeline with the 2D Renderer
- Unity 2D / Tilemaps
- Unity Input System

## Art direction

Rustline uses an original pixel-art visual identity centered on a derelict orbital salvage facility: dark industrial metals, corrosion, exposed machinery, warning markings, and cold electronic accents.

Environment art uses a **16×16 modular tile grid**. The first structural atlas is specified as **128×96 px** with an 8×6 grid of 48 fixed slots; the first 16 slots cover the canonical N/E/S/W structural connectivity cases for Rule Tile-style selection.

All final pixel art uses the fixed **Rustline Canonical 28** palette plus transparency, nearest-neighbor sampling, and binary alpha.

All redistributable game artwork committed to this repository will be created specifically for Rustline or come from dependencies whose licenses explicitly permit repository redistribution. Third-party reference packs are not included.

See:

- [`docs/ART_DIRECTION.md`](docs/ART_DIRECTION.md) — visual specification and asset workflow
- [`docs/PALETTE.md`](docs/PALETTE.md) — canonical production palette
- [`docs/TILESET_SPEC.md`](docs/TILESET_SPEC.md) — structural atlas layout and connectivity contract
- [`docs/ENVIRONMENT_GAMEPLAY_METRICS.md`](docs/ENVIRONMENT_GAMEPLAY_METRICS.md) — traversal-safe environment/collision measurements
- [`docs/MOVEMENT.md`](docs/MOVEMENT.md) — controller architecture, controls, and movement tuning
- [`docs/RELEASE_COLLISION.md`](docs/RELEASE_COLLISION.md) — release-critical Tilemap/Composite startup contract and regression history

## Development approach

The prototype is intentionally milestone-driven:

1. **Visual foundation** — import conventions, player animation integration, structural tiles, visual showcase
2. **Movement** — responsive controller, jump feel, coyote time, buffering, acceleration, air control
3. **Gunplay** — 360° aim, weapon pivot, projectiles/hitscan, recoil, hit feedback
4. **Combat** — health, damage, enemies, death/restart
5. **Extraction loop** — loot, inventory, extraction objective
6. **Polish** — presentation, audio, performance, automated tests, portfolio build

See [`docs/ROADMAP.md`](docs/ROADMAP.md) for the current milestone plan.

## Status

**The current player movement set, first-weapon Longwatch presentation/gunplay slice, and the M3A repeatable combat prototype are implemented.**

`Assets/Scenes/MovementLab.unity` contains grounded forward/backpedal movement, combat crouch, Wall Brace / Wall Kick, committed six-frame LedgeClimb, the release-hardened Tilemap + Composite collision path, native-pixel presentation, Longwatch DMR continuous-aim hitscan, and a killable patrol enemy that resets in place. The Longwatch has authored aim-capable Idle, Run, Backpedal, Crouch, and Fall presentation plus non-firing three-frame Jump and two-frame Land carry. Wall Brace and LedgeClimb intentionally render no Longwatch carry; their traversal poses use the unarmed overlay and block firing. The accepted M0 pixel-art pipeline and `Assets/Scenes/ArtShowcase.unity` remain intact.

The next production focus is the **environment itself**: expand the modular structural family and build a first polished playable environment slice around the now-stable movement/collision metrics before multiplying additional gameplay content.

## License

Licensing will be finalized before the first public release. Code and original artwork may use separate licenses.
