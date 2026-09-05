# Rustline Native-Pixel Presentation Prototype

This document is the technical companion to `ART_DIRECTION.md` for Rustline's camera/viewport/penumbra prototype.

It defines the behavior the implementation must satisfy. The implementation may choose appropriate Unity 6 / URP 2D APIs, but it should not change these presentation rules without an explicit design decision.

## Canonical constants

- Production art density: **16 pixels per Unity unit**.
- Environment tile: **16×16 production pixels**.
- Canonical maximum logical viewport: **1072×1072 logical pixels**.
- Canonical maximum logical viewport in tiles: **67×67 tiles**.
- Canonical full-darkness color: **Deep Space `#01020B`**.
- Fractional presentation scaling is forbidden.
- Logical gameplay rendering should remain nearest-neighbor / point sampled.

The canonical square is a **maximum visible gameplay envelope**, not a minimum render target size. Smaller displays crop the logical world rather than downscaling it.

## Integer-scale and crop rules

For a physical display/window size `screenWidth × screenHeight`, compute the integer presentation scale as:

```text
integerScale = max(
    1,
    floor(min(screenWidth / 1072, screenHeight / 1072))
)
```

Then compute the logical gameplay render size as:

```text
logicalWidth  = min(1072, floor(screenWidth  / integerScale))
logicalHeight = min(1072, floor(screenHeight / integerScale))
```

The presented gameplay image occupies:

```text
outputWidth  = logicalWidth  * integerScale
outputHeight = logicalHeight * integerScale
```

That image is centered on the physical display. Every remaining physical pixel outside the centered output rectangle is filled with Deep Space `#01020B`.

Consequences:

- The presentation never scales below 1×.
- A display smaller than 1072 px on an axis simply sees less world on that axis.
- A display larger than the canonical viewport does not reveal more than 1072 logical pixels on either axis.
- 2× begins only when the physical display can contain the full canonical viewport at 2× on **both** axes; the same rule applies to 3×, 4×, and later integer scales.
- Point/nearest sampling must be used for the final integer upscale.
- Resizing the window may recreate logical render targets, but render targets must not be allocated/recreated every frame.

### Reference cases

These values are useful for automated tests:

| Physical size | Integer scale | Logical render | Presented output | Deep Space surround |
|---|---:|---:|---:|---|
| `128×128` | 1× | `128×128` | `128×128` | none |
| `800×600` | 1× | `800×600` | `800×600` | none |
| `1086×420` | 1× | `1072×420` | `1072×420` | 7 px left/right |
| `1920×1080` | 1× | `1072×1072` | `1072×1072` | 424 px left/right, 4 px top/bottom |
| `2560×1440` | 1× | `1072×1072` | `1072×1072` | 744 px left/right, 184 px top/bottom |
| `3840×2160` | 2× | `1072×1072` | `2144×2144` | 848 px left/right, 8 px top/bottom |
| `5760×3240` | 3× | `1072×1072` | `3216×3216` | 1272 px left/right, 12 px top/bottom |

## Camera/player relationship

The visibility mask is always centered on the **actual rendered player position**, not merely on the geometric center of the screen.

The existing accepted movement physics must not change. The existing camera follow may be adapted as required for the native-pixel render target, but its smooth/pixel-snapped feel should be preserved unless a presentation constraint makes a specific change necessary.

For the first prototype:

- no combat look-ahead;
- no aim look-ahead;
- no cinematic camera zones;
- no camera dead-zone redesign;
- no movement tuning.

If camera smoothing means the player is momentarily not at the exact screen center, the penumbra follows the player's rendered pixel position. This preserves the core rule that visibility is player-centered without forcing movement/camera-feel changes during the presentation prototype.

## Penumbra geometry

The penumbra is a perfect circle measured in **logical production pixels**, independent of physical display scale.

- Fully visible radius: **456 px = 28.5 tiles**.
- Fully visible diameter: **912 px = 57 tiles**.
- Penumbra radial thickness: **64 px = 4 tiles**.
- Full darkness begins at radius: **520 px = 32.5 tiles**.
- Full-darkness diameter: **1040 px = 65 tiles**.
- Canonical square half-size: **536 px = 33.5 tiles**.

Therefore, when the full canonical 1072×1072 logical viewport is visible, at least 16 logical pixels / one complete tile of solid Deep Space remains between the outer penumbra edge and each cardinal viewport edge. Corners naturally contain more solid darkness.

On smaller cropped displays, portions of the penumbra may simply fall outside the physical view. A sufficiently tiny display can show only fully visible world and therefore reveal no penumbra at all. This is intentional.

## Palette-safe darkness

The penumbra is not a conventional alpha vignette and must not multiply/fade RGB values into arbitrary shades.

Rules:

- The canonical source/output color set remains the 28 colors in `PALETTE.md`.
- Solid darkness is always Deep Space `#01020B`; do not introduce literal black `#000000`.
- Penumbra darkening uses **discrete palette remapping** through canonical darker colors.
- Controlled ordered pixel dithering may mix adjacent palette-safe darkness levels spatially.
- No alpha gradients, blur, bilinear filtering, antialiasing, fractional scaling, or synthesized in-between colors.
- The source PNGs and tile/sprite assets remain unchanged; this is a presentation effect.

### Prototype LUT direction

The prototype should centralize its palette-darkening data so every mapping can be audited and tuned without rewriting shader logic.

A good first implementation is:

- level 0: identity / original palette color;
- several progressively darker palette-safe remap levels;
- final level: Deep Space for every source color.

Material families should darken coherently where practical:

- metals through Steel / Dark Metal / Steel Shadow / Shadow / Deep Navy / Deep Space;
- warm/rust/fabric colors through their existing warm shadow/rust ramp before the deepest neutrals;
- cyan/technology colors through Cyan Dark before Shadow / Deep Navy / Deep Space;
- bright neutral/FX colors through sensible canonical neutral/metal shadows;
- selective accents such as Green/Violet/Red may retain identity for early shadow levels before collapsing into the deep neutral ramp.

The exact first-pass artistic mapping is tuneable. The hard requirement is that **every LUT output is one of the canonical 28 colors**.

### Ordered dithering

Use a small deterministic ordered pixel pattern for the prototype, preferably a **4×4 Bayer-style threshold pattern** or a technically equivalent small fixed pattern.

The pattern should be evaluated in logical-pixel space and, where practical, anchored to world/source-pixel coordinates rather than physical display pixels. The goal is:

- stable pixel-art texture;
- no temporal shimmer merely because the camera moves by one source pixel;
- identical logical appearance at 1×, 2×, 3×, etc.;
- the 2×/3× output simply duplicates the already-resolved logical pixels.

Within the 64 px penumbra band, the implementation may choose multiple discrete shadow steps and dither between adjacent levels so the transition reads progressively rather than as four hard rings.

## Performance architecture

The canonical full logical target is only:

```text
1072 × 1072 = 1,149,184 logical pixels
```

The intended architecture should preserve that advantage.

Prefer this conceptual pipeline:

1. Render gameplay/world at the current **logical** render size (never larger than 1072×1072).
2. Apply palette-constrained penumbra at logical resolution.
3. Present the already-resolved logical image to the physical display using a trivial nearest-neighbor integer upscale.
4. Clear/fill unused physical display area with Deep Space.

Do **not** implement the expensive palette remap/dither pass independently for every duplicated physical pixel at 2×/3×/4× if it can instead be resolved once at logical resolution.

The implementation may use RenderTextures and an appropriate Unity 6 / URP 2D presentation path. Favor current supported APIs, deterministic cleanup, and low allocation pressure over cleverness.

Runtime requirements:

- no per-frame RenderTexture creation/destruction;
- recreate targets only when required by a resolution/integer-scale change;
- no per-frame managed allocations in the steady-state presentation loop where practical;
- point filtering, no mipmaps, no MSAA, no HDR for the logical pixel target unless a later explicit feature requires otherwise;
- release temporary/persistent GPU resources deterministically;
- avoid adding many scene lights or scene objects to create the penumbra;
- ideally one logical-resolution penumbra pass plus a cheap presentation composite;
- presentation-only cameras/passes must not pay for the full 2D Renderer when they only need isolated unlit composition work.

The World Camera still uses Renderer2D. Current ordinary world sprites and Tilemaps use URP's
`Sprite-Unlit-Default`, and MovementLab/ArtShowcase contain no identity Global Light2D. This was
accepted only after an exact rendered-pixel comparison against the former Lit plus white,
intensity-1 Global Light setup. Future purposeful 2D lighting remains supported by assigning a
Lit material to the intended renderer and authoring a light that materially changes the image.

## Development diagnostics

The prototype must support direct A/B performance comparison in Development/Editor builds.

Keep the existing MovementLab performance HUD behavior when it is visible:

- left click copies the completed sample;
- the existing camera-follow diagnostic may remain available.

The HUD is hidden by default and toggled with `H` or `F3`. While hidden it does not sample its
performance window, format output, or issue IMGUI labels. `P` remains active while hidden.

Add a simple development-only way to toggle the penumbra **without rebuilding/reloading the scene**. A `P` key diagnostic toggle is acceptable and should not be added to the production Player input action map.

The copied HUD output should report at least:

- Penumbra `ON/OFF`;
- current integer scale;
- current logical render size;
- current physical screen size;
- FPS / AVG / WORST / frame count;
- VSync and target frame rate.

Toggling the penumbra should reset the current measurement window so the transition/allocation frame is not included in the next completed sample.

## M1B implementation

MovementLab now implements this specification through a **two-camera RenderGraph path**:

1. `World Camera - Native Pixel Follow` keeps the accepted `PixelCameraFollow2D` smoothing and 1/16-unit source-pixel snap, renders the actual 2D world through Rustline's default `Renderer2D`, and writes into a logical 8-bit sRGB RenderTexture sized by `NativePixelViewportMath`.
2. `Native Pixel Driver Camera` renders no scene geometry (`cullingMask == 0`). It uses `Assets/Settings/RustlineUtilityRenderer.asset`, the lightweight Universal Renderer registered as renderer index `1`, solely to drive `RustlineNativePixelPresentFeature`.

`RustlineNativePixelPresentFeature` records the remaining work through supported Unity 6 / URP RenderGraph APIs:

- when Penumbra is ON, one logical-resolution raster pass samples the world target and writes the palette-safe result into the persistent resolved logical target;
- the final raster pass point-samples either the resolved target (ON) or raw world target (OFF), clears the physical backbuffer to canonical Deep Space, and writes only the centered integer-scaled output rectangle;
- `UniversalResourceData.SwitchActiveTexturesToBackbuffer()` marks the physical resolve as complete so URP does not need a redundant final blit;
- external persistent RenderTextures are imported with explicit `RenderTargetInfo` metadata;
- fullscreen rendering uses a procedural clip-space triangle rather than legacy camera-dependent quad geometry or `CommandBuffer.Blit`.

There is **no physical Presentation Camera and no presentation/penumbra quad in the consolidated scene contract**. The reversible Experiment 2 fallback objects were removed from the runtime contract and from the deterministic MovementLab generator once the RenderGraph path rendered correctly. The world camera remains on Renderer2D; only the driver camera uses the utility renderer.

Both logical textures are persistent and are recreated only when required logical dimensions change. They use ARGB32 sRGB color, Point filtering, clamp wrapping, no mipmaps, no anisotropy, no MSAA, and no HDR. The resolved target is depthless because it is RenderGraph-only. The World Camera target retains 16-bit depth: a graphical D3D11 experiment confirmed that Unity 6.4 RenderGraph rejects a camera output RenderTexture whose depth/stencil format is None, even though `Renderer2D.asset` keeps `m_UseDepthStencilBuffer: 0`, URP depth texture is disabled, and no current effect consumes scene depth. The retained resolved-target saving remains 2,298,368 bytes (about 2.19 MiB) at the maximum logical viewport, before platform-specific alignment.

`_LogicalSize` is sent only when logical dimensions/material lifetime require it. Player pixel center and world-pixel Bayer origin are exact-dirty-checked, and stationary player/camera/projection inputs bypass `WorldToViewportPoint` plus material vector writes. Stable source texture and scale/bias state is bound when materials are created, targets are recreated, or Penumbra is toggled. TextureHandles and `builder.UseTexture` still express RenderGraph dependencies; execution callbacks only clear where needed, set the viewport, and draw.

The palette pass leaves pixels inside radius 456 unchanged, remaps/dithers only through Canonical 28 across the 64 px annulus, and emits exact Deep Space from radius 520 outward. In the annulus it quantizes sampled linear RGB to 5 bits per channel and point-samples a runtime-generated `1024×160` RGBA32 sRGB darkness lookup. The 640 KiB persistent texture trades a small amount of GPU memory for removal of the former 28-way nearest-color loop from every annulus fragment. Final physical integer upscaling only duplicates already-resolved logical pixels.

The raw World Camera RenderTexture has a storage-orientation difference on top-left graphics APIs when it is bound directly as a persistent texture. The Penumbra material owns the platform-specific camera-target Y scale/bias and normalizes its resolved output. The final presentation material owns the accepted no-extra-flip scale/bias for both its raw and resolved persistent sources, preserving repeated-toggle history independence.

The final backbuffer clear is supplied in linear form because raster commands write linear values into the sRGB physical target. This preserves the authored display result `#01020B` rather than double-encoding it to a brighter blue.

In Editor and Development Builds, `P` toggles the logical penumbra pass without rebuilding or reallocating targets, including while the HUD is hidden. The driver camera remains active in both states because it owns the final RenderGraph presentation pass. `H` or `F3` shows the opt-in HUD and resets its 2.0-second measurement window; hiding it stops sampling, string formatting, and IMGUI label work. When visible, it reports physical resolution, logical resolution, integer scale, Penumbra `ON/OFF`, Camera `ON/FROZEN`, FPS, AVG, WORST, frame count, VSync, and target frame rate. Left-click still copies the current text and right-click still toggles camera follow.

The accepted Experiment 1 utility-renderer specialization produced a large Editor recovery after the first M1B implementation: measured Penumbra-ON samples averaged about **75.1 FPS at logical 1072×468** and **66.6 FPS at the full logical 1072×1072 viewport**, with a later three-sample full-viewport validation averaging about **75.2 FPS**, versus the approximately 35–40 FPS regression observed before specialization. Experiment 2 performance numbers are intentionally not claimed here until the two-camera RenderGraph path is visually validated and measured. Detailed samples and caveats live in `PERFORMANCE_LOG.md`; standalone Development Build measurements are still required before making hardware-tier claims.

Pure EditMode tests cover every reference viewport case and all palette/LUT invariants. PlayMode smoke coverage verifies logical target properties, raw/resolved routing without reallocation, actual non-Deep-Space pixels through the world, logical penumbra, and physical RenderGraph stages, and the accepted MovementLab movement/respawn behavior. Renderer-routing coverage locks the two-camera contract: the driver camera must use utility renderer index `1` with `cullingMask == 0`, while the gameplay/world camera must remain on Renderer2D. ArtShowcase retains its M0 presentation and does not receive the penumbra.

## Validation expectations

Automated tests should cover the pure viewport math and palette data independent of rendering hardware.

At minimum validate:

- the reference resolution cases in this document;
- integer scale is never below 1;
- logical dimensions never exceed 1072×1072;
- presented dimensions fit inside the physical display;
- presentation offsets are centered and integral;
- 16 PPU mapping is preserved;
- penumbra constants are 456 px / 64 px / 520 px;
- every palette remap/LUT entry belongs to Rustline Canonical 28;
- final darkness mappings resolve to Deep Space;
- the RenderGraph driver camera routes through the dedicated utility renderer rather than Renderer2D;
- the driver camera renders no scene geometry;
- the consolidated MovementLab contains no legacy physical presentation camera or fullscreen presentation quads;
- no source PNG changes;
- existing M0 and M1A validation continues to pass after the presentation integration.

The visual prototype still requires human inspection at multiple window/display sizes because automated tests cannot determine whether the penumbra looks artistically good.

## Out of scope for this prototype

Do not implement as part of the first presentation pass:

- enemy visibility logic;
- AI sleeping/activation radius;
- audio attenuation or spatial ambience rules;
- procedural generation;
- combat/aim camera look-ahead;
- accessibility zoom UI;
- fullscreen settings menu;
- arbitrary fractional zoom;
- new art assets;
- new production palette colors;
- stamina/exertion systems;
- movement physics changes.

Those systems may consume the presentation ranges later, but they should not be coupled into the first camera/penumbra implementation.
