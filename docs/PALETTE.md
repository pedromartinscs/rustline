# Rustline Canonical Palette

Rustline uses a fixed **28-color palette** for production pixel art.

**Hard rule:** final production sprites, tiles, props, UI pixel art, and pixel FX must use only the colors below, plus full transparency where applicable. Generated candidates are not considered production-ready until their pixels have been validated or quantized to this palette.

| # | Name | Hex |
|---:|---|---|
| 1 | Deep Space | `#01020B` |
| 2 | Deep Navy | `#0D172C` |
| 3 | Shadow | `#22374D` |
| 4 | Steel Shadow | `#35405A` |
| 5 | Dark Metal | `#425970` |
| 6 | Steel | `#687C90` |
| 7 | Light Metal | `#C9BBB1` |
| 8 | Concrete | `#BE997E` |
| 9 | Warm Shadow | `#46241E` |
| 10 | Rust Mid | `#7C3312` |
| 11 | Rust Dark | `#B0461C` |
| 12 | Rust Orange | `#ED7527` |
| 13 | Hazard Yellow | `#FDD045` |
| 14 | Warning Orange | `#FBAB29` |
| 15 | Red | `#F43C2C` |
| 16 | Skin Beige | `#FBD3B5` |
| 17 | Fabric Tan | `#C89F7E` |
| 18 | Fabric Brown | `#996F56` |
| 19 | Cyan Dark | `#02869A` |
| 20 | Cyan | `#0BD3D6` |
| 21 | Neon Cyan | `#20EDE5` |
| 22 | Green | `#56B753` |
| 23 | Violet | `#8C35D0` |
| 24 | Muzzle Yellow | `#FED437` |
| 25 | Muzzle White | `#FEFEFE` |
| 26 | Smoke | `#B0AAAB` |
| 27 | Blood | `#990E0E` |
| 28 | UI Blue | `#15D8F2` |

## Why 28 colors

The original 24-color palette established Rustline's identity and gameplay accents well, but early character quantization exposed gaps in the dark metallic and warm/rust ramps.

Four bridge colors were added after comparing the canonical palette against an optimized 24-color palette generated from the first Rustline salvager concept:

- **Deep Navy `#0D172C`** — bridge between Deep Space and Shadow.
- **Steel Shadow `#35405A`** — intermediate steel/armor shadow.
- **Warm Shadow `#46241E`** — deep warm shadow for fabric, leather, grime, and rusted material.
- **Rust Mid `#7C3312`** — bridge from warm shadows into Rust Dark/Rust Orange.

These additions improve tonal continuity without replacing the semantic accent colors needed for gameplay readability.

## Usage principles

- Do not introduce near-duplicate colors for convenience.
- Prefer large readable value groups over noisy dithering.
- Deep Space, Deep Navy, and Shadow are the principal deep outline/shadow colors.
- Steel Shadow, Dark Metal, Steel, and Light Metal form the main metallic ramp.
- Warm Shadow, Rust Mid, Rust Dark, and Rust Orange form the principal warm/corrosion ramp.
- Cyan Dark, Cyan, Neon Cyan, and UI Blue are reserved for technology, visors, energy, and readable interactive feedback.
- Hazard Yellow, Warning Orange, Red, Green, and Violet should remain selective gameplay accents.
- Muzzle Yellow and Muzzle White are primarily FX colors and should not dominate environment materials.
- Blood is reserved for damage/gore feedback.

## Generation rule

Every image-generation request for production pixel art must explicitly include the canonical palette and instruct the generator not to introduce other colors. Because generative image models cannot guarantee exact hexadecimal output, generated images must still be checked after generation and, when needed, remapped to the nearest canonical color before being accepted as production assets.

The palette is a **global allowed set**, not a requirement that every asset use all 28 colors. Individual assets should use the smallest useful subset while staying inside the canonical set.
