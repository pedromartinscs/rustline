using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>
    /// Temporary incremental-art harness for the Latch-9. During Editor Play Mode,
    /// it replaces Longwatch presentation with the authored Latch-9 Idle package.
    /// States without Latch-9 art remain unarmed. Nothing is persisted to the scene
    /// or player prefab, and Longwatch gameplay is disabled for this visual preview.
    /// </summary>
    [InitializeOnLoad]
    public static class Latch9IdlePreviewPlayMode
    {
        private const string IdleRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim/Idle";
        private const int CellWidth = 80;
        private const int CellHeight = 96;
        private const int SheetWidth = CellWidth * 2;
        private const float PixelsPerUnit = 16f;

        private static readonly string[] DirectionSuffixes =
        {
            "p90", "p80", "p70", "p60", "p50", "p40", "p30", "p20", "p10", "0",
            "m10", "m20", "m30", "m40", "m50", "m60", "m70", "m80", "m90",
        };

        private static readonly int[] DirectionAngles =
        {
            90, 80, 70, 60, 50, 40, 30, 20, 10, 0,
            -10, -20, -30, -40, -50, -60, -70, -80, -90,
        };

        static Latch9IdlePreviewPlayMode()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            if (EditorApplication.isPlaying)
            {
                EditorApplication.delayCall += InstallPreview;
            }
        }

        private static void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state == PlayModeStateChange.EnteredPlayMode)
            {
                EditorApplication.delayCall += InstallPreview;
            }
        }

        private static void InstallPreview()
        {
            if (!EditorApplication.isPlaying)
            {
                return;
            }

            PlayerLongwatchAimPresenter2D[] longwatchPresenters =
                Object.FindObjectsByType<PlayerLongwatchAimPresenter2D>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

            int installedCount = 0;
            foreach (PlayerLongwatchAimPresenter2D longwatchPresenter in longwatchPresenters)
            {
                if (longwatchPresenter == null || !longwatchPresenter.gameObject.scene.IsValid() ||
                    !longwatchPresenter.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (!TryBuildIdlePoses(out Latch9IdleAimPose[] poses))
                {
                    Debug.LogError("Latch-9 Idle preview was not installed because its 19 authored sheets are incomplete.");
                    return;
                }

                if (longwatchPresenter.BodyIdleFrameCount != 2)
                {
                    Debug.LogError("Latch-9 Idle preview requires the canonical two-frame player Idle body.");
                    return;
                }

                Sprite[] bodyIdleFrames =
                {
                    longwatchPresenter.GetBodyIdleFrame(0),
                    longwatchPresenter.GetBodyIdleFrame(1),
                };

                longwatchPresenter.enabled = false;

                PlayerWeaponController2D weaponController =
                    longwatchPresenter.GetComponent<PlayerWeaponController2D>();
                if (weaponController != null)
                {
                    weaponController.enabled = false;
                }

                PlayerLatch9AimPresenter2D latchPresenter =
                    longwatchPresenter.GetComponent<PlayerLatch9AimPresenter2D>();
                if (latchPresenter == null)
                {
                    latchPresenter = longwatchPresenter.gameObject.AddComponent<PlayerLatch9AimPresenter2D>();
                }

                latchPresenter.Configure(
                    longwatchPresenter.PlayerAim,
                    longwatchPresenter.PlayerAnimator,
                    longwatchPresenter.UnarmedPresenter,
                    longwatchPresenter.BodySpriteRenderer,
                    longwatchPresenter.ArmsWeaponSpriteRenderer,
                    bodyIdleFrames,
                    poses);
                latchPresenter.enabled = true;
                installedCount++;
            }

            if (installedCount > 0)
            {
                Debug.Log(
                    $"Latch-9 Idle preview active on {installedCount} player instance(s): " +
                    "19-direction Idle uses Latch-9; all unsupported states use Unarmed; " +
                    "weapon gameplay is disabled for this presentation-only test.");
            }
        }

        private static bool TryBuildIdlePoses(out Latch9IdleAimPose[] poses)
        {
            poses = new Latch9IdleAimPose[DirectionSuffixes.Length];
            for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
            {
                string suffix = DirectionSuffixes[directionIndex];
                string path = IdleRoot + "/player_salvager_latch_9_idle_aim_" + suffix + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (texture == null || texture.width != SheetWidth || texture.height != CellHeight)
                {
                    Debug.LogError(
                        $"Latch-9 Idle sheet must be exactly {SheetWidth}x{CellHeight}: {path}");
                    poses = null;
                    return false;
                }

                Sprite frame0 = CreatePreviewSprite(texture, 0, suffix);
                Sprite frame1 = CreatePreviewSprite(texture, 1, suffix);
                poses[directionIndex] =
                    new Latch9IdleAimPose(DirectionAngles[directionIndex], frame0, frame1);
            }

            return true;
        }

        private static Sprite CreatePreviewSprite(Texture2D texture, int frameIndex, string suffix)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(frameIndex * CellWidth, 0f, CellWidth, CellHeight),
                new Vector2(24f / CellWidth, 8f / CellHeight),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"latch_9_idle_aim_{suffix}_{frameIndex}_preview";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }
    }
}
