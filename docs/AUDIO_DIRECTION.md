# Rustline Audio Direction & Asset Intake (planning)

**Status (2026-10-30): SOURCE RESEARCH ONLY.** This document authorizes no imported or committed third-party audio. The current branch has Music/SFX preferences and `AudioBusSource` routing, but no production audio clips. Do not claim audible implementation or commercial rights for an unapproved file.

## Artistic direction

Rustline takes place in a derelict orbital salvage/refinery facility. Aim for oppressive industrial sci-fi: cold electronic drones, distant ventilation, low mechanical pulses, spacious metallic reflections, damaged servos and dry, readable weapon transients. The player must always hear actionable telegraphs and enemy threats clearly.

- **Explore:** slow, subdued, dark ambient soundbeds; leave room for footsteps, weapons and threats; avoid constant intrusive melody.
- **Combat:** short, restrained mechanical percussion/pulses and increased texture, without a bombastic genre shift. Gameplay telegraphs must remain audible.
- **Extraction:** rising tension during the final hold, a brief intelligible completion cue, then a controlled resolution.
- **Silence:** a valid design tool. Do not fill every quiet moment with music.
- **Loops:** a generative “loop-friendly” music prompt is not proof of a seamless loop; review and edit loop edges before integration.

## Initial SFX priority / intended triggers

**P0 (combat clarity):**
1. Latch-9 shot (2-3 close variants, restrained sci-fi/mechanical snap).
2. Longwatch shot (2-3 heavier variations, immediate transient; keep the weapon distinction).
3. Longwatch reload / magazine manipulation and weapon switching (separate from firing).
4. Player hit/damage; Sentry hit, alert/telegraph, attack and death.
5. Bombardier alert, launch, bomb flight cue if helpful, detonation, death/dissolution.
6. Metal/ground impact and destructible terrain cracking/breaking.

**P1 (play feel and navigation):**
7. Metal footsteps with occasional variation, landing, wall contact/kick.
8. Checkpoint ready and saved; recovery/resources confirmation.
9. Objective completion, extraction hold and success.
10. Menu selection, confirmation, cancel and volume preview.

**P2 (atmosphere):**
11. Industrial hum, motors, far machinery, warning electronics and ventilation.
12. Exploration background music, optional escalation and extraction layers.

Sound design should favor *few strong samples with variations* over hundreds of unreviewed clips. Do not overlayer every event, create infinite voice stacks or allow musical/ambient material to mask combat readability.

## Candidate libraries (not yet downloaded or approved individually)

| Candidate | Intended use | License information at time of research |
| --- | --- | --- |
| [Kenney — Sci-fi Sounds](https://kenney.nl/assets/sci-fi-sounds) | Energy shots, machinery, sci-fi events | Creator page: CC0, 70 assets |
| [Kenney — Digital Audio](https://kenney.nl/assets/digital-audio) | Electronic interaction/energy transients | Creator page: CC0, 60 assets |
| [Kenney — Impact Sounds](https://kenney.nl/assets/impact-sounds) | Hit layers, debris, grounded impacts | Creator page: CC0, 130 assets |
| [Kenney — Interface Sounds](https://kenney.nl/assets/interface-sounds) | UI, checkpoint, menus | Creator page: CC0, 100 assets |
| [Freesound](https://freesound.org/help/faq/) | Specific mechanical/industrial ambience | Per-file license; prefer CC0. CC BY requires attribution, CC BY-NC is unsuitable for commercial demo |
| [Suno](https://suno.com/terms/) | Original music/ambience experimentation | Paid-plan permitted downloads and current conditions required for commercial game use; preserve generation and download evidence |

The Kenney creator's [license FAQ](https://kenney.nl/support) describes its game-asset downloads as CC0, usable commercially without mandatory attribution. Recheck each actual download's included license. A compilation/download mirror is not a substitute for the original creator page.

Suno's policy and download rules changed in September 2026. Avoid creating final commercial tracks on a free tier. For final music, **create and download while subscribed to an eligible paid tier**, archive the download receipt/date, terms snapshot and generation metadata, and check current restrictions before shipping. A platform's grant of commercial use rights is not a guarantee of copyright exclusivity or registration.

## Intake gate for each candidate clip

Before putting any third-party clip in Git or shipping a build:
1. **Listen** to the isolated original at native level, then with the actual gameplay mix.
2. Record original creator/asset name, canonical source URL, source version/download date, exact license, mandatory attribution and any restrictions.
3. Keep a source/processed-file mapping, edit/transformation notes and a checksum of the obtained source.
4. Confirm distribution/redistribution of the processed file within the game is permitted; reject uncertain or non-commercial-only assets.
5. Trim silence, normalize conservatively, avoid clipping, use sensible loudness and apply tiny gameplay variations only where useful.
6. Evaluate how it behaves with Settings volume at 0/50/100, Pause/Resume, rapid firing, repeated spawns and scene changes.
7. Do not commit bulk ZIP libraries, unused alternatives, downloaded sample packs or generated audition experiments. Keep only explicitly approved production files.

Proposed per-clip metadata when selected: `AssetName | SourceURL | Author | License | AcquisitionDate | SHA256 | Changes | AttributionText | ApprovedBy`.

## Existing code and implementation constraints

- `Assets/Scripts/Presentation/RustlineAudioSettings.cs`: independent persistent Music/Sfx gains in PlayerPrefs.
- `Assets/Scripts/Presentation/AudioBusSource.cs`: multiplies a routed AudioSource's authored gain by its selected bus level.
- The frontend pauses ordinary audio via `AudioListener.pause`. Preserve modal behavior and ensure optional UI sounds during pause intentionally opt out only when appropriate.
- Preserve weapon fire-rate gating, all movement/collision contracts, damage, encounter timing and camera behavior. Attach sounds to authoritative events instead of inferring them via polling.
- Design for bounded voices/concurrency: Longwatch automatic fire, rapid Latch projectiles, Bombardier detonations and many concurrent enemies must not stack indefinitely.
- No new Unity scene, prefab, mixer, generated clip or playback class is approved by this *planning* document. Implementation follows asset audition.

## Immediate approval checklist

- [ ] Listen to 4 Kenney source packs and shortlist actual files, not just pack names.
- [ ] Decide whether a particular effect needs a bespoke recording/edited layer versus an existing CC0 sample.
- [ ] Generate and audition a subdued **instrumental** Suno exploration cue; no vocals.
- [ ] Confirm final Suno subscription/download rights and keep provenance.
- [ ] Approve actual import files and target trigger mapping.
- [ ] Only then hand an audio-implementation branch to Codex.

