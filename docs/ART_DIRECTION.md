# Rustline Art Direction

This document is the working visual specification for Rustline. It is intentionally small and should evolve only when a change improves consistency or gameplay readability.

## Visual identity

**Theme:** derelict orbital salvage refinery / industrial extraction site.

The environment should feel functional, worn, and dangerous rather than sleek or pristine. Machinery should read as serviceable industrial equipment that has survived years of neglect, patchwork repair, heat, and corrosion.

### Core motifs

- Dark steel and layered industrial plating
- Oxidized / heat-stained metal
- Exposed pipes, conduits, vents, and structural beams
- Warning markings and maintenance labels
- Cold electronic displays and status lights
- Strong silhouettes and readable interactables
- Selective bright accents for gameplay-relevant objects

## Pixel-art constraints

### Environment

- Base tile grid: **16×16 px**
- Modular construction preferred over large baked room illustrations
- Nearest-neighbor filtering only
- No anti-aliasing in final sprites
- Avoid noisy single-pixel detail that disappears during gameplay

### Player

- Side-view humanoid
- Target visible footprint: approximately **24×32 to 32×32 px**
- Weapon rendered as a separate object from the body
- Body animation must remain readable when horizontally flipped
- Stable weapon-hand / weapon-pivot location is more important than ornamental detail

### Enemies

Prefer designs that produce strong gameplay silhouettes and modest animation requirements:

- Small ground crawler / melee unit
- Flying drone
- Turret or heavy stationary/slow unit

## Weapon presentation

Weapons are independent sprites attached to an aim pivot. This allows continuous 360° aiming without requiring directional firing animations for the player body.

Initial weapon family:

- Sidearm
- Rifle
- Shotgun

## Working palette

Do **not** copy a third-party palette file directly.

Rustline will establish its own compact palette. Desired color families:

- Near-black / blue-black structural shadows
- Cool dark steel
- Neutral mid steel
- Oxidized copper / rust orange
- Dirty off-white / ceramic armor
- Hazard yellow
- Warning red
- Cold cyan / blue electronic accents
- A small number of skin / fabric colors if required by the player design

Target roughly **16–24 core colors** before optional FX colors.

## Asset-generation workflow

1. Generate an original Rustline canonical asset from the written brief.
2. Select and clean it manually where necessary.
3. Treat the approved Rustline asset as the visual reference for subsequent Rustline generations.
4. Keep silhouettes, proportions, light direction, pixel density, and palette consistent.
5. Use GIMP/manual pixel editing for alignment, frame cleanup, palette correction, and sprite-sheet packing.
6. Commit only assets whose redistribution rights are clear.

### Reference rule

Third-party packs may be studied for broad design principles, genre conventions, modularity, and readability, but Rustline assets should not be produced by modifying or redistributing those third-party source files.

Once an original Rustline visual baseline exists, prefer Rustline's own artwork as the reference source for future generations.

## Initial asset checklist

### Player

- Canonical neutral side-view
- Idle
- Run
- Jump
- Fall
- Roll / dodge
- Hit
- Death

### Environment

- Floor/platform center + edges
- Walls
- Inside/outside corners
- Structural beams
- Pipes/conduits
- Vents
- Background panels/machinery
- Warning-stripe variants
- Damaged/corroded variants

### Gameplay props

- Door
- Extraction terminal
- Loot container
- Scrap crate
- Medkit
- Ammo pickup
- Scrap/resource pickup

### Enemies

- Ground crawler
- Drone
- Turret/heavy unit

### FX

- Muzzle flash
- Projectile/tracer
- Impact spark
- Small explosion
- Damage feedback
- Extraction beacon/effect

## Quality bar

The art does not need to be content-rich. It does need to look like one coherent game.

A small, consistent set of excellent reusable assets is preferable to a large set with inconsistent proportions, palette, or pixel density.
