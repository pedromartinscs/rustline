# M7 environment visual hardening

Historical M7 report. [M7.1 coherence cleanup](environment-coherence-cleanup.md)
supersedes the modular reinforced-plate/destruction policy below: reinforced traversal
walls are now continuous permanent structure, and only `industrial_surface.png`
represents destructible gameplay material. The original assets remain unchanged.

## Audit and plan

Baseline: merged main `ca14060`; clean branch `feat/m7-environment-visual-hardening`. Read AGENTS, ART_DIRECTION, PALETTE, TILESET_SPEC, ENVIRONMENT_GAMEPLAY_METRICS, RELEASE_COLLISION, FIRST_PLAYABLE, saved SalvageIntake, imported environment PNGs/metadata, production/expansion/dressing builders, breaching authority, geometry/render tests and Release helper.

The scene is a deterministic graybox plus named managed art roots. Original machinery, west wall, floor and first shaft skins live under Art Dressing - Preserve, but specialized dressing tools replace their managed children. Expansion owns terrain east of x=46 and its own art root; full production setup rebuilds the intro, reapplies dressing, then expansion and gameplay. Author the pass in these builders, not as disposable hand edits. Preserve unrelated authored roots.

Mandatory wall-kick routes: first service shaft, interior x=28..32, entry y=0, exit y=17; second shaft x=122..126, entry y=17, exit y=29. Rail recovery well x=144..148, bottom y=21, exit y=29 is conditional traversal after a missed bridge jump. All retain their existing four-unit openings and collision planes. The initial shaft already has non-breachable architectural skins; the second shaft/recovery walls are currently generic RuleTiles. The west boundary, floor skins and shaft skins show heavy chipped/corroded material despite being non-breachable.

Important repository distinction: generic structural RuleTiles really ARE breachable by Longwatch. Eligibility requires both visual and collision cells. Removing visual cells to cover the second shaft would silently change gameplay; instead use intact reinforced visual tiles while retaining cell presence and the existing breaching authority. Do not change destruction logic. Keep exposed modular seams readable on these replaceable plates, versus continuous bolted architecture on existing permanent skins.

Expansion repeats the hero salvage claw twice (x=62 and x=115), eight tall gantry uprights across gallery/maintenance/terminal and three identical machine housings. The repeated handlers have no convincing local overhead mounting. Retain the supported central intro handler and its two original uprights; replace late claws and repeated gantries with anchored pressure, exhaust and conduit infrastructure. Preserve visual mass with distinct silhouettes, not empty deletions.

Implementation: (1) author continuous steel/ribbed shaft, west-wall and floor families at native dimensions, using deterministic pixel construction for exact edges; (2) apply reinforced visual tiles to the second shaft and recovery well without changing collision or breach eligibility; (3) integrate three original generated machinery sprites, canonical-quantized with binary alpha, as low-contrast background dressing; (4) update deterministic builders, add asset/scene/idempotency/geometry/render checks and export route views. Every new decorative renderer stays native 16 PPU, scale one, unrotated, behind player/enemies/projectiles and without colliders. No geometry or gameplay adjustment is planned.

Validation: baseline and full Unity suites, focused saved-scene traversal and rendering checks, production regeneration/idempotency, exact Canonical 28/binary alpha/import checks, normal Windows x64 Release and existing smoke. Artistic acceptance, combat sightlines and full-route human traversal remain human gates.

## Implemented pass

| Zone | Revision |
| --- | --- |
| West boundary and lower bay | Closed bolted steel panels replace chipped permanent wall skins. Continuous reinforced floor edges replace hole/rubble-like cap shapes on the existing non-breachable floor/armor envelope. |
| First service shaft, x=28..32 | Continuous ribbed architectural skins retain the exact original 33/36x240 source dimensions, cropped slices, pivots and contact planes. The two exit rows above the right skin remain modular cells. |
| Second shaft, x=122..126 | Intact ribbed modular plates replace generic mottled tiles on both walls. Existing cells and Longwatch eligibility remain. |
| Rail recovery well, x=144..148 | The same aligned plate language identifies the safe return route from below the bridge. |
| Terminal end stop, x=195 | Closed structural plate language replaces the generic cracked-looking wall. |
| Gallery, maintenance and terminal | Two late claws, eight tall gantry repeats, the late overhead rail and three housing repeats are replaced by six service-machine placements and seven braced columns. The supported introductory claw and its two original uprights remain. |

New machinery is deliberately background dressing: pressure reclaimers at x=62/135, exhaust stations at x=84/164, conduit manifolds at x=113/181. Pressure/exhaust feet meet the existing y=17/29 deck levels; columns span from those decks to existing y=31/34/39 beams; conduit mounting rails meet those columns/upper beams. Every new renderer has scale one and an untinted unlit material. Equipment uses sorting order -16, frames -18, existing structural skins 1/2. Actor, projectile and gameplay-marker layers remain unchanged.

There are **no geometry/gameplay changes**. `Tools/EnvironmentArt/validate_scene_preservation.py` compares the saved scene to `ca14060` and verifies all **114 protected MonoBehaviour/physics/map blocks** are byte-identical, with no added/removed physics or gameplay components. The collision maps retain **890 ground cells and 6 precision-ceiling cells**, including identical serialized data. The scene diff contains replacement art objects/references, visual Tilemap data and child lists; Unity's object ordering can make unchanged blocks appear moved in textual diffs.

Exactly **78 existing structural visual cells** receive reinforced Tile assets with no visual-map collider. This retains the existing two-map breach eligibility test; no visible cell is cleared for the new modular panels. Initial permanent architecture still has no visual RuleTile behind it and remains non-breachable. Remaining worn generic structural tiles really are breachable; distant background weathering is retained. Generic expansion floor strips retain their previous visual/destruction behavior. No collider, player, encounter, checkpoint, frontend, enemy, native compositor or runtime breaching code was changed.

## Assets and reproducibility

All final assets live in `Assets/Art/Environment/Hardening/`:

| Family | Native source dimensions |
| --- | --- |
| reinforced_shaft_a / b | 33x240 / 36x240 |
| reinforced_wall_top / mid / bottom | 50x150; legacy top slice remains 50x145 |
| reinforced_floor_left / mid_a / mid_b / right | 48x32; legacy right slice remains 47x32 |
| reinforced_plate_left / right | 16x16; two corresponding visual Tile assets |
| service_column | 32x64, vertically repeated at native pixel density |
| pressure_reclaimer | 160x192 |
| exhaust_station | 224x144 |
| conduit_rack | 256x128 |

The 15 PNGs use Canonical 28, binary alpha, Point/clamp, 16 PPU, no mipmaps/compression/resampling at import, Full Rect meshes and no generated physics shapes. Old approved source PNGs/XCFs are retained unchanged. Structural assets are authored as deterministic pixels; original machinery was generated with built-in image_gen, then source-alpha thresholded, nearest-neighbor normalized and exactly palette-quantized. High-resolution generated sources are retained in `Assets/Art/Source/Environment/M7/` and are not gameplay sprites. Full generation prompts and provenance are in `Tools/EnvironmentArt/prompts.md`.

From the repository root, `python Tools/EnvironmentArt/build_m7_art.py` rebuilds final PNGs deterministically from those sources (Pillow required). Existing metadata is preserved; legacy-compatible new metadata retains slice/pivot geometry under separate GUIDs. `python Tools/EnvironmentArt/validate_scene_preservation.py` repeats the baseline preservation check.

Final regeneration reproduced all **15 PNGs byte-for-byte**, `Logs/m7-art-reproducibility.txt`.

## Tooling and saved scene

The saved SalvageIntake is ready for direct Editor Play. `RustlineEnvironmentVisualHardeningSetup.Apply` / **Tools > Rustline > Apply Environment Visual Hardening** reapplies only the four existing structural skin passes, owned expansion renderers and reinforced visual cells. `RefreshDressing` leaves terrain and the precision grid alone. It replaces owned children rather than hiding obsolete duplicates and preserves unrelated art outside managed roots.

Full **Apply Production Setup** uses the updated skin assets and invokes the M7 visual-cell pass after expansion dressing. The existing release helper validates the M7 saved-scene contract before exporting. Build scene order and the frozen foundation-validation bridge remain unchanged. Automated tests execute full production regeneration, validate the result and restore the original saved scene afterward, avoiding incidental regenerated IDs in the commit. Repeated dressing produces the same placements/counts and collision footprint.

## Validation and review

Baseline EditMode: **397/397**, `Logs/m7-baseline-edit.xml`. Focused art/tooling: **18/18**, `Logs/m7-focused-edit.xml`. Full EditMode: **415/415**, `Logs/m7-full-edit.xml`. Full PlayMode: **109/109**, `Logs/m7-full-play.xml`. These cover all 15 PNGs, native import/physics-shape rules, saved references, golden collision footprints, idempotent visual refresh, unrelated art preservation, full production regeneration and existing gameplay/frontend/combat regressions.

The focused geometry run passed all nine traversal/geometry cases, including new real-input coverage of the first shaft, the existing second shaft and recovery-well wall kicks, ledge exits, bridge jump, crouch underpass and continuous gallery seam. The two new runtime art/breaching cases passed in `Logs/m7-render-breach.xml`: actual Longwatch hit delivery still removes the reinforced visual/collision cell, and the permanent first-shaft skin remains solid. Native world and utility-camera renders are exported for ten route viewpoints under `Logs/M7/RouteRenders/`, including separate strict-palette renders of the new sprites. Baseline composition captures remain under `Logs/M7/Before/`.

Rendering detail: the test explicitly drives both existing cameras in batch; rendering only the world camera leaves the utility resolve target uninitialized. Full views retain actors/checkpoints for readability review. Existing gameplay Warning Orange produces `(251,171,42)` on this GPU versus authored `(251,171,41)`, also present in pre-M7 bay/upper-defense captures. The compositor is preserved. New scenery avoids that feedback color; every opaque pixel of every new source PNG and every isolated native resolved sprite render is checked strictly against Canonical 28. Full-world captures are not claimed as proof that pre-existing gameplay feedback is byte-exact canonical.

Windows x64 **normal Release build succeeded**, BuildOptions.None, `Logs/m7-release-build.log`, executable `Builds/FirstPlayable/Rustline.exe`. The actual exported Composites retain SalvageIntake ground **4 paths/78 points**, maintenance precision ceiling **1/4**, MovementLab **9/48**. With the Editor closed, the unchanged opt-in smoke **passed with exit 0**, `Logs/m7-release-smoke.log`: title startup, deterministic playable entry, supported spawn, real Tilemap input traversal/jump/landing, all seven staged hitscan encounters, checkpoints/retry, extraction and restart. This preserves the accepted collision contract; no smoke assertions were removed. The existing URP shutdown ComputeBuffer disposal warning remains, as in the pre-M7 logs.

The final full PlayMode run includes all nine geometry tests and both new art/breaching cases. Earlier render probes retained in Logs were harness investigations, not acceptance runs. Reproduce suites with Unity 6000.4.0f1 and graphics enabled (`-runTests -testPlatform EditMode` or `PlayMode`); build with `-executeMethod Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease -quit`. Repeat the Release smoke with `--first-playable-smoke --smoke-auto-quit`. `Logs/m7-preservation.txt` retains the saved-scene comparison and `Logs/M7/gallery-before-after.png` provides a local composition comparison. Hidden-window Release smoke screenshots are not used as artistic evidence.

Human review remains required: play the entire route at normal native presentation, wall-kick both mandatory shafts, recover from the rail well, assess grips/ledge edges and visual trust, aim/fire through all seven combat spaces, check checkpoint/extraction visibility and the machinery's background contrast, and approve variety/coherence at 800x600, 1280x720 and 1920x1080. Automated renders and traversal tests do not constitute human artistic or gameplay acceptance.
