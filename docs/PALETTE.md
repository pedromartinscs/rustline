# Rustline Canonical Palette

Rustline uses a fixed 24-color palette for production pixel art.

**Hard rule:** final production sprites, tiles, props, UI pixel art, and pixel FX must use only the colors below, plus full transparency where applicable. Generated candidates are not considered production-ready until their pixels have been validated or quantized to this palette.

| # | Name | Hex |
|---:|---|---|
| 1 | Deep Space | `#01020B` |
| 2 | Shadow | `#22374D` |
| 3 | Dark Metal | `#425970` |
| 4 | Steel | `#687C90` |
| 5 | Light Metal | `#C9BBB1` |
| 6 | Concrete | `#BE997E` |
| 7 | Rust Dark | `#B0461C` |
| 8 | Rust Orange | `#ED7527` |
| 9 | Hazard Yellow | `#FDD045` |
| 10 | Warning Orange | `#FBAB29` |
| 11 | Red | `#F43C2C` |
| 12 | Skin Beige | `#FBD3B5` |
| 13 | Fabric Tan | `#C89F7E` |
| 14 | Fabric Brown | `#996F56` |
| 15 | Cyan Dark | `#02869A` |
| 16 | Cyan | `#0BD3D6` |
| 17 | Neon Cyan | `#20EDE5` |
| 18 | Green | `#56B753` |
| 19 | Violet | `#8C35D0` |
| 20 | Muzzle Yellow | `#FED437` |
| 21 | Muzzle White | `#FEFEFE` |
| 22 | Smoke | `#B0AAAB` |
| 23 | Blood | `#990E0E` |
| 24 | UI Blue | `#15D8F2` |

## Usage principles

- Do not introduce near-duplicate colors for convenience.
- Prefer large readable value groups over noisy dithering.
- Deep Space and Shadow are the principal outline/shadow colors.
- Dark Metal, Steel, and Light Metal form the main metallic ramp.
- Rust Dark and Rust Orange establish Rustline's signature corrosion/accent family.
- Cyan Dark, Cyan, Neon Cyan, and UI Blue are reserved for technology, visors, energy, and readable interactive feedback.
- Hazard Yellow, Warning Orange, Red, Green, and Violet should remain selective gameplay accents.
- Muzzle Yellow and Muzzle White are primarily FX colors and should not dominate environment materials.
- Blood is reserved for damage/gore feedback.

## Generation rule

Every image-generation request for production pixel art must explicitly include the canonical palette and instruct the generator not to introduce other colors. Because generative image models cannot guarantee exact hexadecimal output, generated images must still be checked after generation and, when needed, remapped to the nearest canonical color before being accepted as production assets.
