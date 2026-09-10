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

The purpose is to create a simple resource decision without requiring a full loot/ammo economy before the extraction loop exists. The ammunition system and Latch-9 runtime/presentation are **not implemented yet**; this section records the current demo design direction only.

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
- mouse-left primary fire drives a semi-automatic Longwatch hitscan from exact continuous aim at a `0.25 s` interval, `80` unit range, and `40` prototype damage;
- firing is allowed during Idle, Run, Backpedal, Crouch Idle, Crouch Move, and Fall; Jump, Land, Wall Brace, Wall Kick, and LedgeClimb remain blocked;
- exact generated muzzle metadata is imported Editor-side into compact runtime presentation data, and successful shots drive a persistent two-rendered-frame Longwatch muzzle flash beneath the recoil-driven weapon overlay;
- an active muzzle flash is canceled immediately if presentation transitions to a state with no muzzle-capable Longwatch pose, preventing carry/traversal bleed;
- MovementLab contains reusable diagnostic targets, Ground occlusion coverage, short distal tracer feedback, compact Ground/target impact feedback, restrained Longwatch overlay recoil, and a deterministic one-pixel camera impulse;
- the shared aim origin is 38 source pixels / 2.375 Unity units above the renderer pivot;
- the first-weapon fixed-cell import, Body-clock, renderer-ownership, carry, aim, metadata, and muzzle-flash conventions are implemented and are now considered frozen for the current movement set.

Do not add new Longwatch locomotion states merely to create more art. Future changes to this first-weapon contract should be driven by a demonstrated gameplay/visual need.

## Production rules

- Author the right-facing presentation first.
- Final production pixels use Rustline Canonical 28 plus binary transparency only.
- Do not bake antialiasing into production sprites.
- Strong silhouettes matter more than micro-detail.
- Weapons within a family should share visual language without becoming silhouette clones.
- A weapon is not considered production-ready merely because a standalone gun sprite exists; its player presentation package must follow the discrete aim/carry system in `PLAYER_WEAPON_ART.md`.
- Exact per-frame/per-direction Longwatch muzzle metadata and the production two-frame muzzle flash are implemented for presentation. Crouch `m70`/`m80`/`m90` remain explicitly unsupported and omit the flash without changing the shot. Clearance, the planned Crouch `m60` clamp/red crosshair, casing ejection, reload, audio, and production recoil/impact art remain deferred. Gun Feel v1 hitscan and its distal tracer still originate at `AimOriginWorld`; muzzle metadata has not migrated ballistics.

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
11. Build the next weapon only when the demo needs it. **Current candidate: Latch-9.**

A long weapon was the correct first stress test because angular, clipping, hand-placement, and pivot errors were easier to see than with a compact pistol. The Latch-9 should reuse proven concepts where appropriate without mechanically inheriting Longwatch-sized art requirements that its smaller silhouette does not need.
