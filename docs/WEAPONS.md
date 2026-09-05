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

The canonical Body cell is 48×64 px, so weapon scale must always be judged in relation to that character rather than in isolation. Armed overlay cells may be larger than the Body cell; the first Longwatch DMR package uses the documented 80×96 armed cell so the weapon silhouette is not clipped or artificially shortened.

## Longwatch DMR — first pipeline weapon

The Longwatch DMR is the first representative weapon chosen to stress-test directional armed presentation before scaling the pipeline to the remaining roster.

Current authored reference/source art:

```text
ArtSource/Concepts/Longwatch_DMR_concept.png
ArtSource/Concepts/Longwatch_DMR_zero_degrees_concept.png
ArtSource/Characters/Player/player_salvager_idle_armed.xcf
ArtSource/Characters/Player/player_salvager_run_armed.xcf
```

Current production status:

- standalone visual identity established;
- right-facing Idle authored at all 19 canonical angles from `+90°` to `-90°`;
- two Idle animation frames authored for every angle;
- each angle stored as one `160×96` PNG containing two `80×96` cells;
- common armed-cell Body reference and pivot documented in `PLAYER_WEAPON_ART.md`;
- runtime import, mouse-driven angle quantization, mirroring, renderer ownership, and automated in-engine validation are implemented for Idle;
- right-facing Run is authored at all 19 angles with six `80×96` frames in each `480×96` sheet;
- runtime Run integration uses the sole Body Animator as its six-frame clock and keeps aim-facing independent of movement direction;
- right-facing Backpedal is authored at all 19 angles with exactly four `80×96` frames in each `320×96` sheet;
- runtime Backpedal integration uses the same sole Body Animator clock, with a 4 units/s grounded cap versus 7 units/s forward;
- generic `PlayerAim2D` now owns continuous world aim, the explicit AimOrigin, native-pixel mapping, and 5° vertical facing hysteresis; Longwatch only selects authored visuals;
- the shared Idle/Run/Backpedal aim origin is 38 source pixels / 2.375 Unity units above the renderer pivot;
- the corrected aim origin, Run presentation, and revised four-frame Backpedal presentation are human-approved; 4 units/s is the current Backpedal movement-feel target;
- Fall aim and Jump/Land/Roll carry art remain deferred.

This staged validation is intentional. Do not multiply an unproven art/runtime contract into hundreds of sprites before the first 19-direction Idle package is accepted in motion.

## Production rules

- Author the right-facing presentation first.
- Final production pixels use Rustline Canonical 28 plus binary transparency only.
- Do not bake antialiasing into production sprites.
- Strong silhouettes matter more than micro-detail.
- Weapons within a family should share visual language without becoming silhouette clones.
- A weapon is not considered production-ready merely because a standalone gun sprite exists; its player presentation package must follow the discrete aim/carry system in `PLAYER_WEAPON_ART.md`.
- Muzzle, casing-ejection, projectile, reload, and impact metadata/effects may be added when the first weapon gameplay implementation begins; they are not prerequisites for validating the Idle armed-presentation pipeline.

## Initial production order

Do not generate all twenty complete weapon packages before validating the pipeline.

1. Complete and validate the layered unarmed player. **Done.**
2. Choose one representative first weapon. **Longwatch DMR chosen.**
3. Produce its right-facing 19-direction Idle presentation. **Done.**
4. Validate import, pivot, 360° mirrored visual aim, animation synchronization, and armed presenter ownership in Unity. **Automated Idle validation done.**
5. Expand the accepted Longwatch contract to Run. **Authored, integrated, and human-approved.**
6. Expand the accepted Longwatch contract to four-frame Backpedal. **Authored and integrated; human visual approval pending.**
7. Expand to Fall aim and non-firing carry states after the current visual gate.
8. Freeze the reusable weapon art/import/runtime convention.
9. Expand into the remaining roster in controlled batches.

A long weapon remains the correct first stress test because angular, clipping, hand-placement, and pivot errors are easier to see than with a compact pistol.
