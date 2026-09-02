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
| 06 | **Longwatch DMR** | Marksman rifle | Security | Long, controlled silhouette with integrated optic/sensor treatment. |
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

The canonical player cell is 48×64 px, so weapon scale must always be judged in relation to that character rather than in isolation.

## Production rules

- Author the right-facing presentation first.
- Final production pixels use Rustline Canonical 28 plus binary transparency only.
- Do not bake antialiasing into production sprites.
- Strong silhouettes matter more than micro-detail.
- Weapons within a family should share visual language without becoming silhouette clones.
- A weapon is not considered production-ready merely because a standalone gun sprite exists; its player presentation package must follow the discrete aim/carry system in `PLAYER_WEAPON_ART.md`.
- Muzzle, casing-ejection, projectile, reload, and impact metadata/effects may be added when the first weapon is implemented; they are not required for the current Body/Arms decomposition pass.

## Initial production order

Do not generate all twenty complete weapon packages before validating the pipeline.

1. Complete and validate the layered unarmed player.
2. Choose one representative first weapon.
3. Produce its full authored aim/carry presentation package.
4. Validate aiming, facing, locomotion, and firing restrictions in Unity.
5. Freeze the final weapon sheet/import convention.
6. Expand into the remaining roster in controlled batches.

A rifle or other long weapon is a useful first stress test because angular and hand-placement errors are easier to see than with a compact pistol.
