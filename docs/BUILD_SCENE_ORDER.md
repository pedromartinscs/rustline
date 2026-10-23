# Build Scene Order

Rustline's canonical build-scene order is:

1. `Assets/Scenes/UI/MainMenu.unity` — **Build Index 0 / release entry point**.
2. `Assets/Scenes/Demo/SalvageIntake.unity` — **Build Index 1 / playable run**.
3. `Assets/Scenes/MovementLab.unity` — **Build Index 2 / movement diagnostic**.
4. `Assets/Scenes/ArtShowcase.unity` — **Build Index 3 / art diagnostic**.

Any additional scenes remain after these four in their existing relative order, including SampleScene.

`ProjectSettings/EditorBuildSettings.asset` is committed with this ordering so normal Development and Release builds start in `MainMenu`. Start Game loads a fresh `SalvageIntake` scene. Opening SalvageIntake directly in the Editor still supports immediate gameplay and pause.

`Assets/Editor/RustlineBuildSceneOrder.cs` owns the invariant. It listens for Unity build-scene-list changes and restores the canonical leading order if another editor tool changes it. This deliberately protects the release entry point from older deterministic builders that may temporarily insert `SalvageIntake` after the lab scenes.

The helper also exposes:

- **Tools -> Rustline -> Apply Canonical Build Scene Order**
- **Tools -> Rustline -> Validate Canonical Build Scene Order**

Opening a scene in the Editor does not determine standalone startup. Unity starts the player from the first enabled scene. The opt-in `--first-playable-smoke` explicitly loads the playable scene by path before resolving the director and running every existing assertion; ordinary startup retains the title.

`RunWithFoundationValidationOrder` temporarily exposes MovementLab(0)/ArtShowcase(1) only to the frozen foundation validator. Its finally block restores the four-scene production order even after an exception. Production setup validates the saved MainMenu and gameplay build references. The per-scene Release collision guard still bakes and validates the actual SalvageIntake and MovementLab export instances.

See [frontend-menu-system.md](frontend-menu-system.md) for architecture, authoring and validation.
