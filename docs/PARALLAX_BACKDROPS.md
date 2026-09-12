# Parallax Backdrops

This document records the canonical artistic and technical direction for Rustline's deepest environment layers. These backdrops are presentation-only: they exist to create spatial depth, scale and a persistent sense of altitude inside the installation. They never own gameplay collision.

## Shared presentation contract

- Source-art design reference: **1080x1080 px**.
- Runtime native logical viewport is dynamic and currently capped by `NativePixelViewportMath.MaximumLogicalDimension = 1072` px per axis.
- Canonical 28 palette only.
- 16 PPU.
- Point filtering.
- Mipmaps off.
- No texture compression for canonical source presentation.
- Transform scale `1,1,1`.
- No fractional unsnapped runtime movement.
- Final backdrop positions are snapped to the same `1/16 u` source-pixel grid as the rest of Rustline's native-pixel presentation.
- Backdrops must not contain shapes whose readability strongly implies reachable gameplay structure unless that implication is intentional.

The 1080-pixel source reference remains intentional even though the runtime logical viewport currently tops out at 1072 px. The extra source pixels provide a small amount of overscan rather than requiring runtime scaling.

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

The preferred language is sparse and low-value. Deep Space `#01020b` may appear inside authored industrial masses, with one or a small number of dark Canonical 28 structural colors. Ordered pixel dithering may imply intermediate values without alpha blending or non-canonical colors.

The source must be horizontally seamless. Important forms may continue through the left/right boundaries, and those boundaries must join without a visible discontinuity. The repeated image should read as one continuous industrial plane rather than three copies or an isolated rectangular sprite.

Where the deeper Vertical Depth Backdrop is intended to remain visible, the horizontal layer must preserve transparent/negative-space regions rather than covering the entire frame with an opaque Deep Space rectangle.

## Vertical Depth Backdrop

The accepted production vertical layer is:

`Assets/Art/Environment/Parallax/VerticalDepth/vertical_depth_backdrop_a.png`

The source is **1080x2160 px** at native **16 PPU** and does not repeat vertically. It is the deepest current environment plane and renders behind the horizontal far parallax.

Runtime motion is driven by `PixelSnappedVerticalDepthBackdrop2D`.

### Motion

- The layer is always horizontally framed to the camera. It has **no relative horizontal parallax**.
- Its X position follows the already pixel-snapped World Camera exactly.
- The final rendered camera position owns framing, including presentation feedback such as recoil.
- Altitude mapping uses `PixelCameraFollow2D.ContinuousFollowPosition.y` when available, so presentation-only recoil/impulse does **not** advance or retreat the geological altitude field. If that component is unavailable, the rendered camera Y is the fallback sample.
- Camera altitude, not momentary raw player jump height, drives the vertical reveal.
- The asset never repeats vertically.
- At the phase bottom, the camera sees the lowest available portion of the source.
- As camera altitude increases, progressively higher source rows are revealed.
- Final X/Y placement is snapped to `1/16 u`.

The runtime does **not** hardcode a 1080-pixel viewport. It reads the World Camera's actual orthographic height and computes:

`revealTravelWorld = max(0, sourceHeightWorld - currentViewportHeightWorld)`

At the current maximum logical viewport height of 1072 px, the 2160-pixel source has **1088 px of available vertical reveal travel**. At smaller logical viewports the available reveal travel is correspondingly larger. This keeps the source edge-safe without scaling it.

Conceptually:

`altitudeT = clamp01((baseCameraAltitude - phaseBottom) / effectivePhaseHeight)`

`verticalOffset = lerp(+revealTravel / 2, -revealTravel / 2, altitudeT)`

The backdrop center is then placed relative to the final rendered camera at:

`(renderedCameraX, renderedCameraY + verticalOffset)`

and snapped to the normal source-pixel grid.

### Effective phase-height rule

The vertical mapping uses the height of the complete phase, with a minimum reference span of **four 1080-pixel source-design screens**:

`effectivePhaseHeight = max(actualPhaseHeight, 4320 px)`

At 16 PPU:

`minimumEffectivePhaseHeight = 4320 / 16 = 270 u`

If a phase is vertically shorter than 270 world units, the backdrop still maps as though the phase were 270 u tall. The player may therefore never reveal the entire `1080x2160` source, and that is intentional: the deepest plane should suggest a world larger than the currently reachable route.

If the actual complete phase exceeds 270 u vertically, the real full phase height becomes the mapping span.

The current Salvage Intake test configuration uses `phaseBottom = 0 u` and `phaseTop = 19 u`; the 270 u minimum therefore dominates and only a small fraction of the backdrop is intentionally revealed while climbing the current room.

### Authored vertical color progression

`vertical_depth_backdrop_a.png` uses Deep Space `#01020b` as the continuous base. Warm colors increase toward the source top / surface direction, then transition into cooler Shadow pixels and finally almost pure Deep Space toward the source bottom / deepest direction.

The accepted authored bands, measured downward from the top of the 2160-pixel source, are:

- `0–100 px`: 37.5% warm total — 25% Rust Dark `#b0461c`, 12.5% Concrete `#be997e`;
- `100–200 px`: 25% warm total — 18.75% Rust Dark, 6.25% Concrete;
- `200–300 px`: 18.75% warm total — 12.5% Rust Dark, 6.25% Concrete;
- `300–400 px`: 12.5% Rust Dark;
- `400–700 px`: 6.25% Rust Dark;
- `700–1000 px`: 3.125% Rust Dark;
- `1000–1100 px`: temperature transition — 4.6875% Shadow `#22374d` plus 1.5625% Rust Dark;
- `1100–1300 px`: 6.25% Shadow;
- `1300–1600 px`: 3.125% Shadow;
- `1600–1900 px`: 1.5625% Shadow;
- `1900–2160 px`: 0.78125% Shadow, using a sparse 16x16 ordered pattern.

These are ordered static pixel patterns, not gradients, runtime noise or alpha fades. The result should read as **surface = warmer, depth = colder, abyss = Deep Space**, while remaining visibly part of the same Canonical-28 pixel language.

## Salvage Intake integration

Horizontal layer:

**Tools -> Rustline -> Apply Salvage Intake Far Parallax**

Vertical layer:

**Tools -> Rustline -> Apply Salvage Intake Vertical Depth Backdrop**

The vertical managed root is:

`Art Dressing - Preserve/Vertical Depth Backdrop v0 - Managed`

and renders at sorting order `-40`. The horizontal far parallax renders at `-30`, so the vertical altitude field remains the deeper plane.

Both roots resolve `Camera.main` dynamically and survive normal Salvage Intake graybox rebuilding because they live below `Art Dressing - Preserve`.

## Layer relationship

The intended deepest-to-nearest ordering is:

1. **Vertical Depth Backdrop** — non-repeating altitude field, sorting `-40` in Salvage Intake.
2. **Horizontal Far Parallax** — seamless/repeating distant industrial silhouettes, sorting `-30`.
3. **Normal background dressing** — world-space machinery, pipes, gantries and architecture.
4. **Gameplay structure / player / foreground** according to the established environment hierarchy.

These two backdrop layers deliberately use orthogonal motion rules. The vertical layer communicates long-range altitude; the horizontal layer supplies continuous lateral depth and distant industrial scale. Neither should ever read as an isolated sprite floating in empty space.
