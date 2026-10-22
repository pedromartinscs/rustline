# M5 — Bombardier art and Sentry finish

## Audit and scope

Baseline: current main merge `05cd1fe` (M4 `60fe63c`), clean dedicated branch `feat/m5-enemy-art-and-sentry-polish`, Unity 6000.4.0f1. Read AGENTS, palette/art/presentation, M4 source/prompt/converter, first-playable and expansion design/validation, saved scenes, enemy gameplay/presenters, production and MovementLab builders, and related tests. The accepted Sentry is the primary visual reference; player 48×64 cells and the industrial environment establish scale/materials. Production art requires exact Canonical 28 RGB and binary alpha, 16 PPU, Point/Full Rect, stable bottom-center pivots, no compression/mipmaps/generated physics.

M4 Sentry uses sixteen fixed 48×64 sprites and a dedicated state-driven presenter. Its authoritative brain owns alert/windup/recovery; only its windup changes from 0.55 to 0.48 seconds. The original source, sixteen-frame sheet and sprite IDs must remain unchanged. A separate three-frame sheet will progressively remove pixels from the final wreck, after the accepted collapse and a 0.6-second hold, then hide only the renderer. Health reset must restore visibility from every phase.

Bombardier still uses a cyan seven-point LineRenderer. Its presenter owns hit/death feedback and the locked-target diamond; gameplay also reads that silhouette's material to create bombs. Replace the body with sprites and give gameplay an explicit projectile material, retaining the existing diamond/material and bomb mechanics. Its 0.70-second windup and 1.30-second recovery remain authoritative. Launch pose follows entry into recovery; presentation never spawns bombs. Movement metadata must record commanded kinematic movement rather than sample consumed Rigidbody velocity.

Production authoring is deterministic: FirstPlayable setup owns the managed gameplay subtree in Salvage Intake. The same Bombardier is also authored by RustlineM1ASetup in MovementLab; adapt only its enemy visual/material authoring to preserve that regression fixture. No terrain, player, camera, penumbra, collision initializer or build-guard changes. Saved scenes must work directly and rebuilt scenes must retain equivalent presentation.

## Art direction and source

Original heavy demolition automaton: compact armored head/cyan slit, stocky steel/rust body, hazard bands, exposed hydraulics, twin explosive canisters and an asymmetric throwing claw. Distinct from the red-visor melee Sentry; consistent industrial material/value groups. Sixteen reviewed poses cover idle, patrol, three windup keys, release, recovery, hit and four collapse keys. Generation uses the built-in image_gen tool; original source and exact prompt/conversion recipe are retained with the assets.

## Validation record

Baseline: **337/337 EditMode, 84/84 PlayMode**, no failures/skips (`Logs/M5/baseline-*.xml`). Sentry milestone: **19/19 focused EditMode, 5/5 focused PlayMode** (`Logs/M5/sentry-*.xml`). The latter covers the actual strike/hit pause/damage, native-pixel rendering and reset/retry/restart during collapse, wreck hold, all three dissolution frames and after hidden. The original six M4 art/source/provenance files (including PNG metadata) retain their SHA-256 hashes.

Sentry collapse retains four 0.12-second poses, then holds the wreck through 1.08 seconds, dissolves with three 0.13-second masks and hides at 1.47 seconds. The masks retain deterministic two-pixel fragments and are nested exact-color subsets of the original wreck: no pixels move, fade or change color. Reset immediately restores idle/white tint/visibility. Disabling and re-enabling an already-dead presenter does not resurrect its presentation. Only the SpriteRenderer disappears; the dead actor remains for ordered progression.

Further implementation, pixel/import checks, rendered inspection, complete suites, production reapplication and Windows Release results will be recorded here after execution. Automated rendering/state tests are evidence for their tested fixtures, not a human playthrough or a measured performance claim.
