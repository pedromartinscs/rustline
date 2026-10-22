# Original Sentry sprite source

Created with the built-in image_gen tool on 2026-10-22 for this project. No third-party sprites or external asset packs were used. The generated PNG is an intermediate source, not a production sprite: its irregular row spacing, high resolution, partial alpha and noncanonical colors are converted by RustlineSentryArtSetup into fixed, binary-alpha, Canonical-28 production cells. The source image is retained unmodified.

## Prompt

Create ORIGINAL game-ready pixel art for Rustline's SENTRY melee enemy. This is an original industrial salvage-refinery security automaton, not a copy of any existing game/artist/IP.
OUTPUT: transparent PNG sprite sheet, four columns by four rows, EXACT uniform grid, 16 frames total. Desired native sheet 192x256 pixels, each cell 48x64 source pixels. You may deliver exact integer 8x enlargement 1536x2048, but all pixel clusters must correspond to the 48x64 native cell grid. Hard pixel edges, no smoothing, no antialiasing, no gradients, binary alpha, no shadows on ground, no grid lines, no labels, no text.
STYLE: detailed but clean side-view pixel art, dark industrial sci-fi salvage world. Dense compact humanoid industrial automaton about 40 pixels high standing; helmet with narrow RED visor facing RIGHT, broad angular steel shoulders, yellow safety shoulder plate, dark cloth-like flexible joints, hydraulic forearm impact gauntlet on front arm (no gun), worn riveted steel, a few orange rust chips, sturdy separated boots. Distinct from a slim orange-suited human player. Readable large clusters, selective 1px highlights, convincing anatomy and consistent proportions across frames. Keep every pose strictly side-view facing RIGHT; no front-facing or rear-facing frames. Full figure always visible, no cropping.
COMPOSITION: 4x4 precisely equal cells. Every upright frame feet on same baseline near native y=62 from top, body pivot at cell x=24, generous transparent space above head. Body standing spans native x=15..31 and y=23..62; attack arm can reach x=46. Do not make character fill the whole 64px height. All poses share the same helmet, shoulder plates, joints, and colors.
Frame order LEFT TO RIGHT then TOP TO BOTTOM:
ROW 1: idle relaxed; idle subtle breathing/weight shift; patrol walk left foot forward; patrol passing stride.
ROW 2: patrol walk right foot forward; patrol passing opposite stride; alert braced stance with head raised; attack windup half-cocked front impact gauntlet drawn BACK.
ROW 3: attack windup fully raised/back gauntlet; committed strike gauntlet thrust horizontally FORWARD to RIGHT (no swoosh); recovery retract arm and bent knees; hit flinch recoil leaning backward.
ROW 4: death knees buckle; death dropping onto side; death collapsed on floor; death settled broken slumped heap. Dead figures remain anchored on same ground baseline, no disappearing parts.
ONLY these exact Rustline Canonical 28 hexadecimal colors (use a restrained 10-14 color subset plus alpha, do not introduce any other colors): #01020B #0D172C #22374D #35405A #425970 #687C90 #C9BBB1 #BE997E #46241E #7C3312 #B0461C #ED7527 #FDD045 #FBAB29 #F43C2C #FBD3B5 #C89F7E #996F56 #02869A #0BD3D6 #20EDE5 #56B753 #8C35D0 #FED437 #FEFEFE #B0AAAB #990E0E #15D8F2.
Favor dark neutral steel with light-metal edge groups, hazard-yellow plate and RED visor. No cyan glow, no magical effects, no background scenery, no extra props, no photorealism. This sheet will be sampled onto its 192x256 native pixel grid and quantized to this palette for Unity 16 PPU production.

## Conversion recipe

Reviewed source: 1086×1448 RGBA, four columns, row windows in top-origin pixels [50,390), [410,745), [750,1110), [1140,1425). Use one source-to-production scale of 7:1 for every pose, nearest point sampling, column-center x reference, per-cell opaque-foot baseline alignment. Alpha threshold 200; accepted pixels map to nearest Canonical-28 RGB in sRGB byte distance. No interpolation, runtime tint, rotation or sprite stretching. Output: 192×256, sixteen 48×64 frames, 16 PPU, bottom-center pivot. Production sheet and its imported sprite subasset IDs are checked into the repository; rebuilding reuses names/IDs.

Reviewed boundary corrections keep the nominal column-center reference: frame 9 extends its source right edge to x=570 to include its gauntlet; frame 10 starts at x=580 to exclude that neighboring fragment. Frame 12 ends at x=250 and frame 13 starts at x=250 so the falling pose's foot belongs only to its own frame. These are source cleanup coordinates, not gameplay offsets or per-pose stretching.

