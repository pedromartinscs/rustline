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

The initial `1296x410` far-industrial panorama was a successful technical prototype for pixel-snapped parallax, but its bounded transparent canvas made the sprite read as a floating object. It is therefore not the final backdrop composition contract.

## Horizontal Far Parallax

The first production horizontal layer uses the existing asset identity:

`Assets/Art/Environment/Parallax/FarIndustrial/far_industrial_silhouette_a.png`

The production source is being re-authored as **2160x1080 px**.

### Motion

- The layer is always vertically framed to the camera. It has **no relative vertical parallax**.
- Its Y position follows the camera exactly, then remains pixel-snapped.
- Relative parallax occurs only on X.
- Horizontal motion remains deliberately subtle; the first tuning target stays near the current `0.94` camera-follow factor unless playtesting motivates a change.
- The asset repeats/wraps horizontally so the camera can never expose a left or right edge of the backdrop.
- Runtime implementation may keep multiple adjacent instances and recycle instances that leave the visible range, but the visible result must read as one continuous world plane.

### Art direction

The horizontal layer represents very distant industrial infrastructure: large masses, towers, pipes, trusses, service structures and silhouettes. It should remain visually quieter than normal background machinery.

The preferred language is sparse and low-value. Deep Space `#01020b` can dominate the image, with one or a small number of dark Canonical 28 structural colors. Ordered pixel dithering may imply intermediate values without alpha blending or non-canonical colors.

The source should be authored so horizontal repetition does not reveal a seam or an obvious isolated rectangular sprite. Important forms may continue through the left/right boundaries when useful for seamless tiling.

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
