# Latch-9 Bouncing Projectile Contract

This document defines the runtime behavior of the Latch-9 `Bouncing` shot mode.

## Reflection rule

A bouncing projectile reflects from receiverless collision geometry using the actual 2D surface normal returned by the collision query.

Let:

```text
d = normalized incoming projectile direction
n = normalized surface normal
```

The outgoing direction is the specular reflection:

```text
r = d - 2 * dot(d, n) * n
```

This means:

- the component of projectile motion tangent to the surface is preserved;
- the component along the surface normal is inverted;
- angle of incidence equals angle of reflection;
- projectile speed is unchanged by the bounce;
- the reflected vector is not quantized to the 10-degree weapon-art buckets.

Examples:

```text
Floor normal:  ( 0, +1)
Incoming:      ( +x, -y)
Reflected:     ( +x, +y)

Right wall hit, outward normal: (-1, 0)
Incoming:      ( +x, +y)
Reflected:     ( -x, +y)
```

The runtime implementation lives in `WeaponBounceMath2D.Reflect` and is consumed by `Latch9Projectile2D` after a receiverless geometry hit.

## Bounce count

The Latch-9 supports a maximum of **3 completed reflections**.

`bounceCount` means the number of reflections already completed:

```text
bounceCount 0 -> first surface may reflect
bounceCount 1 -> second surface may reflect
bounceCount 2 -> third surface may reflect
bounceCount 3 -> no further reflection
```

After the third reflection, the projectile continues normally along that reflected direction. It ends when it reaches the next blocking surface, a valid hit receiver, or its remaining range reaches zero.

## Valid targets versus geometry

An `IWeaponHitReceiver2D` is a valid target. Reaching one ends the projectile immediately and applies the current bouncing damage stage.

Receiverless level geometry is a bounce surface. It changes projectile direction but receives no damage and is never mutated by this weapon path.

Current bouncing damage by completed-reflection count:

```text
0 bounces -> 4 damage
1 bounce  -> 3 damage
2 bounces -> 2 damage
3 bounces -> 1 damage
```

## Range and speed

Bounces do not reset range. The projectile has one total range budget across the complete path, including every reflected segment.

Bounces also do not alter speed. Conventional and Bouncing Latch-9 projectiles currently use the same prototype speed configured in `Assets/Config/Weapons/Latch9.asset`.

## Post-bounce surface separation and immediate re-hit guard

A reflected projectile must leave the collision boundary before its next raycast. This matters especially with `CompositeCollider2D`, where a cast starting numerically on the contact boundary can immediately rediscover the same collider at distance zero and consume multiple ricochets in one frame.

After each completed reflection the runtime therefore:

1. moves the projectile **0.25 source pixel** along the collision normal and **0.25 source pixel** along the newly reflected direction;
2. remembers the collider that caused that reflection;
3. temporarily ignores only an immediate re-hit against that same collider when both the new hit distance and the actual distance travelled since the bounce are within **0.5 source pixel**.

The guard is intentionally narrow. It does **not** ignore the surface globally: once the projectile has travelled beyond the guard distance, that same collider may be hit again normally at another point and can consume another legitimate bounce.

The numerical separation is not counted as gameplay travel distance and does not alter the reflection angle, projectile speed, damage stage, or total range budget.
