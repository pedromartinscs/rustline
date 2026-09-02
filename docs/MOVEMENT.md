# M1A Core Movement

M1A is a compact single-player Rigidbody2D controller intended for rapid feel tuning. It contains no combat, networking, roll/dodge, moving-platform, or future-gameplay abstractions.

## Controls

- Keyboard: A/D or Left/Right Arrow to move; Space to jump.
- Gamepad: Left Stick or D-pad to move; South button to jump.

The existing `InputSystem_Actions` asset contains one focused `Player` map with only `Move` and `Jump` actions.

## Runtime structure

- `PlayerInputReader` collects Input System callbacks and latches jump press/release edges until the physics step consumes them.
- `PlayerMotor2D` applies horizontal velocity, explicit gravity, jump cutting, coyote time, and jump buffering to a Dynamic Rigidbody2D in `FixedUpdate`.
- `PlayerGroundProbe2D` casts the stable player CapsuleCollider2D a short distance downward against the `Ground` layer and accepts only sufficiently upward-facing normals. Side-wall contacts do not ground the player.
- `PlayerAnimator2D` selects Idle, Run, Jump, Fall, or Land from grounded state and velocity. It flips only the presentation sprite; collision is unchanged. Land presentation never blocks movement.
- `PixelCameraFollow2D` smooths in continuous world space, then snaps the rendered camera position to the 1/16-unit pixel grid.
- `PlayerMovementConfig` stores all important tuning in `Assets/Config/Player/PlayerMovementConfig.asset`.

The prefab root uses a fixed vertical CapsuleCollider2D with size `1.05 × 2.75` and offset `(0, 1.375)`. Its bottom remains at the full-cell bottom-center pivot while excluding the antenna, backpack silhouette, and transparent cell width. The separate `Visual - 48x64 Full Cell` child is presentation-offset to `(0, -0.25, 0)` (four source pixels at 16 PPU); the physics root and collider never change with animation frames.

MovementLab preserves the M0 separation of concerns: `IndustrialSurfaceRuleTile` supplies visuals, while a hidden Tilemap of simple Grid collider tiles feeds `TilemapCollider2D` into a `CompositeCollider2D` to avoid per-cell seams.

## Jump presentation

The physical jump impulse remains immediate and is not delayed or repositioned by animation. The three-frame layered Jump presentation runs once at 20 fps:

- Frame 1 is the fast anticipation/loading pose from `0.00` to about `0.05` seconds.
- Frame 2 is the fast impulse/dust pose from about `0.05` to `0.10` seconds.
- Frame 3 begins at about `0.10` seconds and is held as the ascent pose while Jump remains selected.
- Fall begins when the existing velocity-based state selector transfers presentation to Fall.

This is presentation timing only: there is no visual anchoring, root offset, collider adjustment, or movement-tuning change. Further timing changes should follow a human MovementLab feel test rather than altering physics to compensate for artwork.

## Initial tuning

These values are a starting point, not final feel approval.

| Setting | Initial value |
| --- | ---: |
| Maximum ground / air speed | 7 units/s |
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
| Land presentation duration | 0.25 s |

Edit the config asset in the Inspector, then play `Assets/Scenes/MovementLab.unity`. The course is arranged to exercise flat acceleration/reversal, gaps and ledge exits, different platform heights, a drop for buffered landing jumps, and a short step course. Falling below `-12` respawns the diagnostic specimen without introducing health or death systems.

## Deterministic rebuild and validation

Use `Tools > Rustline > Rebuild M1A Movement Lab` to regenerate the controller asset, prefab, collision tile, scene, and build order. Existing movement-config values are retained. `Tools > Rustline > Validate M1A Movement` checks M1A assets and reruns the accepted M0 integration assertions.
