using System;
using System.IO;
using System.Linq;
using Rustline.Diagnostics;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Rustline.Editor
{
    /// <summary>Adds production gameplay without rebuilding accepted terrain or art.</summary>
    public static class RustlineFirstPlayableSetup
    {
        public const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string ManagedName = "First Playable - Managed";

        [UnityEditor.MenuItem("Tools/Rustline/First Playable/Apply Gameplay")]
        public static void ApplyGameplay()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RustlineSalvageIntakeExpansionSetup.ValidateScene(scene);
            Transform preserve = Find<Transform>(scene, "Gameplay Content - Preserve");
            PlayerMotor2D player = Find<PlayerMotor2D>(scene);
            if (preserve == null || player == null) throw new InvalidOperationException("Production scene/rig missing.");
            Transform old = preserve.Find(ManagedName);
            if (old != null) UnityEngine.Object.DestroyImmediate(old.gameObject);
            // Production owns retry; the laboratory retains its immediate-reset behavior unchanged.
            MovementLabRespawn diagnostic = player.GetComponent<MovementLabRespawn>();
            if (diagnostic != null) UnityEngine.Object.DestroyImmediate(diagnostic);
            CombatHealth2D playerHealth = player.GetComponent<CombatHealth2D>();
            if (playerHealth == null) playerHealth = player.gameObject.AddComponent<CombatHealth2D>();
            GameObject root = Child(preserve, ManagedName);
            FirstPlayableDirector2D director = root.AddComponent<FirstPlayableDirector2D>();
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (material == null) throw new InvalidOperationException("Existing sprite material missing.");
            var waves = new FirstPlayableEncounter2D[7];
            waves[0] = Encounter(root.transform, "01 West Security", new Rect(-18f, -1f, 16f, 10f));
            waves[1] = Encounter(root.transform, "02 Transfer Bay", new Rect(1f, -1f, 26f, 14f));
            waves[2] = Encounter(root.transform, "03 Exit Defense", new Rect(33.5f, 16f, 13f, 15f));
            SetString(waves[2], "objective", "CLIMB SHAFT / CLEAR UPPER DECK");
            waves[3] = Encounter(root.transform, "04 Processing Gallery", new Rect(52f, 16f, 43f, 16f));
            waves[4] = Encounter(root.transform, "05 Maintenance", new Rect(96f, 16f, 32f, 19f));
            waves[5] = Encounter(root.transform, "06 Rail Battery", new Rect(148f, 28f, 23f, 10f));
            waves[6] = Encounter(root.transform, "07 Terminal Defense", new Rect(172f, 28f, 24f, 10f));
            CombatHealth2D west = Sentry(waves[0].transform, "West Sentry", new Vector2(-10f, 1.02f),
                -11.25f, -8.75f, player, material);
            SetMembers(waves[0], west);
            CombatHealth2D bay = Sentry(waves[1].transform, "Bay Sentry", new Vector2(7f, 0.02f),
                3.5f, 15f, player, material);
            CombatHealth2D bayBomb = Bombardier(waves[1].transform, "Bay Bombardier", new Vector2(13f, 0.02f), 2f, material);
            SetMembers(waves[1], bay, bayBomb);
            CombatHealth2D upper = Sentry(waves[2].transform, "Upper Sentry", new Vector2(36f, 17.02f),
                34.75f, 40f, player, material);
            CombatHealth2D upperBomb = Bombardier(waves[2].transform, "Upper Bombardier", new Vector2(42f, 17.02f), 1f, material);
            SetMembers(waves[2], upper, upperBomb);
            SetMembers(waves[3],
                Sentry(waves[3].transform, "Gallery Sentry", new Vector2(60f,17.02f),56f,64f,player,material),
                Bombardier(waves[3].transform, "Gallery Battery", new Vector2(82f,20.02f),1.5f,material));
            SetMembers(waves[4],
                Sentry(waves[4].transform, "Maintenance Sentry", new Vector2(114f,17.02f),111f,119f,player,material),
                Bombardier(waves[4].transform, "Maintenance Bombardier", new Vector2(118f,17.02f),0.6f,material));
            SetMembers(waves[5],
                Sentry(waves[5].transform, "Rail Sentry", new Vector2(155f,29.02f),150f,157f,player,material),
                Bombardier(waves[5].transform, "Raised Rail Battery", new Vector2(164f,32.02f),2f,material));
            SetMembers(waves[6],
                Sentry(waves[6].transform, "Terminal West Sentry", new Vector2(176f,29.02f),173f,179f,player,material),
                Sentry(waves[6].transform, "Terminal East Sentry", new Vector2(185f,29.02f),183f,186.3f,player,material),
                Bombardier(waves[6].transform, "Terminal Bombardier", new Vector2(188f,29.02f),1f,material));
            var checkpoints = new[]
            {
                Station(root.transform,"SERVICE STATION",new Vector2(22f,1.08f),new Rect(19f,0.8f,4.5f,4f),2,material),
                Station(root.transform,"UPPER RELAY",new Vector2(43f,17.08f),new Rect(40f,16.8f,6f,4f),3,material),
                Station(root.transform,"SERVICE REFUGE",new Vector2(135f,29.08f),new Rect(133f,28.8f,5f,4f),5,material)
            };
            LineRenderer exit = Marker(root.transform, "Extraction Beacon", new Vector2(193f, 29.02f), material, 10);
            Set(director, "player", player);
            Set(director, "playerHealth", playerHealth);
            Set(director, "entrySpawn", Find<Transform>(scene, "Player Spawn"));
            SetArray(director, "checkpoints", checkpoints);
            SetRect(director, "extractionArea", new Rect(191.5f, 28.8f, 3.3f, 3.5f));
            SetArray(director, "encounters", waves);
            FirstPlayableHud2D hud = root.AddComponent<FirstPlayableHud2D>();
            Set(hud, "director", director);
            Set(hud, "uiShader", Shader.Find("UI/Default"));
            Set(hud, "exitMarker", exit);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ValidateScene(scene);
            Debug.Log("RUSTLINE_FIRST_PLAYABLE_SETUP_OK: 7 encounters, 14 defenders, 3 checkpoints, extraction x=193 saved.");
        }

        public static void ValidateScene(Scene scene)
        {
            FirstPlayableDirector2D director = Find<FirstPlayableDirector2D>(scene);
            if (director == null || director.Player == null || director.PlayerHealth == null ||
                director.Encounters == null || director.Encounters.Length == 0 ||
                director.Checkpoints == null || director.Checkpoints.Length == 0 ||
                Find<MovementLabRespawn>(scene) != null)
                throw new InvalidOperationException("Production run references/respawn policy invalid.");
            int count = 0;
            foreach (FirstPlayableEncounter2D encounter in director.Encounters)
            {
                if (encounter == null || encounter.Members == null || encounter.Members.Length == 0)
                    throw new InvalidOperationException("Empty production encounter.");
                foreach (CombatHealth2D health in encounter.Members)
                {
                    WeaponHitbox2D hitbox = health != null ? health.GetComponentInChildren<WeaponHitbox2D>() : null;
                    if (hitbox == null || hitbox.Health != health || hitbox.gameObject.layer != 7)
                        throw new InvalidOperationException("Enemy explicit weapon-hitbox route invalid.");
                    count++;
                }
            }
            if (count == 0) throw new InvalidOperationException("No authored defenders.");
            int previousRequired = 0;
            foreach (FirstPlayableCheckpoint2D checkpoint in director.Checkpoints)
            {
                if (checkpoint == null || checkpoint.Beacon == null || checkpoint.RequiredClears <= previousRequired ||
                    checkpoint.RequiredClears > director.Encounters.Length || !checkpoint.Area.Contains(checkpoint.transform.position))
                    throw new InvalidOperationException("Checkpoint references/order/spawn area invalid.");
                previousRequired = checkpoint.RequiredClears;
            }
            RustlineSalvageIntakeExpansionSetup.ValidateScene(scene);
            RustlineBuildSceneOrder.ValidateOrThrow();
        }

        [UnityEditor.MenuItem("Tools/Rustline/First Playable/Build Windows Release")]
        public static void BuildWindowsRelease()
        {
            ValidateScene(EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single));
            Directory.CreateDirectory("Builds/FirstPlayable");
            string[] sideEffects = { "Assets/Settings/UniversalRP.asset", "Assets/UniversalRenderPipelineGlobalSettings.asset",
                "ProjectSettings/ProjectSettings.asset", "ProjectSettings/UnityConnectSettings.asset" };
            byte[][] originals = sideEffects.Select(File.ReadAllBytes).ToArray();
            BuildReport report;
            try
            {
                report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
                {
                    scenes = EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray(),
                    locationPathName = "Builds/FirstPlayable/Rustline.exe",
                    target = BuildTarget.StandaloneWindows64,
                    options = BuildOptions.None
                });
            }
            finally
            {
                for (int i = 0; i < sideEffects.Length; i++) File.WriteAllBytes(sideEffects[i], originals[i]);
                AssetDatabase.Refresh();
            }
            if (report.summary.result != BuildResult.Succeeded)
                throw new BuildFailedException("First playable Release build failed: " + report.summary.result);
            Debug.Log("RUSTLINE_FIRST_PLAYABLE_RELEASE_OK: Builds/FirstPlayable/Rustline.exe");
        }

        private static FirstPlayableEncounter2D Encounter(Transform parent, string name, Rect area)
        {
            var encounter = Child(parent, name).AddComponent<FirstPlayableEncounter2D>();
            SetRect(encounter, "activationArea", area);
            SetString(encounter, "objective", "CLEAR " + name.Substring(3).ToUpperInvariant());
            return encounter;
        }

        private static FirstPlayableCheckpoint2D Station(Transform parent, string label, Vector2 spawn, Rect area, int clears, Material material)
        {
            var station = Child(parent,label).AddComponent<FirstPlayableCheckpoint2D>();
            station.transform.position = spawn;
            SetString(station,"label",label); SetInt(station,"requiredClears",clears); SetRect(station,"area",area);
            Set(station,"beacon",Marker(station.transform,label + " Beacon",spawn,material,13));
            return station;
        }

        private static CombatHealth2D Actor(Transform parent, string name, Vector2 position, Material material,
            out Rigidbody2D body, out BoxCollider2D collider, out LineRenderer line)
        {
            GameObject actor = Child(parent, name);
            actor.transform.position = position;
            body = actor.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            var health = actor.AddComponent<CombatHealth2D>();
            GameObject hitbox = Child(actor.transform, "Hitbox");
            hitbox.layer = 7;
            hitbox.transform.localPosition = Vector3.up * 1.25f;
            collider = hitbox.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(1.2f, 2.5f);
            collider.isTrigger = true;
            hitbox.AddComponent<WeaponHitbox2D>().Configure(health);
            line = Child(actor.transform, "Temporary Enemy Silhouette").AddComponent<LineRenderer>();
            line.sharedMaterial = material;
            line.useWorldSpace = false;
            line.widthMultiplier = 0.125f;
            line.sortingOrder = 20;
            line.positionCount = 7;
            line.SetPositions(new[] { new Vector3(-0.6f, 0f), new Vector3(-0.6f, 1.8f),
                new Vector3(-0.3f, 2.5f), new Vector3(0.3f, 2.5f), new Vector3(0.6f, 1.8f),
                new Vector3(0.6f, 0f), new Vector3(-0.6f, 0f) });
            return health;
        }

        private static CombatHealth2D Sentry(Transform parent, string name, Vector2 position, float minX,
            float maxX, PlayerMotor2D player, Material material)
        {
            CombatHealth2D health = Actor(parent, name, position, material, out _, out BoxCollider2D collider, out LineRenderer line);
            var actor = health.gameObject.AddComponent<Sentry2D>();
            SetInt(health, "maximumHealth", 80);
            // A lower, angular silhouette distinguishes close combat from the tall thrower.
            line.SetPosition(2, new Vector3(-0.6f, 2.1f));
            line.SetPosition(3, new Vector3(0.6f, 2.1f));
            line.startColor = line.endColor = RustlinePalette.GetColor(10);
            Set(actor, "playerCollider", player.GetComponent<CapsuleCollider2D>());
            Set(actor, "playerHealth", player.GetComponent<CombatHealth2D>());
            Set(actor, "weaponHitCollider", collider);
            Set(actor, "silhouette", line);
            SetFloat(actor, "minimumX", minX);
            SetFloat(actor, "maximumX", maxX);
            return health;
        }

        private static CombatHealth2D Bombardier(Transform parent, string name, Vector2 position, float patrol, Material material)
        {
            CombatHealth2D health = Actor(parent, name, position, material, out Rigidbody2D body,
                out BoxCollider2D collider, out LineRenderer line);
            var actor = health.gameObject.AddComponent<Bombardier2D>();
            Transform origin = Child(health.transform, "Throw Origin").transform;
            origin.localPosition = Vector3.up * 2f;
            var presenter = line.gameObject.AddComponent<BombardierPresenter2D>();
            line.startColor = line.endColor = RustlinePalette.GetColor(19);
            Set(actor, "health", health); Set(actor, "body", body); Set(actor, "weaponHitCollider", collider);
            Set(actor, "throwOrigin", origin); Set(actor, "presenter", presenter);
            SetFloat(actor, "patrolHalfDistance", patrol);
            Set(presenter, "health", health); Set(presenter, "enemy", actor); Set(presenter, "silhouetteRenderer", line);
            return health;
        }

        private static LineRenderer Marker(Transform parent, string name, Vector2 position, Material material, int color)
        {
            var line = Child(parent, name).AddComponent<LineRenderer>();
            line.transform.position = position;
            line.sharedMaterial = material;
            line.useWorldSpace = false; line.loop = true; line.widthMultiplier = 0.125f; line.sortingOrder = 12;
            line.startColor = line.endColor = RustlinePalette.GetColor(color);
            line.positionCount = 4;
            line.SetPositions(new[] { new Vector3(-0.75f, 0f), new Vector3(-0.75f, 3f),
                new Vector3(0.75f, 3f), new Vector3(0.75f, 0f) });
            return line;
        }

        private static GameObject Child(Transform parent, string name)
        {
            var child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }
        private static T Find<T>(Scene scene, string name = null) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true))
                .FirstOrDefault(c => name == null || c.name == name);
        private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetFloat(UnityEngine.Object target, string field, float value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).floatValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetInt(UnityEngine.Object target, string field, int value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetString(UnityEngine.Object target, string field, string value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).stringValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetRect(UnityEngine.Object target, string field, Rect value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).rectValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetArray<T>(UnityEngine.Object target, string field, T[] values) where T : UnityEngine.Object
        {
            var serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            property.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void SetMembers(FirstPlayableEncounter2D encounter, params CombatHealth2D[] health) => SetArray(encounter, "members", health);
    }
}
