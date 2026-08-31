# Rustline Prototype Roadmap

Rustline is developed as a sequence of proof points. Each milestone should be playable and evaluable before the next layer is added.

## M0 — Visual foundation

**Goal:** establish a redistributable visual baseline before production gameplay work begins.

- [x] Approve canonical player design
- [x] Establish original Rustline palette
- [x] Produce first player idle/run test
- [ ] Produce first modular environment tile family
- [ ] Define sprite import conventions
- [ ] Decide art-source licensing for original Rustline assets

Current M0 art work:

- Canonical 48×64 player sprite is versioned.
- Idle and run sprite sheets are versioned and use the canonical Rustline palette.
- Rustline Canonical 28 is the production palette.
- The structural tile atlas contract is fixed at 128×96 with 48 slots of 16×16; canonical N/E/S/W connectivity occupies slots 00–15.
- Environment tile production is currently in progress. See [`TILESET_SPEC.md`](TILESET_SPEC.md).

**Exit criterion:** the player and one small environment mockup clearly look like the same game and can be redistributed with the repository.

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

**Exit criterion:** traversing a small graybox room is fun without enemies or weapons.

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

**Goal:** create the first repeatable combat encounter.

- [ ] Health/damage model
- [ ] Ground enemy
- [ ] Flying enemy
- [ ] Enemy damage / hit reactions
- [ ] Player death/restart
- [ ] Basic encounter spawning
- [ ] First-pass combat audio

**Exit criterion:** a short room can be traversed and cleared repeatedly without debug intervention.

## M4 — Loot & extraction loop

**Goal:** turn the combat prototype into a tiny game loop.

- [ ] Loot drops
- [ ] Pickup interaction
- [ ] Small inventory model
- [ ] Resource/scrap value
- [ ] Extraction terminal/zone
- [ ] Successful extraction result
- [ ] Death/loss behavior
- [ ] Minimal HUD

**Exit criterion:** Deploy → Traverse → Fight → Loot → Extract works end-to-end locally.

## M5 — Multiplayer architecture

**Goal:** make the core loop network-capable without compromising gameplay feel.

Networking technology is intentionally not selected during pre-production. Choose it only after M1–M4 clarify the simulation requirements.

- [ ] Networking stack evaluation / ADR
- [ ] Server-authoritative player state
- [ ] Authoritative damage/combat validation
- [ ] Authoritative projectile strategy
- [ ] Client prediction for movement where required
- [ ] Reconciliation/correction strategy
- [ ] Remote interpolation
- [ ] Latency/loss simulation
- [ ] Disconnect and reconnect behavior
- [ ] 2–4 player playable session

**Exit criterion:** multiple players can complete the extraction loop under simulated non-ideal network conditions.

## M6 — Persistence & scale demonstration

**Goal:** demonstrate the architecture expected of a larger persistent shooter without attempting to build a full MMO.

- [ ] Persistent player profile/inventory
- [ ] Session/shard lifecycle
- [ ] Server-side persistence boundaries
- [ ] Idempotent extraction/reward handling
- [ ] Load/simulation harness
- [ ] Profiling and bottleneck documentation
- [ ] Scale findings and proposed 40–48-player architecture documented

**Exit criterion:** portfolio documentation can clearly explain what is implemented, what was load-tested, and how the architecture would scale beyond the small playable demo.

## M7 — Portfolio polish

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

Whenever there is a choice between more content and higher-quality movement, combat, networking, testing, or documentation, prefer the latter.
