using System.Linq;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    [InitializeOnLoad]
    public static class Latch9GameplayPreviewPlayMode
    {
        private const string DefinitionPath = "Assets/Config/Weapons/Latch9.asset";
        private const string MetadataPath = "Assets/Resources/Generated/Latch9MuzzleMetadata.asset";
        private const string ConventionalFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash.png";
        private const string BouncingFlashPath =
            "Assets/Art/Effects/Weapons/latch_9/latch_9_muzzle_flash_bouncing.png";
        private static int _remainingInstallAttempts;

        static Latch9GameplayPreviewPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
            {
                QueueInstall();
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.ExitingEditMode)
            {
                RustlineLatch9MuzzleSetup.BuildAndValidate();
            }
            else if (state == PlayModeStateChange.EnteredPlayMode)
            {
                QueueInstall();
            }
        }

        private static void QueueInstall()
        {
            _remainingInstallAttempts = 4;
            EditorApplication.delayCall += Install;
        }

        private static void Install()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            PlayerLatch9AimPresenter2D[] latchPresenters =
                Object.FindObjectsByType<PlayerLatch9AimPresenter2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);
            if (latchPresenters.Length == 0 && --_remainingInstallAttempts > 0)
            {
                EditorApplication.delayCall += Install;
                return;
            }

            WeaponDefinition2D definition =
                AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(DefinitionPath);
            Latch9MuzzleMetadata2D metadata =
                AssetDatabase.LoadAssetAtPath<Latch9MuzzleMetadata2D>(MetadataPath);
            Sprite[] conventional = LoadFlashSprites(ConventionalFlashPath);
            Sprite[] bouncing = LoadFlashSprites(BouncingFlashPath);
            if (definition == null || metadata == null ||
                conventional.Length != Latch9MuzzleFlashPresenter2D.RequiredSpriteCount ||
                bouncing.Length != Latch9MuzzleFlashPresenter2D.RequiredSpriteCount)
            {
                Debug.LogError("Latch-9 gameplay preview could not load its weapon definition, metadata, or muzzle-flash sprites.");
                return;
            }

            int installed = 0;
            foreach (PlayerLatch9AimPresenter2D latchPresenter in latchPresenters)
            {
                PlayerWeaponController2D controller =
                    latchPresenter.GetComponent<PlayerWeaponController2D>();
                if (controller == null)
                {
                    continue;
                }

                LongwatchMuzzleFlashPresenter2D longwatchFlash =
                    latchPresenter.GetComponentInChildren<LongwatchMuzzleFlashPresenter2D>(true);
                GameObject flashObject;
                SpriteRenderer flashRenderer;
                if (longwatchFlash != null)
                {
                    flashObject = longwatchFlash.gameObject;
                    flashRenderer = longwatchFlash.FlashRenderer;
                    longwatchFlash.enabled = false;
                }
                else
                {
                    flashObject = new GameObject("Latch-9 Muzzle Flash");
                    flashObject.transform.SetParent(latchPresenter.transform, false);
                    flashRenderer = flashObject.AddComponent<SpriteRenderer>();
                    SpriteRenderer arms = latchPresenter.ArmsWeaponSpriteRenderer;
                    if (arms != null)
                    {
                        flashRenderer.sortingLayerID = arms.sortingLayerID;
                        flashRenderer.sortingOrder = arms.sortingOrder + 1;
                    }
                }

                foreach (LongwatchRecoilPresenter2D recoil in
                         latchPresenter.GetComponentsInChildren<LongwatchRecoilPresenter2D>(true))
                {
                    recoil.enabled = false;
                }

                Latch9MuzzleFlashPresenter2D muzzleFlash =
                    flashObject.GetComponent<Latch9MuzzleFlashPresenter2D>();
                if (muzzleFlash == null)
                {
                    muzzleFlash = flashObject.AddComponent<Latch9MuzzleFlashPresenter2D>();
                }

                SerializedObject serialized = new SerializedObject(muzzleFlash);
                serialized.FindProperty("weaponController").objectReferenceValue = controller;
                serialized.FindProperty("latchPresenter").objectReferenceValue = latchPresenter;
                serialized.FindProperty("metadata").objectReferenceValue = metadata;
                serialized.FindProperty("flashRenderer").objectReferenceValue = flashRenderer;
                AssignSprites(serialized.FindProperty("conventionalFrames"), conventional);
                AssignSprites(serialized.FindProperty("bouncingFrames"), bouncing);
                serialized.FindProperty("profile").enumValueIndex = 0;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                muzzleFlash.enabled = true;

                Latch9MuzzleFlashShotModeBinder2D binder =
                    flashObject.GetComponent<Latch9MuzzleFlashShotModeBinder2D>();
                if (binder == null)
                {
                    binder = flashObject.AddComponent<Latch9MuzzleFlashShotModeBinder2D>();
                }
                binder.Configure(controller, muzzleFlash);

                controller.EquipWeapon(definition);
                controller.enabled = true;
                installed++;
            }

            if (installed > 0)
            {
                Debug.Log(
                    $"Latch-9 gameplay active on {installed} player instance(s): left click fires; " +
                    "right click toggles Conventional/Bouncing; bouncing damage is 4/3/2/1 across up to three ricochets.");
            }
        }

        private static Sprite[] LoadFlashSprites(string path)
        {
            return AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => sprite.rect.x)
                .ToArray();
        }

        private static void AssignSprites(SerializedProperty target, Sprite[] sprites)
        {
            target.arraySize = sprites.Length;
            for (int index = 0; index < sprites.Length; index++)
            {
                target.GetArrayElementAtIndex(index).objectReferenceValue = sprites[index];
            }
        }
    }
}
