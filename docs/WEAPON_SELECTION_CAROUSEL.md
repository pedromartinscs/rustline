# Weapon Selection & Carousel Contract

This document defines Rustline's persistent weapon-selection model and lower-left weapon-selector presentation.

The gameplay/equipment foundation is already persistent: Unarmed, Latch-9, and Longwatch DMR are serialized production equipment states, selection is authoritative in the equipment system, and the HUD is composited above the native-pixel world/penumbra output. The presentation contract below supersedes the earlier three-card visible cylinder stack and defines the next HUD simplification.

## Initial arsenal

Rustline reserves direct weapon slots `0..9`.

Initial slot mapping:

- `0` — Unarmed / fists
- `1` — Latch-9
- `2` — Longwatch DMR

Slot numbers are stable weapon identities for direct selection. Future weapons may occupy the remaining slots without changing the selector contract.

The available arsenal is ordered by slot number and treated as a cycle. Unavailable slots are skipped by wheel navigation and are not rendered as empty entries. Pressing a direct-number key for an unavailable weapon does nothing.

The production card assets are versioned at `Assets/Art/UI/WeaponCarousel/` as:

- `weapon_carousel_unarmed.png`
- `weapon_carousel_latch_9.png`
- `weapon_carousel_longwatch_dmr.png`

All three are exactly **360×175 px**, use only Rustline Canonical 28 colors plus one fully transparent palette entry, and contain no partial-alpha pixels. The full card rectangle is the presentation unit; do not independently resize or reposition the weapon drawing inside a card.

## Resting selector layout

At rest, the HUD shows **exactly one weapon card**: the currently equipped weapon.

No upper/lower neighbor previews are visible at rest.

All weapon cards use the **same presentation scale**. There is no selected-vs-neighbor scaling and no apparent-depth resize during transitions.

The card remains anchored in the lower-left HUD area. Its horizontal anchor is screen-relative rather than world-viewport-relative: the production default places the card's left edge **20 logical pixels from the physical window's left edge**, even when that position lies in the Deep Space surround outside the centered world output. Vertical placement remains tied to the accepted lower world-output position so this change does not move the card up or down.

The earlier visible three-card cylinder stack is no longer the target presentation. Cyclic order still exists in navigation and transition direction, but neighboring weapons are revealed only while entering during a switch.

## Input

### Mouse wheel

Mouse-wheel navigation moves one available weapon at a time.

Direction contract:

- wheel up -> cyclic successor;
- wheel down -> cyclic predecessor.

Explicit wheel direction remains first-class even when only two weapons are available and successor/predecessor both identify the same other weapon.

The mapping may later be exposed as an input preference without changing selection logic.

### Number keys

Number keys `0..9` request the corresponding weapon slot directly.

If the requested slot is available, selection moves toward it through the shortest cyclic path.

For a cycle of `N` available entries, with current arsenal index `i` and target arsenal index `j`:

```text
forwardSteps  = (j - i + N) % N
backwardSteps = (i - j + N) % N
```

- if `forwardSteps < backwardSteps`, use successor direction;
- if `backwardSteps < forwardSteps`, use predecessor direction;
- if equal, use successor as the deterministic direct-selection tie-break.

Direct selection several steps away is represented as repeated discrete one-weapon steps. Intermediate weapons are therefore still observable as they pass through the selected position rather than the UI teleporting directly to the final target.

## Single-step transition model

A visual switch uses at most **two reusable card views**:

1. outgoing card — the currently selected/equipped weapon at the beginning of the step;
2. incoming card — the weapon that will become selected when the step completes.

The two cards always use the same scale.

They move through the same vertical track but remain spatially separated throughout the transition. Their rectangles must never touch or overlap.

Define:

- `cardHeight` = presented card height in logical pixels;
- `visualGap` = minimum clear logical-pixel gap between the two rectangles;
- `stepDistance >= cardHeight + visualGap`.

If both cards use the same position interpolation over the same distance, their center-to-center separation remains constant for the whole step. This is preferred because overlap becomes impossible by construction and no card-order/sorting illusion is needed.

### Successor step

For a successor step:

- outgoing/current card moves downward from the resting aperture toward the lower penumbra and disappears below it;
- incoming/successor card begins above the resting aperture, moves downward through the upper penumbra, and finishes exactly at the resting position;
- incoming and outgoing rectangles remain separated by at least `visualGap` for the entire transition.

Conceptually:

```text
         incoming card
        moving downward
              |
              v

      [ upper penumbra ]

      [ resting aperture ]

      [ lower penumbra ]

              |
              v
         outgoing card
        moving downward
```

At completion, the incoming card occupies the resting aperture and becomes the sole visible card.

### Predecessor step

Predecessor is the exact vertical mirror:

- outgoing/current card moves upward through the upper penumbra and disappears above it;
- incoming/predecessor card begins below the resting aperture, moves upward through the lower penumbra, and finishes at the resting position;
- the two rectangles remain spatially separated for the whole transition.

No direction is privileged. Both rotations are first-class behavior.

## Fixed spatial penumbra aperture

Card appearance is controlled by **fixed HUD-space vertical penumbra bands**, not by a whole-card temporal opacity/fade value.

The selector defines one fixed fully visible resting aperture corresponding to the card's resting rectangle.

Directly above and below that aperture are horizontal penumbra bands. Initial tuning target:

- upper penumbra thickness: **20 logical pixels**;
- lower penumbra thickness: **20 logical pixels**.

The bands are anchored to the logical HUD/screen coordinate system. They do **not** move with a card.

Therefore the moving card naturally becomes darker or brighter row-by-row as its pixels cross the fixed bands.

### Upper penumbra

For pixels moving upward/outward:

- at the inner edge adjacent to the resting aperture: original Canonical-28 source color;
- progressively farther upward through the band: progressively darker canonical remapping;
- at the outer/top edge: maximum darkness / Deep Space;
- beyond the outer edge: fully transparent/discarded.

For an incoming successor moving downward, the same band is traversed in reverse: its first visible pixels emerge from maximum darkness and recover their original canonical colors as they approach the aperture.

### Lower penumbra

The lower band is the exact vertical mirror:

- at the inner/top edge adjacent to the aperture: original source color;
- progressively farther downward: progressively darker canonical remapping;
- at the outer/bottom edge: Deep Space;
- below the band: fully transparent/discarded.

For an incoming predecessor moving upward, the band is traversed in reverse.

### Spatial, not temporal

The transition's time/easing controls **position only**.

A pixel's darkness is determined from its current logical-screen Y position relative to the fixed aperture and penumbra boundaries.

Changing transition duration later must not require retuning the penumbra fade curve.

No global whole-card alpha/fade parameter should drive the visual disappearance.

## Palette-safe penumbra rules

Weapon-selector imagery remains part of Rustline's Canonical-28 presentation.

The spatial penumbra must reuse the visual language/data of Rustline's existing palette darkness system:

- source transparent pixels remain transparent;
- fully visible opaque pixels retain their authored Canonical-28 colors;
- pixels inside a penumbra band progress through the existing canonical darkness mapping toward Deep Space `#01020B`;
- transitions between darkness levels may use deterministic Bayer/pixel dithering;
- at the outer penumbra boundary, surviving visible pixels are Deep Space / maximum darkness;
- beyond the outer boundary, pixels are discarded/transparent;
- every output alpha remains binary (`0` or `255`);
- no ordinary alpha gradient;
- no interpolated noncanonical RGB shade;
- no bilinear filtering, blur, or anti-aliasing.

Dither must be deterministic and stable. Prefer anchoring it to logical HUD/pixel coordinates or another stable pixel coordinate system so the pattern does not crawl randomly from frame to frame.

The UI implementation does not need to reuse the exact world penumbra shader. A dedicated lightweight selector shader is appropriate, but it should reuse the same palette data, darkness semantics, and deterministic dither language.

## Pixel-art motion

All cards retain one constant presentation scale throughout a transition.

Rendering must preserve Rustline's pixel-art identity:

- Point/nearest sampling only;
- logical positions quantized to pixel-stable coordinates where practical;
- no scaling tween;
- no filtering introduced to smooth movement;
- motion/easing may be tuned, but visual output must remain nearest-neighbor safe.

Because incoming and outgoing rectangles never overlap, sorting order must not be used as a visual solution for crossing cards.

## Weapon information panel

The selector includes a text/status block immediately to the **right of the weapon card**. The card remains the visual identity; the right-side block communicates the equipped weapon's current functional state.

Initial layout contract:

- keep a clear horizontal gap between the card and information block; initial tuning target: **16 logical px**;
- the lower part of the information block shows the weapon name and current mode at **2 logical pixels per bitmap-font pixel**;
- the upper part shows ammunition/resource state when applicable at **4 logical pixels per bitmap-font pixel**, making resource state intentionally twice the identity-line scale;
- vertical composition should read approximately as **upper resource line / one line of breathing room / lower identity line**, without requiring literal text rows or a grid;
- the information block uses the same logical-HUD coordinate system as the card and may occupy the Deep Space surround;
- exact glyph spacing and vertical insets remain centralized tuning values.

Display strings are uppercase programmer-facing HUD labels:

| Equipment state | Upper resource line | Lower identity/mode line |
|---|---|---|
| Longwatch DMR, Semi | `50 / 50 ×3` style | `LONGWATCH DMR (SEMI)` |
| Longwatch DMR, Automatic | `50 / 50 ×3` style | `LONGWATCH DMR (AUTO)` |
| Latch-9, Conventional | `∞` | `LATCH-9 (PLASMA)` |
| Latch-9, Bouncing | `∞` | `LATCH-9 (PHOTON FIELD)` |
| Unarmed | no resource line | `HANDS` |

The HUD labels do **not** rename the gameplay enums. Runtime/code may continue to use `WeaponFireMode2D.SemiAutomatic` / `Automatic` and `WeaponShotMode2D.Conventional` / `Bouncing`; the strings above are presentation terminology.

### Information-panel transitions

The card and its information block form one visual equipment unit.

During a selector step:

- outgoing card + outgoing information move together;
- incoming card + incoming information move together;
- both use the same Y offset as their corresponding card;
- the existing fixed upper/lower spatial penumbra applies to **both card pixels and text glyphs**;
- text must not globally alpha-fade independently from the card;
- outgoing/incoming visual units retain the existing no-overlap vertical separation.

At rest, mode/ammunition changes update the information block in place without moving the card.

Right-click mode changes must be visible immediately:

- Longwatch: `(SEMI)` ↔ `(AUTO)`;
- Latch-9: `(PLASMA)` ↔ `(PHOTON FIELD)`.

Longwatch firing/reloading must update its upper line immediately.

### Pixel-text rules

Text is part of the same native-pixel HUD and must obey Rustline's presentation constraints:

- deterministic bitmap/pixel glyphs;
- Point/nearest sampling only;
- Canonical-28 opaque RGB plus binary transparency only;
- no OS/dynamic-font antialiasing;
- no ordinary alpha fade;
- spatial darkness uses the same canonical darkness lookup and HUD-space penumbra semantics as the weapon card;
- stable glyph raster with no crawling/shimmer while the HUD is stationary.

A small internal bitmap font is acceptable. The implementation should not add a large UI/font dependency merely for these few HUD strings. The required glyph set includes uppercase A-Z, digits 0-9, space, `-`, `/`, `(`, `)`, multiplication sign `×`, and infinity `∞`. Ordinary glyphs use the compact 5×7 family; `∞` is intentionally a wider **9-pixel** special glyph so it reads as infinity rather than a theta-like oval at native scale.

## Gameplay selection model

There is one explicit current weapon slot / current weapon definition owned by the equipment system.

The HUD is not a second source of truth.

The selector requests/observes gameplay selection. Longwatch, Latch-9, and Unarmed presentation/gameplay are activated from the resulting equipped slot.

Existing weapon-specific contracts remain intact:

- continuous gameplay aim is not quantized by selector state;
- Longwatch remains hitscan;
- Latch-9 remains projectile-based with its current Conventional/Bouncing behavior;
- locomotion, firing-state policy, muzzle metadata, and Body-clocked weapon presentation are not redefined by this UI.

Equipment is committed when the incoming card completes its discrete step into the resting aperture.

## Unarmed slot

Slot `0` is a real equipment state, not an absence/error state.

When Unarmed is selected:

- the normal unarmed arms presentation owns the shared overlay where appropriate;
- firearm firing logic is inactive;
- slot `0` participates in wheel wrap and shortest-path direct selection exactly like every other available entry.

## Architecture requirements

Keep these concerns separate:

- arsenal/catalog: slot -> equipment entry;
- availability: which entries can currently be selected;
- authoritative current equipment;
- cyclic navigation / shortest-path math;
- latest requested target while transitions are active;
- selector presentation;
- weapon-specific runtime behavior;
- card art referenced by equipment data rather than hard-coded UI conditionals.

Navigation math must remain testable independently of rendering.

The selector presentation should require only **two reusable card views** for the target design. Do not instantiate/destroy cards for each weapon switch.

Do not encode navigation as a special case for exactly three weapons. The production set starts with three entries, but the equipment model supports direct slots `0..9`.

The HUD continues to render through a dedicated logical HUD target and final native-pixel compositor above world penumbra. The HUD target covers the full physical window in native logical pixels, while the world image remains centered inside its existing output rectangle. This allows screen-edge UI to occupy Deep Space surround without moving or resizing the world.

Temporary selector SpriteRenderers use the dedicated `RustlineHUD` layer at index 8. The selector camera culls only that layer and production World Cameras exclude it.

The HUD camera should remain render-on-demand: render while the selector is dirty/animating, retain its logical target while static, and avoid continuous redundant scene rendering at rest.

## Input during transitions

New wheel/number input may arrive before the current visual step finishes.

The authority keeps the latest requested available target and preserves deterministic discrete steps:

- current visual step completes;
- incoming card reaches the resting aperture and that equipment state commits;
- route toward the latest requested target is then recomputed;
- stale requests are never replayed;
- visual selected card and authoritative equipped slot converge after every completed step.

Rapid explicit wheel input advances relative to the latest requested target, not merely the still-equipped center.

Explicit wheel direction is preserved for its requested step. Direct-number selection uses shortest-path routing with successor tie-break.

## Implemented presentation

The production HUD now uses the single-card/two-view spatial-penumbra model defined above.

The implementation preserves the already-validated foundations:

- persistent equipment/loadout state;
- input semantics and shortest-path navigation;
- rapid retargeting;
- persistent Latch-9 production wiring;
- `RustlineHUD` isolation;
- native-pixel HUD composition above penumbra;
- canonical palette darkness lookup/dither infrastructure;
- render-on-demand HUD target.

At rest only the equipped card is rendered. During a step one outgoing and one incoming view move at the same fixed scale and constant separation. Their center distance is card height plus the configured visual gap, so their rectangles cannot overlap. The selector shader derives darkness from each fragment's logical-HUD Y coordinate relative to fixed upper/lower penumbra bands; time controls only card position.

## Deferred visual tuning

These remain centralized/serialized tuning values for native-scale playtesting:

- screen-left padding — production default: **20 logical px**;
- exact vertical bottom margin;
- single constant card scale;
- transition duration;
- motion easing;
- `visualGap` between incoming/outgoing rectangles;
- upper/lower spatial penumbra thickness — initial target: **20 logical px** each;
- exact distribution/dither between canonical darkness levels inside the bands;
- precise logical-pixel motion quantization.

Do not reintroduce neighbor scale, visible neighbor stack, or whole-card fade as tuning parameters.

## Acceptance examples

### Resting state

With `0,1,2` available and `2` equipped:

- only Longwatch's card is visible;
- Latch-9 and Unarmed cards are not visible until a selection transition needs them.

### Wheel successor

With `2` equipped and wheel-up selecting `0`:

- Longwatch moves downward from the resting aperture;
- only the portion entering the lower 20-px penumbra darkens;
- pixels below the outer lower boundary disappear;
- Unarmed enters from above;
- only the portion crossing the upper 20-px penumbra is progressively restored from darkness;
- the two card rectangles never touch/overlap;
- Unarmed finishes at the exact resting position and becomes the sole visible card.

### Wheel predecessor

The same behavior mirrors vertically:

- current card exits upward;
- requested predecessor enters from below;
- upper/lower spatial penumbra behavior mirrors exactly.

### Shortest direct route

With `0..5` available and `4` selected:

- key `5` -> one successor step;
- key `3` -> one predecessor step;
- key `0` -> two successor steps (`4 -> 5 -> 0`) rather than four predecessor steps.

Each intermediate discrete selection uses the same two-card transition.

## Testing expectations

At minimum, automated coverage should verify:

- cyclic next/previous selection and wrap;
- direct-number unavailable slot is ignored;
- shortest modular path in both directions;
- deterministic equal-distance direct-selection tie-break;
- explicit successor/predecessor direction with a two-entry arsenal;
- initial `0/1/2` wrap behavior;
- rapid retargeting advances from the latest requested target;
- Unarmed is selectable as a normal slot;
- selecting equipment activates the correct weapon/unarmed runtime behavior;
- target selector implementation uses at most two reusable card views;
- incoming/outgoing card bounds never overlap for configured step distance/gap;
- resting state exposes exactly one card;
- spatial penumbra boundaries map aperture edge -> source color, outer edge -> Deep Space, outside -> transparent;
- palette-transition implementation never produces non-Canonical opaque RGB or partial alpha;
- `RustlineHUD` remains excluded from production World Cameras;
- current movement/aim/weapon/native-pixel regression suites remain green.

Human validation should verify:

- the single-card lower-left composition reads clearly at native scale;
- the right-side identity/mode text is legible without competing with the card;
- Longwatch ammo, Latch infinity, and Hands/no-resource states read immediately;
- the card can sit farther left without feeling cramped;
- successor/predecessor motion is immediately understandable;
- cards appear to enter/leave through fixed darkness bands rather than globally fading;
- the spatial penumbra reads as Rustline's visual language;
- no incoming/outgoing rectangles visibly touch or cross;
- rapid wheel and direct-number changes remain understandable and responsive.
