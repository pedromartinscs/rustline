# M6 frontend menu system

## Architecture audit and decision

Baseline: merged main c4a9416 (M5.1), branch feat/m6-frontend-menus. Audited AGENTS.md, PALETTE, ART_DIRECTION, PRESENTATION, BUILD_SCENE_ORDER, FIRST_PLAYABLE, RELEASE_COLLISION, bitmap font, both HUDs, native viewport/compositor, run director, scene-order helper, production/gameplay builders, release smoke, Player Input System actions and existing art/test harnesses before implementation.

The world is rendered at up to 1072x1072 logical pixels by NativePixelPresentation through Renderer2D, with a utility-camera RenderGraph penumbra and point presentation pass. WeaponCarouselHud2D owns the compositor HUD texture. FirstPlayableHud2D already uses bitmap meshes on a screen overlay Canvas at order 1000. The frontend follows that overlay precedent at a higher order, constraining its root to NativePixelViewportMath's centered output rectangle and scaling only by the same integer factor. It does not replace the compositor, claim the carousel texture, or alter world visibility/collision.

A saved MainMenu scene contains the existing native presentation rig and reused original industrial environment art. A scene-local frontend controller also installs on direct SalvageIntake loads through a sceneLoaded hook. There are no persistent run objects: Start and Quit load Single scenes, so director/checkpoint/enemy state belongs to the scene and a new run is fresh. Only volume preferences are persistent. An explicit state machine distinguishes Title, TitleOptions, Credits, Playing, Paused, PauseOptions and Transition.

Pause owns Time.timeScale and AudioListener.pause only while modal; previous values are restored on resume/teardown, with normal time on scene transitions. Input consumption is suppressed by the scene-local controller, with a release barrier to prevent held submit/mouse input becoming gameplay actions. Direct director restart keys are gated while modal. Existing gameplay clocks remain scaled; UI uses Input System device edges and unscaled time.

The release scene-order helper remains the sole order authority. Its frozen foundation-validation compatibility bridge is retained. The existing opt-in first-playable smoke explicitly loads SalvageIntake from the title before resolving gameplay references, preserving all assertions. ReleaseCollisionBuildGuard continues processing every actual exported gameplay scene.

Volume architecture: independent centralized Music/SFX preferences multiply each AudioBusSource's authored gain. This avoids an unused mixer dependency and supplies an explicit future playback route without modifying clips. No production clips or soundtrack are introduced.

## Scenes and authoring

The saved `Assets/Scenes/UI/MainMenu.unity` is directly loadable. It reuses the original far industrial silhouette, salvage handler, gantry upright and overhead rail at 16 PPU and unit scale, with the existing world/utility cameras and native compositor. Penumbra is disabled for title scenery only. The accepted playable scene, world shader and collision components are unchanged.

Build order: MainMenu(0), SalvageIntake(1), MovementLab(2), ArtShowcase(3), then additional existing scenes. `RustlineBuildSceneOrder` enforces it on scene-list changes and Editor initialization. The foundation bridge retains its historical temporary lab-first view and finally restoration. `RustlineFrontendSetup.RebuildMainMenu` authors only the title; production setup validates its saved references and the normal Windows Release builder validates both title and gameplay. Title validation uses a temporary preview scene when necessary, preserving the active authoring scene and unsaved content. Production setup leaves the gameplay scene open for development after validation.

## Interface lifecycle and input

`FrontendStateMachine` owns legal navigation; `FrontendController` owns scene loading, pause and device input. A runtime scene-loaded hook installs one controller for direct gameplay entry, while MainMenu contains a saved controller. Single-mode asynchronous scene loading destroys the old run and UI. A transition guard ignores repeated Start/Quit activation. No checkpoints, encounters, ammunition or completed-run state survive leaving the run.

| Context | Escape / Back |
| --- | --- |
| Playing | Paused |
| Paused | Playing |
| Pause Options | Paused |
| Title Options / Credits | Title |
| Title / Transition | No action |

Mouse hover and keyboard/gamepad navigation share one selected index. Up/Down or W/S selects; Enter/Space or gamepad South activates; Left/Right or A/D adjusts a selected slider by 5. Gamepad D-pad/stick navigation and Start/East Back are supported. Held directions repeat using unscaled time. Mouse sliders continuously map their track to 0–100, including exact endpoints. Mouse activation requires matching press/release controls. There is no EventSystem or second UI input module.

PlayerInputReader owns a runtime instance of the existing action definitions, reuses it across enable/disable within a run, and destroys it with the player. The imported asset remains definition data. This narrow compatibility change prevents a scene from inheriting stale enabled-map state across title/diagnostic transitions and Input System test-runtime restoration; bindings and movement/combat policy are unchanged.

Pause uses Unity's automatic scaled physics/time: movement, AI, bombs, Latch projectiles, retry/extraction and elapsed clocks stop. PlayerInputReader discards consumed edges and held state while modal; aim refresh is also suppressed, so menu pointer motion cannot turn the character. The director ignores restart keys and equipment/weapon Update do not consume modal input. Resume preserves the run and waits for menu-held keys/buttons to be released, plus one frame, before allowing world input. Run clocks resume immediately with simulation, independently of this input-release barrier. Programmatic RestartRun closes pause/options before resetting the run. Scene destruction restores owned global pause values; title/new-run loads normalize time and listener pause. A missing gameplay AudioListener is installed on the player, enabling future audio without changing the authored gameplay scene.

## Pixel presentation

`FrontendView` draws one Canvas mesh at sorting order 2000 above FirstPlayableHud2D (1000). The logical root sits exactly within the compositor's centered native output; its transform uses only the viewport integer scale. Deep Space surround is preserved. `FrontendLayout` uses integral dimensions with compact layouts for smaller viewports; reference layouts include 800x600, 1280x720, 1920x1080, 320x180 and narrow portrait windows. Extremely small windows still require enough physical pixels to display text; 320x180 is the visually inspected compact reference.

The existing 5x7 bitmap font supplies every menu glyph and numeric value. Its RGB becomes a binary mask, with point filtering and no mipmaps. The dedicated `Rustline/Frontend Pixel Mask` shader alpha-clips that mask and writes opaque canonical vertex colors with explicit sRGB conversion. This avoids the old HUD's near-white font tint multiplication. Panels are opaque Canonical 28 steel/navy/rust; cyan selection, hazard numeric values and pressed offsets remain integral. No alpha dimmer, smooth font, fractional sprite scale, antialiasing or compositor modification is used. Mesh/material/atlas resources and callbacks are destroyed with the scene. The only idle animation is an unscaled two-frame status lamp. Credits strings are centralized in FrontendView and use the verified repository author handle and technologies.

## Volume storage and routing

`RustlineAudioSettings` stores independent integer strings under `Rustline.Audio.Music` and `Rustline.Audio.Sfx` in Unity PlayerPrefs. Missing, wrong-type, malformed, out-of-range or non-integer preferences default safely to 100. Setters clamp to 0–100 and update routed sources immediately. Preferences are flushed on navigation, transition, application pause/quit and controller teardown; reopening Options or reloading a scene does not reset them.

For future playback, add `AudioBusSource` beside an AudioSource and select Music or Sfx. Its serialized `authoredGain` is multiplied by the bus gain; runtime callers can use `Configure(bus, gain)`. Clip, spatial settings and playback remain owned by AudioSource. Gain 0 is exact mute, without logarithm edge cases. Runtime sources subscribe only while enabled. AudioListener.pause freezes ordinary playback during pause; a future UI-only source can explicitly set ignoreListenerPause if needed. No production clips exist and no soundtrack is claimed; actual music/SFX listening remains pending.

## Validation and remaining work

Baseline EditMode: 377/377 passed on merged main. Final EditMode: 397/397 passed. This includes legal/invalid transitions, repeated navigation, malformed preferences, glyph coverage, integral layout bounds (including 128x128 and portrait), saved order, foundation bridge restoration after failure, and title validation preserving unsaved authoring content.

The new PlayMode fixture covers real scene round trips, repeated Start suppression, independent routed AudioSources, settings reload/scene persistence, real physics/AI/bomb/Latch projectile/timer pause, retry/extraction/restart lifecycle, input suppression and actual rendered palette/glyph/focus/mouse behavior. Its cleanup unloads scene-owned action maps before restoring InputTestFixture's temporary runtime. The minimized menus -> identity-lighting diagnostic -> original MovementLab input regression failed with shared imported action state, then passed 8/8 with per-reader runtime action ownership. Earlier exploratory failing runs are retained in Logs; they are not acceptance results.

Rendered frontend evidence is exported to `Logs/FrontendRenders/frontend-<width>x<height>-<page>.png` (pages 0 title, 1 options, 2 credits). Tests assert that every rendered pixel is Canonical 28, every UI vertex is integral and the mask is point sampled. Standard references, 320x180 and 128x128 are covered. These images verify UI rendering independently of the world; standalone composition and human artistic acceptance are separate gates.

Reproduce suites with Unity 6000.4.0f1 using `-batchmode -projectPath <repo> -runTests -testPlatform EditMode` or `PlayMode`, plus `-testResults <xml> -logFile <log>`; retain graphics support. Build with `-executeMethod Rustline.Editor.RustlineFirstPlayableSetup.BuildWindowsRelease -quit`. With all project Editors closed, launch the executable normally for title/menu interaction, and separately with `-batchmode --first-playable-smoke --smoke-auto-quit -screen-width 1280 -screen-height 720 -screen-fullscreen 0` to repeat the existing gameplay smoke.

Human checks still required: approve title/background composition and text readability at the target displays; play a real run through repeated pause/options/resume, death/retry and extraction; confirm hover/pressed feel, slider behavior and fresh Start after abandoning a checkpoint; check sustained standalone performance. No human acceptance is claimed. Production clips do not exist, so real music/SFX listening remains pending. Full geometry/art regeneration is outside this UI change; its bridge/order and saved-scene validation paths are covered without rewriting approved terrain/art.
