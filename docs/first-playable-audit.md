# First playable audit — 2026-10-22

Baseline: clean `feat/first-playable-phase-01`, HEAD `ae4eb0f`. No changes to the experimental branch, main, remotes, or packages are needed.

## Implemented and reusable

- Unity **6000.4.0f1**, URP **17.4.0** 2D, Input System **1.19.0**, Test Framework **1.6.0**. Windows x64 build profile; installed matching Editor. No CI workflow directory exists.
- Release entry is `Assets/Scenes/Demo/SalvageIntake.unity`; MovementLab, ArtShowcase and SampleScene follow. Player prefab is `Assets/Prefabs/Player/Player.prefab`.
- Rigidbody player motor separates input, simulation, environment probes and authored animation. Coyote/buffered jumps, crouch, aim-facing backpedal, wall brace/kick and committed ledge climb already work through dedicated components.
- Longwatch hitscan, Latch-9 swept/ricochet projectiles, explicit same-collider `WeaponHitbox2D` routing, generic `CombatHealth2D`, finite player-owned DMR magazines and infinite sidearm, equipment carousel and native-pixel HUD already exist. Preserve these boundaries.
- `Bombardier2D` already supplies bounded patrol, Ground LOS, locked-target windup, ballistic bombs, occluded explosions, recovery, damage/hit pause/death and in-place reset. Its MovementLab owner automatically resurrects it; that diagnostic policy must not enter the production scene.
- Salvage Intake already has substantial original industrial art, dressed Tilemap terrain, machinery/plinth, raised catwalk, four-unit service shaft, upper exit deck, horizontal parallax and vertical backdrop. Preserve authored terrain and rendering.
- Canonical palette, authored player/weapon sprites, recoil, muzzle flashes, tracer, bitmap font and camera follow are reusable. No imported audio files were found. Enemy silhouettes/bombs currently use explicitly temporary LineRenderer art.
- NUnit EditMode and PlayMode suites cover movement, collision startup, aiming, weapons, health, Bombardier, presentation and frame pacing. Standalone benchmark tooling exists; development benchmark results do not prove Release gameplay or FPS.

## Incomplete systems and required work

Salvage Intake has no production encounter progression, checkpoint, objective/completion, death presentation or level HUD. Its rig still uses the diagnostic respawn policy. Only one enemy archetype exists. Add a small close-range sentry, reuse Bombardier, author encounters into the existing route, and supply checkpoint/retry/restart plus clear health/objective/control feedback. Use one saved production scene with explicit serialized references and a repeatable content-only setup pass.

## Risks and decisions

- Release-only Tilemap/Composite failure is documented in `RELEASE_COLLISION.md`. Compared the runtime initializer with human-verified `047c49e7`: the processing/generation/synchronization sequence remains intact. Preserve initializer, builder bake and actual-build-scene fail-closed guard.
- Read `ENVIRONMENT_GAMEPLAY_METRICS.md`, `MOVEMENT.md`, COMBAT, WEAPONS, BOMBARDIER, SALVAGE_INTAKE, PRESENTATION, PERFORMANCE and build tooling/source. Do not change terrain, capsule, movement tuning or the precision MovementLab ceiling. New sentries must detect walls/unsupported edges rather than walk through them.
- Existing gamepad movement/jump/crouch bindings exist; aim currently uses pointer only. Preserve the actual support and document its limits instead of claiming a complete controller experience.
- Production setup rebuilds the managed player rig. The gameplay setup must run after it and replace serialized rig references, while preserving unrelated authored roots.
- Prefer a complete compact route over stretching this room into an untested 8–12-minute level. Duration, difficulty, finished enemy art/audio and portfolio polish require human playtesting.

## Baseline validation

Matching Unity batch EditMode run requested before source changes. First attempt stopped at a stale scene-backup dialog (not allowed in batch). Preserved `Temp/__Backupscenes` under `Logs/pre-first-playable-scene-backups`, then retried. Full results and subsequent validation are recorded in `first-playable-validation.md`; do not infer passing tests from a successful process launch.
