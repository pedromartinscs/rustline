# M1 Movement

M1 is a compact single-player Rigidbody2D controller intended for rapid feel tuning. Gunplay and combat live in separate weapon/combat components; networking, roll/dodge, and moving platforms remain outside the current movement implementation.

## Controls

- Keyboard: A/D or Left/Right Arrow to move; Space to jump; S or Down Arrow to crouch.
- Gamepad: Left Stick or D-pad to move; South button to jump; downward Left Stick or D-pad to crouch.

The existing `InputSystem_Actions` asset contains one focused `Player` map with `Move`, `Jump`, `Crouch`, `PointerPosition`, and `Fire`. `PointerPosition` is a Vector2 PassThrough binding to `<Pointer>/position`. `PlayerAim2D` converts it through the native-pixel viewport and World Camera into continuous world-space aim, then owns the stable facing hemisphere independently of A/D movement. `Fire` supplies the separate weapon controller's semi-automatic input edge.

## Runtime structure

- `PlayerInputReader` collects Input System callbacks and latches jump press/release edges until the physics step consumes them.
- `PlayerAim2D` owns the explicit `AimOrigin`, continuous aim direction, valid-aim state, native-pixel pointer conversion, and a 5° facing hysteresis zone around both vertical axes.
- `PlayerMotor2D` applies horizontal velocity, posture changes, wall interaction, the committed LedgeClimb traversal, explicit gravity, jump cutting, coyote time, and jump buffering to a Dynamic Rigidbody2D in `FixedUpdate`. Grounded input with aim-facing uses 7 units/s forward and 4 units/s backward, crouch uses 3 units/s, and air speed remains 7 units/s regardless of aim.
- `PlayerGroundProbe2D` casts the stable player CapsuleCollider2D a short distance downward against the `Ground` layer and accepts only sufficiently upward-facing normals. Side-wall contacts do not ground the player.
- `PlayerEnvironmentProbe2D` uses the Ground-layer contact filter and fixed query storage for stand-clearance, near-vertical wall casts, terrain-defined ledge wall/top detection, destination capsule clearance, and support validation.
- `PlayerAnimator2D` selects Idle, Run, Backpedal, Jump, Fall, Land, Crouch Idle, Crouch Move, Wall Brace, or the motor-owned LedgeClimb override. Crouch, Wall Brace, and LedgeClimb use dedicated authored Body/Unarmed Arms packages. Wall Brace facing follows `WallSide`; LedgeClimb locks facing to captured `LedgeSide`; neither mutates continuous aim. Wall Kick deliberately uses the accepted Jump/Fall fallback; dedicated Wall Kick art is not currently required.
- `PixelCameraFollow2D` smooths in continuous world space, then snaps the rendered camera position to the 1/16-unit pixel grid.
- `PlayerMovementConfig` stores all important tuning in `Assets/Config/Player/PlayerMovementConfig.asset`.

The prefab root uses a standing vertical CapsuleCollider2D with size `1.05 × 2.75` and offset `(0, 1.375)`. Its bottom remains at the full-cell bottom-center pivot while excluding the antenna, backpack silhouette, and transparent cell width. The crouched capsule is `1.05 × 2.375` (38 source pixels) at offset `(0, 1.1875)`, preserving the exact same lower boundary while matching the authored crouch silhouette much more closely. The separate `Visual - 48x64 Full Cell` child is presentation-offset to `(0, -0.25, 0)` (four source pixels at 16 PPU); animation frames never change the physics root or collider.

`AimOrigin` is an explicit child of `Visual - 48x64 Full Cell` at local `(0, 2.375, 0)`, exactly 38 source pixels above the shared renderer pivot. Pointer input remains unclamped through Deep Space margins. Aim direction stays continuous; only the left/right facing hemisphere is hysteretic. Inside `abs(normalizedAim.x) <= sin(5°)`, the prior hemisphere is retained, defaulting to right when no prior aim exists.

Ground acceleration, deceleration, direction-change acceleration, and `Mathf.MoveTowards` behavior are unchanged. Crossing the aim hemisphere while holding movement therefore approaches the new 4 or 7 units/s cap naturally instead of snapping velocity. The Backpedal cap is grounded-only and both speed values remain human-tunable in `PlayerMovementConfig`.

Human runtime testing confirms the generic `PlayerAim2D` architecture, Run/Backpedal switching, the mechanical 7 units/s forward versus 4 units/s Backpedal policy, and the 5° vertical facing hysteresis. The revised four-frame Backpedal art is accepted; 4 units/s is the current movement-feel target.

MovementLab preserves the M0 separation of concerns: `IndustrialSurfaceRuleTile` supplies visuals, while a hidden Tilemap of simple Grid collider tiles feeds `TilemapCollider2D` into a `CompositeCollider2D` to avoid per-cell seams.

## Crouch and wall interaction

Combat crouch is grounded-only. Holding crouch changes the capsule to `1.05 × 2.375` at offset `(0, 1.1875)`, exactly **10 source pixels taller** than the previous crouch collider while keeping its lower boundary invariant. Releasing crouch performs an upward capsule cast for the exact missing standing height; a ceiling keeps the player crouched, and standing happens automatically after clearance returns. Airborne crouch input never shrinks a standing capsule. A crouched ground jump restores the standing capsule and uses the normal `12.5` units/s impulse only when that clearance query succeeds.

The canonical crouch presentation comes from the single six-frame sheets `player_salvager_body_crouch.png` and `player_salvager_arms_crouch.png`. `CrouchIdle` statically holds authored frame 0. Forward `CrouchMove` loops authored frames 0→5 at 7 fps; when horizontal travel is opposite aim-facing, the internal crouch-backpedal presentation uses the **same sprites** in reverse order 5→0 at the same 7 fps. No additional source artwork is required. Crouch intentionally has no breathing animation. Only the normal standing Idle keeps its accepted two-frame breathing motion. Body remains the sole Animator clock and the unarmed overlay follows the displayed Body frame one-to-one.

Longwatch crouch art is implemented and fire-capable. During `CrouchIdle` and `CrouchMove`, the Longwatch presenter owns the overlay and follows the displayed shared crouch Body frame.

Wall brace requires an airborne, non-ascending player to hold movement into a detected near-vertical wall. It caps descent at `4` units/s without freezing or climbing. A buffered Jump while braced launches at `8` units/s away and `11.5` units/s upward. The contacted side remains locked for `0.12` seconds; horizontal input cannot cancel the launch during that window and the same wall cannot immediately reattach. Normal air control resumes afterward. Ground contact remains solely the responsibility of `PlayerGroundProbe2D`, so walls cannot emit Land.

## Committed ledge climb

There is no LedgeGrab, hanging state, idle hang, shimmy, or separate climb input. `LedgeClimb` is an immediate committed one-shot transition from Fall to grounded locomotion. Entry requires the player to be airborne, at the apex or descending (`verticalVelocity <= 0.1`), outside Wall Kick lock, holding horizontal movement toward the ledge, and aim-facing the same hemisphere. The input direction—not horizontal velocity—is the intent authority, so a left-facing player can fall from a platform with positive backpedal momentum and climb its left-side ledge as soon as A/left is held. Neutral input, input away from the ledge, or facing away never auto-grabs.

Detection is normal Ground geometry rather than tags or trigger volumes. A near-vertical side wall is ray-probed around the authored contact zone, then a downward probe one source pixel inside the platform must find an upward-facing top. Their corner must lie within `0.25` units of the expected frame-0 contact. The geometry-derived destination places the standing root one source pixel above the top and one source pixel beyond the wall plus half the standing-capsule width; a non-mutating standing capsule overlap and downward support cast must both pass before entry.

The Body and Unarmed Arms sheets contain six `48×64` frames keyed at `0.00`, `0.10`, `0.20`, `0.30`, `0.40`, and `0.50` seconds. At 16 PPU, frames 0..4 drive the physical Rigidbody2D root through the right-side authored climb/contact offsets `(0,0)`, `(0.5,0.5)`, `(0.875,0.875)`, `(1.125,1.0625)`, and `(1.1875,1.1875)`; left-side traversal negates only X. Frame 4 remains at that calibrated ledge-contact position for its entire `0.40`–`0.50` second interval and is never translated after the hands reach the edge. Frame 5 is the recovery onto the platform, not Idle: its X is the geometry-derived final standing X, while its Y is exactly `capture.y + 31/16` units (`+31` source pixels). At the exact `0.60` second state boundary, the remaining vertical difference is handed off atomically to the validated standing physics root, the standing collider is restored, and presentation returns directly to normal Idle/Run. There is no post-animation settle or easing, and completion does not emit `Landed`.

Once started, movement release, opposite movement, aim changes, Jump, Crouch, and Fire cannot cancel or mirror the climb. Jump takeoff presentation is reset to the normal Visual baseline on entry. Longwatch has no LedgeClimb carry artwork: it releases `ArmsWeaponSpriteRenderer` to the six-frame unarmed presenter, and firing is blocked for the full traversal.

MovementLab keeps the human-verified Windows Release runtime repair from commit `047c49e`: `TilemapCompositeColliderInitializer2D.Awake` executes `TilemapCollider2D.ProcessTilemapChanges() → CompositeCollider2D.GenerateGeometry() → Physics2D.SyncTransforms()`. The later regression did **not** remove that code. The missing deterministic step was on the Editor/build side after the collision Tilemap was expanded: authored cells could be saved/exported without explicitly baking and validating the updated Composite geometry first. `RustlineM1ASetup` now bakes after Tilemap synchronization, and `ReleaseCollisionBuildGuard` repeats the bake against the exact scene copy Unity exports to a Player and aborts the build if the Composite has zero paths/points. The hidden collision Tilemap must remain `Ground` layer 6 with an enabled `TilemapCollider2D` using `Merge`, an enabled polygon `CompositeCollider2D`, a static `Rigidbody2D`, and the runtime initializer. See [`RELEASE_COLLISION.md`](RELEASE_COLLISION.md) before changing this pipeline.

## Jump presentation

The physical jump impulse remains immediate and all movement tuning is unchanged. `PlayerMotor2D` emits a presentation-only notification when a buffered or coyote jump is actually consumed; it does not delay or modify the impulse. The three-frame layered Jump clip is non-looping and uses explicit non-uniform keys:

- Frame 1 begins at `0.00` seconds and holds the takeoff compression for 100 ms. During this phase only the shared Visual parent's world Y is anchored at takeoff.
- Frame 2 begins at `0.10` seconds and holds the leg-extension launch pose for 160 ms. Visual Y catches the rising root using `1 - (1 - t)^3`, targeting the root's current normal visual position every rendered frame.
- Frame 3 begins at `0.26` seconds, after the Visual has returned exactly to its configured `(0, -0.25, 0)` local position, and is held while Jump remains selected.
- Fall begins through the unchanged velocity-based state selector. A short hop may enter Fall before Frame 3; the timed catch-up still restores the exact baseline without forcing the Jump pose.

Horizontal presentation is never anchored, so running-jump X motion remains immediate. `PixelCameraFollow2D` still tracks the physical root, not the compensated Visual child. Neither the root, Rigidbody2D, collider, ground probe, nor movement configuration is adjusted by presentation.

A grounded successful jump spawns one `PlayerJumpDust` object at the full-cell Visual pivot's takeoff world position. The dust uses three 48×64 cells at 16 PPU, holds each sprite for 80 ms, renders at sorting order 9 below the player, and destroys itself after its non-looping sequence. It is never parented to the player, snapshots takeoff facing once, and remains fixed while the player moves. Coyote jumps receive Visual takeoff presentation from their current position but deliberately spawn no floating dust. Landing dust is not implemented.

Human MovementLab inspection remains authoritative for compression readability, camera/root separation, eased extension, dust grounding and timing, mirrored appearance, and short-hop transitions.

## Initial tuning

These values are a starting point, not final feel approval.

| Setting | Initial value |
| --- | ---: |
| Maximum ground / air speed | 7 units/s |
| Maximum grounded Backpedal speed | 4 units/s |
| Maximum crouched ground speed | 3 units/s |
| Ground acceleration | 55 units/s² |
| Ground deceleration | 70 units/s² |
| Direction-change acceleration | 90 units/s² |
| Air acceleration | 30 units/s² |
| Input dead zone | 0.10 |
| Jump speed | 12.5 units/s |
| Early-release velocity multiplier | 0.45 |
| Coyote time | 0.12 s |
| Jump buffer | 0.12 s |
| Ascent gravity multiplier | 3.0 |
| Fall gravity multiplier | 4.5 |
| Maximum fall speed | 18 units/s |
| Ground cast distance | 0.075 units |
| Minimum ground normal Y | 0.65 |
| Land presentation duration | 0.22 s |
| Standing capsule size / offset | 1.05 × 2.75 / (0, 1.375) |
| Crouch capsule size / offset | 1.05 × 2.375 / (0, 1.1875) |
| Wall brace maximum fall speed | 4 units/s |
| Wall kick horizontal / vertical speed | 8 / 11.5 units/s |
| Wall-kick input / same-wall lock | 0.12 s |
| Ledge-climb maximum upward speed | 0.1 units/s |
| Ledge capture tolerance | 0.25 units |
| Ledge authored frame / committed duration | 0.10 s / 0.50 s |

Edit the config asset in the Inspector, then play `Assets/Scenes/MovementLab.unity`. The course exercises the original movement cases plus a crouch-only low tunnel with open auto-stand space and a deep wall-brace/wall-kick shaft. The main terrain floor remains the release-hardened Composite Tilemap; the tunnel ceiling is a deliberate precision Ground collider shifted upward by exactly **10 source pixels**, producing a 42 px opening (crouch collider 38 px fits; standing collider 44 px does not). This sub-cell ceiling is not a workaround for the historical Release floor bug. The shaft keeps the left wall at x=92, has exactly four open cells at x=93..96, and begins its right wall at x=97 so repeated alternating kicks are more comfortable without changing wall-kick tuning. The right block still ends at x=112, where the existing Longwatch firing-range floor begins. Falling below `-12` respawns the diagnostic specimen independently of the M3A enemy health/death model.

## Deterministic rebuild and validation

Use `Tools > Rustline > Rebuild M1A Movement Lab` to regenerate the controller asset, prefab, collision tile, scene, and build order. Existing movement-config values are retained. `Tools > Rustline > Validate M1A Movement` checks M1A assets and reruns the accepted M0 integration assertions.
