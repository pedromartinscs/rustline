# Salvage Intake — First Production Area

`SalvageIntake` is Rustline's first production-area graybox and the first step away from the diagnostic `MovementLab` toward the itch.io vertical slice.

The initial goal is deliberately narrow: build one coherent industrial room that is enjoyable to traverse and aim through with the frozen player/Longwatch foundation before creating a broad environment asset library.

## Scene and builder

Target scene:

`Assets/Scenes/Demo/SalvageIntake.unity`

Deterministic graybox builder:

`Assets/Editor/RustlineSalvageIntakeSetup.cs`

Unity menu:

**Tools → Rustline → Rebuild Salvage Intake Graybox**

The scene itself is generated from the accepted project assets. The builder first runs the existing M1 validation, then creates/synchronizes the production-area graybox without modifying `MovementLab`.

The builder owns only these roots:

- `Environment - Managed Graybox`
- `Player Rig - Managed`

These roots are deliberately preserved across rebuilds and must be used for hand-authored work that should survive graybox regeneration:

- `Art Dressing - Preserve`
- `Gameplay Content - Preserve`

Do not place permanent hand-authored dressing under a `Managed` root.

## Current spatial contract

The first room is **56 canonical tiles wide** at the outer boundary. All initial gameplay geometry is aligned to the normal **16×16 px / 1×1 unit** structural grid.

The first pass intentionally contains no sub-cell collision. The accepted **24 source pixel / 1.5 unit Minimum Traversable Gap** remains the hard authoring floor, while ordinary production openings should normally use at least **2 whole tiles / 32 px** when practical.

The room starts with:

- a continuous safety floor;
- left/right bulkhead boundaries;
- a west-side service rise with readable 1–2 tile height changes;
- a six-unit central salvage-machinery plinth;
- an upper solid transfer catwalk;
- an east catwalk support and low staging/cover block;
- a player spawn on the west side;
- an exit-staging marker on the east side.

The critical route does not require a diagnostic sequence of every movement ability. The geometry should read as an industrial place first. Jump, LedgeClimb, Wall Brace/Kick, crouch, and future combat positioning should emerge from useful architecture rather than isolated ability-test stations.

## Visual geometry versus gameplay collision

The scene contains three initial Tilemaps:

1. `Background Structure - Visual` — non-colliding compositional masses and the placeholder footprint for the future hero machinery;
2. `Industrial Surface - Visual` — the visible structural graybox corresponding to the current traversable solids;
3. `Ground Collision - Hidden` — the authoritative gameplay geometry.

`Ground Collision - Hidden` keeps the release-hardened contract from `RELEASE_COLLISION.md`:

- Ground layer 6;
- renderer disabled;
- static `Rigidbody2D`;
- `TilemapCollider2D` with `Merge`;
- polygon `CompositeCollider2D`;
- `TilemapCompositeColliderInitializer2D`;
- immediate geometry generation/validation before the scene is accepted.

The current visible structural graybox mirrors the collision cells because that is useful during blockout. This is **not** a permanent requirement. As dressing begins, visual wear, cracks, recesses, machine silhouettes, pipes, foreground pieces, and background structure may diverge freely from collision while gameplay collision remains simple and traversal-safe.

## Presentation/performance invariants

Salvage Intake reuses the accepted player prefab and the same scene-level presentation architecture proven in MovementLab:

- 16 PPU;
- Rustline Canonical 28 production art contract;
- `Sprite-Unlit-Default` for the initial graybox;
- native-pixel logical rendering;
- integer presentation scaling;
- palette-constrained penumbra;
- no identity/decorative `Light2D` objects in the first graybox;
- existing 60 FPS runtime policy unchanged.

The builder wires `PlayerAim2D` to the scene's `NativePixelPresentation` and preserves the accepted Longwatch camera-impulse integration without changing player, movement, weapon, muzzle, recoil, or carry behavior.

## Hero-room composition

The central visual idea is a **salvage transfer / sorting machine** whose eventual art occupies much more visual space than its simple collision plinth. The machine should become the first unmistakable Rustline environment silhouette.

The initial background Tilemap reserves a broad central mass, overhead beam, side structural columns, and connector shapes for this composition. These are placeholders, not final environment art.

The intended visual hierarchy is:

1. readable player and combat silhouettes;
2. collision-authoritative structural floor/catwalk edges;
3. hero transfer machine;
4. large background structure;
5. pipes, conduits, vents, warning markings, corrosion, and restrained foreground dressing.

Do not solve the room by adding many overlapping lights, full-screen effects, or large transparent illustrations. Prefer composition, modular tiles, bounded sprites/overlays, and the existing penumbra presentation.

## First environment-asset gate

Do **not** fill every unused slot in the structural atlas before inspecting this room in-engine.

After the graybox is generated and human-tested at native scale, the first art batch should be chosen from what the room visibly needs. The expected first candidates are:

- top-surface variants (`industrial_surface` slots 24/25);
- left/right wall variants (26/27);
- ceiling variant (28);
- interior plate variants (29/30/31);
- structural support / bracket pieces (44/45);
- one large Rustline-owned salvage-transfer-machine hero asset;
- a minimal detail family for one pipe/conduit, vent, warning marking, and status-light treatment.

This list is a starting expectation, not a requirement to manufacture unused assets.

## Immediate acceptance gate

Before environment art production begins, inspect the generated graybox in Unity and verify:

- the room is traversable without getting stuck;
- the service rise and catwalk feel like architecture rather than a movement tutorial;
- the central plinth creates useful sightline/positioning changes with the Longwatch;
- dropping from the upper route gives natural opportunities for Fall aim / Wall Brace without requiring them;
- the west-to-east route is readable under the existing penumbra;
- no new collision seam, phantom Land, or Release-only floor regression appears;
- the player/Longwatch presentation remains unchanged.

Only after this gate should the first GIMP environment-production pass begin.
