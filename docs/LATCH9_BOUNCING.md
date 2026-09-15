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

## Numerical separation from the surface

After a reflection, the runtime advances the projectile by a very small epsilon along both the collision normal and the new reflected direction. This prevents the following raycast from immediately rediscovering the same surface because of floating-point/contact-boundary ambiguity. The epsilon is not part of gameplay distance or an artificial change to the reflection angle.
