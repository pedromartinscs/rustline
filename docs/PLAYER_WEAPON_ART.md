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
player_salvager_body_backpedal.png
player_salvager_body_jump.png
player_salvager_body_fall.png
player_salvager_body_land.png
player_salvager_body_crouch.png
player_salvager_body_wall_brace.png
player_salvager_body_ledge_climb.png

player_salvager_arms_idle.png
player_salvager_arms_run.png
player_salvager_arms_backpedal.png
player_salvager_arms_jump.png
player_salvager_arms_fall.png
player_salvager_arms_land.png
player_salvager_arms_crouch.png
player_salvager_arms_wall_brace.png
player_salvager_arms_ledge_climb.png
```

Future roll/dodge artwork follows the same convention:

```text
player_salvager_body_roll.png
player_salvager_arms_roll.png
```

Each Body/Unarmed Arms pair preserves the source sheet's frame count, frame order, **48×64** cell dimensions, pivot, and timing.

The crouch pair is one authoritative six-frame sheet per layer, not separate Idle and Move artwork. `CrouchIdle` statically reuses crouch frame 0 with no breathing, while `CrouchMove` loops frames 0..5 at an initial 7 fps. The accepted two-frame breathing motion remains exclusive to standing Idle.

The Wall Brace pair is two frames per layer and loops at 6 fps. It is authored touching a wall on the right: `WallSide == +1` presents unflipped and `WallSide == -1` presents with `flipX`; that visual override does not mutate continuous aim or its normal facing hemisphere. There is intentionally no armed Longwatch Wall Brace overlay: both hands and one leg are committed to wall contact, so unarmed Arms own the renderer, the weapon is not rendered, and firing is blocked.

The LedgeClimb pair is exactly six frames per layer and plays once at 10 fps. Frames 0..4 are authored for a platform on the right and mirror for a left ledge; physical root motion follows the source-pixel climb/contact offsets `(0,0)`, `(8,8)`, `(14,14)`, `(18,17)`, `(19,19)`. Frame 4 remains pinned at its calibrated ledge contact for the full 0.40–0.50 s interval. Frame 5 is recovery onto the platform, not Idle: its root Y is `capture + 31` source pixels and its X is the geometry-derived final standing X. At 0.60 s the physics root/collider moves directly to the validated standing placement and normal Idle/Run follows, with no post-animation settle or easing. There is deliberately no LedgeGrab/Hang state. No Longwatch LedgeClimb carry artwork exists, so the unarmed overlay owns all six matching Arms frames and firing is blocked.

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

The Longwatch Backpedal package deliberately uses four authored frames per direction rather than expanding to the six-frame Run cadence:

```text
sheet size = 320×96 px
cell size  = 80×96 px
frames     = x 0, 80, 160, 240
```

Production folder is `Aim/Backpedal`, filenames follow `player_salvager_longwatch_dmr_backpedal_aim_<direction>.png`, and the 19 sheets contain exactly **76 final Backpedal armed sprites**. The Body and Unarmed Arms Backpedal sheets are each exactly `192×64`, four horizontal `48×64` frames. Four frames are the authored animation contract, not an import or storage optimization; do not duplicate, interpolate, or reorder them. Standing Backpedal plays at 8 fps with keys at `0`, `0.125`, `0.250`, and `0.375` seconds for a 0.50-second loop. The physical movement cap remains 4 units/s, and Longwatch inherits this cadence directly from the sole displayed Body Animator clock.

The Longwatch Crouch package is one authoritative six-frame set per direction. Each of the 19 directions is one `480×96` PNG containing six horizontal `80×96` cells, stored in `Aim/Crouch` as `player_salvager_longwatch_dmr_crouch_aim_<direction>.png`, for **114 final crouch armed sprites**. Crouch Idle reuses armed frame 0 and never advances a breathing cycle. Forward Crouch Move follows displayed Body frames 0..5 one-to-one. Presentation-only crouch backpedal uses the same armed sprites as the Body Animator displays the shared crouch cycle in reverse, 5..0; there is no separately authored Longwatch CrouchBackpedal package.

For the current Run, Backpedal, and Fall packages, preserve the same principles:

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
ArtSource/Characters/Player/player_salvager_backpedal.xcf
ArtSource/Characters/Player/player_salvager_backpedal_armed.xcf
ArtSource/Characters/Player/player_salvager_crouch_armed.xcf
```

Versioned concept/reference art includes:

```text
ArtSource/Concepts/Longwatch_DMR_concept.png
ArtSource/Concepts/Longwatch_DMR_zero_degrees_concept.png
ArtSource/Concepts/Player_concept.png
```

The standalone weapon concept is a design reference. The final production authority for hand placement, silhouette, palette, and per-angle pixel cleanup is the authored Arms/Weapon overlay artwork.

The Longwatch Idle, Run, Backpedal, Crouch, and Fall packages are authored, deterministically imported, and integrated at runtime for all 19 right-facing angles. Idle supplies 2 frames per direction, Run and Crouch each supply 6, Backpedal supplies exactly 4, and Fall supplies one Body-clocked frame per direction. Jump and Land carry are also authored and integrated: Jump uses three 48×64 frames and Land uses two 48×64 frames, all selected one-to-one by the displayed Body frame. Longwatch crouch and carry integration are automated and complete, but human native-scale visual approval remains separate. No Longwatch locomotion carry assets remain pending; dedicated Wall Kick art is not required because the accepted Jump/Fall fallback remains adequate.

## Longwatch muzzle metadata authoring

Exact per-frame muzzle coordinates are generated as **offline-generated art metadata**, rather than maintained manually in Unity.

The canonical authoring reference is:

```text
ArtSource/Metadata/Weapons/longwatch_dmr/Muzzle/longwatch_dmr_muzzle_reference.png
```

It overlays Longwatch Idle frame 0 for all 19 authored right-facing directions and contains one blue muzzle marker per direction plus a single red radial-ordering anchor. The composite is used only to seed direction-specific extraction; clean pixel signatures must come from each isolated production Idle frame 0 so neighboring overlaid angles cannot contaminate matching.

The implemented Python/Pillow generator and JSON contract are specified in [`WEAPON_MUZZLE_METADATA.md`](WEAPON_MUZZLE_METADATA.md). It resolves 343 supported Longwatch muzzle points across Idle, Run, Backpedal, Crouch, and Fall and writes deterministic pivot-relative source-pixel offsets. `RustlineM1ASetup` consumes that JSON in the Editor and deterministically writes the strongly typed `LongwatchDMRMuzzleMetadata.asset`; runtime presentation performs only direct serialized lookups and never inspects JSON or PNGs.

Longwatch Crouch `m70`, `m80`, and `m90` are a deliberate exception: the muzzle leaves the 80×96 authored cell at those downward poses. They are therefore treated as unsupported muzzle poses rather than generator failures. The approved future presentation rule is to clamp those crouch visuals to `m60`; invalid-angle firing and red-crosshair feedback remain separate follow-up runtime work.

The reference, generator, deterministic per-frame JSON, and compact runtime presentation asset are versioned. The muzzle-flash presenter consumes the runtime copy for placement only. Hitscan and tracer-origin migration are **not** part of this integration and continue to use the existing `AimOriginWorld` path.

## Longwatch muzzle flash asset

A production Longwatch muzzle-flash spritesheet is versioned at:

```text
Assets/Art/Effects/Weapons/longwatch_dmr/longwatch_dmr_muzzle_flash.png
```

The sheet is exactly **180×9 px** and contains **10 deterministic variants × 2 frames**, laid out as 20 horizontal **9×9** cells:

```text
V0 F0 | V0 F1 | V1 F0 | V1 F1 | ... | V9 F0 | V9 F1
```

Each cell is authored right-facing with its attachment point at the center of source pixel `(0,4)`. The deterministic custom pivot is therefore normalized `(0.5 / 9, 0.5)`, not the left cell boundary. The effect uses only canonical Rustline FX colors: Muzzle White `#FEFEFE`, Muzzle Yellow `#FED437`, Warning Orange `#FBAB29`, Rust Orange `#ED7527`, plus full transparency. Alpha is binary.

The asset is deterministically imported as 20 `9×9`, 16 PPU, Point-filtered, Full Rect sprites. A persistent `LongwatchMuzzleFlash` child under the recoil-driven arms/weapon transform responds only to resolved shots. It resolves the pose actually rendered by `PlayerLongwatchAimPresenter2D` after that presenter runs, converts the serialized source-pixel offset at 16 PPU (negating only X when left-facing), and rotates to the authored visual angle. A local stride-7 sequence visits all ten variants before repeating; frame 0 is rendered on the shot frame, frame 1 on the next rendered frame, then the renderer is hidden. Crouch `m70`/`m80`/`m90` have no fabricated position and silently omit the presentation flash while gameplay remains unchanged. Human native-scale visual approval remains a separate gate.

## Aim/fire locomotion rules

The current artistic/gameplay direction deliberately distinguishes locomotion states.

### Current first-shot fire-capable states

- Idle
- Run
- Backpedal
- Crouch Idle
- Crouch Move
- Fall

These states use the full 19-direction authored aim set for the equipped weapon.

Fall uses its authored 19-direction, one-frame package. Its visual angle is quantized to the nearest 10 degrees while hitscan/fire direction remains the exact continuous aim.

### Weapon visible, but no aim/fire pose set

- Jump / upward launch phase
- Land
- Roll / dodge
- Wall Kick

The equipped weapon remains visible, but these states use a single authored carried/locked weapon presentation per animation frame rather than the 19-direction set. Firing is disabled while these states are active. Longwatch Jump is an exact three-frame 144×64 sheet and Land is an exact two-frame 96×64 sheet, both sliced into bottom-center-pivot 48×64 cells. These smaller cells are valid because the carry silhouettes fit inside the canonical Body cell and do not need the angle-extension space of an 80×96 aim pose.

Jump uses the Body clip's existing non-uniform keys at `0.00`, `0.10`, and `0.26` seconds; Land likewise follows its two displayed Body frames. The Body Animator is the sole clock. Carry presentation does not quantize aim, rotate the weapon, expose a muzzle-capable rendered pose, or require muzzle metadata. Background aim remains continuous, normal directional Fall aim resumes immediately after Jump, and normal grounded aim resumes after Land.

Wall Brace is a deliberate exception: no Longwatch carry is planned. Both hands and one leg are committed to the wall, so the Longwatch presenter releases the overlay, dedicated unarmed Wall Brace Arms are shown, the weapon is not rendered, and firing remains blocked.

Suggested filenames:

```text
player_salvager_<weapon_id>_jump_carry.png
player_salvager_<weapon_id>_land_carry.png
player_salvager_<weapon_id>_roll_carry.png
```

Aim input may still be tracked internally so aiming resumes immediately when the character returns to an aim-capable state.

`LedgeClimb` also intentionally has no Longwatch carry art, so the equipped overlay is released and the authored six-frame unarmed Arms package is shown. Firing stays disabled, while background aim remains available again immediately after the committed climb finishes.

## Facing and mirroring

Production player/weapon artwork is authored facing right.

The runtime horizontally mirrors the complete player presentation when aim crosses into the left hemisphere. Body and Arms/Weapon must use the same facing state.

Do not double the arsenal merely to preserve asymmetrical details such as ejection ports. A dedicated left-facing artwork variant may be added later for a specific weapon only when mirroring creates a meaningful visual problem.

## Layering contract

Runtime layering remains conceptually simple:

```text
Player
└── Visual
    ├── AimOrigin
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

The renderer transform does not move when switching between unarmed and Longwatch artwork; sprite pivots encode the shared body reference. Gun Feel v1 may temporarily offset the Longwatch overlay from that exact cached baseline after a resolved shot, but it restores the baseline exactly and never moves the Body or player root.

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
Player_Body_Backpedal.anim
Player_Body_Jump.anim
Player_Body_Fall.anim
Player_Body_Land.anim
Player_Body_CrouchIdle.anim
Player_Body_CrouchMove.anim
Player_Body_CrouchBackpedal.anim
Player_Body_WallBrace.anim
Player_Body_LedgeClimb.anim
```

`Player_Body_Jump.anim` is a non-looping takeoff sequence with Body keys at `0.00`, `0.10`, and `0.26` seconds. Frame 1 is a 100 ms Y-anchored compression pose; Frame 2 is a 160 ms leg-extension pose with cubic ease-out catch-up to the root's current normal Visual position; Frame 3 is held while locomotion remains Jump. X movement, physical impulse, and camera root-follow are unchanged.

Jump dust is separate production art at `Assets/Art/Effects/Movement/player_jump_dust.png`: three 48×64 bottom-center-pivot cells. A grounded jump spawns its serialized one-shot prefab at the takeoff world position and facing; coyote jumps do not spawn dust.

The runtime deliberately uses one Animator on `BodySpriteRenderer`. `PlayerUnarmedArmsPresenter2D` observes the final displayed Body sprite and maps it to the matching Unarmed Arms sprite. `PlayerLongwatchAimPresenter2D` takes ownership of `ArmsWeaponSpriteRenderer` in supported armed states and returns ownership for unsupported states; do not add a second Animator merely for armed aiming. Crouch Backpedal reverses the same six crouch frames at 7 fps, while forward Crouch Move plays frames 0..5 and Crouch Idle holds frame 0.

Weapon-independent `PlayerAim2D` is wired on the Player prefab. It owns `Player/PointerPosition`, the scene-specific `NativePixelPresentation` reference, unclamped physical-to-logical viewport conversion, continuous World Camera aim, valid-aim retention, the explicit AimOrigin transform, and the authoritative facing hemisphere. `PlayerLongwatchAimPresenter2D` consumes that state and contains no input or viewport reconstruction.

The Longwatch aim origin is exactly **38 source pixels above** the shared Body/overlay pivot: `(0, 38 / 16) = (0, 2.375)` Unity units. This offset affects only the continuous pointer-to-world aim vector; neither renderer transform, the Visual transform, sprite pivots, nor physics move.

`LongwatchAimMath` mirrors left-hemisphere vectors conceptually into the right-authored hemisphere, retains continuous angle/direction data, and quantizes only the displayed pose to the nearest 10 degrees. Exact vertical input retains the last facing hemisphere and zero-length input retains the last valid selection. No final weapon sprite is rotated at runtime.

While locomotion presentation is Idle, Run, Backpedal, Crouch Idle, Crouch Move, or Fall, the Longwatch presenter calls `SetRendererOwnership(false)` on the unarmed presenter and maps the final Animator-displayed Body frame directly to the same frame of the selected Longwatch angle. `PlayerAnimator2D` converts authoritative `PlayerAim2D.FacingLeft` gameplay state into matching `flipX` values on both renderers. The sole Body Animator remains the clock: 2 Idle, 6 Run, 4 Backpedal, 6 shared Crouch, and 1 Fall Body frame map one-to-one to their selected-angle overlays. Crouch Idle holds shared frame 0; crouch backpedal naturally follows reverse Body playback 5..0. Aim can change without resetting the Body frame, and Body frames can change without resetting aim.

Idle, Run, Backpedal, Crouch Idle, Crouch Move, and Fall share the continuous directional selection path. During Jump and Land the Longwatch presenter keeps renderer ownership and maps the final displayed Body frame one-to-one to the matching carry frame without exposing a muzzle pose. On Wall Brace or LedgeClimb it releases ownership so the dedicated unarmed overlay resumes; generic aim-facing remains continuous throughout. Crouch Idle holds authored crouch frame 0 and Crouch Move uses all six authored crouch frames in forward or reverse presentation order. Wall Brace uses its dedicated two-frame Body/Unarmed Arms presentation, LedgeClimb uses its six-frame one-shot package, and Wall Kick retains its accepted Jump/Fall fallback. Jump, Land, Wall Brace, Wall Kick, and LedgeClimb remain non-firing.

Mouse/pointer remains the only armed-aim input. Mouse-left fires the semi-automatic Longwatch hitscan during Idle, Run, Backpedal, Crouch Idle, Crouch Move, and Fall. The shot uses the exact continuous aim direction; the 10° selection remains presentation-only and also controls the attached muzzle-flash rotation. Gun Feel v1 applies a 1.5-source-pixel overlay kick and one-source-pixel camera impulse opposite that continuous shot direction, both recovering over 0.10 s. The metadata-positioned flash follows that recoil transform, while the reused 3-unit distal tracer and hitscan still derive from the resolved `AimOriginWorld` path. Jump, Land, Wall Brace, Wall Kick, and LedgeClimb remain blocked.

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
7. Implement continuous gameplay aim → right-authored hemisphere normalization → nearest 10° visual selection → horizontal mirroring for the opposite hemisphere. **Done for mouse/pointer Idle, Run, Backpedal, and Crouch validation.**
8. Let the armed presenter own `ArmsWeaponSpriteRenderer` without changing the Body animation or jump semantics. **Done for Idle, Run, Backpedal, Crouch Idle, Crouch Move, Fall, Jump carry, and Land carry, with intentional unarmed ownership for Wall Brace and LedgeClimb.**
9. Automate validation of all 19 directions, both Idle frames, all six Run and Crouch frames, full 360° mirroring, transform/pivot stability, palette/import rules, and frame synchronization. **Done; Run, the corrected origin, and revised four-frame Backpedal are human-approved as recorded below. Crouch awaits human native-scale approval.**
10. Correct the Longwatch pointer aim origin to 38 source pixels / 2.375 Unity units above the shared renderer pivot. **Done and human-approved.**
11. Add grounded armed Backpedal with four authored frames at all 19 directions, driven by generic aim-facing and a 4 units/s cap. **Implemented and human-approved; movement-speed feel tuning continues at 4 units/s.**
12. Add grounded armed Crouch using one six-frame set at all 19 directions, with Crouch Idle reusing frame 0 and reverse movement following the displayed Body frame. **Implemented; human native-scale approval remains open.**
13. Expand the Longwatch package to Fall aim and Jump/Land carry poses. **Done; Roll remains a future ability rather than a pending locomotion carry asset.**
14. Freeze the reusable armed import/presenter contract, then scale to additional weapons.

## Non-negotiable pixel-art rules

All existing Rustline production rules still apply:

- Body and Unarmed Arms cells remain canonical **48×64**;
- Backpedal remains exactly **4 authored frames** for Body, Unarmed Arms, and every Longwatch direction;
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
