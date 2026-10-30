# Rustline

**Rustline** is a Unity 6 2D single-player industrial sci-fi combat-and-extraction prototype focused on responsive platforming, mouse-aimed gunplay, PvE encounters and a compact playable route. Loot and inventory remain planned, not implemented.

The project is being built as a compact technical showcase: prove that movement and combat feel good first, then build a small, polished extraction loop on top of a clean gameplay architecture.

## Core loop

**Current playable loop:** Deploy → Traverse → Fight → Extract  
**Planned extension:** Loot/pickup and a small inventory/resource layer.

## Goals

- Responsive 2D platformer movement
- 360° mouse aiming; complete gamepad combat/aim support remains to be verified
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

All redistributable game artwork committed to this repository will be created specifically for Rustline or come from dependencies whose licenses explicitly permit repository redistribution. Third-party reference packs are not included. The same provenance and licensing standard applies to future audio assets.

See:

- [`docs/ART_DIRECTION.md`](docs/ART_DIRECTION.md) — visual specification and asset workflow
- [`docs/PALETTE.md`](docs/PALETTE.md) — canonical production palette
- [`docs/TILESET_SPEC.md`](docs/TILESET_SPEC.md) — structural atlas layout and connectivity contract
- [`docs/ENVIRONMENT_GAMEPLAY_METRICS.md`](docs/ENVIRONMENT_GAMEPLAY_METRICS.md) — traversal-safe environment/collision measurements
- [`docs/MOVEMENT.md`](docs/MOVEMENT.md) — controller architecture, controls, and movement tuning
- [`docs/RELEASE_COLLISION.md`](docs/RELEASE_COLLISION.md) — release-critical Tilemap/Composite startup contract and regression history
- [`docs/AUDIO_DIRECTION.md`](docs/AUDIO_DIRECTION.md) — planned sound direction, source candidates and asset-license intake checklist

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

The latest development work lives on `codex/gameplay-ux-polish-phase4` (HEAD `16baaf5`), **six commits ahead of `main`** (HEAD `a8b1649` as checked on 2026-10-30). Changes on that branch must be reviewed and merged before they can be described as released on `main`.

Normal builds start at the **Main Menu**, with Start Game, Options, Credits and Quit. Salvage Intake is a continuous authored route from x=-20 to x=193 with seven ordered Sentry/Bombardier encounters, three checkpoints, retry/death handling, objective HUD and a two-second final extraction. It contains native-pixel industrial environments and parallax, destructible areas, animated enemies and an authored Bombardier explosion. Player movement includes backpedal, crouch, Wall Brace/Wall Kick and LedgeClimb. The equipment set is Unarmed, Latch-9 and Longwatch DMR.

Phase 4 adds weapon-number bindings, pre-engagement patrols, checkpoint lamps, compact HUD/developer F3 view, enemy floor/void fixes and pixel-accurate deep-space border correction. Its documented final validation records **439/439 EditMode and 143/143 PlayMode tests**, a successful non-development Windows x64 Release and standalone smoke. Those are implementation reports, **not** evidence of human full-run/balance acceptance. See [Phase 4 report](docs/gameplay-ux-polish-phase4.md) and [first playable guide](docs/FIRST_PLAYABLE.md).

**Audio is not finished.** The menus already persist independent Music/SFX volume levels, and the project contains `RustlineAudioSettings` plus `AudioBusSource`, but production SFX/music clips have not been committed. Priorities are licensed asset sourcing, combat/movement/UI SFX, then industrial ambience and a loopable score. See [audio direction](docs/AUDIO_DIRECTION.md).

**Still open for a public demo:** human gameplay/control/audio acceptance, full gamepad combat review if advertised, a decision on loot/inventory scope, packaging, marketing captures and finalized licenses/attributions. The actual playable loop currently ends in combat-driven extraction; **do not promise loot/inventory** until implemented. See [roadmap](docs/ROADMAP.md).


## License

Licensing will be finalized before the first public release. Code and original artwork may use separate licenses.
