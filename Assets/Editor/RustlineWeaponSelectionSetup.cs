using System.Collections.Generic;
using System.Linq;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>Deterministic production wiring for the Player loadout and Latch-9 package.</summary>
    public static class RustlineWeaponSelectionSetup
    {
        private const string PlayerPrefabPath = "Assets/Prefabs/Player/Player.prefab";
        private const string LatchDefinitionPath = "Assets/Config/Weapons/Latch9.asset";
        private const string LongwatchDefinitionPath = "Assets/Config/Weapons/LongwatchDMR.asset";
        private const string LatchMetadataPath = "Assets/Resources/Generated/Latch9MuzzleMetadata.asset";
        private const string CarouselShaderPath = "Assets/Shaders/RustlineWeaponCarouselFade.shader";
        private const string CarouselRoot = "Assets/Art/UI/WeaponCarousel/";
        private static readonly string[] Directions = { "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10", "0", "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90" };
        private static readonly int[] Angles = { 90,80,70,60,50,40,30,20,10,0,-10,-20,-30,-40,-50,-60,-70,-80,-90 };

        [MenuItem("Tools/Rustline/Weapons/Rebuild Persistent Weapon Selection")]
        public static void RebuildFromMenu() => BuildAndConfigure();
        public static void BuildFromCommandLine() => BuildAndConfigure();

        public static void BuildAndConfigure()
        {
            RustlineM0ArtSetup.ConfigureLatch9Sheets();
            RustlineLatch9MuzzleSetup.BuildAndValidate();
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Configure(root);
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        private static void Configure(GameObject root)
        {
            PlayerWeaponController2D controller = root.GetComponent<PlayerWeaponController2D>();
            PlayerInputReader input = root.GetComponent<PlayerInputReader>();
            PlayerAim2D aim = root.GetComponent<PlayerAim2D>();
            PlayerAnimator2D animator = root.GetComponent<PlayerAnimator2D>();
            PlayerUnarmedArmsPresenter2D unarmed = root.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerLongwatchAimPresenter2D longwatch = root.GetComponent<PlayerLongwatchAimPresenter2D>();
            Require(controller != null && input != null && aim != null && animator != null && unarmed != null && longwatch != null,
                "Player prefab is missing its accepted weapon/presentation baseline.");
            SpriteRenderer body = longwatch.BodySpriteRenderer;
            SpriteRenderer arms = longwatch.ArmsWeaponSpriteRenderer;
            PlayerLatch9AimPresenter2D latch = GetOrAdd<PlayerLatch9AimPresenter2D>(root);
            ConfigureLatchPresenter(latch, aim, animator, unarmed, body, arms, longwatch);

            Latch9MuzzleMetadata2D metadata = AssetDatabase.LoadAssetAtPath<Latch9MuzzleMetadata2D>(LatchMetadataPath);
            WeaponDefinition2D latchDefinition = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LatchDefinitionPath);
            WeaponDefinition2D longwatchDefinition = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(LongwatchDefinitionPath);
            Require(metadata != null && latchDefinition != null && longwatchDefinition != null, "Persistent weapon assets are missing.");
            Latch9ProjectileEmitter2D emitter = GetOrAdd<Latch9ProjectileEmitter2D>(root);
            emitter.Configure(controller, latch, metadata);

            Transform flashTransform = arms.transform.Find("LongwatchMuzzleFlash");
            Require(flashTransform != null, "Player prefab Longwatch flash child is missing.");
            SpriteRenderer flashRenderer = flashTransform.GetComponent<SpriteRenderer>();
            Latch9MuzzleFlashPresenter2D latchFlash = GetOrAdd<Latch9MuzzleFlashPresenter2D>(flashTransform.gameObject);
            ConfigureLatchFlash(latchFlash, controller, latch, metadata, flashRenderer);
            Latch9MuzzleFlashShotModeBinder2D binder = GetOrAdd<Latch9MuzzleFlashShotModeBinder2D>(flashTransform.gameObject);
            binder.Configure(controller, latchFlash);

            LongwatchRecoilPresenter2D recoil = root.GetComponent<LongwatchRecoilPresenter2D>();
            LongwatchMuzzleFlashPresenter2D longwatchFlash = flashTransform.GetComponent<LongwatchMuzzleFlashPresenter2D>();
            PlayerWeaponEquipment2D equipment = GetOrAdd<PlayerWeaponEquipment2D>(root);
            ConfigureEquipment(equipment, input, controller, latchDefinition, longwatchDefinition, longwatch, recoil, longwatchFlash, latch, emitter, latchFlash, binder);
            WeaponCarouselHud2D hud = GetOrAdd<WeaponCarouselHud2D>(root);
            Set(hud, "equipment", equipment);
            Set(hud, "paletteFadeShader", AssetDatabase.LoadAssetAtPath<Shader>(CarouselShaderPath));
        }

        private static void ConfigureLatchPresenter(PlayerLatch9AimPresenter2D presenter, PlayerAim2D aim, PlayerAnimator2D animator, PlayerUnarmedArmsPresenter2D unarmed, SpriteRenderer body, SpriteRenderer arms, PlayerLongwatchAimPresenter2D longwatch)
        {
            SerializedObject serialized = new SerializedObject(presenter);
            SetRef(serialized, "playerAim", aim); SetRef(serialized, "playerAnimator", animator); SetRef(serialized, "unarmedPresenter", unarmed); SetRef(serialized, "bodySpriteRenderer", body); SetRef(serialized, "armsWeaponSpriteRenderer", arms);
            SetSprites(serialized.FindProperty("bodyIdleFrames"), new[] { longwatch.GetBodyIdleFrame(0), longwatch.GetBodyIdleFrame(1) });
            SetSprites(serialized.FindProperty("bodyRunFrames"), LoadBody("run", 6));
            SetSprites(serialized.FindProperty("bodyBackpedalFrames"), LoadBody("backpedal", 4));
            SetSprites(serialized.FindProperty("bodyCrouchFrames"), LoadBody("crouch", 6));
            SetSprites(serialized.FindProperty("bodyFallFrames"), LoadBody("fall", 1));
            SetSprites(serialized.FindProperty("bodyJumpFrames"), LoadBody("jump", 3));
            SetSprites(serialized.FindProperty("bodyLandFrames"), LoadBody("land", 2));
            SetSprites(serialized.FindProperty("jumpCarryFrames"), LoadLatchCarry("Jump", 3));
            SetSprites(serialized.FindProperty("landCarryFrames"), LoadLatchCarry("Land", 2));
            SetPoses(serialized.FindProperty("idleAimPoses"), "idle", 2);
            SetPoses(serialized.FindProperty("runAimPoses"), "run", 6);
            SetPoses(serialized.FindProperty("backpedalAimPoses"), "backpedal", 4);
            SetPoses(serialized.FindProperty("crouchAimPoses"), "crouch", 6);
            SetPoses(serialized.FindProperty("fallAimPoses"), "fall", 1);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureLatchFlash(Latch9MuzzleFlashPresenter2D flash, PlayerWeaponController2D controller, PlayerLatch9AimPresenter2D latch, Latch9MuzzleMetadata2D metadata, SpriteRenderer renderer)
        {
            SerializedObject serialized = new SerializedObject(flash);
            SetRef(serialized, "weaponController", controller); SetRef(serialized, "latchPresenter", latch); SetRef(serialized, "metadata", metadata); SetRef(serialized, "flashRenderer", renderer);
            SetSprites(serialized.FindProperty("conventionalFrames"), LoadSprites("Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash.png"));
            SetSprites(serialized.FindProperty("bouncingFrames"), LoadSprites("Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash_bouncing.png"));
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureEquipment(PlayerWeaponEquipment2D equipment, PlayerInputReader input, PlayerWeaponController2D controller, WeaponDefinition2D latchDefinition, WeaponDefinition2D longwatchDefinition, PlayerLongwatchAimPresenter2D longwatch, LongwatchRecoilPresenter2D recoil, LongwatchMuzzleFlashPresenter2D longwatchFlash, PlayerLatch9AimPresenter2D latch, Latch9ProjectileEmitter2D emitter, Latch9MuzzleFlashPresenter2D latchFlash, Latch9MuzzleFlashShotModeBinder2D binder)
        {
            SerializedObject serialized = new SerializedObject(equipment);
            SetRef(serialized, "input", input); SetRef(serialized, "weaponController", controller);
            SerializedProperty entries = serialized.FindProperty("loadout"); entries.arraySize = 3;
            SetEntry(entries.GetArrayElementAtIndex(0), 0, LoadCard("unarmed"), null, null);
            SetEntry(entries.GetArrayElementAtIndex(1), 1, LoadCard("latch_9"), latchDefinition, new Behaviour[] { latch, emitter, latchFlash, binder });
            SetEntry(entries.GetArrayElementAtIndex(2), 2, LoadCard("longwatch_dmr"), longwatchDefinition, new Behaviour[] { longwatch, recoil, longwatchFlash });
            serialized.FindProperty("initialSlot").intValue = 2;
            serialized.FindProperty("carouselStepDuration").floatValue = 0.14f;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetEntry(SerializedProperty entry, int slot, Sprite card, WeaponDefinition2D definition, Behaviour[] components)
        {
            entry.FindPropertyRelative("slot").intValue = slot; entry.FindPropertyRelative("available").boolValue = true; entry.FindPropertyRelative("carouselSprite").objectReferenceValue = card; entry.FindPropertyRelative("weaponDefinition").objectReferenceValue = definition;
            SerializedProperty presentation = entry.FindPropertyRelative("activePresentationComponents"); presentation.arraySize = components == null ? 0 : components.Length;
            for (int index = 0; components != null && index < components.Length; index++) presentation.GetArrayElementAtIndex(index).objectReferenceValue = components[index];
        }

        private static void SetPoses(SerializedProperty poses, string state, int frameCount)
        {
            poses.arraySize = Directions.Length;
            for (int direction = 0; direction < Directions.Length; direction++)
            {
                SerializedProperty pose = poses.GetArrayElementAtIndex(direction); pose.FindPropertyRelative("angleDegrees").intValue = Angles[direction];
                List<Sprite> frames = LoadSprites(LatchAimPath(state, Directions[direction]));
                Require(frames.Count == frameCount, "Latch-9 frame count mismatch: " + state + "/" + Directions[direction]);
                for (int frame = 0; frame < frameCount; frame++) pose.FindPropertyRelative("frame" + frame).objectReferenceValue = frames[frame];
            }
        }

        private static string LatchAimPath(string state, string direction) => "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim/" + char.ToUpper(state[0]) + state.Substring(1) + "/player_salvager_latch_9_" + state + "_aim_" + direction + ".png";
        private static List<Sprite> LoadBody(string state, int expected) { List<Sprite> result = LoadSprites("Assets/Art/Characters/Player/Sprites/Body/player_salvager_body_" + state + ".png"); Require(result.Count == expected, "Body frame count mismatch: " + state); return result; }
        private static List<Sprite> LoadLatchCarry(string state, int expected) { List<Sprite> result = LoadSprites("Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Carry/" + state + "/player_salvager_latch_9_" + state.ToLowerInvariant() + "_carry.png"); Require(result.Count == expected, "Latch carry frame count mismatch: " + state); return result; }
        private static Sprite LoadCard(string id) => LoadSprites(CarouselRoot + "weapon_carousel_" + id + ".png").Single();
        private static List<Sprite> LoadSprites(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.rect.x).ToList();
        private static T GetOrAdd<T>(GameObject objectToConfigure) where T : Component => objectToConfigure.GetComponent<T>() ?? objectToConfigure.AddComponent<T>();
        private static void Set(Behaviour target, string name, UnityEngine.Object value) { SerializedObject serialized = new SerializedObject(target); SetRef(serialized, name, value); serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void SetRef(SerializedObject serialized, string name, UnityEngine.Object value) { SerializedProperty property = serialized.FindProperty(name); Require(property != null, "Missing property " + name); property.objectReferenceValue = value; }
        private static void SetSprites(SerializedProperty target, IList<Sprite> sprites) { target.arraySize = sprites.Count; for (int index = 0; index < sprites.Count; index++) target.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index]; }
        private static void Require(bool condition, string message) { if (!condition) throw new System.InvalidOperationException(message); }
    }
}
