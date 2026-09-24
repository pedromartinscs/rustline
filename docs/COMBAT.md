# M3 Combat Foundation and Bombardier

M3A established generic health and explicit weapon-hitbox routing. M3B graduates the single MovementLab prototype encounter into a programmer-art Bombardier with a committed ranged attack and repeatable player combat death.

## Generic damage and health

`DamageInfo2D` is a readonly value containing an integer amount, world hit point, shot/damage direction, and an optional generic `UnityEngine.Object` source identity. It does not reference `WeaponDefinition2D`. `DamageResult2D` records previous/current health, the clamped amount actually applied, and whether that transition killed the entity.

`CombatHealth2D` owns serialized maximum health, current health, alive/dead state, damage application, reset, and damaged/died/reset notifications. Non-positive damage and damage after death do nothing. Health is clamped at zero, death is emitted once per life, and `ResetHealth()` restores the exact configured maximum and permits one new death transition. The ordinary damage path uses value types and performs no component discovery.

This foundation is intentionally reusable by later enemies, the player, and destructibles. It contains no weapon, enemy, patrol, presentation, loot, or audio policy.

## Explicit weapon-hitbox routing

`PlayerWeaponController2D` retains its same-collider receiver lookup:

```text
nearest hit Collider2D
  -> GetComponent<IWeaponHitReceiver2D>()
  -> WeaponHitbox2D on that exact child object
  -> explicit serialized CombatHealth2D reference on the entity root
  -> CombatHealth2D.ApplyDamage(DamageInfo2D)
```

There is no `GetComponentInParent` fallback. A child collider becomes damageable only when it explicitly carries `WeaponHitbox2D` and points to its owner health. The router preserves Longwatch damage, world point, continuous shot direction, and weapon-definition identity while translating into the weapon-independent damage contract. Damage multipliers and headshots are not part of M3A.

## Bombardier encounter

The MovementLab encounter uses narrow, production-named components:

- `CombatHealth2D` on the enemy root, configured to `100` maximum health.
- `Bombardier2D` on the root for deterministic kinematic patrol, Ground-only player detection, committed Windup, Recover, hit pause, death, and owned-bomb reset.
- `BombardierPresenter2D` on the LineRenderer child for Canonical 28 Patrol, Windup, hit, and Dead presentation plus a locked-target marker.
- `WeaponHitbox2D` beside a trigger `BoxCollider2D` on the explicit `Hitbox` child, routed to root health.
- `MovementLabBombardierEncounter2D` on the diagnostic encounter root for reuse after a `1.25 s` delay and after owned bombs resolve. Explicit reset clears them.
- `BombardierBallistics2D` solves the exact `0.85 s`, `22 u/s²` trajectory. `BombardierBomb2D` samples it in FixedUpdate and CircleCasts each segment against only Ground and PlayerDamageable. First contact immediately explodes without bounce; `3.0 s` timeout cleans up without damage.
- `BombardierExplosion2D` applies one flat `25` damage to a player inside `1.75 u`, subject to Ground occlusion except on direct player contact.

The enemy spawns at `(164, 0.02)`, after the existing x=150 Ground occluder. It patrols at `1.75 units/s` between x=162 and x=166, initially moving right, never changes its authored spawn Y, and pauses for `0.12 s` after a successful nonlethal hit. Its trigger-only CombatTarget-layer hitbox does not push the player. Detection uses a `12 u` horizontal and `6 u` vertical inclusive window and clear Ground-only LOS to the player capsule center. Windup locks that center once for `0.70 s`, launches one bomb, then Recover lasts `1.30 s`. Hit pause stops both timers. Death cancels an unlaunched attack and leaves launched bombs active.

The rhythm remains `100 HP / 40 Longwatch damage`: 100 → 60 → 20 → 0, so the third normal shot kills. Death stops patrol, disables the weapon collider so later shots pass through, and disables the programmer-art silhouette. Reset returns the same instance to its exact spawn, restores full health and initial patrol direction, reenables the hitbox and living presentation, and clears active bombs.

The Player root has `CombatHealth2D` at `100 HP`; its existing physical capsule is on PlayerDamageable layer 9. `MovementLabRespawn.RespawnNow()` serves both combat death and failure-height resets, restoring spawn position, Rigidbody and motor state, weapon transient state, full health, and the same Bombardier encounter. The player survives three valid explosions and resets on the fourth. See [`BOMBARDIER.md`](BOMBARDIER.md).

Still pending after the M3B programmer-art pass: production enemy art, advanced AI/pathing if later required, the flying enemy only if the demo needs it, combat audio, broader production encounter spawning, and final death/checkpoint presentation.
