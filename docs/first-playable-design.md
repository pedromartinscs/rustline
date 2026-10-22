# Salvage Intake — first playable design

Use the existing industrial room and accepted geometry. This phase delivers a compact combat/traversal loop; **8–12 minutes is a future playtest target, not a measured claim**.

| Beat | Space | Gameplay goal |
| --- | --- | --- |
| Arrival | West bulkhead, x=-20 | Safe introduction; show movement, aiming, weapon selection and ammo controls. |
| Security approach | Service step, x=-12..-8 | One close-range sentry; visible alert and committed strike teach spacing/backpedal. |
| Transfer bay | Floor below catwalk, x=4..18 | Sentry plus Bombardier; use plinth/catwalk, crouch, aim and movement to avoid locked bomb targets. Clear both enemies to proceed. |
| Service station | East staging, x=19..23 | Crossing the lit station after clearing the bay records a checkpoint, restores health and replenishes magazines. Earlier kills remain cleared on retry. |
| Shaft | x=28..31, four-unit opening | Existing wall brace, alternating wall kicks and ledge climb lead to y=17. No movement/collision retuning. |
| Exit defense | Upper deck, x=34..45 | Sentry and Bombardier combine pressure in a shorter engagement. Defeat both. |
| Extraction | x=42, y=17 | Remain in the marked zone for two seconds after all encounters are clear. Show explicit success, elapsed time and retry count; offer full restart. |

Enemies activate as the player reaches each encounter, remain defeated during that run, and never automatically resurrect. Bay and final stages require defeating every member. Leaving extraction or taking damage resets its hold timer. Victory disables combat/movement so the conclusion is stable.

Death/falling presents a brief failure state and automatically restores the same player at the latest checkpoint. Before the station, all encounters reset; after it, only the final defense resets. Retry clears owned bombs, player projectiles, motor/weapon transients, restores health and full demo magazines. Full restart clears checkpoint, kills, time and retry count. No inventory, persistent save or new lore is introduced.

Sentry states: Patrol → Alert → Pursuit → Windup → Recovery, plus Dead. Ground occlusion and range bound detection; chase stays on the authored patrol deck and turns at walls/edges. Windup locks facing; strike rechecks reach, height, player life and Ground LOS exactly once. Recovery creates a readable counterattack window. Nonlethal hits pause activity briefly; death cancels pending strikes. Bombardier retains its existing behavior and telegraphs.

Presentation: reuse Canonical 28, original environment/player art, existing weapon effects and pixel font. Temporary sentry silhouettes differ from Bombardiers; windup flashes warning amber, hits flash white, death leaves a brief collapse. Health, current objective, checkpoint/retry and victory feedback must remain legible alongside the existing weapon HUD. No external assets or audio are added.

Acceptance gates:

- Saved SalvageIntake loads directly as build index zero with exactly one player and the authored encounters.
- Both existing weapons damage both archetypes through `WeaponHitbox2D`; enemy attacks damage player through `CombatHealth2D`.
- Detection/strike/death/reset policies and progression/checkpoint/completion conditions have automated coverage; scene integration is exercised in PlayMode.
- Death before/after checkpoint restores the intended encounter state, health, ammunition and spawn; full restart is reusable.
- Completion requires all encounters plus physical arrival and uninterrupted extraction hold; it cannot trigger from the floor below the exit.
- Existing suites, serialized references and Windows Release collision need explicit validation. Automated teleports/damage do not prove human traversal, combat feel or 60 FPS.
