# Latch-9 Plasma Sidearm

This document records the **current implemented gameplay and presentation contract** for the Latch-9 in the Rustline itch.io demo.

The Latch-9 is the compact, always-available fallback to the Longwatch DMR. It is intentionally weaker per shot, but uses visible projectile ballistics and effectively infinite energy rather than finite magazines.

The runtime configuration in `Assets/Config/Weapons/Latch9.asset` is the source of truth for current numerical tuning.

## Current gameplay configuration

Current implemented values:

- delivery: **projectile**;
- supported fire mode: **SemiAutomatic**;
- supported shot modes: **Conventional** and **Bouncing**;
- shot interval: **1/12 s** (12 accepted shots/s maximum);
- projectile speed: **30 world units/s**;
- total projectile range/path budget: **80 world units**;
- Conventional damage: **5**;
- Bouncing damage: **4 -> 3 -> 2 -> 1** across bounce stages 0..3;
- maximum completed reflections: **3**;
- ammunition policy: **Infinite**;
- reload: none.

These values are current prototype/demo tuning, not a final balance lock. Changes must be made deliberately in the weapon definition and corresponding tests/docs rather than inferred from older design notes.

## Shot modes

The gameplay enum remains:

- `WeaponShotMode2D.Conventional`
- `WeaponShotMode2D.Bouncing`

Right Mouse Button toggles between the supported shot modes while Latch-9 is equipped.

The HUD intentionally uses player-facing terminology:

- `Conventional` -> **LATCH-9 (PLASMA)**
- `Bouncing` -> **LATCH-9 (PHOTON FIELD)**

This is presentation terminology only; do not rename the runtime enum merely to match the HUD.

Both shot modes use the same infinite energy resource and the same current cadence/range/speed configuration.

### Conventional / PLASMA

Conventional is the direct Latch-9 projectile:

- launches one visible projectile from the exact resolved Latch muzzle point;
- travel direction is the exact continuous aim direction;
- authored 10-degree visual buckets do not quantize ballistics;
- deals **5 damage** to a valid combat target;
- stops at the first blocking collision;
- environmental hit receivers are not damaged by Latch-9.

### Bouncing / PHOTON FIELD

Bouncing uses the same projectile system but may reflect from non-combat geometry.

Current damage by completed reflections before the enemy hit:

| Completed reflections | Damage |
|---:|---:|
| 0 | **4** |
| 1 | **3** |
| 2 | **2** |
| 3 | **1** |

The projectile may complete at most **3 reflections**. After the third reflection it continues along the reflected direction until the next blocking collision, combat target, or range exhaustion; it may not perform a fourth reflection.

The full reflection, range-budget, geometry-vs-target, post-bounce separation, and immediate re-hit rules are specified in [`LATCH9_BOUNCING.md`](LATCH9_BOUNCING.md).

## Ammunition contract

Latch-9 uses effectively infinite energy for the current demo.

Therefore:

- firing never decrements a finite ammo counter;
- both shot modes share the same infinite resource;
- `R` is a no-op for Latch-9;
- there is no auto-reload or magazine state;
- the HUD resource line displays **∞**.

The gameplay constraint is cadence/positioning rather than ammunition conservation.

## Production presentation

Latch-9 is no longer an Editor-only Idle preview. It is a persistent production equipment state integrated with the same authoritative equipment selector as Longwatch and Unarmed.

Current authored presentation:

- Idle: 19 right-facing aim directions × 2 frames;
- Run: 19 directions × 6 frames;
- Backpedal: 19 directions × 4 frames;
- Crouch: 19 directions × 6 frames;
- Fall: 19 directions × 1 frame;
- Jump: 3 non-firing carry frames;
- Land: 2 non-firing carry frames;
- opposite hemisphere supplied by horizontal mirroring.

Aim-capable/firing states are:

- Idle;
- Run;
- Backpedal;
- Crouch Idle;
- Crouch Move;
- Fall.

Jump and Land keep Latch-9 visible through fixed carry art but do not expose a muzzle-capable pose and cannot fire.

Wall Brace and LedgeClimb intentionally release the armed overlay to the authored unarmed traversal presentation. Wall Kick follows the existing non-firing movement/presentation fallback.

## Muzzle and projectile origin

Latch-9 has exact muzzle metadata for all supported authored aim frames.

The runtime projectile origin is resolved from the **currently rendered Latch pose**. The launch direction remains `PlayerAim2D.ContinuousAimDirection`.

This separation is intentional:

- sprite bucket / authored angle -> presentation and muzzle attachment;
- continuous aim -> projectile direction.

Do not quantize projectile direction to the 10-degree art pose.

## Muzzle flash

Latch-9 uses separate authored flash banks for its two shot modes:

- Conventional / PLASMA: compact cyan Security-family flash;
- Bouncing / PHOTON FIELD: predominantly violet/electric flash.

`Latch9MuzzleFlashShotModeBinder2D` keeps the active flash bank synchronized with the gameplay shot mode.

The flash is driven by launch-time `ShotFired`, because the projectile begins immediately even though `ShotResolved` may occur later after travel/ricochets.

## Combat-target contract

Only an `IWeaponCombatTarget2D` is a valid Latch-9 damage target.

`IWeaponHitReceiver2D` is intentionally broader. Environmental receivers such as destructible/breachable scenery remain collision geometry for Latch-9:

- Conventional stops without notifying them;
- Bouncing may reflect from them without applying environmental damage.

This preserves Latch-9 as a combat projectile rather than a general-purpose destruction tool.

## Runtime ownership

The persistent production path is:

```text
PlayerWeaponEquipment2D
  -> PlayerWeaponController2D
  -> PlayerLatch9AimPresenter2D
  -> Latch9ProjectileEmitter2D
  -> Latch9Projectile2D
```

The equipment system is the sole authoritative selected-slot owner. The HUD observes this state; it is not a gameplay authority.

Latch projectiles already launched into the world are not destroyed merely because the player switches weapons.

## Current status

Implemented and production-wired:

- persistent weapon selection;
- full current locomotion presentation package;
- continuous aim + authored visual buckets;
- exact muzzle metadata;
- Conventional projectile mode;
- Bouncing projectile mode;
- maximum-three-reflection contract;
- mode-specific muzzle flash;
- infinite ammunition policy;
- HUD identity/mode/resource display;
- production prefab/setup wiring;
- focused EditMode/PlayMode coverage.

Still intentionally deferred:

- dedicated final balance pass;
- combat audio;
- final impact/breakup particle polish;
- any Latch-specific reload/resource economy (not required by the current demo design).

Older preview-only notes and the original 0.60 s / 22 damage / ~36 unit design targets are historical and are **not** the current runtime contract.
