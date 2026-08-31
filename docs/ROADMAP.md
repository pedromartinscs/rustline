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
- [ ] Correct and standardize pixel-art sprite import conventions
- [ ] Build an art showcase scene using the current player and environment assets
- [ ] Complete the first modular environment tile family beyond the canonical 16 cases
- [ ] Decide art-source licensing for original Rustline assets

Current M0 art work:

- Canonical player cell is 48×64.
- Idle, run, jump, fall, and land sprite sheets are versioned.
- Rustline Canonical 28 is the production palette.
- The structural tile atlas contract is fixed at 128×96 with 48 slots of 16×16; canonical N/E/S/W connectivity occupies slots 00–15.
- Unity is bootstrapped with the Universal 2D template, URP 2D Renderer, Input System, Tilemap Extras, and Test Framework.
- The current Unity-generated texture imports are not yet canonical for Rustline pixel art and must be corrected before visual evaluation.

See [`TILESET_SPEC.md`](TILESET_SPEC.md) for the structural atlas contract.

**Exit criterion:** the current player animation set and structural tiles render in Unity at native pixel-art quality, with stable frame alignment and a small visual showcase demonstrating that the character and environment belong to the same game.

## M1 — Movement prototype

**Goal:** prove that locomotion feels responsive before building combat systems.

- [ ] Ground acceleration/deceleration
- [ ] Air control
- [ ] Jump
- [ ] Variable jump height
- [ ] Coyote time
- [ ] Jump buffering
- [ ] Fall behavior / terminal velocity
- [ ] Roll or dodge
- [ ] Moving-platform behavior if useful
- [ ] Pixel-perfect camera and presentation
- [ ] Controller tuning exposed as data/configuration

**Exit criterion:** traversing a small room is fun without enemies or weapons.

## M2 — Gunplay prototype

**Goal:** prove 360° aiming and weapon feel.

- [ ] Independent weapon pivot
- [ ] Mouse aiming
- [ ] Gamepad aiming
- [ ] Horizontal player facing based on aim direction
- [ ] Primary weapon implementation
- [ ] Projectile and/or hitscan abstraction
- [ ] Fire rate / reload
- [ ] Recoil
- [ ] Muzzle flash
- [ ] Impact feedback
- [ ] Camera feedback where appropriate

**Exit criterion:** shooting targets while moving feels deliberate and responsive.

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
