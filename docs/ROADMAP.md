# Rustline Prototype Roadmap

Rustline is developed as a sequence of proof points. Each milestone should be playable and evaluable before the next layer is added.

Rustline is currently a **single-player project**. Multiplayer, networking, server authority, prediction/reconciliation, matchmaking, network persistence, and multiplayer backend architecture are outside the project scope unless the project direction is explicitly changed in the future.

## M0 — Visual foundation

**Goal:** establish a redistributable visual baseline and integrate it correctly into Unity before production gameplay work begins.

- [x] Approve canonical player design
- [x] Establish original Rustline palette
- [x] Produce player idle/run/jump/fall/land sheets
- [x] Author the first 16 canonical structural tile connectivity cases
- [x] Bootstrap Unity 6 Universal 2D project
- [x] Correct and standardize pixel-art sprite import conventions
- [x] Build an art showcase scene using the current player and environment assets
- [ ] Complete the first modular environment tile family beyond the canonical 16 cases
- [ ] Decide art-source licensing for original Rustline assets

Current M0 art work:

- Canonical player cell is 48×64.
- Idle, run, jump, fall, and land sprite sheets are versioned.
- Rustline Canonical 28 is the production palette.
- The structural tile atlas contract is fixed at 128×96 with 48 slots of 16×16; canonical N/E/S/W connectivity occupies slots 00–15.
- Unity is bootstrapped with the Universal 2D template, URP 2D Renderer, Input System, Tilemap Extras, and Test Framework.
- Production PNG imports now use 16 PPU, Point filtering, no mipmaps or compression, Full Rect meshes, and deterministic fixed-cell slicing.
- `Assets/Scenes/ArtShowcase.unity` presents the five current player states, all 16 canonical tiles, and Rule Tile adjacency structures for visual acceptance.

See [`TILESET_SPEC.md`](TILESET_SPEC.md) for the structural atlas contract.

**Exit criterion:** the current player animation set and structural tiles render in Unity at native pixel-art quality, with stable frame alignment and a small visual showcase demonstrating that the character and environment belong to the same game.

## M1 — Movement prototype

**Goal:** prove that locomotion feels responsive before building combat systems.

- [x] Ground acceleration/deceleration
- [x] Air control
- [x] Jump
- [x] Variable jump height
- [x] Coyote time
- [x] Jump buffering
- [x] Fall behavior / terminal velocity
- [x] Grounded combat crouch with clearance-safe standing
- [x] Wall brace / wall kick
- [ ] Roll or dodge
- [ ] Moving-platform behavior if useful
- [x] Pixel-perfect camera and presentation
- [x] Controller tuning exposed as data/configuration

M1 movement is implemented in `Assets/Scenes/MovementLab.unity`. It includes the reusable player prefab, physics-driven animation presentation, grounded crouch, wall brace/kick, a separate visual/collision Tilemap course with deterministic Composite geometry initialization, pixel-snapped camera follow, failsafe respawn, and automated edit/play-mode validation. Authored crouch and wall art, roll/dodge, and moving-platform behavior remain pending.

**Exit criterion:** traversing a small room is fun without enemies or weapons.

## M2 — Gunplay prototype

**Goal:** prove directional aiming, authored weapon presentation, and weapon feel.

### M2A — Layered player presentation

Before integrating the first gun, decompose the accepted player artwork into synchronized full-cell layers.

- [x] Produce Body-only idle/run/backpedal/jump/fall/land sheets
- [x] Produce matching Unarmed Arms idle/run/backpedal/jump/fall/land sheets
- [x] Preserve exact 48×64 cells, pivots, frame order, and timing
- [x] Implement synchronized Body + Arms sprite presentation
- [x] Verify that Body + Unarmed Arms reconstructs the current accepted player appearance
- [x] Preserve movement physics and animation timing while changing presentation only

See [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md) for the production contract.

### M2B — First weapon presentation package

- [x] Choose the Longwatch DMR as the representative first weapon
- [x] Author 19 right-facing 10-degree aim directions for Idle
- [x] Author 19 right-facing 10-degree aim directions for Run
- [x] Author 19 right-facing 10-degree aim directions for Backpedal
- [ ] Author 19 right-facing 10-degree aim directions for Fall
- [ ] Author carry-only weapon presentation for Jump / Land / Roll as those states exist
- [x] Mirror the authored right-facing set for the left hemisphere
- [x] Keep gameplay aim continuous while visual aim selects the nearest authored direction
- [x] Gate first-pass firing to authored Idle / Run / Backpedal presentation; block Jump / Fall / Land / crouch / wall states
- [ ] Validate the sheet/import/runtime convention before scaling the art pipeline to the remaining arsenal

### M2C — Gunplay systems

- [x] Mouse aiming
- [ ] Gamepad aiming
- [x] Horizontal player facing based on aim direction
- [x] Primary Longwatch DMR firing
- [x] Continuous-aim Longwatch hitscan and first-obstruction resolution
- [x] Semi-automatic fire-rate gate
- [ ] Reload
- [ ] Recoil
- [ ] Muzzle flash
- [x] Prototype target impact feedback
- [ ] Camera feedback where appropriate

The first gunplay slice uses mouse-left primary fire, a config-driven `0.25 s` shot interval, `80` unit range, and `40` prototype damage. Hitscan uses `PlayerAim2D.ContinuousAimDirection`, never the quantized Longwatch visual pose, and resolves the nearest Ground or CombatTarget hit through an explicit allocation-free query. MovementLab supplies clear, angled, and Ground-occluded diagnostic targets plus a reused prototype trace and target flash. Ammo, reload, production recoil/muzzle/impact art, combat audio, gamepad aim, inventory, enemy health/death, and unsupported-state Longwatch art remain pending.

**Exit criterion:** shooting targets while moving feels deliberate and responsive, and the authored directional weapon presentation remains visually coherent across locomotion/facing changes.

## M3 — Combat slice

**Goal:** create the first repeatable PvE combat encounter.

- [ ] Health/damage model
- [ ] Ground enemy
- [ ] Flying enemy
- [ ] Enemy damage / hit reactions
- [ ] Player death/restart
- [ ] Basic encounter spawning
- [ ] First-pass combat audio

**Exit criterion:** a short room can be traversed and cleared repeatedly without debug intervention.

## M4 — Loot & extraction loop

**Goal:** turn the combat prototype into a tiny single-player game loop.

- [ ] Loot drops
- [ ] Pickup interaction
- [ ] Small inventory model
- [ ] Resource/scrap value
- [ ] Extraction terminal/zone
- [ ] Successful extraction result
- [ ] Death/loss behavior
- [ ] Minimal HUD

**Exit criterion:** Deploy → Traverse → Fight → Loot → Extract works end-to-end locally.

## M5 — Portfolio polish

- [ ] Stable Windows build
- [ ] Screenshots / short gameplay capture
- [ ] Architecture diagram
- [ ] README screenshots and feature summary
- [ ] Automated test summary
- [ ] Known limitations
- [ ] Build instructions
- [ ] Final licensing

**Exit criterion:** a technical reviewer can understand, run, and evaluate Rustline without needing project-specific guidance.

---

## Scope rule

Rustline is a **technical vertical slice**, not a production content project.

Whenever there is a choice between more content and higher-quality movement, combat, game feel, testing, presentation, or documentation, prefer the latter.
