# Rustline Player / Weapon Art Pipeline

This document defines the production contract for the Rustline player presentation before M2 gunplay implementation.

The goal is to preserve authored pixel-art quality while supporting a large weapon roster and directional aiming. Runtime transform rotation of a complete weapon/arm rig is not the canonical production solution.

## Core decision

The player is a layered sprite character.

The historical composite player artwork was decomposed into two perfectly aligned visual layers:

1. **Body** — head, torso, legs, equipment, and every non-arm pixel.
2. **Arms** — the arm/hand pixels that complete the current pose.

Both layers use the same canonical **48×64 px** cell, the same bottom-center pivot `(0.5, 0.0)`, the same frame boundaries, and the same animation timing.

For an unarmed player:

```text
Body layer
+
Unarmed Arms layer
=
accepted layered player appearance
```

The first implementation milestone proves this decomposition with idle/run/jump/fall/land artwork before any weapon artwork is integrated.

The old root-level composite player PNGs were intentionally removed from HEAD in `5cf163b`. They must not be restored as production assets. Historical copies remain recoverable from Git history at `da2fbb96c051c9402f46ca220138d6c7eb57ef79` for informational comparison only.

The authored split is not a literal pixel subtraction. Adjacent body pixels were deliberately retouched where the historical composite contained arm-dependent shading, occlusion, contouring, or cleanup. The accepted diagnostic comparison contains 372 visible differences across the 14 frames, including 55 alpha/silhouette differences. RGB values beneath fully transparent pixels are ignored for visual-equivalence diagnostics and must not be sanitized merely to reduce raw RGBA mismatch counts.

## Why full-cell overlays

Arms and armed poses are authored as **full 48×64 aligned sprite cells**, not as a small floating sprite plus a runtime position coordinate.

This intentionally trades asset count for absolute artistic control. Rustline production sprites are tiny, so storage cost is negligible compared with the benefit of deterministic alignment and authored silhouettes.

A full-cell transparent overlay means:

- every arm/weapon pixel has its final production position;
- no per-state shoulder coordinate is required merely to line up an overlay;
- body and overlay can be composited directly at the same transform/pivot;
- GIMP/XCF source files can preserve exact frame alignment;
- pixel-art cleanup is performed once in authored art instead of delegated to runtime rotation/resampling.

## Unarmed production sheets

Create a matching Body and Unarmed Arms sheet for every current locomotion state.

Canonical production filenames:

```text
player_salvager_body_idle.png
player_salvager_body_run.png
player_salvager_body_jump.png
player_salvager_body_fall.png
player_salvager_body_land.png

player_salvager_arms_idle.png
player_salvager_arms_run.png
player_salvager_arms_jump.png
player_salvager_arms_fall.png
player_salvager_arms_land.png
```

Future roll/dodge artwork follows the same convention:

```text
player_salvager_body_roll.png
player_salvager_arms_roll.png
```

Each Body/Arms pair must preserve the source sheet's frame count, frame order, cell dimensions, and timing.

## Armed aiming model

Rustline uses **continuous gameplay aim** but **discrete authored visual aim sprites**.

Gameplay systems may retain the exact mouse/stick aim vector for projectile/hitscan direction. The player artwork selects the nearest authored direction.

### Canonical right-facing aim set

Every aim-capable armed state is authored facing right at 10-degree intervals from vertical up to vertical down:

```text
+90
+80
+70
+60
+50
+40
+30
+20
+10
  0
-10
-20
-30
-40
-50
-60
-70
-80
-90
```

That is **19 authored directions** and 18 intervals.

Horizontal mirroring supplies the opposite hemisphere. The two vertical directions are shared geometrically, yielding 36 unique 10-degree directions around the full circle.

Maximum visual angular error is 5 degrees while gameplay aim remains continuous.

### Important terminology

A **sprite** here means an individually authored final 48×64 sprite cell. It does not need to be stored as a separate PNG file.

For efficient production, multiple sprites should normally be packed into one sheet.

For an aim-capable animation with `F` animation frames, the recommended sheet grid is:

```text
columns = 19 aim directions, ordered +90 ... 0 ... -90
rows    = F animation frames, in normal animation order
cell    = 48×64 px
```

Therefore a four-frame run sheet contains `19 × 4 = 76` authored sprite cells. This is deliberate. Every `animation frame × aim direction` combination may be corrected independently in GIMP.

Suggested armed sheet naming:

```text
player_salvager_<weapon_id>_idle_aim.png
player_salvager_<weapon_id>_run_aim.png
player_salvager_<weapon_id>_fall_aim.png
```

Example:

```text
player_salvager_latch_9_idle_aim.png
player_salvager_latch_9_run_aim.png
player_salvager_latch_9_fall_aim.png
```

## Aim/fire locomotion rules

The current artistic/gameplay direction deliberately distinguishes locomotion states.

### Aim-capable and fire-capable

- Idle
- Run
- Fall

These states use the full 19-direction authored aim set for the equipped weapon.

### Weapon visible, but no aim/fire pose set

- Jump / upward launch phase
- Land
- Roll / dodge

The equipped weapon remains visible, but these states use a single authored carried/locked weapon presentation per animation frame rather than the 19-direction set. Firing is disabled while these states are active.

Suggested filenames:

```text
player_salvager_<weapon_id>_jump_carry.png
player_salvager_<weapon_id>_land_carry.png
player_salvager_<weapon_id>_roll_carry.png
```

This restriction is an intentional gameplay/animation decision, not an asset-production shortcut. Aim input may still be tracked internally so aiming resumes immediately when the character returns to an aim-capable state.

## Facing and mirroring

Production player/weapon artwork is authored facing right.

The runtime horizontally mirrors the player presentation when aim crosses into the left hemisphere.

Do not double the entire arsenal merely to preserve asymmetrical weapon details such as ejection ports. A dedicated left-facing artwork variant may be added later for a specific weapon only when mirroring creates a meaningful visual problem.

## Layering contract

Initial runtime layering should conceptually remain simple:

```text
PlayerRoot
├── BodySprite
└── ArmsWeaponSprite
```

Unarmed:

```text
BodySprite      = body animation
ArmsWeaponSprite = matching unarmed-arms animation
```

Armed:

```text
BodySprite      = body animation
ArmsWeaponSprite = selected weapon overlay sprite
```

If a future weapon requires more sophisticated overlap, the authored source may later be split into rear-arm / weapon / front-arm visual layers. Do not introduce that complexity until a real visual need appears.

## Production folder contract

New layered production art should use this hierarchy:

```text
Assets/Art/Characters/Player/
├── Sprites/
│   ├── Body/
│   └── Arms/
│       ├── Unarmed/
│       └── Armed/
│           └── <weapon_id>/
│               ├── Aim/
│               └── Carry/
└── Animations/
    ├── Body/
    └── Arms/
        ├── Unarmed/
        └── Armed/
```

The layered production hierarchy is now authoritative. Historical root-level composite sheets are absent from HEAD by design.

Editable source artwork should be kept separately from production PNGs. When source-art storage is introduced, use the documented source-art exclusion convention rather than treating XCF files as runtime production sprites.

## Unity animation naming

Implemented Body clip names:

```text
Player_Body_Idle.anim
Player_Body_Run.anim
Player_Body_Jump.anim
Player_Body_Fall.anim
Player_Body_Land.anim
```

The runtime deliberately does not create a second set of Arms AnimationClips or a second Animator. The sole Animator drives `BodySpriteRenderer`. `PlayerUnarmedArmsPresenter2D` observes the final displayed Body sprite in `LateUpdate`, looks it up in an explicit serialized 14-entry Body-to-Arms map, and changes `ArmsWeaponSpriteRenderer.sprite` only when the Body sprite changes. The lookup dictionary is built once at initialization; runtime asset searches, string parsing, LINQ, `Resources.Load`, and `AssetDatabase` are not used per frame.

`PlayerAnimator2D` continues to select locomotion states, maintain the landing presentation timer, and determine movement-facing. It writes the same `flipX` value to both renderers. A future equipped-weapon presenter can call `SetRendererOwnership(false)` on the unarmed presenter before taking over `ArmsWeaponSpriteRenderer`, then restore unarmed ownership with `SetRendererOwnership(true)` when appropriate.

Weapon-specific animation/runtime naming will be finalized after the first weapon presentation package is produced and tested.

## First implementation sequence

1. Author Body and Unarmed Arms production layers.
2. Preserve exact 48×64 cells, frame counts, frame order, pivots, and timing.
3. Implement two synchronized player sprite layers in Unity.
4. Validate idle/run/jump/fall/land as a coherent layered presentation; historical composite comparison is diagnostic rather than an equality gate.
5. Only after that validation, produce one complete weapon presentation package.
6. Validate the 19-direction aim system in idle/run/fall plus carry-only jump/land/roll behavior.
7. Freeze the weapon-art sheet/import contract.
8. Scale the pipeline to the planned arsenal.

## Non-negotiable pixel-art rules

All existing Rustline production rules still apply:

- canonical 48×64 player cells;
- 16 PPU;
- Point filtering;
- Full Rect meshes;
- binary alpha only;
- no antialiasing;
- Rustline Canonical 28 plus transparency only;
- horizontal mirroring must preserve readability;
- judge final poses at native gameplay scale.
