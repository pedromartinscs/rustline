# M7.1 environment coherence cleanup

## Audit and implementation plan

Baseline: merged `main` d5c6dc8 (M7), cleanup branch `feat/m7-1-environment-coherence-cleanup`. The cleanup branch was created from the fetched merge after checking the local/remote inventory, and its name was confirmed by the user. Read AGENTS, art direction, Canonical 28, tileset specification, gameplay metrics, release collision, M7 documentation, scene serialization, generation tools and existing tests before editing.

The scene combines generated collision/visual Tilemaps with preserved sprite dressing. The production pipeline applies specialized intro skins, expansion terrain/dressing, then the final hardening presentation pass. `BreachableTilemap2D` requires both a structural visual tile and a collision tile. Merely changing a tile's artwork does not make it permanent. M7's 16×16 reinforced tiles therefore both produced the small-block appearance and remained breachable. This cleanup changes authoring eligibility where solid structure is intended; it does not change weapon code.

Only `industrial_surface.png` carries destructible gameplay language. Background weathering remains scenic. No new PNGs, recoloring or asset pack were needed: reuse the existing reinforced shaft and floor sprites at 16 PPU, point sampling and scale one.

| Route/area | Current inconsistency | Targeted correction |
| --- | --- | --- |
| First mandatory shaft, interior x28–32, floor 0 / exit 17 | Right skin ends at 15; four small reinforced tiles form its exit | Extend continuous right skin to 17; preserve left face x28, right face x32 |
| Second mandatory shaft, interior x122–126, floor 17 / exit 29 | Small reinforced tiles on both faces | Continuous solid shaft skins, left x120–122/y21–31 and right x126–128/y17–29 |
| Rail recovery well, interior x144–148, floor 21 / exit 29 | Small blocks below destructible-looking lips | Continuous solid faces x142–144 and x148–150, y21–29, including existing lip cells |
| Terminal end stop x195–196/y29–35 | Small reinforced tiles still breachable | Continuous solid end-wall skin |
| Maintenance crouch ceiling x102–108, underside19.625 | Non-breachable offset ceiling displays industrial destructible tiles | Existing reinforced floor skin; keep six collision cells and 0.625 offset |
| Intro transfer catwalk x4–16/y4–5 | Chipped/rubble-edged permanent skins | Existing clean reinforced floor skin on the same collision row |
| Gallery/maintenance, upper service and terminal decks | 47 damaged decorative floor strips mask actually breachable industrial tiles and survive their destruction | Remove those overlays; show the actual industrial tiles, retaining their existing breaching behavior |

No collision cells, collider dimensions/transforms, traversal metrics, player tuning, encounters, checkpoints, menus or compositor changes are permitted. Clearing structural visual cells under reinforced walls intentionally makes those walls permanent. Existing industrial floors/cover remain breachable. This supersedes M7's policy of retaining breaching on reinforced plates.

Validation plan: baseline focused EditMode; focused art/authoring/breaching/render tests and real-input traversal cases; full EditMode/PlayMode; compare protected serialized gameplay/physics blocks and both collision footprints against main; normal Windows x64 Release and original gameplay smoke. Inspect native route renders, then list remaining human review explicitly.

## Palette audit

Run `python Tools/EnvironmentArt/audit_palette.py --report Logs/M71/palette-audit.json`. This read-only audit parses the exact 28 RGB triples in PALETTE.md, inspects every decoded production pixel, counts alpha values, and records SHA-256 source hashes. All 15 PNGs introduced by M7 are individually reported below; the same scan also covers the other 25 PNGs under `Assets/Art/Environment`, including `Tiles/industrial_surface.png` and the legacy skins replaced in this cleanup. Unity EditMode independently checks all 15 M7 imports for point sampling, 16 PPU, no mipmaps, no compression, no NPOT rescaling and no fallback physics shapes.

| M7 production asset | Dimensions | Opaque pixels | RGB colors | Off-palette | Partial alpha |
| --- | --- | ---: | ---: | ---: | ---: |
| `conduit_rack.png` | 256x128 | 21143 | 17 | 0 | 0 |
| `exhaust_station.png` | 224x144 | 22947 | 14 | 0 | 0 |
| `pressure_reclaimer.png` | 160x192 | 20219 | 17 | 0 | 0 |
| `reinforced_floor_left.png` | 48x32 | 1536 | 6 | 0 | 0 |
| `reinforced_floor_mid_a.png` | 48x32 | 1536 | 5 | 0 | 0 |
| `reinforced_floor_mid_b.png` | 48x32 | 1536 | 6 | 0 | 0 |
| `reinforced_floor_right.png` | 48x32 | 1536 | 6 | 0 | 0 |
| `reinforced_plate_left.png` | 16x16 | 256 | 7 | 0 | 0 |
| `reinforced_plate_right.png` | 16x16 | 256 | 7 | 0 | 0 |
| `reinforced_shaft_a.png` | 33x240 | 7920 | 8 | 0 | 0 |
| `reinforced_shaft_b.png` | 36x240 | 8640 | 8 | 0 | 0 |
| `reinforced_wall_bottom.png` | 50x150 | 7500 | 8 | 0 | 0 |
| `reinforced_wall_mid.png` | 50x150 | 7500 | 8 | 0 | 0 |
| `reinforced_wall_top.png` | 50x150 | 7500 | 8 | 0 | 0 |
| `service_column.png` | 32x64 | 1166 | 4 | 0 | 0 |

Result: **40 production PNGs, zero off-palette opaque pixels, zero partial-alpha pixels**. Opaque pixels match exact canonical RGB values; transparent pixels have alpha 0 and all visible pixels alpha 255. No recoloring, transparency correction, PNG or texture-import changes were needed. The three high-resolution generation references in `Assets/Art/Source/Environment/M7` are source-only, not runtime sprites and not included in this production audit. The old 16x16 reinforced plate PNGs/Tile assets remain as unused historical assets, but no longer appear in the saved level.

## Implementation and lifecycle

The existing `RustlineEnvironmentVisualHardeningSetup` final pass now owns continuous solid skins and validates both their source/bounds and industrial-only breachable tiles. The first shaft's specialized pass also covers the full 17-unit right wall. Expansion generation invokes this same final pass after its terrain/dressing, so regeneration cannot restore the small plates. Presentation refresh removes/recreates only its named managed root; existing first-shaft and catwalk objects are updated in place. Unrelated authored content is preserved.

Expansion dressing now contains exactly 14 intentional objects: six service machines, seven braced supports and the extraction bulkhead. Its validator enforces that exact inventory instead of the former minimum count that included damaged deck overlays. No cranes, props, new meshes or textures were added. The older catwalk-clearance migration recognizes the clean tiled deck when rerun and retains its y4-5 placement; its original collision migration remains intact. Full production generation still runs the legacy steps before this final pass, preserving the foundation-validation bridge and build order.

The solid-skin authoring removes 78 reinforced plate visual cells and eight industrial-looking recovery-lip visual cells. The six precision-ceiling visual cells are also cleared. Hidden collision is unchanged. Those reinforced walls intentionally no longer accept Longwatch breaching; industrial floors, cover and ledges elsewhere retain their prior behavior. No runtime gameplay, physics, weapon, checkpoint, enemy, frontend, audio or compositor source was edited.

## Results and manual review

Unity 6000.4.0f1, graphics enabled (no `-nographics`), 2026-10-23:

| Validation | Result / evidence |
| --- | --- |
| Baseline focused EditMode before source changes | 18/18, `Logs/m71-baseline.xml` |
| Focused EditMode: palette/imports, saved skins, material rejection, idempotency, full production regeneration | 29/29, `Logs/m71-focused-edit.xml` |
| Focused PlayMode: material hits, native renders and all nine existing geometry/traversal cases | 11/11, `Logs/m71-focused-play.xml` |
| Full EditMode | 426/426, `Logs/m71-full-edit.xml` |
| Full PlayMode | 109/109, `Logs/m71-full-play.xml` |
| Production PNG palette audit | 40/40, `Logs/M71/palette-audit.json` |
| Saved-scene comparison against merged main d5c6dc8 | 114 protected component/map blocks identical; ground 890 and ceiling 6 cells unchanged |
| Normal Windows x64 Release | Succeeded, BuildOptions.None, `Logs/m71-release-build.log` |
| Original standalone smoke, Editor closed | PASS / exit 0, `Logs/m71-release-smoke.log` |

The exported executable is `Builds/FirstPlayable/Rustline.exe`. Build processing inspected the real Player scenes: Salvage Intake main Composite **4 paths / 78 points**, precision ceiling **1 / 4**, MovementLab **9 / 48**. The unchanged smoke verified normal title initialization, deterministic entry to gameplay, supported spawn, real-input movement/jump/landing and the extension seam, all seven actual hitscan encounters, checkpoint/retry, extraction and restart. `Debug.isDebugBuild` is false. No smoke or collision assertion was removed. The pre-existing URP shutdown ComputeBuffer disposal warning remains; the process exited 0.

`python Tools/EnvironmentArt/validate_scene_preservation.py d5c6dc8` also confirms no protected gameplay/physics components were added or removed and both collision Tilemaps are byte-identical. Their golden cell hashes remain `682e2f083f1f563383aa4aa3b547519e7f265166d8517fbbe44f6e01354e568e` (ground) and `328ffd3ad0ddf78bb8d21da54bfdff61d2201acd2161c64b09bd43fbfd5396be` (precision ceiling). Unity may retain zero-reference entries for retired plate assets in its serialized visual-Tilemap cache; no occupied cell or renderer uses them. No hidden duplicate props were left behind.

The runtime hit test delivers Longwatch hits to seven exposed permanent wall faces, including the first-shaft cap, both second-shaft walls, both recovery lips and the terminal stop. All retain collision. A positive control on the gallery industrial floor removes both visual and collision cells. It also checks that no detached damaged floor-strip object remains.

Nine exact-bounds tests verify continuous skins, scale one and no colliders. The saved-scene/material test rejects small plate art, requires industrial-only breachable tiles and an empty precision-ceiling visual Tilemap. A negative validator test deliberately substitutes the old plate Tile and verifies production validation rejects it. Existing production regeneration and repeated visual refresh tests preserve collision hashes, director state and unrelated authored art.

Actual Input System traversal tests pass for both mandatory shafts and the rail recovery well, their ledge exits, the bridge jump, the 42px crouch underpass, the maintenance-roof alternate route and the intro/gallery seam. Native world/resolved/isolated scenery captures for ten viewpoints are under `Logs/M71/RouteRenders`. The before/after second-shaft comparison is `Logs/M71/shaft-before-after.png`. New scenery's isolated resolved pixels pass the exact canonical check. Full-world captures retain the pre-existing feedback-color rendering detail documented in M7; the compositor was not modified.

No M7.1 human acceptance is claimed. Human review still required:

- Play the full route and inspect the first shaft exit, second shaft, recovery well/lips and terminal end wall; confirm the continuous steel reads as permanent climb-supportive structure.
- Inspect transfer catwalk and maintenance ceiling at 800x600, 1280x720 and 1920x1080. Check ledge/contact edges and the unchanged 42px clearance; verify no art obscures enemies, bullets, checkpoints or extraction.
- Fire Longwatch at reinforced walls and industrial floors/cover to review the intended material distinction and visible breaches. Review background damage as scenery, then approve visual coherence personally.

Reproduce: `python Tools/EnvironmentArt/audit_palette.py`; `python Tools/EnvironmentArt/validate_scene_preservation.py d5c6dc8`; Unity `-runTests -testPlatform EditMode` / `PlayMode`; normal Release `-executeMethod Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease -quit`; standalone `--first-playable-smoke --smoke-auto-quit`. To reapply only this art pass, use **Tools > Rustline > Apply Environment Visual Hardening**. Full **Apply Production Setup** also produces it. Individual older art-authoring stages should be followed by the final pass, as in production setup.


## Local delivery

- `45d2ec6` — continuous permanent traversal skins, saved-scene cleanup and regression coverage.
- Companion documentation/audit commit records the material rule, reproducible palette audit and final results.
- Branch: `feat/m7-1-environment-coherence-cleanup`. No push, merge or PR. Build/test/render evidence stays in ignored `Logs/` and `Builds/`; production PNGs and metadata are unchanged.
