# Parallax Backdrops

This document is the canonical artistic and technical contract for Rustline's deepest environment layers. These layers create scale, depth and altitude; they never own gameplay collision.

## Shared presentation contract

- Canonical 28 palette only.
- 16 PPU.
- Point filtering.
- Mipmaps off.
- No texture compression for canonical source presentation.
- Transform scale `1,1,1`.
- Final runtime placement snapped to the `1/16 u` source-pixel grid.
- No fractional Transform scaling or unsnapped parallax motion.
- Backdrops must not imply reachable gameplay structure unless that implication is intentional.

The runtime native logical viewport is dynamic and currently capped by `NativePixelViewportMath.MaximumLogicalDimension = 1072` px per axis. A 1080 px source width therefore provides a small native-pixel overscan margin without runtime scaling.

## Horizontal Far Parallax

Production asset:

`Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png`

The source is **2160x1080 px** at native **16 PPU**, giving an exact horizontal tile width of **135 u**.

Runtime motion is driven by `PixelSnappedParallax2D`:

- X follows the World Camera at `0.94`.
- Y is screen-locked to the World Camera.
- Three adjacent copies are used: left, center and right.
- Re-centering happens by exact whole-tile intervals of `135 u`.
- Final placement is snapped to `1/16 u`.
- The source must remain horizontally seamless.

The horizontal layer is quieter than normal background machinery. It should use large distant industrial silhouettes and preserve transparent/negative-space regions where the deeper Vertical Depth Backdrop is intended to remain visible.

## Vertical Depth Backdrop

Production asset:

`Assets/Art/Environment/Parallax/VerticalDepth/vertical_depth_backdrop_a.png`

The accepted source is now **1080x4320 px** at native **16 PPU**. It is non-repeating, opaque, and is the deepest current environment plane at sorting order `-40`, behind the horizontal far parallax at `-30`.

Because the source height exceeds Unity's 4096 texture ceiling, its importer must use **maxTextureSize >= 8192**. The production setup enforces this so Unity cannot silently downscale the 4320 px source and damage pixel fidelity.

Runtime motion is driven by `PixelSnappedVerticalDepthBackdrop2D`:

- X follows the final rendered World Camera exactly.
- The final camera position owns framing, including presentation feedback such as recoil.
- Altitude sampling uses `PixelCameraFollow2D.ContinuousFollowPosition.y` when available, so presentation-only recoil does not advance or retreat the altitude field.
- Camera altitude, not raw player jump height, drives the vertical reveal.
- The asset never repeats vertically.
- Final X/Y placement is snapped to `1/16 u`.

The controller computes:

`revealTravelWorld = max(0, sourceHeightWorld - currentViewportHeightWorld)`

`altitudeT = clamp01((baseCameraAltitude - phaseBottom) / effectivePhaseHeight)`

`verticalOffset = lerp(+revealTravel / 2, -revealTravel / 2, altitudeT)`

The backdrop center is then placed at:

`(renderedCameraX, renderedCameraY + verticalOffset)`

and snapped to the normal source-pixel grid.

At a hypothetical 1072 px-tall logical viewport, the 4320 px source has **3248 px** of available reveal travel. Actual reveal travel depends on the World Camera's current orthographic height.

## Effective phase-height rule

The altitude mapping keeps the canonical minimum reference span of **4320 source pixels**:

`effectivePhaseHeight = max(actualPhaseHeight, 4320 px)`

At 16 PPU:

`minimumEffectivePhaseHeight = 4320 / 16 = 270 u`

The source height and the minimum phase-height are separate concepts. The new source happens to be 4320 px tall, but the minimum phase-height remains an intentional altitude-mapping rule rather than a texture-size-derived value.

The current Salvage Intake span is `phaseBottom = 0 u` and `phaseTop = 20 u`. The 270 u minimum therefore dominates, so the room reveals only a small portion of the complete 1080x4320 altitude field. If the reveal feels too fast or too slow during human testing, tune the virtual phase span rather than scaling the sprite or changing its PPU.

## Procedural Canonical-28 composition

The current `vertical_depth_backdrop_a.png` is procedurally authored, then quantized to exact Canonical 28 colors. The final PNG contains no interpolated/non-canonical colors.

Its visual language deliberately avoids the previous uniform ordered-dot field. Instead it uses multi-scale irregular masses and broken distant structures so the deepest plane has atmosphere without reading as full-screen static.

Primary colors used by the current source are:

- Deep Space `#01020b`;
- Deep Navy `#0d172c`;
- Shadow `#22374d`;
- Steel Shadow `#35405a`;
- Dark Metal `#425970`;
- Warm Shadow `#46241e`;
- Rust Mid `#7c3312`;
- Rust Dark `#b0461c`;
- rare Concrete `#be997e`.

The source top / surface direction carries more structural midtones and warm corrosion. Deeper source rows become progressively cooler and darker. The important rule is not a fixed ordered-dither percentage; it is the hierarchy:

**surface = more structure and warmth -> depth = cool navy/blue masses -> abyss = darker and quieter.**

All noise is baked into the source image. There is no runtime procedural noise, alpha fade, filtering or gradient generation.

## Salvage Intake integration

The normal authoring entry point is now a single command:

**Tools -> Rustline -> Apply Production Setup**

That command rebuilds the canonical Salvage Intake geometry and reapplies the current production dressing, including both parallax layers.

Managed roots:

- `Art Dressing - Preserve/Vertical Depth Backdrop v0 - Managed`
- `Art Dressing - Preserve/Far Parallax v0 - Managed`

Both backdrop controllers resolve `Camera.main` dynamically, so a deterministic scene rebuild cannot leave a stale serialized camera reference.

## Layer relationship

Deepest to nearest:

1. **Vertical Depth Backdrop** — non-repeating altitude field, sorting `-40`.
2. **Horizontal Far Parallax** — seamless/repeating distant industrial silhouettes, sorting `-30`.
3. **Normal background dressing** — machinery, pipes, gantries and architecture.
4. **Gameplay structure / player / foreground** according to the established environment hierarchy.

The two backdrop layers deliberately use orthogonal motion rules: the vertical layer communicates long-range altitude, while the horizontal layer supplies continuous lateral depth and distant industrial scale.
