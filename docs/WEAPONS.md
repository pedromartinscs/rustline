# Rustline Weapon Roster

This document preserves the initial planned Rustline arsenal and its visual families as a production/design reference.

Names are provisional until a weapon is implemented, but the silhouettes, family roles, and broad identity are intentionally versioned so future asset generation does not drift into an unrelated collection of guns.

Weapon presentation follows [`PLAYER_WEAPON_ART.md`](PLAYER_WEAPON_ART.md).

## Visual families

### Security

Clean, compact, manufactured weapons with dark steel, controlled geometry, and restrained cyan electronic accents. These should look like standardized security equipment rather than improvised salvage.

### Salvage / Industrial

Worn, repaired, converted, or repurposed machinery. Rust, exposed fasteners, welded repairs, orange/warm accents, unusual reservoirs, and tool-like mechanisms are welcome.

### Experimental

Weapons built around visibly unusual technology: capacitors, coils, emitters, energy cores, prongs, or unconventional barrels. Cyan may be more prominent, but silhouettes must remain readable and production colors remain restricted to Rustline Canonical 28.

### Heavy

Large mass, thick receivers, warning markings, oversized barrels, visible feed systems, and machinery-scale construction. Heavy weapons should look physically consequential even in small pixel silhouettes.

## Canonical initial roster

| # | Weapon | Type | Family | Visual identity |
|---:|---|---|---|---|
| 01 | **Latch-9** | Compact pistol | Security | Short, square, robust slide; intended as a readable baseline sidearm. |
| 02 | **Warden-12** | Heavy pistol | Security | Large handgun, longer barrel, strong compensator/front mass. |
| 03 | **Needle SMG** | Compact SMG | Security | Very short receiver, vertical magazine, minimal stock. |
| 04 | **Sentinel AR** | Assault rifle | Security | Clean modular industrial rifle; strong default long-gun silhouette. |
| 05 | **Tripwire BR** | Burst rifle | Security | Longer than Sentinel, narrow barrel and heavier receiver. |
| 06 | **Longwatch DMR** | Marksman rifle | Security | Long, controlled silhouette with integrated optic/sensor treatment. **First armed-pipeline validation weapon.** |
| 07 | **Breach-8** | Pump shotgun | Salvage | Oversized pump, worn steel, repaired industrial character. |
| 08 | **Scrapper** | Automatic shotgun | Salvage | Short and bulky with an oversized magazine/feed silhouette. |
| 09 | **Rivet Driver** | Rivet gun conversion | Industrial | Construction tool converted into a weapon; obvious reservoir/cylinder. |
| 10 | **Spikegun** | Nail/spike launcher | Industrial | Long strange profile, forward rail, industrial ammunition language. |
| 11 | **Torchbolt** | Thermal weapon | Industrial | Heavy body, heat/emitter front, small tank or pressure-system cues. |
| 12 | **Grinder LMG** | Light machine gun | Salvage | Large feed box, heavy improvised construction, aggressive asymmetry. |
| 13 | **Arc-7** | Arc gun | Experimental | Twin/front electrodes, exposed capacitors, visibly non-ballistic design. |
| 14 | **Helix Carbine** | Energy carbine | Experimental | Cleaner body with exposed energy core and futuristic industrial profile. |
| 15 | **Coil Lance** | Coil rifle | Experimental | Very long skeletal barrel with visible coils and precision-energy identity. |
| 16 | **Ion Scattergun** | Energy shotgun | Experimental | Broad emitter front with multiple prongs and strong cyan energy focal point. |
| 17 | **Kiln GL** | Grenade launcher | Heavy | Extremely thick barrel and industrial break/rotary loading mechanism. |
| 18 | **Foundry Cannon** | Heavy cannon | Heavy | Machinery-scale blocks of steel, warning markings, nearly oversized for the player. |
| 19 | **Breaker-6** | Rotary gun | Heavy | Compact multi-barrel cluster with visible mechanical feed system. |
| 20 | **Blackline** | Anti-materiel/sniper rifle | Heavy | Very long barrel, oversized muzzle brake, rare and immediately recognizable silhouette. |

## Current itch.io demo weapon direction

The current vertical-slice target is deliberately much smaller than the full roster:

- **Longwatch DMR** — stronger precision weapon with a finite ammunition reserve during the demo level;
- **Latch-9** — weaker compact fallback sidearm with effectively unlimited ammunition for the demo.

The Latch-9 now has both presentation and playable projectile ballistics in the Editor harness. The ammo-economy/switching layer is still deferred, so the current harness directly equips the Latch-9 while its presentation preview is active. The approved damage/ricochet behavior is implemented. Its current `80`-unit range, `1/12 s` cooldown, and `30 units/s` projectile speed are prototype tuning values and are **not balance-locked Latch values**.

## Approximate visual scale

These are silhouette guidelines, not rigid hitbox or gameplay dimensions:

```text
Pistols                 ~20–30 px long
SMGs                    ~28–38 px
Rifles                  ~38–52 px
Shotguns                ~40–54 px
DMR / sniper            ~50–64 px
Heavy weapons           ~50–72 px
```

The canonical Body cell is 48×64 px, so weapon scale must always be judged in relation to that character rather than in isolation. Armed overlay cells may be larger than the Body cell; the first Longwatch DMR aim-capable package uses the documented 80×96 armed cell so the weapon silhouette is not clipped or artificially shortened. Fixed carry poses may use the 48×64 Body cell when their full silhouette fits, as the Longwatch Jump/Land carries do.

## Longwatch DMR — first pipeline weapon

The Longwatch DMR is the first representative weapon chosen to stress-test directional armed presentation before scaling the pipeline to another weapon.

Current authored reference/source art:

```text
ArtSource/Concepts/Longwatch_DMR_concept.png
ArtSource/Concepts/Longwatch_DMR_zero_degrees_concept.png
ArtSource/Characters/Player/player_salvager_idle_armed.xcf
ArtSource/Characters/Player/player_salvager_run_armed.xcf
ArtSource/Characters/Player/player_salvager_crouch_armed.xcf
```

Current production status:

- standalone visual identity established;
- right-facing Idle authored at all 19 canonical angles from `+90°` to `-90°`, two frames per angle;
- right-facing Run authored at all 19 angles with six `80×96` frames per direction;
- right-facing Backpedal authored at all 19 angles with exactly four `80×96` frames per direction and follows the 8 fps Body clock while physical Backpedal remains 4 units/s;
- right-facing Crouch authored at all 19 angles with six `80×96` frames per direction; Crouch Idle reuses frame 0 and forward/reverse movement follows the displayed Body frame;
- right-facing Fall authored at all 19 angles as one `80×96` frame per direction;
- Jump uses three integrated `48×64` carry frames and Land uses two integrated `48×64` carry frames; both follow displayed Body frames one-to-one, remain non-firing, expose no muzzle-capable rendered pose, and have been human-tested in-engine;
- Wall Brace intentionally renders no Longwatch because both hands and one leg are committed to wall contact; dedicated unarmed Arms own presentation and firing is blocked;
- LedgeClimb intentionally renders no Longwatch and releases to the accepted six-frame unarmed traversal overlay;
- Wall Kick keeps the accepted Jump/Fall presentation fallback and remains non-firing;
- generic `PlayerAim2D` owns continuous world aim, the explicit AimOrigin, native-pixel mapping, and 5° vertical facing hysteresis; Longwatch only selects authored visuals;
- mouse-left primary fire drives the Longwatch hitscan from exact continuous aim at a `1/12 s` interval (**12 shots/s**), `80` unit range, and `40` prototype damage;
- right mouse continues to toggle Longwatch Semi/Automatic fire because Longwatch supports one shot mode but multiple fire modes;
- firing is allowed during Idle, Run, Backpedal, Crouch Idle, Crouch Move, and Fall; Jump, Land, Wall Brace, Wall Kick, and LedgeClimb remain blocked;
- exact generated muzzle metadata is imported Editor-side into compact runtime presentation data, and successful shots drive a persistent two-rendered-frame Longwatch muzzle flash beneath the recoil-driven weapon overlay;
- an active muzzle flash is canceled immediately if presentation transitions to a state with no muzzle-capable Longwatch pose, preventing carry/traversal bleed;
- MovementLab contains reusable diagnostic targets, Ground occlusion coverage, short distal tracer feedback, restrained Longwatch overlay recoil, and a deterministic one-pixel camera impulse; the old Ground/target impact line is intentionally disabled while authored collision particles remain deferred;
- the shared aim origin is 38 source pixels / 2.375 Unity units above the renderer pivot;
- the first-weapon fixed-cell import, Body-clock, renderer-ownership, carry, aim, metadata, and muzzle-flash conventions are implemented and are now considered frozen for the current movement set.

Do not add new Longwatch locomotion states merely to create more art. Future changes to this first-weapon contract should be driven by a demonstrated gameplay/visual need.

## Latch-9 — second weapon implementation

The Latch-9 reuses the proven armed-presentation and continuous-aim contracts while adding its own shot-mode and projectile-delivery behavior.

Current production status:

- Security-family compact-pistol identity is established;
- right-facing Idle, Run, Backpedal, Crouch, and Fall are authored at all 19 canonical aim angles in `80×96` armed cells;
- Jump uses three fixed `48×64` carry frames and Land uses two fixed `48×64` carry frames;
- Wall Brace and LedgeClimb intentionally render no Latch-9; Jump/Land and traversal states expose no muzzle-capable pose and remain non-firing;
- the deterministic muzzle generator validates all **361** aim-capable frame points with no unsupported Latch direction; after correcting the reference to the exact Idle-frame raster, every direction resolves uniquely with a `5×5` signature;
- `PlayerLatch9AimPresenter2D` exposes the exact state/direction/frame/facing tuple actually rendered for muzzle presentation and projectile origin;
- mouse-left fires the semi-automatic Latch-9; right mouse toggles `Conventional` / `Bouncing` instead of changing Semi/Automatic fire mode;
- Latch-9 uses a **visible projectile** instead of Longwatch-style hitscan. Both shot modes currently travel at `30 units/s` and share one `80`-unit range budget;
- the current projectile programmer-art visual is a small `4 px × 1 px` line: canonical Neon Cyan (`palette 20`) for `Conventional` and canonical Violet (`palette 22`) for `Bouncing`;
- the projectile launches from the exact muzzle point resolved from the currently rendered Latch pose while its travel direction remains the exact `ContinuousAimDirection`; the discrete 10-degree art bucket never quantizes ballistics;
- `Conventional` damage is **5** and the projectile stops on its first valid collision;
- `Bouncing` starts at **4** damage (80% of base) and may reflect from receiverless solid geometry up to **3** times; cumulative damage after bounce 1/2/3 is **3 / 2 / 1**;
- the ricochet keeps one total range budget across the whole reflected path rather than receiving a fresh full range after each bounce;
- a bouncing projectile stops when it reaches an `IWeaponHitReceiver2D`, applies the current stage damage, and does not continue through that receiver;
- receiverless Ground/level geometry is collision-only for this path: it can reflect/stop the projectile but is never mutated or sent damage, so the Latch-9 does **not** destroy ground tiles;
- after the third reflection, the bouncing projectile continues on that final segment until the next collision or range exhaustion and then disappears; production breakup/impact particles are intentionally deferred;
- the approved conventional flash is a compact cyan Security-family effect; the approved bouncing flash is predominantly violet/electric;
- both sheets use ten variants with two frames each; launch-time `ShotFired` drives the muzzle flash immediately, while `ShotResolved` occurs only when the projectile actually ends;
- `Latch9MuzzleFlashShotModeBinder2D` keeps the flash bank synchronized with the gameplay shot mode;
- the Editor gameplay harness replaces Longwatch presentation, equips `Assets/Config/Weapons/Latch9.asset`, re-enables `PlayerWeaponController2D`, disables Longwatch-specific recoil/muzzle presentation, and installs the Latch flash plus projectile emitter;
- the current prototype Latch definition uses `80` units of range, `1/12 s` cooldown, and `30 units/s` projectile speed as unapproved tuning placeholders until dedicated balance values are chosen.

## Production rules

- Author the right-facing presentation first.
- Final production pixels use Rustline Canonical 28 plus binary transparency only.
- Do not bake antialiasing into production sprites.
- Strong silhouettes matter more than micro-detail.
- Weapons within a family should share visual language without becoming silhouette clones.
- A weapon is not considered production-ready merely because a standalone gun sprite exists; its player presentation package must follow the discrete aim/carry system in `PLAYER_WEAPON_ART.md`.
- Exact per-frame/per-direction Longwatch muzzle metadata and the production two-frame muzzle flash are implemented for presentation. Crouch `m70`/`m80`/`m90` remain explicitly unsupported and omit the flash without changing the shot. Clearance, the planned Crouch `m60` clamp/red crosshair, casing ejection, reload, audio, and production recoil/impact art remain deferred. Longwatch Gun Feel v1 hitscan and its distal tracer continue to originate at `AimOriginWorld`.
- Latch-9 has the same exact-rendered-pose muzzle placement contract for all 361 supported aim frames plus an independent shot-mode contract. Its projectile origin now uses that exact muzzle metadata, but its direction remains continuous. Do not conflate `WeaponShotMode2D` (`Conventional` / `Bouncing`) with `WeaponFireMode2D` (`SemiAutomatic` / `Automatic`) or `WeaponDeliveryMode2D` (`Hitscan` / `Projectile`).
- Collision/impact line feedback is disabled. Final collision feedback will be authored as shot-appropriate particles rather than programmer-art line markers.

## First-weapon production sequence

1. Layered unarmed player. **Done.**
2. Longwatch representative weapon selection. **Done.**
3. 19-direction Idle presentation. **Done.**
4. Fixed import/pivot/mirroring/renderer-ownership validation. **Done.**
5. Run package. **Done.**
6. Four-frame Backpedal package. **Done.**
7. Continuous-aim semi-automatic hitscan proof. **Done.**
8. Crouch directional package. **Done.**
9. Fall aim plus Jump/Land carry. **Done.**
10. Freeze the reusable first-weapon art/import/runtime convention. **Done for the current movement set.**
11. Build the next weapon only when the demo needs it. **Latch-9 presentation and visible projectile ballistics are implemented; ammo economy/weapon switching remains a separate demo-system task.**

A long weapon was the correct first stress test because angular, clipping, hand-placement, and pivot errors were easier to see than with a compact pistol. The Latch-9 reuses proven concepts where appropriate without mechanically inheriting Longwatch-sized art requirements.