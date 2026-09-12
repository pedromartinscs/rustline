# Build Scene Order

Rustline's canonical build-scene order is:

1. `Assets/Scenes/Demo/SalvageIntake.unity` — **Build Index 0 / release entry point**.
2. `Assets/Scenes/MovementLab.unity` — diagnostic movement scene retained in builds.
3. `Assets/Scenes/ArtShowcase.unity` — diagnostic art/presentation scene retained in builds.

Any additional scenes remain after these three in their existing relative order.

`ProjectSettings/EditorBuildSettings.asset` is committed with this ordering so a normal Development or Release build starts directly in `SalvageIntake` without requiring the scene to be open in the Unity Editor.

`Assets/Editor/RustlineBuildSceneOrder.cs` owns the invariant. It listens for Unity build-scene-list changes and restores the canonical leading order if another editor tool changes it. This deliberately protects the release entry point from older deterministic builders that may temporarily insert `SalvageIntake` after the lab scenes.

The helper also exposes:

- **Tools -> Rustline -> Apply Canonical Build Scene Order**
- **Tools -> Rustline -> Validate Canonical Build Scene Order**

Opening a scene in the Editor does not determine which scene a standalone player starts in. Unity starts the player from the first enabled scene in Build Settings, so `SalvageIntake` must remain enabled at Build Index 0 until Rustline gains a dedicated bootstrap/title scene.
