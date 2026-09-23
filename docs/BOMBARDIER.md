# Bombardier — First Production Enemy Contract

This document defines the first programmer-art implementation of Rustline's first production enemy.

The internal working name is **Bombardier**. The final player-facing name and production art are deliberately deferred.

The purpose of this pass is not to build a reusable AI framework. It is to turn the proven M3A ground-enemy combat foundation into one readable, dodgeable PvE opponent that makes the accepted player movement and weapon systems matter.

## Scope

The first Bombardier implementation belongs in **MovementLab** as a controlled combat proof.

Do not place it in Salvage Intake yet.

The human acceptance sequence is:

1. prove the complete enemy loop in MovementLab with programmer art;
2. tune attack readability and player survivability;
3. keep EditMode, PlayMode, and Player tests green;
4. only then place the approved enemy into Salvage Intake and begin production art.

The existing player movement, Longwatch, Latch-9, weapon selector, native-pixel renderer, and environment collision contracts remain frozen unless a real bug is proven.

## Reused combat foundation

The Bombardier must reuse the existing generic combat pieces rather than introduce a parallel damage model:

- `CombatHealth2D`;
- `DamageInfo2D` / `DamageResult2D`;
- `WeaponHitbox2D`;
- the CombatTarget layer for enemy weapon hitboxes;
- existing Longwatch and Latch-9 damage delivery.

Initial enemy health remains **100 HP**.

That intentionally preserves the current Longwatch rhythm:

```text
100 -> 60 -> 20 -> 0
```

Three normal 40-damage Longwatch hits kill the Bombardier.

Latch-9 balance is not changed in this task. Human playtesting against the Bombardier is allowed to reveal whether its current damage needs a later tuning pass.

## High-level behavior

The Bombardier is a ground enemy with a small bounded horizontal patrol and one committed ranged attack.

It does **not** chase, jump, climb, pathfind, dodge, take cover, or use a behavior tree.

The state machine is explicit and intentionally small:

```text
PATROL
  |
  | valid player detection + clear line of sight
  v
WINDUP / TELEGRAPH
  |
  | windup completes
  | launch exactly one bomb
  v
RECOVER
  |
  | recovery completes
  +---- player still detectable -> WINDUP
  |
  +---- otherwise -------------> PATROL

any living state
  |
  | HP reaches 0
  v
DEAD
```

The bomb launch itself is a single transition event between Windup and Recover; it does not require a separate long-lived Throw state.

## Initial programmer-art tuning

These are **first playable tuning values**, not final balance locks:

| Parameter | Initial value |
| --- | ---: |
| Maximum health | 100 |
| Patrol speed | 1.75 u/s |
| Patrol half-distance | 2.0 u |
| Nonlethal hit pause | 0.12 s |
| Detection horizontal range | 12.0 u |
| Detection vertical range | 6.0 u |
| Attack windup / telegraph | 0.70 s |
| Attack recovery | 1.30 s |
| Bomb nominal flight time | 0.85 s |
| Bomb gravity magnitude | 22.0 u/s² |
| Bomb collision radius | 0.1875 u (3 px at 16 PPU) |
| Explosion radius | 1.75 u (28 px at 16 PPU) |
| Explosion damage | 25 |
| Bomb safety lifetime | 3.0 s |

A full-health player therefore survives three explosions and dies on the fourth.

The exact numbers may be tuned after human playtesting, but the behavioral contract below should remain stable.

## Detection

While in Patrol, the Bombardier may begin an attack only when all of these are true:

- the player is alive;
- absolute horizontal separation is at most **12 u**;
- absolute vertical separation is at most **6 u**;
- Ground geometry does not block line of sight between the enemy sensor origin and the player's combat point.

Detection is intentionally simple. There is no hearing, memory, pursuit, search state, or shared aggro system.

Once Windup begins, the attack is **committed**. Losing line of sight during that windup does not cancel the throw.

After Recover completes, detection is evaluated again.

## Telegraph and target locking

At the instant Windup begins:

- stop patrol movement;
- face the player;
- snapshot the player's current target point;
- retain that target point for the entire committed attack;
- begin the **0.70 s** telegraph.

The bomb does not continuously home or retarget during Windup or flight.

This is a deliberate gameplay rule: movement during the telegraph should let the player dodge the predicted landing point.

Programmer art must make Windup clearly distinguishable from Patrol. A simple state color change and/or target marker is sufficient for this pass.

## Bomb trajectory

The bomb follows a deterministic ballistic arc.

For the first implementation:

- use the serialized throw origin on the Bombardier;
- use the target point captured at Windup entry;
- use nominal flight time **0.85 s**;
- use downward gravity magnitude **22 u/s²**;
- solve the initial X/Y velocity required to reach the captured point after that nominal time;
- advance the projectile deterministically and use swept collision between previous and next positions so it cannot tunnel through thin geometry.

The trajectory may collide with terrain before reaching the captured target.

There is:

- **no bounce**;
- **no fuse**;
- **no homing**;
- **no remote detonation**.

## Contact and explosion contract

The bomb explodes on its **first valid contact** with either:

- Ground collision; or
- the player's damageable collider.

The bomb must not bounce.

The impact point becomes the explosion center.

Explosion behavior:

- radius: **1.75 u**;
- flat damage: **25**;
- no distance falloff;
- no knockback in this pass;
- at most one damage application to the player per explosion;
- Ground between explosion center and player blocks blast damage;
- direct contact with the player counts as an unblocked player hit.

The bomb does not damage its owning Bombardier in this first pass and there is no enemy-friendly-fire system.

The **3.0 s** lifetime is only a defensive cleanup bound. If the projectile somehow reaches it without valid contact, destroy it without creating a damaging explosion. Normal authored encounters should resolve by contact first.

## Player health and damage target

The player joins the existing generic health model:

- add `CombatHealth2D` to the player root;
- initial maximum health: **100**;
- the player's physical CapsuleCollider2D becomes an explicit damageable-player collision layer;
- reserve layer index **9** as `PlayerDamageable`;
- Ground remains layer 6;
- CombatTarget remains layer 7;
- RustlineHUD remains layer 8.

Do not add a second movement collider merely for damage.

The player's existing physical capsule is the bomb's direct-contact target and naturally follows the established standing/crouch collider rules.

Bomb collision/damage queries should be explicitly masked to the intended Ground and PlayerDamageable layers instead of broad Default-layer queries.

The player root may expose `CombatHealth2D` directly to the bomb's generic damage path. Do not create a weapon-specific player health contract.

## Player death and programmer-art restart

This pass needs a minimal repeatable death loop, not a final checkpoint system.

For MovementLab:

- when player health reaches zero, reset the player through the existing MovementLab respawn foundation;
- restore full player health;
- restore the player to the configured MovementLab spawn;
- clear player movement and weapon transient state using the existing reset paths;
- remove any active Bombardier bombs belonging to the encounter;
- reset the Bombardier encounter to its spawn/living state.

The reset must be deterministic and must not instantiate a second player or enemy.

A later production-area pass may replace this diagnostic restart policy with a real checkpoint/death presentation without changing the generic health/damage contracts.

## Hit reaction and attack timing

The existing **0.12 s** nonlethal hit pause remains.

During hit pause:

- patrol movement is paused;
- Windup/Recover timers are paused;
- no additional bomb is launched.

After hit pause ends, the prior living state resumes.

On death:

- stop enemy movement immediately;
- cancel an unlaunched Windup attack;
- disable the weapon hitbox;
- enter Dead once;
- do not launch a new bomb.

A bomb that was already launched before the enemy died remains dangerous until it resolves naturally or the encounter is reset.

## Programmer-art presentation

Do not create production sprites in this task.

The current LineRenderer-based prototype language may be evolved into a Bombardier presenter.

The programmer-art presentation must distinguish at least:

- Patrol / normal living state;
- Windup / telegraph;
- nonlethal hit reaction;
- Dead;
- bomb in flight;
- explosion radius briefly after contact.

Canonical 28 colors only.

Suggested first-pass palette roles:

- normal living silhouette: cyan-family existing prototype color;
- Windup: bright yellow/orange warning;
- hit reaction: canonical white;
- bomb: orange/red warning;
- explosion: short bright ring/shape.

Exact programmer-art geometry is not a production-art contract.

## Reset ownership and active bombs

The Bombardier runtime should own or explicitly track the bombs it launches.

Encounter reset must be able to remove all still-active bombs deterministically without a scene-wide search by name.

Avoid instantiate/destroy churn for the enemy itself; the existing enemy instance is reset in place.

Destroying short-lived bomb GameObjects is acceptable for this programmer-art pass. Pooling is deferred until profiling proves it useful.

## MovementLab migration

The existing M3A prototype enemy encounter is the starting point.

Prefer graduating/replacing that encounter rather than placing a second overlapping test enemy in the same firing-range space.

The implementation may rename/refactor the prototype enemy/presenter/encounter classes into Bombardier-specific production names if the migration remains clean and all references/tests are updated.

The generic classes `CombatHealth2D`, `DamageInfo2D`, `DamageResult2D`, and `WeaponHitbox2D` must remain generic.

Do not place the Bombardier in Salvage Intake in this pass.

## Required tests

### EditMode

Cover deterministic math/policy where possible:

- ballistic initial velocity reaches the requested target at nominal flight time in the no-collision case;
- left/right and vertical-offset trajectories;
- detection range boundaries;
- explosion radius boundary;
- Ground occlusion decision;
- reset/death state transitions that do not require frame scheduling.

### PlayMode

At minimum verify:

- Patrol remains within authored bounds;
- detection with clear LOS enters Windup;
- Ground-blocked LOS prevents Windup;
- Windup locks one target point and movement after lock does not retarget the pending bomb;
- one Windup launches exactly one bomb;
- bomb follows an arc and explodes on first Ground contact;
- bomb does not bounce;
- direct player contact explodes immediately;
- explosion inside radius deals exactly 25 once;
- player outside radius takes zero;
- Ground occlusion blocks blast damage;
- four valid explosions kill a 100 HP player;
- player death/reset restores 100 HP, spawn state, weapon transient state, enemy state, and removes active bombs;
- three normal Longwatch hits still kill the 100 HP Bombardier;
- Latch-9 still damages the enemy through the existing WeaponHitbox route;
- enemy death during Windup cancels the unlaunched attack;
- enemy death after launch does not destroy the already-flying bomb;
- encounter reset is reusable without duplicate enemy/player instances.

All current EditMode, PlayMode, and Player suites must remain green.

## Explicitly out of scope

Do not add in this pass:

- drone enemies;
- drone hubs/spawners;
- behavior trees;
- generic AI framework;
- NavMesh/pathfinding;
- pursuit/chase;
- melee attack;
- enemy dodge/cover logic;
- multiple bomb types;
- bouncing bombs;
- fuse timers;
- homing;
- explosion falloff;
- knockback;
- enemy friendly fire;
- loot;
- production enemy sprites/animations;
- production audio;
- production player-health HUD;
- Salvage Intake placement.

The acceptance gate is a **small, repeatable, readable MovementLab fight** that makes the player move to avoid bombs while using the already-approved weapon foundation.
