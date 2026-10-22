# M5 — Bombardier art and Sentry finish

## Audit and scope

Baseline: current main merge `05cd1fe` (M4 `60fe63c`), clean dedicated branch `feat/m5-enemy-art-and-sentry-polish`, Unity 6000.4.0f1. Read AGENTS, palette/art/presentation, M4 source/prompt/converter, first-playable and expansion design/validation, saved scenes, enemy gameplay/presenters, production and MovementLab builders, and related tests. The accepted Sentry is the primary visual reference; player 48×64 cells and the industrial environment establish scale/materials. Production art requires exact Canonical 28 RGB and binary alpha, 16 PPU, Point/Full Rect, stable bottom-center pivots, no compression/mipmaps/generated physics.

M4 Sentry uses sixteen fixed 48×64 sprites and a dedicated state-driven presenter. Its authoritative brain owns alert/windup/recovery; only its windup changes from 0.55 to 0.48 seconds. The original source, sixteen-frame sheet and sprite IDs must remain unchanged. A separate three-frame sheet will progressively remove pixels from the final wreck, after the accepted collapse and a 0.6-second hold, then hide only the renderer. Health reset must restore visibility from every phase.

Bombardier still uses a cyan seven-point LineRenderer. Its presenter owns hit/death feedback and the locked-target diamond; gameplay also reads that silhouette's material to create bombs. Replace the body with sprites and give gameplay an explicit projectile material, retaining the existing diamond/material and bomb mechanics. Its 0.70-second windup and 1.30-second recovery remain authoritative. Launch pose follows entry into recovery; presentation never spawns bombs. Movement metadata must record commanded kinematic movement rather than sample consumed Rigidbody velocity.

Production authoring is deterministic: FirstPlayable setup owns the managed gameplay subtree in Salvage Intake. The same Bombardier is also authored by RustlineM1ASetup in MovementLab; adapt only its enemy visual/material authoring to preserve that regression fixture. No terrain, player, camera, penumbra, collision initializer or build-guard changes. Saved scenes must work directly and rebuilt scenes must retain equivalent presentation.

## Art direction and source

Original heavy demolition automaton: compact armored head/cyan slit, stocky steel/rust body, hazard bands, exposed hydraulics, twin explosive canisters and an asymmetric throwing claw. Distinct from the red-visor melee Sentry; consistent industrial material/value groups. Sixteen reviewed poses cover idle, patrol, three windup keys, release, recovery, hit and four collapse keys. Generation uses the built-in image_gen tool; original source and exact prompt/conversion recipe are retained with the assets.

| Asset | Repository path |
| --- | --- |
| Original unmodified source | `Assets/Art/Source/Enemies/Bombardier/bombardier_generated_source.png` |
| Exact prompt and reviewed conversion recipe | `Assets/Art/Source/Enemies/Bombardier/generation-prompt.md` |
| Production sprites | `Assets/Art/Characters/Enemies/Bombardier/bombardier_demolition_sheet.png` |
| Sentry finish, original sheet untouched | `Assets/Art/Characters/Enemies/Sentry/sentry_dissolution.png` |

`RustlineBombardierArtSetup` converts the reviewed 1086×1448 source with one 7:1 scale, point samples and a common horizontal anchor. Reviewed boundary corrections preserve the release fingers and separate neighboring death feet; opaque foot baselines align to the fixed cell floor. Alpha threshold 200 and nearest Canonical28 RGB selection produce a 192×256 binary-alpha sheet with sixteen 48×64 cells. Upright body/canisters occupy about 40 native pixels vertically; raised claw uses the transparent overhead space. The silhouette is wider than the Sentry, without enlarging collision. Source references import as Default textures with no selectable Sprite subassets. Production imports remain Full Rect, Point/clamp, 16 PPU, no mipmaps/compression/rescaling/generated physics, bottom-center `(24,0)` px pivots. Stable names preserve existing sprite IDs on repeated authoring.

## Animation and gameplay boundaries

| Frames | Selection |
| --- | --- |
| 0–1 idle | Dormant/standing, 2 fps |
| 2–5 patrol | Actual commanded movement, 8 fps; direction from gameplay |
| 6–8 preparation | Three thirds of the existing 0.70 s windup; locked facing/target unchanged |
| 9 empty-claw follow-through | First 0.12 s of the existing 1.30 s recovery, after the real bomb launch |
| 10 recovery | Remaining recovery time |
| 11 hit | Existing authoritative 0.12 s hit pause; windup/recovery clocks remain paused |
| 12–15 collapse | Four 0.14 s poses, final wreck retained |

`BombardierPresenter2D` subscribes to health events, selects sprites, mirrors facing and snaps only its visual child at 16 PPU. No root movement, damage, health, projectile creation, state transitions or encounter writes occur in presentation. Dormant living enemies remain visible/damageable. Death cancels the warning and plays collapse; reset immediately restores idle, facing, white tint and visibility. Existing presentation counters remain available to combat regressions.

`Bombardier2D.projectileMaterial` replaces the gameplay dependency on `SilhouetteRenderer`; the same existing shared unlit material is assigned explicitly to the warning diamond. Bomb creation, fixed throw origin `(0,2)` u, ballistics, locked target, explosion and reset cleanup are unchanged. The launch sprite is an empty-hand follow-through, not a separately timed launch command. Body outline renderers are removed; the legitimate target and bomb LineRenderers remain.

## Scene and deterministic authoring

All six saved Salvage Intake Bombardiers and the MovementLab combat fixture use sprites. `RustlineFirstPlayableSetup` creates bodies without placeholders and configures sprite/material references. `RustlineM1ASetup` uses the same configuration for its diagnostic enemy; its collision/movement generation is unchanged. `Apply Production Setup` / `Apply Gameplay` use committed sprites without regenerating source art. Ordinary scene launch needs no setup.

Explicit source conversion entry points: `Rustline.Editor.RustlineBombardierArtSetup.RebuildAndApply` and the existing Sentry equivalent. The Bombardier command saves only Salvage Intake; MovementLab shares the stable sprite IDs and its own builder configures its actor. `Rustline.Editor.RustlineBombardierArtSetup.ValidateRegeneration` runs both conversions twice and rejects any PNG, metadata, source or saved-scene byte changes across twelve tracked files. These are Editor batch `-executeMethod` entry points; they do not execute at runtime/import automatically.

## Validation record

Baseline: **337/337 EditMode, 84/84 PlayMode**, no failures/skips (`Logs/M5/baseline-*.xml`). Sentry milestone: **19/19 focused EditMode, 5/5 focused PlayMode** (`Logs/M5/sentry-*.xml`). The latter covers the actual strike/hit pause/damage, native-pixel rendering and reset/retry/restart during collapse, wreck hold, all three dissolution frames and after hidden. The original six M4 art/source/provenance files (including PNG metadata) retain their SHA-256 hashes.

Sentry collapse retains four 0.12-second poses, then holds the wreck through 1.08 seconds, dissolves with three 0.13-second masks and hides at 1.47 seconds. The masks retain deterministic two-pixel fragments and are nested exact-color subsets of the original wreck: no pixels move, fade or change color. Reset immediately restores idle/white tint/visibility. Disabling and re-enabling an already-dead presenter does not resurrect its presentation. Only the SpriteRenderer disappears; the dead actor remains for ordered progression.

Automated rendering/state tests are evidence for their tested fixtures, not a human playthrough or a measured performance claim.

Bombardier focused validation: **8/8 EditMode, 4/4 PlayMode** (`Logs/M5/bombardier-*.xml`). Exact pixels/alpha/imports/pivots and reimport ID stability passed. Real state integration confirmed dormant visible/damageable sprites, hit pause, locked target marker, one actual bomb with the explicit shared material and unchanged ballistic velocity, launch/recovery poses, collapse and retry/restart/projectile cleanup. Actual patrol poses and snapped visual positions were observed in camera-render callbacks. Resolved native-pixel foreground-on/off comparisons at 4, 30.5 and 35 world units passed for Canonical28 visible/penumbra pixels and zero contribution in full darkness.

Inspected `Logs/BombardierArt/contact-sheet-4x.png`, `world-visible.png`, `world-penumbra.png` and the Sentry dissolution PNG. Contact sheets are nearest-neighbor review enlargements; world images come from the real resolved native target with a held camera/disabled player physics to isolate enemy visibility. These are automated offscreen fixtures, not normal-window human gameplay review.

Two complete source conversion/reapplication passes passed with **all twelve checked PNG/meta/source/scene files byte-identical** (`Logs/M5/art-determinism.log`, `RUSTLINE_ENEMY_ART_DETERMINISTIC_OK`). Saved-scene scope versus main: Salvage Intake **68 canonical block changes**, MovementLab **10**, all within enemy roots. Unity refreshed two unrelated MovementLab visual RuleTile caches during its one-time enemy migration; those blocks were restored to the original serialized values before final validation. Collision Tilemaps/Composite configuration, terrain, player, camera, encounter/checkpoint references and level dimensions remain unchanged (`Logs/M5/scene-scope.json`).

Full `Apply Production Setup` passed (`Logs/M5/production-reapply.log`). All **620 scene blocks** remained semantically identical after canonicalizing regenerated local IDs (`Logs/M5/production-scene-preservation.json`). Retained the pre-rebuild serialized scene IDs to keep the final diff scoped to enemy changes. The full regression suites run against that directly launchable final scene.

Full Unity regressions: **356/356 EditMode, 90/90 PlayMode**, zero failures/skips (`Logs/M5/full-edit.xml`, `full-play.xml`). This includes all existing combat/checkpoint/extraction/restart/geometry tests. A further real resolved-target Sentry death test captures the wreck, each dissolution mask and hidden presentation; foreground pixel counts strictly decrease while remaining Canonical28. Inspected `Logs/SentryArt/m5-death-{0..4}.png`; the actor remains dead/active with its hitbox disabled. The production dissolution frames retain **201 → 117 → 39** original pixels, then no rendered contribution. Bombardier production uses **26 permitted canonical RGB values**, with alpha restricted to **0/255**.

## Remaining human review

Open the committed Salvage Intake scene and play normally; no regeneration is needed. Review the slightly faster Sentry strike and its collapse/hold/digital fragmentation at native 1× and integer enlargement. Retry during the effect and restart after it vanishes. Assess Bombardier silhouette, foot contact, patrol, three anticipation poses, empty-claw release, hit and collapse against each combat space, including elevated/mirrored enemies. Check the fixed launch anchor and warning marker in motion and review ordinary-window readability around the clear-radius/penumbra boundaries in Editor and Windows Release.

No M5 human art/playability sign-off, measured FPS or gameplay duration is claimed. The art uses four patrol keys and mirrored side views; the release is a keyed follow-through rather than continuous skeletal animation. The Bombardier intentionally retains its settled wreck. No enemy audio, extra terrain, new enemy AI, projectile redesign or rendering rewrite is included. Further cadence/pose refinement should follow human review rather than changing validated combat timing.
