# Rustline Prototype Roadmap

Rustline is developed as a sequence of proof points. Each milestone should be playable and evaluable before the next layer is expanded.

Rustline is currently a **single-player project**. Multiplayer, networking, server authority, prediction/reconciliation, matchmaking, network persistence, and multiplayer backend architecture are outside the project scope unless the project direction is explicitly changed in the future.

## Current vertical-slice direction — 2026-09-10

The player movement/presentation foundation and the first Longwatch DMR pipeline are now sufficiently complete to stop adding movement features and start building a real environment around them.

The current production order is:

1. build a first polished production **environment slice** using the accepted movement/collision metrics;
2. expand that visual language into the demo's single playable level;
3. add the second demo weapon / sidearm and its deliberately smaller presentation package;
4. replace programmer-art combat presentation with production enemy/impact/audio work;
5. complete the small Deploy → Traverse → Fight → Loot → Extract loop.

Current demo weapon direction is **two weapons**: the Longwatch DMR as the stronger, limited-ammunition weapon and the compact **Latch-9** pistol as the weaker fallback with effectively unlimited ammunition for the demo. This is a gameplay/content target, not an implemented ammo system yet.

Environment collision authoring must follow [`ENVIRONMENT_GAMEPLAY_METRICS.md`](ENVIRONMENT_GAMEPLAY_METRICS.md), including the current **24 source pixel / 1.5 unit Minimum Traversable Gap** rule. Visual cracks may be narrower when hidden gameplay collision bridges them.

The current art-production checklist is tracked in [`ASSET_SPRINTS.md`](ASSET_SPRINTS.md). Native-scale in-engine inspection remains the authority for visual approval.

The first production-area target is now **Salvage Intake**. Its deterministic graybox builder and room contract are implemented; the generated Unity scene still requires native-scale human playtesting before the graybox is locked or environment art production begins. See [`SALVAGE_INTAKE.md`](SALVAGE_INTAKE.md).

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

**Exit criterion:** the current player animation set and structural tiles render in Unity at native pixel-art quality with stable frame alignment and a coherent visual baseline. **Satisfied for the current player; environment production is now the active extension of M0.**

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
- [ ] Demo ammunition model
- [ ] Reload, only if retained by the demo weapon design
- [ ] Latch-9 second-weapon implementation
- [ ] Production impact FX
- [ ] Combat audio

The current Longwatch uses mouse-left fire, `0.25 s` shot interval, `80` unit range, and `40` prototype damage. Hitscan direction remains exact continuous aim rather than the quantized visual angle. Muzzle metadata drives presentation only; ballistics and the distal tracer still originate at `AimOriginWorld`. An active muzzle flash is canceled immediately when presentation enters a state that no longer exposes a muzzle-capable Longwatch pose, preventing a flash from bleeding into Jump/Land carry or unarmed traversal.

**Exit criterion:** shooting targets while moving feels deliberate and responsive, and authored weapon presentation remains coherent across locomotion/facing changes. **Satisfied for the Longwatch proof weapon; second-weapon/ammo work belongs to the demo-content pass.**

## M3 — Combat slice

**Goal:** create the first repeatable PvE combat encounter.

- [x] Reusable health/damage model
- [x] First killable ground enemy prototype
- [x] Enemy hit reaction and death
- [x] Repeatable MovementLab prototype encounter/reset
- [ ] Enemy attacks
- [ ] Player health
- [ ] Player death/restart
- [ ] Production ground-enemy art
- [ ] Flying enemy, only if the demo needs it
- [ ] Advanced enemy AI/pathing, only if required by the level
- [ ] Broader encounter spawning
- [ ] First-pass combat audio

M3A is a narrow combat proof point, not completion of M3. The programmer-art ground enemy has `100` health, deterministic horizontal patrol, a `0.12 s` nonlethal hit pause, explicit child-hitbox routing, and dies after three normal `40`-damage Longwatch hits before resetting in place. See [`COMBAT.md`](COMBAT.md).

**Exit criterion:** a short room can be traversed and cleared repeatedly without debug intervention. **Satisfied by the MovementLab prototype; production encounter content remains pending.**

## M4 — Loot & extraction loop

**Goal:** turn the combat prototype into a tiny single-player game loop.

- [ ] Loot drops
- [ ] Pickup interaction
- [ ] Small inventory/resource model
- [ ] Extraction terminal/zone
- [ ] Successful extraction result
- [ ] Death/loss behavior
- [ ] Minimal HUD

**Exit criterion:** Deploy → Traverse → Fight → Loot → Extract works end-to-end locally inside the demo level.

## M5 — Portfolio / itch.io polish

- [ ] Stable Windows release build of the demo
- [ ] Production environment pass complete
- [ ] Production enemy/impact/audio pass complete
- [ ] Screenshots / short gameplay capture
- [ ] README screenshots and feature summary
- [ ] Automated test summary
- [ ] Known limitations
- [ ] Build instructions
- [ ] Final licensing

**Exit criterion:** a player can download a compact polished demo, while a technical reviewer can understand, run, and evaluate the project without project-specific guidance.

---

## Scope rule

Rustline is a **small polished vertical slice**, not a production content project.

Whenever there is a choice between multiplying content and improving the quality/readability of the current demo loop, prefer the smaller, better-finished option.
