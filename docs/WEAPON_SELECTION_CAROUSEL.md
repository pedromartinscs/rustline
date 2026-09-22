# Weapon Selection & Carousel Contract

This document defines Rustline's weapon-selection model and the lower-left weapon carousel UI. It is a gameplay/UI contract for the upcoming persistent equipment system.

The current repository still treats Longwatch as the persistently wired weapon and installs Latch-9 through Editor Play Mode tooling. The purpose of this feature is to replace that temporary boundary with an explicit current-weapon selection model that can drive both gameplay equipment and presentation.

## Initial arsenal

Rustline reserves direct weapon slots `0..9`.

Initial slot mapping:

- `0` — Unarmed / fists
- `1` — Latch-9
- `2` — Longwatch DMR

Slot numbers are stable weapon identities for direct selection. Future weapons may occupy the remaining slots without changing the carousel contract.

The carousel contains the player's currently available arsenal, ordered by slot number and treated as a cycle. Unavailable slots are not rendered as empty carousel entries and are skipped by wheel navigation. Pressing a direct-number key for an unavailable weapon does nothing.

## Resting carousel layout

The weapon selector is anchored in the lower-left area of the screen.

At rest it presents at most three distinct entries:

1. the currently selected weapon in the center;
2. the cyclic successor above it;
3. the cyclic predecessor below it.

The center weapon is the primary/largest entry. The upper and lower neighbors are smaller, creating the visual impression that the entries belong to a horizontal cylinder whose circular cross-section is being viewed from the side.

The invisible cylinder can rotate in either direction.

With the initial three-slot arsenal:

```text
        successor / upper
             2
             |
             1   <- selected / center
             |
             0
        predecessor / lower
```

The exact screen offset, icon dimensions, neighbor scale, spacing, easing, and transition duration are intentionally not frozen until the production weapon images are present in the repository and can be judged at native scale.

If fewer than three distinct arsenal entries are available, the UI must not duplicate one weapon merely to fill all three positions.

## Input

### Mouse wheel

Mouse-wheel navigation rotates the carousel one available weapon at a time.

Default direction contract:

- wheel up -> select the cyclic successor / upper entry;
- wheel down -> select the cyclic predecessor / lower entry.

The mapping may later be exposed as an input preference without changing carousel logic.

### Number keys

Number keys `0..9` request the corresponding weapon slot directly.

If the requested slot is available, the carousel must rotate toward it through the shortest cyclic path instead of always moving in one fixed direction.

For a carousel of `N` available entries, with current arsenal index `i` and target arsenal index `j`:

```text
forwardSteps  = (j - i + N) % N
backwardSteps = (i - j + N) % N
```

- if `forwardSteps < backwardSteps`, rotate toward the successor / upper side;
- if `backwardSteps < forwardSteps`, rotate toward the predecessor / lower side;
- if both are equal, use successor / upper as the deterministic provisional tie-break.

The tie-break is a UX detail, not an architectural dependency, and may be changed after playtesting.

Example with six available entries `0..5`:

- current = `4`
- direct key = `5`
- shortest path = one successor step

The carousel therefore rotates through the upper entry rather than wrapping backward through `3, 2, 1, 0, 5`.

## One-step successor rotation

Assume the resting visible entries are:

```text
upper:   5
center:  4
lower:   3
```

Selecting the successor produces one coherent cylinder step:

1. lower entry `3` exits downward and palette-fades out;
2. center entry `4` travels downward and shrinks into the lower-neighbor presentation;
3. upper entry `5` travels toward center and grows into the selected presentation;
4. the next cyclic successor, `0`, enters above `5` using the palette-safe fade-in;
5. the stable result becomes:

```text
upper:   0
center:  5
lower:   4
```

The movement should read as one shared rotating cylinder, not three unrelated UI tweens.

## One-step predecessor rotation

Predecessor rotation is the exact visual mirror:

1. upper entry exits upward and palette-fades out;
2. center travels upward and shrinks into the upper-neighbor presentation;
3. lower entry travels toward center and grows into the selected presentation;
4. the next cyclic predecessor enters below using the palette-safe fade-in.

No direction is privileged. Both rotations are first-class behavior.

## Multi-step direct selection

A direct-number selection more than one carousel step away is represented as repeated one-step rotations in the chosen shortest direction.

Intermediate weapons therefore travel naturally through the carousel rather than teleporting or cross-fading directly to the requested weapon.

The implementation may internally queue or retarget steps, but valid user input must not corrupt the logical ring order or produce duplicate/missing visible entries.

The final resting center must always match the requested available slot.

## Palette-safe appearance and disappearance

Weapon carousel imagery is part of Rustline's pixel-art presentation and must obey the Canonical 28 rules.

The enter/exit effect must visually reuse the existing penumbra language instead of a conventional alpha-opacity fade:

- opaque production pixels remain Canonical 28 colors throughout the transition;
- darkening/remapping should reuse Rustline's canonical darkness mapping / lookup behavior where practical;
- deterministic palette-safe dithering may progressively replace visible pixels as the entry disappears;
- the final darkness color is Deep Space `#01020B`;
- fade-in performs the corresponding transition in reverse;
- source transparent pixels remain transparent;
- do not generate interpolated RGB shades, alpha gradients, bilinear filtering, blur, or anti-aliased transition colors.

The UI implementation does not have to reuse the exact world-space penumbra shader if that creates an inappropriate coupling. It should reuse the same palette data, darkness semantics, and deterministic pixel-dither language through a UI-appropriate implementation.

Because the carousel is screen-space UI, any dither pattern should be anchored deterministically in UI/icon pixel space rather than randomly changing every frame.

## Pixel-art motion and scaling

The selected icon must visibly grow when entering the center and shrink when leaving it, because apparent depth is part of the cylinder illusion.

Rendering must still preserve the project's pixel-art identity:

- Point/nearest sampling only;
- no filtering that invents colors;
- motion and scale should be pixel-stable/quantized where practical;
- no fractional filtering or smooth texture sampling merely to make the tween look conventional.

Exact scale levels and interpolation strategy are intentionally deferred until the three initial production icons can be evaluated in-engine.

## Gameplay selection model

There must be one explicit current weapon slot / current weapon definition owned by the equipment system.

UI selection must not become a second source of truth.

The carousel observes and requests changes to that gameplay selection. Longwatch, Latch-9, and Unarmed presentation/gameplay are activated from the resulting equipped slot.

This feature should replace the current temporary Editor-only Latch-9 installation boundary rather than layering another independent selection mechanism on top of it.

The existing weapon-specific contracts remain intact:

- continuous gameplay aim is not quantized by carousel/UI state;
- Longwatch remains hitscan;
- Latch-9 remains projectile-based with its current Conventional/Bouncing behavior;
- locomotion, firing-state policy, muzzle metadata, and Body-clocked weapon presentation are not redefined by this UI feature.

## Unarmed slot

Slot `0` is a real equipment state, not an absence/error state.

When Unarmed is selected:

- the normal unarmed arms presentation owns the shared overlay where appropriate;
- firearm firing logic is inactive;
- the carousel continues to treat slot `0` exactly like every other selectable cyclic entry.

This makes the fists participate naturally in wheel wrap and shortest-path direct selection.

## Architecture requirements

The implementation should keep these concerns separate:

- arsenal/catalog: which slot maps to which weapon/equipment entry;
- availability: which entries the player currently owns/can select;
- current equipment: authoritative selected slot;
- navigation: cyclic next/previous and shortest-path target calculation;
- carousel presentation: three visible entries and transition state;
- weapon-specific runtime behavior: Longwatch, Latch-9, Unarmed;
- icon art: production sprites referenced by equipment data, not hard-coded UI conditionals.

The navigation math should be testable without rendering.

Carousel animation should consume a logical transition description rather than decide weapon ownership itself.

Do not encode this system as special cases for exactly three weapons. The first production set has three entries, but the contract is designed for up to the ten direct slots `0..9`.

## Input during transitions

The implementation must remain deterministic when new wheel/number input arrives before a transition finishes.

The preferred initial policy is:

- maintain an authoritative requested target;
- finish/retarget through valid discrete carousel steps;
- never replay stale input;
- never allow visual center, logical selection, and arsenal ordering to diverge after the transition settles.

Exact animation interruption/blending mechanics may be chosen during implementation provided the final state obeys those invariants.

## Deferred visual tuning

The following values are deliberately left open until the three production weapon images are committed:

- icon source dimensions;
- exact lower-left anchor and safe margin;
- selected icon scale;
- neighbor scale;
- vertical spacing / implied cylinder radius;
- transition duration;
- easing curve;
- precise pixel-dither progression;
- whether UI movement uses discrete pixel steps or another nearest-neighbor-safe quantization;
- exact gameplay equip-commit moment during a visual transition.

These should be tuned from the actual art instead of invented before it exists.

## Acceptance examples

### Wrap by wheel

With `0,1,2` available and `2` selected:

- wheel up selects `0`;
- the old lower entry leaves;
- `2` becomes the lower neighbor;
- `0` becomes center;
- `1` becomes upper.

Wheel down from `0` returns to `2`.

### Shortest direct route

With `0..5` available and `4` selected:

- key `5` -> one successor step;
- key `3` -> one predecessor step;
- key `0` -> two successor steps (`4 -> 5 -> 0`) rather than four predecessor steps.

### Initial Rustline set

With only `0,1,2` available, every selected weapon always has two distinct cyclic neighbors, so the resting UI naturally displays exactly three icons.

## Testing expectations

At minimum, automated coverage should verify:

- cyclic next/previous selection and wrap;
- direct-number unavailable slot is ignored;
- shortest modular path in both directions;
- deterministic equal-distance tie-break;
- initial `0/1/2` wrap behavior;
- logical state remains valid under rapid queued/retargeted input;
- Unarmed is selectable as a normal slot;
- selecting equipment activates the correct weapon/unarmed runtime behavior;
- carousel presentation never duplicates an entry when three or more distinct entries exist;
- palette-transition implementation never produces non-Canonical opaque colors;
- current movement/aim/weapon regression suites continue to pass.

Human validation should verify:

- the lower-left composition reads clearly at native scale;
- both rotational directions convincingly read as the same invisible cylinder;
- growth/shrinkage communicates depth without making pixel art look filtered;
- palette fade reads as Rustline's penumbra language rather than a generic UI dissolve;
- rapid wheel and direct-number changes remain understandable and responsive.
