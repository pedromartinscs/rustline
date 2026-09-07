# M3A Combat Foundation

M3A proves that the accepted Longwatch feels useful against one real, repeatable combat entity. It deliberately stops short of a complete enemy, AI, player-health, or encounter-spawning system.

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

## Prototype ground enemy

The MovementLab prototype is split into narrow components:

- `CombatHealth2D` on the enemy root, configured to `100` maximum health.
- `PrototypeGroundEnemy2D` on the root for a deterministic kinematic horizontal patrol, short damage pause, death stop, hitbox disable, and exact reset.
- `PrototypeGroundEnemyPresenter2D` on a removable LineRenderer child for Canonical28 living/flash colors and a disabled dead visual.
- `WeaponHitbox2D` beside a trigger `BoxCollider2D` on the explicit `Hitbox` child, routed to root health.
- `MovementLabPrototypeEnemyEncounter2D` on the diagnostic encounter root for reuse after a `1.25 s` delay; no instantiate/destroy loop or production spawning framework is involved.

The enemy spawns at `(164, 0.02)`, after the existing x=150 Ground occluder. It patrols at `1.75 units/s` between x=162 and x=166, initially moving right, never changes its authored spawn Y, and pauses for `0.12 s` after a successful nonlethal hit. Its trigger-only CombatTarget-layer hitbox does not push the player.

The prototype rhythm is intentionally `100 HP / 40 Longwatch damage`: 100 → 60 → 20 → 0, so the third normal shot kills. Death stops patrol, disables the weapon collider so later shots pass through, and disables the programmer-art silhouette. Reset returns the same instance to its exact spawn, restores full health and initial patrol direction, reenables the hitbox and living presentation, and clears stale flash/death state.

Still pending for later M3 work: enemy attacks, player health and death/restart, production enemy art, advanced AI/pathing, the flying enemy, combat audio, and broader encounter spawning.
