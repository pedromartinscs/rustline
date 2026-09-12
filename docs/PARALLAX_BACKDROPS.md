# Parallax Backdrops

This document records the canonical artistic and technical direction for Rustline's deepest environment layers. These backdrops are presentation-only: they exist to create spatial depth, scale and a persistent sense of altitude inside the installation. They never own gameplay collision.

## Shared presentation contract

- Native logical presentation reference: **1080x1080 px**.
- Canonical 28 palette only.
- 16 PPU.
- Point filtering.
- Mipmaps off.
- No texture compression for canonical source presentation.
- Transform scale `1,1,1`.
- No fractional unsnapped runtime movement.
- Final backdrop positions are snapped to the same `1/16 u` source-pixel grid as the rest of Rustline's native-pixel presentation.
- Backdrops must not contain shapes whose readability strongly implies reachable gameplay structure unless that implication is intentional.

The initial `1296x410` far-industrial panorama was a successful technical prototype for pixel-snapped parallax, but its bounded transparent canvas made the sprite read as a floating object. It is retained only as historical prototype context; it is no longer the production composition contract.

## Horizontal Far Parallax

The first production horizontal layer uses:

`Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png`

The accepted production source is **2160x1080 px** at native **16 PPU**, giving an exact horizontal tile width of **135 world units**.

### Motion

- The layer is always vertically framed to the camera. It has **no relative vertical parallax**.
- Its Y position follows the already pixel-snapped World Camera exactly, then receives the normal final `1/16 u` snap.
- Relative parallax occurs only on X.
- The current horizontal camera-follow factor is `0.94`.
- The asset repeats/wraps horizontally so the camera can never expose a left or right edge of the backdrop.
- Runtime motion is driven by `PixelSnappedParallax2D`, which resolves `Camera.main` dynamically so a Salvage Intake graybox rebuild cannot leave a stale serialized camera reference.

The managed Salvage Intake implementation uses **three adjacent instances** of the same seamless source:

`[ left ][ center ][ right ]`

Each segment is separated by exactly `2160 px / 16 PPU = 135 u`. The controller keeps the three-segment strip near the camera. When the relative X displacement crosses half a tile width, the complete managed root is re-centered by one exact `135 u` tile interval. Because the source is seamless and the wrap distance is an exact source-pixel multiple, the re-centering must be visually indistinguishable from continuous scrolling.

The runtime order is important:

1. `PixelCameraFollow2D` finishes the World Camera's native-pixel position.
2. `PixelSnappedParallax2D` consumes that camera position.
3. horizontal parallax and whole-tile wrapping are evaluated;
4. the backdrop Y is locked to camera Y;
5. the final backdrop transform is snapped to `1/16 u`.

Do not replace this with fractional unsnapped Transform motion, UV scrolling, filtering, or runtime scaling.

### Art direction

The horizontal layer represents very distant industrial infrastructure: large masses, towers, pipes, trusses, service structures and silhouettes. It should remain visually quieter than normal background machinery.

The preferred language is sparse and low-value. Deep Space `#01020b` can dominate the image, with one or a small number of dark Canonical 28 structural colors. Ordered pixel dithering may imply intermediate values without alpha blending or non-canonical colors.

The source must be horizontally seamless. Important forms may continue through the left/right boundaries, and those boundaries must join without a visible discontinuity. The repeated image should read as one continuous industrial plane rather than three copies or an isolated rectangular sprite.

## Vertical Depth Backdrop

A second, deeper layer is planned as a **1080x2160 px** non-repeating vertical backdrop.

This layer is not primarily a traditional scrolling panorama. It acts as a subtle visual **altitude/depth indicator** for the phase: as the camera rises through the installation, progressively higher portions of the backdrop are revealed.

### Motion

- The layer is always horizontally framed to the camera. It has **no relative horizontal parallax**.
- Its X position follows the camera exactly, then remains pixel-snapped.
- The asset does **not** repeat vertically.
- The bottom of the backdrop corresponds to the lowest canonical altitude of the phase.
- The top of the backdrop corresponds to the highest altitude reference used by the mapping.
- Camera altitude, not momentary player jump height, drives the reveal.

The visible 1080 px viewport inside the 2160 px source leaves **1080 px of total vertical reveal travel**.

### Effective phase-height rule

The vertical mapping uses the height of the complete phase, with a minimum reference span of **four logical screens**:

`effectivePhaseHeight = max(actualPhaseHeight, 4 * 1080 px)`

Therefore:

`minimumEffectivePhaseHeight = 4320 px`

If a phase is vertically shorter than four screens, the backdrop still maps as though the phase were 4320 px tall. The player may therefore never reveal the entire `1080x2160` backdrop, and that is intentional: the deepest plane should suggest a world larger than the currently reachable route.

If the actual phase exceeds four screens vertically, the real full phase height becomes the mapping span.

Conceptually:

`altitudeT = clamp01((cameraAltitude - phaseBottom) / effectivePhaseHeight)`

`verticalReveal = altitudeT * 1080 px`

The resulting placement must be quantized to whole source pixels / `1/16 u` before rendering.

## Vertical color language

The Vertical Depth Backdrop should be much simpler than the horizontal industrial silhouette. Its primary job is atmospheric altitude communication, not readable machinery.

The baseline is Deep Space `#01020b`. Warm Canonical 28 pixels become progressively more frequent toward the upper/surface end of the asset.

The transition should be created through **ordered dithering / density changes**, not a conventional smooth gradient, runtime alpha or arbitrary blended colors. Near the deepest region the image may be almost pure Deep Space. With increasing altitude, sparse warm-brown pixels appear, then grow denser; higher regions may introduce a second lighter warm Canonical 28 tone if needed.

The intended read is not simply `black -> brown`. It is closer to:

- deep region: almost pure Deep Space;
- lower/mid region: rare dark warm pixels dispersed through Deep Space;
- upper region: increasing warm-pixel density;
- near-surface region: visibly warmer, while Deep Space still remains a major part of the pattern.

Exact warm colors and density thresholds should be chosen in GIMP while evaluating the asset at native presentation scale. Rust Dark `#b0461c` is a likely dark warm candidate, but the final palette combination is an art decision rather than a hardcoded runtime rule.

## Layer relationship

The intended deepest-to-nearest ordering is conceptually:

1. **Vertical Depth Backdrop** — non-repeating altitude field, deepest plane.
2. **Horizontal Far Parallax** — seamless/repeating distant industrial silhouettes.
3. **Normal background dressing** — world-space machinery, pipes, gantries and architecture.
4. **Gameplay structure / player / foreground** according to the established environment hierarchy.

These two backdrop layers deliberately use orthogonal motion rules. The vertical layer communicates long-range altitude; the horizontal layer supplies continuous lateral depth and distant industrial scale. Neither should ever read as an isolated sprite floating in empty space.
