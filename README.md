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
- [`docs/MOVEMENT.md`](docs/MOVEMENT.md) — M1A controller architecture, controls, and initial tuning

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

**M1 — Core movement, combat crouch, and wall interaction implemented.**

The accepted M0 pixel-art pipeline and `Assets/Scenes/ArtShowcase.unity` remain intact. `Assets/Scenes/MovementLab.unity` provides a playable single-player course for the accepted core movement plus a clearance-safe 3 units/s combat crouch and controlled wall brace/kick. Its real Tilemap + Composite collision course now includes a low tunnel and wall shaft. Crouch and wall presentation intentionally use existing animation fallbacks until Pedro's authored art is ready; gun firing remains unimplemented.

## License

Licensing will be finalized before the first public release. Code and original artwork may use separate licenses.
