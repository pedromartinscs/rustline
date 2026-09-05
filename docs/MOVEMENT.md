# M1A Core Movement

M1A is a compact single-player Rigidbody2D controller intended for rapid feel tuning. It contains no combat, networking, roll/dodge, moving-platform, or future-gameplay abstractions.

## Controls

- Keyboard: A/D or Left/Right Arrow to move; Space to jump.
- Gamepad: Left Stick or D-pad to move; South button to jump.

The existing `InputSystem_Actions` asset contains one focused `Player` map with `Move`, `Jump`, and `PointerPosition`. `PointerPosition` is a Vector2 PassThrough binding to `<Pointer>/position`. `PlayerAim2D` converts it through the native-pixel viewport and World Camera into continuous world-space aim, then owns the stable facing hemisphere independently of A/D movement.

## Runtime structure

- `PlayerInputReader` collects Input System callbacks and latches jump press/release edges until the physics step consumes them.
- `PlayerAim2D` owns the explicit `AimOrigin`, continuous aim direction, valid-aim state, native-pixel pointer conversion, and a 5° facing hysteresis zone around both vertical axes.
- `PlayerMotor2D` applies horizontal velocity, explicit gravity, jump cutting, coyote time, and jump buffering to a Dynamic Rigidbody2D in `FixedUpdate`. Grounded input with aim-facing uses 7 units/s forward and 4 units/s backward; air speed remains 7 units/s regardless of aim.
- `PlayerGroundProbe2D` casts the stable player CapsuleCollider2D a short distance downward against the `Ground` layer and accepts only sufficiently upward-facing normals. Side-wall contacts do not ground the player.
- `PlayerAnimator2D` selects Idle, Run, Backpedal, Jump, Fall, or Land from grounded state and actual velocity. Run versus Backpedal uses the same generic velocity-against-facing helper as the motor. It flips only the presentation layers; collision is unchanged. Land presentation never blocks movement.
- `PixelCameraFollow2D` smooths in continuous world space, then snaps the rendered camera position to the 1/16-unit pixel grid.
- `PlayerMovementConfig` stores all important tuning in `Assets/Config/Player/PlayerMovementConfig.asset`.

The prefab root uses a fixed vertical CapsuleCollider2D with size `1.05 × 2.75` and offset `(0, 1.375)`. Its bottom remains at the full-cell bottom-center pivot while excluding the antenna, backpack silhouette, and transparent cell width. The separate `Visual - 48x64 Full Cell` child is presentation-offset to `(0, -0.25, 0)` (four source pixels at 16 PPU); the physics root and collider never change with animation frames.

`AimOrigin` is an explicit child of `Visual - 48x64 Full Cell` at local `(0, 2.375, 0)`, exactly 38 source pixels above the shared renderer pivot. Pointer input remains unclamped through Deep Space margins. Aim direction stays continuous; only the left/right facing hemisphere is hysteretic. Inside `abs(normalizedAim.x) <= sin(5°)`, the prior hemisphere is retained, defaulting to right when no prior aim exists.

Ground acceleration, deceleration, direction-change acceleration, and `Mathf.MoveTowards` behavior are unchanged. Crossing the aim hemisphere while holding movement therefore approaches the new 4 or 7 units/s cap naturally instead of snapping velocity. The Backpedal cap is grounded-only and both speed values remain human-tunable in `PlayerMovementConfig`.

Human runtime testing confirms the generic `PlayerAim2D` architecture, Run/Backpedal switching, the mechanical 7 units/s forward versus 4 units/s Backpedal policy, and the 5° vertical facing hysteresis. The revised four-frame Backpedal art is accepted; 4 units/s is the current movement-feel target.

MovementLab preserves the M0 separation of concerns: `IndustrialSurfaceRuleTile` supplies visuals, while a hidden Tilemap of simple Grid collider tiles feeds `TilemapCollider2D` into a `CompositeCollider2D` to avoid per-cell seams.

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

Edit the config asset in the Inspector, then play `Assets/Scenes/MovementLab.unity`. The course is arranged to exercise flat acceleration/reversal, gaps and ledge exits, different platform heights, a drop for buffered landing jumps, and a short step course. Falling below `-12` respawns the diagnostic specimen without introducing health or death systems.

## Deterministic rebuild and validation

Use `Tools > Rustline > Rebuild M1A Movement Lab` to regenerate the controller asset, prefab, collision tile, scene, and build order. Existing movement-config values are retained. `Tools > Rustline > Validate M1A Movement` checks M1A assets and reruns the accepted M0 integration assertions.
