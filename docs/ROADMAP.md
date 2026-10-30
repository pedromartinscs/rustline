# Rustline Prototype Roadmap

Rustline is a milestone-driven **single-player** Unity 6 game. Multiplayer, networking, matchmaking and backend services are out of scope. Preserve the responsive movement, weapon, native-pixel rendering and Windows Release collision contracts.

## Current vertical-slice status — 2026-10-30

This status describes the latest `codex/gameplay-ux-polish-phase4` branch (`16baaf5`), **not** the older `main` (`a8b1649`). Phase 4 contains six additional commits and has not yet been merged to `main`. Its validation results below are **recorded by the implementation**, not independently rerun during this documentation update.

- **Playable now:** Main Menu, Options/Credits/Pause, Salvage Intake (x=-20 to x=193), movement and mouse-aimed Longwatch/Latch-9 combat, original Sentry/Bombardier art, visual explosion/death effects, seven authored encounters with fourteen defenders, three checkpoints, retries, two-second final extraction, HUD and industrial background/parallax.
- **Phase 4 refinements:** numeric weapon selection, pre-engagement patrols, checkpoint status lamps, compact player HUD and F3 diagnostics, enemy support/void cleanup, seamless canonical Deep Space presentation.
- **Audio state:** independent persistent Music/SFX settings, `AudioBusSource` routing and menu pause behavior exist, but **no production music or SFX clips are committed**.
- **Evidence:** Phase 4.3 reports **439/439 EditMode**, **143/143 PlayMode**, successful Windows x64 non-development Release, standalone smoke and targeted rendering/void reviews. These automated results do not substitute for human full-route, balance, sound or hardware acceptance.
- **Incomplete by design:** pickups/loot and inventory are not implemented; full mouse-to-gamepad combat/aim acceptance remains pending. Avoid describing the current build as a complete Deploy → Traverse → Fight → Loot → Extract loop.

Next production order:
1. Review/merge the completed Phase 4 after human acceptance, keeping release regressions clean.
2. Source and approve properly licensed SFX and music, retaining provenance before importing any files (see [`AUDIO_DIRECTION.md`](AUDIO_DIRECTION.md)).
3. Implement combat/movement/UI SFX first; then ambient soundbeds and loopable exploration/combat/extraction music.
4. Add only targeted visual impact polish when verified necessary by playtest.
5. Decide whether the public demo needs a minimal **loot/pickup** loop or should be positioned honestly as a combat-and-extraction vertical slice.
6. Finish human playtesting, standalone performance and distributable itch.io release packaging/licensing.

The current demo equipment is Unarmed, Latch-9 (unlimited fallback) and Longwatch DMR (finite ammunition/reload). The native-pixel weapon carousel is implemented. Environment collision remains governed by the **24 source pixel / 1.5 unit Minimum Traversable Gap** rule in [`ENVIRONMENT_GAMEPLAY_METRICS.md`](ENVIRONMENT_GAMEPLAY_METRICS.md). Use [`ASSET_SPRINTS.md`](ASSET_SPRINTS.md) and native-scale in-engine inspection for visual review. [`FIRST_PLAYABLE.md`](FIRST_PLAYABLE.md) describes actual gameplay/controls.

## M0 — Visual foundation

**Goal:** establish a redistributable visual baseline and integrate it correctly into Unity before production gameplay work begins.

- [x] Approve canonical player design
- [x] Establish original Rustline palette
- [x] Produce the current player locomotion Body / Unarmed Arms packages
- [x] Author the first 16 canonical structural tile connectivity cases
- [x] Bootstrap Unity 6 Universal 2D project
- [x] Correct and standardize pixel-art sprite import conventions
- [x] Build an art showcase scene using the current player and environment assets
- [ ] Complete the first modular environment tile family beyond the canonical 16 cases
- [ ] Decide art-source licensing for original Rustline assets

Current M0 art work:

- Canonical player Body / Unarmed Arms cell is `48×64` at 16 PPU.
- Idle, Run, Backpedal, Jump, Fall, Land, six-frame Crouch, two-frame Wall Brace, and six-frame LedgeClimb Body/Unarmed Arms sheets are versioned and integrated.
- Rustline Canonical 28 is the production palette.
- The structural tile atlas contract is fixed at `128×96` with 48 slots of `16×16`; canonical N/E/S/W connectivity occupies slots 00–15.
- Production PNG imports use 16 PPU, Point filtering, no mipmaps or compression, Full Rect meshes, binary alpha, and deterministic fixed-cell slicing.
- `Assets/Scenes/ArtShowcase.unity` remains the M0 visual inspection scene.

See [`TILESET_SPEC.md`](TILESET_SPEC.md) for the structural atlas contract and [`ART_DIRECTION.md`](ART_DIRECTION.md) for the visual language.

**Exit criterion:** player and environment art render at accepted native-pixel quality. **Satisfied for the current production slice;** optional modular tile expansion and final art-source licensing remain open. Human visual approval of the newest branch remains separate.

## M1 — Movement prototype

**Goal:** prove that locomotion feels responsive before building combat systems.

- [x] Ground acceleration/deceleration
- [x] Air control
- [x] Jump / variable jump height
- [x] Coyote time / jump buffering
- [x] Fall behavior / terminal velocity
- [x] Grounded combat crouch with clearance-safe standing
- [x] Wall Brace / Wall Kick
- [x] Committed LedgeClimb
- [x] Pixel-perfect camera and presentation
- [x] Controller tuning exposed as data/configuration
- [ ] Roll or dodge, only if the demo later proves it needs one
- [ ] Moving-platform behavior, only if production level design requires it

M1 is implemented in `Assets/Scenes/MovementLab.unity`. Grounded movement is aim-relative: `7 u/s` forward, `4 u/s` Backpedal, and `3 u/s` crouched. Standing Backpedal uses exactly four authored frames at 8 fps. Crouch uses a `38 px` capsule and six authored frames; Wall Brace uses two authored frames at 6 fps and requires its calibrated vertical wall-contact band; LedgeClimb is a committed six-frame 0.60 s traversal with no grab/hang state.

The hidden collision Tilemap remains separate from visual environment art and uses the release-hardened TilemapCollider2D → CompositeCollider2D path. The canonical geometry and authoring measurements are documented in [`MOVEMENT.md`](MOVEMENT.md), [`ENVIRONMENT_GAMEPLAY_METRICS.md`](ENVIRONMENT_GAMEPLAY_METRICS.md), and [`RELEASE_COLLISION.md`](RELEASE_COLLISION.md).

**Exit criterion:** traversing a small room is fun without enemies or weapons. **Satisfied for the current demo movement set.**

## M2 — Gunplay prototype

**Goal:** prove directional aiming, authored weapon presentation, and weapon feel.

### M2A — Layered player presentation

- [x] Produce and integrate synchronized Body / Unarmed Arms presentation
- [x] Preserve exact fixed cells, pivots, frame order, and Body-owned animation timing
- [x] Preserve movement physics while changing presentation only

See [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md).

### M2B — Longwatch DMR first-weapon presentation package

- [x] Choose the Longwatch DMR as the representative first weapon
- [x] Idle: 19 right-facing aim directions × 2 frames
- [x] Run: 19 right-facing aim directions × 6 frames
- [x] Backpedal: 19 right-facing aim directions × 4 frames
- [x] Crouch: 19 right-facing aim directions × 6 frames
- [x] Fall: 19 right-facing aim directions × 1 frame
- [x] Jump carry: 3 non-firing `48×64` frames
- [x] Land carry: 2 non-firing `48×64` frames
- [x] Mirror the authored right-facing aim set for the left hemisphere
- [x] Keep gameplay aim continuous while visuals select the nearest authored 10° direction
- [x] Keep Body Animator as the sole locomotion-frame clock
- [x] Validate the fixed-cell import / renderer-ownership convention for the first weapon

Aim/fire-capable Longwatch states are Idle, Run, Backpedal, Crouch Idle, Crouch Move, and Fall. Jump and Land retain the weapon through fixed carry art but do not expose a muzzle-capable pose and cannot fire. Wall Brace intentionally shows no Longwatch because both hands and one leg are committed to the wall; LedgeClimb likewise releases to its unarmed traversal overlay. Wall Kick uses the accepted Jump/Fall presentation fallback and remains non-firing.

The first-weapon locomotion presentation package is considered **closed for the current movement set**. New states should not be added merely to create more weapon artwork.

### M2C — Gunplay systems

- [x] Mouse aiming
- [ ] Gamepad aiming
- [x] Horizontal player facing based on aim direction
- [x] Primary Longwatch DMR continuous-aim hitscan
- [x] Semi-automatic fire-rate gate
- [x] Prototype presentation-only Longwatch recoil
- [x] Authored muzzle metadata / runtime presentation asset
- [x] Production two-frame muzzle flash
- [x] Prototype target/obstruction impact feedback
- [x] Restrained deterministic camera impulse
- [x] Demo ammunition model
- [x] Longwatch manual reload / magazine discard contract
- [x] Latch-9 second-weapon implementation
- [x] Bombardier explosion/death production VFX (M5.1)
- [ ] Additional weapon/terrain impact polish as determined by playtesting
- [ ] Production combat audio (no committed clips)

The current Longwatch uses mouse-left fire, `1/12 s` shot interval (**12 shots/s**), `80` unit range, and `40` prototype damage. Hitscan direction remains exact continuous aim rather than the quantized visual angle. Muzzle metadata drives presentation only; ballistics and the distal tracer still originate at `AimOriginWorld`. An active muzzle flash is canceled immediately when presentation enters a state that no longer exposes a muzzle-capable Longwatch pose, preventing a flash from bleeding into Jump/Land carry or unarmed traversal.

**Exit criterion:** shooting targets while moving feels deliberate and responsive, authored weapon presentation remains coherent across locomotion/facing changes, and the current Longwatch/Latch/Unarmed equipment loop is usable without debug intervention. **Satisfied for the current demo weapon foundation; remaining work is regression hardening plus production combat content/polish.**

## M3 — Combat slice

**Goal:** readable, repeatable PvE action with original enemies and stable encounter progression.

- [x] Reusable health/damage model and player damage/death/retry
- [x] Original Sentry and Bombardier sprites, telegraphs, death presentation and bomb VFX
- [x] Two enemy archetypes with patrol, detection/engagement and reset
- [x] Seven ordered Salvage Intake encounters (fourteen defenders)
- [x] Checkpoint-preserved encounter progress and extraction-gated completion
- [x] Terrain-support/void-death corrections covered by automated regression
- [ ] Human difficulty, timing, audio-feedback and full-route acceptance
- [ ] First-pass combat SFX
- [ ] Flying enemy, advanced pathing or additional encounter types **only if** playtesting justifies them

Sentry uses melee telegraph/hit behavior; Bombardier uses a locked-target thrown explosive with original six-frame VFX. Both retain existing hit/death and reset contracts. See [`BOMBARDIER.md`](BOMBARDIER.md), [`sentry-art-pass.md`](sentry-art-pass.md), [`bombardier-art-pass.md`](bombardier-art-pass.md) and [`combat-vfx-polish.md`](combat-vfx-polish.md).

**Exit criterion:** real encounters and progression run without debug intervention. **Technically satisfied for the authored Salvage Intake route;** independent human feel/balance acceptance remains open.

## M4 — Loot & extraction loop

**Goal:** turn traversal and PvE combat into a small, satisfying single-player run.

- [ ] Loot drops from defeated enemies or world containers
- [ ] Pickup interaction
- [ ] Small inventory/resource model
- [x] Final extraction zone with two-second uninterrupted hold
- [x] Successful extraction/result and restart behavior
- [x] Player death, checkpoint retry and persistent committed progress
- [x] Minimal objective/health HUD and weapon/ammo carousel

**Exit criterion:** the current **Traverse → Fight → Extract** loop is implemented and automatically exercised; the originally envisioned **Loot** stage is **not implemented**. Before marketing the demo as a full extraction-shooter loop, either deliver a small, tested loot mechanic or narrow the description.

## Next production pass — Audio (SFX first)

- [ ] Approve licensing/provenance for every imported SFX/music asset
- [ ] Weapon fire, reload, hits, enemy alerts/damage/death and Bombardier blast SFX
- [ ] Traversal, destructible terrain, checkpoint/extraction and UI feedback
- [ ] Subtle industrial ambient soundbeds and an exploration music loop
- [ ] Combat and extraction musical layers/transitions, if justified by feel
- [ ] Audio concurrency, pause/resume, separate bus gains, restart and scene-change regression tests
- [ ] Human audio listening, mix/readability and loop-seam acceptance

The persistent `RustlineAudioSettings` and `AudioBusSource` are already implemented. Audio asset sourcing must happen before integration; do not mistake existing volume sliders for finished sound design. See [`AUDIO_DIRECTION.md`](AUDIO_DIRECTION.md).

## M5 — Portfolio / itch.io polish

- [x] Windows x64 non-development Release builder and automated standalone smoke
- [x] First playable environment art and enemy presentation integrated
- [ ] Human approval of Phase 4 and full production route
- [ ] Final combat SFX, soundtrack/ambience and any essential impact polish
- [ ] Decide and implement minimum loot scope (or update positioning)
- [ ] Complete gamepad combat/control review, if supporting gamepad at public release
- [ ] Stable performance, audio levels and gameplay balance on target hardware
- [ ] Screenshots / short gameplay capture for publication
- [ ] README screenshots and end-user feature summary
- [ ] Publish automated-test summary and honest known limitations
- [ ] Downloadable package instructions and release test on a clean machine
- [ ] Final code/art/audio licensing and third-party attributions

**Exit criterion:** someone can download a coherent, appropriately described and properly licensed demo and play it from title screen to extraction without project-specific instructions. Successful automated tests are necessary, not sufficient.

---

## Scope rule

Rustline is a **small polished vertical slice**, not a production content project.

Whenever there is a choice between multiplying content and improving the quality/readability of the current demo loop, prefer the smaller, better-finished option.
