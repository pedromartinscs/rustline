# Original Bombardier explosion source

Created for Rustline on 2026-10-22 with the built-in image_gen tool. No external sprite packs. The original 1536×1024 RGBA source is retained unchanged as an intermediate; its halo/partial alpha and approximate colors are not production assets.

## Exact prompt

Create ORIGINAL pixel-art animation source for a compact industrial grenade explosion in Rustline, a dark steel/rust refinery action-platformer. Transparent PNG spritesheet of SIX frames, exactly THREE equal columns and TWO equal rows, left-to-right top-to-bottom.
Native cell 64x64 pixels; native sheet192x128. Prefer exact integer8x enlarged source1536x1024 with clearly pixel-clustered native64x64 style. Every frame shares the EXACT SAME CENTER at local(32,32) on its cell. No foot baseline alignment: this is a radial blast around a fixed point. No camera motion, no shifting effect center. Leave4nativepixels transparent margin all sides, NOTHING beyond radius28nativepixels from center.
Frame1 compact intense WHITE/YELLOW jagged detonation flash radius10, sharp irregular bursts, a few orange points.
Frame2 rapid growth into asymmetric angular tongues of WHITE/YELLOW hot energy and ORANGE flame, radius21.
Frame3 full developed blast radius27: dramatic irregular silhouette, jagged curling flames, white-hot inner tear shapes, yellow lobes and orange/red rusty flame edges. NOT a perfect disk, not concentric circles, not a cartoon star icon.
Frame4 outward fragmentation at radius26: broken tongues of yellow/orange flame, separated hot pixel particles and small dark steel-gray smoke puffs. Center beginning to open.
Frame5 dissipation within radius25: fewer orange embers, broken warm-dark smoke clusters and gray-blue smoke, no white-hot center.
Frame6 final sparse dark steel-gray smoke fragments and very few tiny rust-orange cinders, within radius24. Transparent space between separated fragments, clearly near completion.
STYLE: cohesive detailed but readable native pixel art, chunky hard-edged color clusters, selective small spark pixels. Strong explosive force, industrial incendiary detonation. Match worn steel/rust game art. No geometric ring outlines, no circular stroke, no vector shapes, no radial gradient, no glow blur, no lens effects, no antialiasing, no semitransparency. Binary alpha only, no background, ground, shadows, text, grid lines, frame labels or unrelated objects.
Use ONLY this exact Rustline Canonical28 RGB palette plus transparency: #01020B #0D172C #22374D #35405A #425970 #687C90 #C9BBB1 #BE997E #46241E #7C3312 #B0461C #ED7527 #FDD045 #FBAB29 #F43C2C #FBD3B5 #C89F7E #996F56 #02869A #0BD3D6 #20EDE5 #56B753 #8C35D0 #FED437 #FEFEFE #B0AAAB #990E0E #15D8F2.
Prefer fire colors MuzzleWhite #FEFEFE, MuzzleYellow #FED437, HazardYellow #FDD045, WarningOrange #FBAB29, RustOrange #ED7527, Red #F43C2C, RustDark #B0461C, RustMid #7C3312, WarmShadow #46241E; smoke DeepNavy #0D172C, Shadow #22374D, SteelShadow #35405A, DarkMetal #425970, Steel #687C90. Do not introduce other colors. An original six-key animation rather than six unrelated explosions; constant fixed center and coherent expanding then dispersing motion. This source will be converted to exact palette, binary-alpha 64x64 Unity16PPU sprites for a real1.75world-unit damage radius.

## Reviewed conversion recipe

`RustlineCombatVfxSetup.RebuildExplosionSprites`: six source cells of 512×512, three columns/two rows. Sample about each fixed cell center at (256,256), one global 9:1 nearest-point scale. No per-frame centering, baseline shifts or resizing. Output six 64×64 cells in a 192×128 sheet. Remove pixels with alpha below 200, including the generated halo. Keep pixel centers only within 28 native pixels of the common (32,32) pivot, matching the unchanged 1.75 u damage radius at 16 PPU. Quantize surviving pixels to nearest Canonical28 RGB in byte-distance space, with alpha255; all other pixels become fully transparent. Slice Full Rect/center pivots with stable per-name IDs, Point/clamp/no mipmaps/no compression/no physics. The 4× review contact sheet is written under ignored Logs/CombatVfx.
