# Latch-9 Plasma Sidearm

This document records the current gameplay/design contract for the **Latch-9**, Rustline's second demo weapon. Its gameplay runtime remains a design target while the player presentation package is being authored incrementally.

The Latch-9 is intended to complement, not compete with, the Longwatch DMR. The Longwatch remains the stronger limited-ammunition primary weapon; the Latch-9 is the always-available fallback.

## Core identity

The Latch-9 is a **compact, rustic plasma sidearm** with an industrial/salvage character rather than a clean military-energy-weapon aesthetic.

Its energy supply is effectively unlimited for the itch.io demo. The resource constraint is therefore **time**, not ammunition: after every shot the weapon must recharge / dissipate heat before it can fire again.

The intended feel is a deliberate, visible plasma discharge rather than hitscan. It should remain accurate and dependable, but its slow cadence prevents it from replacing the Longwatch as the preferred damage weapon.

Initial tuning target:

- visible plasma projectile;
- effectively infinite ammunition / energy;
- `0.60 s` cooldown between shots;
- `22` base damage in Standard Plasma mode;
- approximately `36` unit effective range;
- no conventional reload requirement for the demo.

These numerical values are first-pass balance targets and may be tuned after the weapon is playable. The behavioral contract below is the important part.

## Fire modes

The Latch-9 has two fire modes:

1. **Standard Plasma**
2. **Bouncing Plasma**

**Right Mouse Button toggles between the two modes.** The selection remains active until toggled again.

Both modes use the same weapon, energy supply, base projectile system, and shot cooldown. Bouncing Plasma is not an alternate ammunition type and does not consume an additional resource.

### Standard Plasma

Standard Plasma is the baseline Latch-9 shot.

- fires one visible plasma projectile;
- uses the normal Latch-9 cooldown;
- uses effectively infinite energy;
- deals **100% of the Latch-9 base damage** on an enemy hit;
- does not ricochet as part of its intended gameplay behavior.

### Bouncing Plasma

Bouncing Plasma uses the same fundamental shot and the same cooldown, but changes collision behavior.

The projectile may **bounce up to three times from non-enemy collision surfaces** before it is exhausted. Enemy contact resolves the hit immediately rather than producing another bounce.

Its damage depends on how many non-enemy bounces occurred before the enemy hit:

| Bounces before enemy hit | Damage relative to Standard Plasma |
|---:|---:|
| 0 | **80%** |
| 1 | **60%** |
| 2 | **40%** |
| 3 | **20%** |

In other words, Bouncing Plasma deliberately sacrifices direct-hit damage in exchange for the ability to attack around geometry. A direct Bouncing Plasma hit is therefore still weaker than a Standard Plasma hit.

After the projectile has already completed its third allowed bounce, it may continue until it hits an enemy or otherwise reaches the projectile's normal termination condition; it may not perform a fourth bounce.

The exact deterministic handling of fractional damage, projectile lifetime/range across ricochets, reflection normals, corner contacts, and degenerate multi-collider contacts should be fixed during implementation and covered by tests. Those details must preserve the damage ladder and maximum-three-bounce contract above.

## Gameplay role

The Latch-9 should create a different decision from the Longwatch:

- **Longwatch DMR:** high damage, very fast current cadence, hitscan, long range, finite demo ammunition.
- **Latch-9 Standard Plasma:** weaker, slower, visible projectile, effectively infinite energy.
- **Latch-9 Bouncing Plasma:** lower damage still, but able to reach enemies through deliberate ricochet geometry.

The Bouncing mode is intended as a tactical geometry tool, not a raw-DPS upgrade. A player with a clear line of sight should generally prefer Standard Plasma when using the Latch-9; Bouncing Plasma earns its value when the environment makes an indirect shot useful.

## Presentation direction

The approved concept establishes a compact rustic plasma pistol with an industrial/salvage silhouette. Production player art must remain inside Rustline Canonical 28 and follow the established layered-player presentation conventions.

Current visual direction:

- compact pistol-scale silhouette;
- rustic plasma / industrial salvage construction;
- visibly technological enough to communicate an energy weapon;
- should not look like a clean modern ballistic handgun or polished sci-fi service pistol;
- charging/cooling feedback should make the slow shot cadence readable without requiring ammunition UI.

## Incremental player-art integration — 2026-09-14

The first production package is now authored:

- Idle uses all **19 canonical right-facing aim directions** from `+90°` through `-90°`;
- each direction contains the canonical **two Idle frames**;
- the opposite hemisphere continues to use runtime horizontal mirroring;
- the source sheets are `160×96`, representing two horizontal `80×96` armed cells with the shared body pivot at `(24, 8)` source pixels.

The current integration is deliberately a **presentation-only Editor Play Mode preview**. `PlayerLatch9AimPresenter2D` owns the shared Arms/Weapon renderer only while the body is in Idle. Any state without authored Latch-9 art — currently Run, Backpedal, Crouch, Jump, Fall, Land, Wall Brace, Wall Kick, LedgeClimb, and any other unsupported state — immediately releases the renderer to `PlayerUnarmedArmsPresenter2D`, so the existing unarmed animation is shown instead.

`Latch9IdlePreviewPlayMode` temporarily disables Longwatch presentation and weapon gameplay only for the running Editor play session, builds the 19 Latch-9 Idle directions from their full fixed-cell textures, and leaves the prefab/scene untouched. This exists so angle, scale, hand placement, mirroring, and silhouette can be human-tested in-engine before hundreds of additional Latch-9 frames are authored. It is not the final weapon-selection architecture and is not included as a claim that Latch-9 gameplay exists.

The committed Unity sprite metadata for this first art pass currently contains tight per-sprite rectangles rather than the final fixed `80×96` import rectangles. The preview intentionally constructs fixed-cell sprites in memory from each `160×96` texture, so the visual test uses the canonical body-relative pivot without mutating the imported assets. Production integration should normalize the importer metadata when the package is promoted from preview to permanent runtime content.

## Current status

- gameplay behavior: **documented / approved direction**;
- visual concept: **approved as the production reference direction**;
- Idle player art: **19 directions × 2 frames authored**;
- Idle in-engine angle preview: **implemented for Editor Play Mode**;
- unsupported Latch-9 states: **explicit Unarmed fallback during preview**;
- Standard Plasma runtime: **not implemented**;
- Bouncing Plasma runtime: **not implemented**;
- fire-mode toggle runtime: **not implemented**;
- remaining player presentation package: **in progress**.

The approved concept and authored player overlays are the authority for the eventual Latch-9 appearance. Do not treat the temporary Idle preview harness as the final weapon-selection or gameplay implementation.
