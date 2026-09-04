# Rustline Player / Weapon Art Pipeline

This document defines the production contract for the Rustline layered player and authored weapon presentation used by M2 gunplay.

The goal is to preserve authored pixel-art quality while supporting a large weapon roster and directional aiming. Runtime transform rotation of the final arm/weapon artwork is not the canonical production solution.

## Core decision

The player is a layered sprite character.

The historical composite player artwork was decomposed into two aligned visual layers:

1. **Body** — head, torso, legs, equipment, and every non-arm pixel.
2. **Arms / Weapon overlay** — the arm/hand pixels that complete the current pose, plus the equipped weapon when armed.

The Body remains in the canonical **48×64 px** player cell. Unarmed Arms also remain **48×64 px** and share the same bottom-center pivot `(0.5, 0.0)`, frame boundaries, and animation timing.

Armed overlays are allowed to use a larger authored cell when the weapon silhouette needs space outside the Body cell. The first validated armed geometry is the Longwatch DMR **80×96 px** cell documented below.

For an unarmed player:

```text
Body layer
+
Unarmed Arms layer
=
accepted layered player appearance
```

The old root-level composite player PNGs were intentionally removed from HEAD in `5cf163b`. They must not be restored as production assets. Historical copies remain recoverable from Git history at `da2fbb96c051c9402f46ca220138d6c7eb57ef79` for informational comparison only.

The authored split is not a literal pixel subtraction. Adjacent body pixels may be deliberately retouched where a historical composite contained arm-dependent shading, occlusion, contouring, or cleanup. Historical composite comparison is a visual diagnostic, not a byte-equality oracle.

## Why authored full-cell overlays

Arms and armed poses are authored in fixed transparent cells, not as a small floating sprite plus a runtime shoulder coordinate.

This intentionally trades asset count for absolute artistic control. Rustline production sprites are tiny, so storage cost is negligible compared with the benefit of deterministic alignment and authored silhouettes.

A fixed-cell transparent overlay means:

- every arm/weapon pixel has its final production position;
- no per-state shoulder coordinate is required merely to line up the overlay;
- Body and overlay share one world-space body reference point;
- GIMP/XCF source files preserve exact frame alignment;
- pixel-art cleanup is performed once in authored art instead of delegated to runtime rotation/resampling;
- different overlay cell sizes may coexist as long as their pivots represent the same body reference point.

## Unarmed production sheets

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

Each Body/Unarmed Arms pair preserves the source sheet's frame count, frame order, **48×64** cell dimensions, pivot, and timing.

## Armed overlay geometry

### Current canonical first-weapon cell

The Longwatch DMR establishes the first production armed overlay geometry:

```text
armed overlay cell = 80×96 px
PPU                = 16
```

Inside each right-facing `80×96` armed cell, the canonical `48×64` Body reference rectangle is:

```text
x      = 0
 y     = 8
width  = 48
height = 64
```

Equivalent authored padding relative to the Body reference is:

```text
left   = 0 px
right  = 32 px
top    = 24 px
bottom = 8 px
```

The Body and armed overlay must represent the same world-space body pivot. Therefore the armed sprite pivot is:

```text
pivot in armed-cell pixels = (24, 8)
normalized pivot           = (24/80, 8/96)
                           = (0.30, 0.083333333...)
```

The Body itself remains:

```text
Body cell  = 48×64
Body pivot = (24, 0) px = normalized (0.5, 0.0)
```

This lets `BodySpriteRenderer` and `ArmsWeaponSpriteRenderer` remain on the same transform without a weapon-specific runtime position offset.

For right-facing authored art, the extra 32 horizontal pixels live in front of the character. Horizontal `flipX` moves that extra space to the opposite side when the player faces left, while preserving the common pivot.

The `80×96` geometry is the approved Longwatch first-weapon contract. Future weapons should reuse it when practical. A larger armed cell may be introduced only when a real silhouette demonstrably requires it; do not shrink or distort artwork merely to satisfy an arbitrary cell size.

## Armed aiming model

Rustline uses **continuous gameplay aim** but **discrete authored visual aim sprites**.

Gameplay systems retain the exact mouse/stick aim vector for projectile/hitscan direction. The player artwork selects the nearest authored direction.

### Angle convention

For right-facing authored artwork:

- `0°` is perfectly horizontal to the right;
- positive angles point upward;
- negative angles point downward.

### Canonical right-facing aim set

Every aim-capable armed state is authored facing right at 10-degree intervals:

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

Horizontal mirroring supplies the opposite hemisphere. The two vertical directions are shared geometrically, yielding 36 unique 10-degree visual directions around the full circle. Maximum visual angular error is 5 degrees while gameplay aim remains continuous.

## Armed sheet storage contract

A **sprite** means one individually authored final armed cell. It does not need to be stored as a separate PNG file.

For the first Longwatch DMR Idle package, each aim direction is stored as one horizontal two-frame PNG:

```text
sheet size = 160×96 px
cell size  = 80×96 px
frame 0    = left cell
frame 1    = right cell
```

There are 19 direction sheets, therefore **38 final Idle armed sprites**.

Production folder:

```text
Assets/Art/Characters/Player/Sprites/Arms/Armed/
└── longwatch_dmr/
    └── Aim/
        └── Idle/
```

Canonical Longwatch Idle filenames:

```text
player_salvager_longwatch_dmr_idle_aim_p90.png
player_salvager_longwatch_dmr_idle_aim_p80.png
player_salvager_longwatch_dmr_idle_aim_p70.png
player_salvager_longwatch_dmr_idle_aim_p60.png
player_salvager_longwatch_dmr_idle_aim_p50.png
player_salvager_longwatch_dmr_idle_aim_p40.png
player_salvager_longwatch_dmr_idle_aim_p30.png
player_salvager_longwatch_dmr_idle_aim_p20.png
player_salvager_longwatch_dmr_idle_aim_p10.png
player_salvager_longwatch_dmr_idle_aim_0.png
player_salvager_longwatch_dmr_idle_aim_m10.png
player_salvager_longwatch_dmr_idle_aim_m20.png
player_salvager_longwatch_dmr_idle_aim_m30.png
player_salvager_longwatch_dmr_idle_aim_m40.png
player_salvager_longwatch_dmr_idle_aim_m50.png
player_salvager_longwatch_dmr_idle_aim_m60.png
player_salvager_longwatch_dmr_idle_aim_m70.png
player_salvager_longwatch_dmr_idle_aim_m80.png
player_salvager_longwatch_dmr_idle_aim_m90.png
```

`p` means positive/upward and `m` means negative/downward. Avoid `+` and `-` characters in production filenames.

The earlier proposal to manually author one giant 19-column multi-row PNG is superseded for the first weapon by these per-angle sheets. A build/import tool may pack or index sprites internally later, but manual atlas assembly is not a production requirement.

The Longwatch Run package uses the same 19 directions and armed-cell geometry. Each direction is one horizontal six-frame PNG:

```text
sheet size = 480×96 px
cell size  = 80×96 px
frame 0    = x 0
frame 1    = x 80
frame 2    = x 160
frame 3    = x 240
frame 4    = x 320
frame 5    = x 400
```

Production folder:

```text
Assets/Art/Characters/Player/Sprites/Arms/Armed/
└── longwatch_dmr/
    └── Aim/
        └── Run/
```

Run filenames follow `player_salvager_longwatch_dmr_run_aim_<direction>.png`, using the same `p90` through `0` to `m90` ordering as Idle. There are **114 final Run armed sprites**.

For the current Run package and future Fall aim-capable art, preserve the same principles:

- one authored sprite for every `animation frame × aim direction` combination;
- fixed armed-cell geometry and common body pivot;
- deterministic frame order;
- direction naming using `pNN`, `0`, `mNN`;
- do not infer arm placement through runtime rotation.

## Longwatch DMR first-weapon source art

The Longwatch DMR is the first representative weapon used to validate the armed pipeline.

Editable authored source:

```text
ArtSource/Characters/Player/player_salvager_idle_armed.xcf
ArtSource/Characters/Player/player_salvager_run_armed.xcf
```

Versioned concept/reference art includes:

```text
ArtSource/Concepts/Longwatch_DMR_concept.png
ArtSource/Concepts/Longwatch_DMR_zero_degrees_concept.png
ArtSource/Concepts/Player_concept.png
```

The standalone weapon concept is a design reference. The final production authority for hand placement, silhouette, palette, and per-angle pixel cleanup is the authored Arms/Weapon overlay artwork.

The Longwatch Idle and Run packages are authored, deterministically imported, and integrated at runtime for all 19 right-facing angles. Idle supplies 2 frames per direction and Run supplies 6. Automated import, pointer mapping, renderer ownership, mirroring, facing independence, and Body-clock frame synchronization are implemented. Human native-scale approval of the corrected aim origin and Run presentation remains pending. Fall aim and Jump/Land/Roll carry art remain deferred.

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

Aim input may still be tracked internally so aiming resumes immediately when the character returns to an aim-capable state.

## Facing and mirroring

Production player/weapon artwork is authored facing right.

The runtime horizontally mirrors the complete player presentation when aim crosses into the left hemisphere. Body and Arms/Weapon must use the same facing state.

Do not double the arsenal merely to preserve asymmetrical details such as ejection ports. A dedicated left-facing artwork variant may be added later for a specific weapon only when mirroring creates a meaningful visual problem.

## Layering contract

Runtime layering remains conceptually simple:

```text
Player
└── Visual
    ├── BodySpriteRenderer
    └── ArmsWeaponSpriteRenderer
```

Unarmed:

```text
BodySpriteRenderer      = Body animation sprite
ArmsWeaponSpriteRenderer = matching 48×64 Unarmed Arms sprite
```

Armed:

```text
BodySpriteRenderer      = Body animation sprite
ArmsWeaponSpriteRenderer = selected 80×96 weapon overlay sprite
```

The renderer transform does not move when switching between unarmed and Longwatch artwork; sprite pivots encode the shared body reference.

If a future weapon requires more sophisticated overlap, source art may later be split into rear-arm / weapon / front-arm visual layers. Do not introduce that complexity until a real visual need appears.

## Production folder contract

```text
Assets/Art/Characters/Player/
├── Sprites/
│   ├── Body/
│   └── Arms/
│       ├── Unarmed/
│       └── Armed/
│           └── <weapon_id>/
│               ├── Aim/
│               │   ├── Idle/
│               │   ├── Run/
│               │   └── Fall/
│               └── Carry/
│                   ├── Jump/
│                   ├── Land/
│                   └── Roll/
└── Animations/
    ├── Body/
    └── Arms/
        ├── Unarmed/
        └── Armed/
```

Not every future folder must exist before its art exists. Do not create empty hierarchy merely for aesthetics.

Editable XCF and large reference/concept artwork belongs under top-level `ArtSource/`, outside Unity `Assets/`, so Unity does not import authoring files as runtime assets.

## Existing runtime presentation

Implemented Body clip names:

```text
Player_Body_Idle.anim
Player_Body_Run.anim
Player_Body_Jump.anim
Player_Body_Fall.anim
Player_Body_Land.anim
```

`Player_Body_Jump.anim` is a non-looping takeoff sequence with Body keys at `0.00`, `0.10`, and `0.26` seconds. Frame 1 is a 100 ms Y-anchored compression pose; Frame 2 is a 160 ms leg-extension pose with cubic ease-out catch-up to the root's current normal Visual position; Frame 3 is held while locomotion remains Jump. X movement, physical impulse, and camera root-follow are unchanged.

Jump dust is separate production art at `Assets/Art/Effects/Movement/player_jump_dust.png`: three 48×64 bottom-center-pivot cells. A grounded jump spawns its serialized one-shot prefab at the takeoff world position and facing; coyote jumps do not spawn dust.

The runtime deliberately uses one Animator on `BodySpriteRenderer`. `PlayerUnarmedArmsPresenter2D` observes the final displayed Body sprite and maps it to the matching Unarmed Arms sprite. A future equipped-weapon presenter takes ownership of `ArmsWeaponSpriteRenderer` while armed and returns ownership when unequipped; do not add a second Animator merely for armed aiming.

Longwatch Idle and Run use `PlayerLongwatchAimPresenter2D`, wired on the Player prefab and given its scene-specific `NativePixelPresentation` reference by the authoritative MovementLab setup. The script asset was safely renamed from `PlayerLongwatchIdleAimPresenter2D` while preserving its Unity `.meta` GUID. `PlayerInputReader` exposes the `Player/PointerPosition` Vector2 PassThrough action bound to `<Pointer>/position`. Physical pointer coordinates are converted through the current centered native-pixel output offset and integer scale into unclamped logical viewport coordinates, then through the actual World Camera into a continuous world-space aim direction.

The Longwatch aim origin is exactly **38 source pixels above** the shared Body/overlay pivot: `(0, 38 / 16) = (0, 2.375)` Unity units. This offset affects only the continuous pointer-to-world aim vector; neither renderer transform, the Visual transform, sprite pivots, nor physics move.

`LongwatchAimMath` mirrors left-hemisphere vectors conceptually into the right-authored hemisphere, retains continuous angle/direction data, and quantizes only the displayed pose to the nearest 10 degrees. Exact vertical input retains the last facing hemisphere and zero-length input retains the last valid selection. No final weapon sprite is rotated at runtime.

While locomotion presentation is Idle or Run, the Longwatch presenter calls `SetRendererOwnership(false)` on the unarmed presenter, drives coherent Body/overlay `flipX` from the aim hemisphere, and maps the final Animator-displayed Body frame directly to the same frame of the selected Longwatch angle. The sole Body Animator remains the clock: 2 Idle Body frames map to the 2 selected-angle Idle overlays, and 6 Run Body frames map one-to-one to the 6 selected-angle Run overlays. Aim can change without resetting the Body frame, Body frames can change without resetting aim, and movement direction remains independent of aim-facing so armed backward running is supported.

Idle and Run share one continuous selection and ownership path, including transitions between them. On Jump, Fall, or Land the presenter immediately releases its facing override and renderer ownership so the existing movement-facing and unarmed overlay resume unchanged. This unsupported-state fallback is intentional until those authored armed packages exist.

Mouse/pointer is the only armed-aim acceptance input in this pass. Firing, Fall armed aim, carry states, gamepad aim, inventory, and all weapon gameplay remain deferred.

`PlayerAnimator2D` continues to own locomotion-state selection. Armed aim presentation must not alter the accepted movement physics, jump presentation, camera behavior, collider, coyote time, jump buffer, or other M1A semantics.

## First weapon implementation sequence

Completed foundations:

1. Author Body and Unarmed Arms production layers.
2. Implement and validate synchronized layered unarmed presentation.
3. Freeze movement/jump presentation v1.
4. Choose the Longwatch DMR as the first representative weapon.
5. Author Longwatch Idle at all 19 right-facing directions using two Idle frames per direction.

Implemented validation milestones:

6. Import/slice all Longwatch Idle direction sheets as **80×96** cells with pivot `(24,8)` / normalized `(0.30, 0.083333333...)`. **Done.**
7. Implement continuous gameplay aim → right-authored hemisphere normalization → nearest 10° visual selection → horizontal mirroring for the opposite hemisphere. **Done for mouse/pointer Idle and Run validation.**
8. Let the armed presenter own `ArmsWeaponSpriteRenderer` without changing the Body animation or locomotion semantics. **Done for Idle and Run, with intentional unarmed fallback for unsupported states.**
9. Automate validation of all 19 directions, both Idle frames, all six Run frames, full 360° mirroring, transform/pivot stability, palette/import rules, and frame synchronization. **Done; human native-scale approval remains pending.**
10. Correct the Longwatch pointer aim origin to 38 source pixels / 2.375 Unity units above the shared renderer pivot. **Done; human approval remains pending.**
11. Expand the Longwatch package to Fall aim and Jump/Land/Roll carry poses only after the current visual gate.
12. Freeze the reusable armed import/presenter contract, then scale to additional weapons.

## Non-negotiable pixel-art rules

All existing Rustline production rules still apply:

- Body and Unarmed Arms cells remain canonical **48×64**;
- Longwatch armed aim cells are **80×96** with the documented common-body pivot;
- **16 PPU**;
- Point filtering;
- Full Rect meshes;
- binary alpha only;
- no antialiasing;
- Rustline Canonical 28 plus transparency only;
- no importer rescaling of production pixels;
- horizontal mirroring must preserve Body/Weapon alignment;
- judge final poses at native gameplay scale.
