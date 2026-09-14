using System.Linq;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Editor
{
    /// <summary>
    /// Temporary incremental-art harness for the Latch-9. During Editor Play Mode,
    /// it replaces Longwatch presentation with authored Latch-9 Idle, Run, and Backpedal packages.
    /// States without Latch-9 art remain unarmed. Nothing is persisted to the scene
    /// or player prefab, and Longwatch gameplay is disabled for this visual preview.
    /// </summary>
    [InitializeOnLoad]
    public static class Latch9IdlePreviewPlayMode
    {
        private const string IdleRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim/Idle";
        private const string RunRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim/Run";
        private const string BackpedalRoot =
            "Assets/Art/Characters/Player/Sprites/Arms/Armed/latch_9/Aim/Backpedal";
        private const string BodyRunPath =
            "Assets/Art/Characters/Player/Sprites/Body/player_salvager_body_run.png";
        private const string BodyBackpedalPath =
            "Assets/Art/Characters/Player/Sprites/Body/player_salvager_body_backpedal.png";
        private const int CellWidth = 80;
        private const int CellHeight = 96;
        private const int IdleFrameCount = 2;
        private const int RunFrameCount = 6;
        private const int BackpedalFrameCount = 4;
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

            if (!TryBuildIdlePoses(out Latch9IdleAimPose[] idlePoses) ||
                !TryBuildRunPoses(out Latch9RunAimPose[] runPoses) ||
                !TryBuildBackpedalPoses(out Latch9BackpedalAimPose[] backpedalPoses) ||
                !TryLoadBodyFrames(BodyRunPath, RunFrameCount, "Run", out Sprite[] bodyRunFrames) ||
                !TryLoadBodyFrames(
                    BodyBackpedalPath,
                    BackpedalFrameCount,
                    "Backpedal",
                    out Sprite[] bodyBackpedalFrames))
            {
                Debug.LogError(
                    "Latch-9 Idle/Run/Backpedal preview was not installed because its authored package is incomplete.");
                return;
            }

            int installedCount = 0;
            foreach (PlayerLongwatchAimPresenter2D longwatchPresenter in longwatchPresenters)
            {
                if (longwatchPresenter == null || !longwatchPresenter.gameObject.scene.IsValid() ||
                    !longwatchPresenter.gameObject.scene.isLoaded)
                {
                    continue;
                }

                if (longwatchPresenter.BodyIdleFrameCount != IdleFrameCount)
                {
                    Debug.LogError("Latch-9 preview requires the canonical two-frame player Idle body.");
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
                    idlePoses,
                    bodyRunFrames,
                    runPoses,
                    bodyBackpedalFrames,
                    backpedalPoses);
                latchPresenter.enabled = true;
                installedCount++;
            }

            if (installedCount > 0)
            {
                Debug.Log(
                    $"Latch-9 Idle/Run/Backpedal preview active on {installedCount} player instance(s): " +
                    "19-direction Idle, Run, and Backpedal use Latch-9; all unsupported states use Unarmed; " +
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
                if (!ValidateSheet(texture, IdleFrameCount, "Idle", path))
                {
                    poses = null;
                    return false;
                }

                Sprite frame0 = CreatePreviewSprite(texture, 0, suffix, "idle");
                Sprite frame1 = CreatePreviewSprite(texture, 1, suffix, "idle");
                poses[directionIndex] =
                    new Latch9IdleAimPose(DirectionAngles[directionIndex], frame0, frame1);
            }

            return true;
        }

        private static bool TryBuildRunPoses(out Latch9RunAimPose[] poses)
        {
            poses = new Latch9RunAimPose[DirectionSuffixes.Length];
            for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
            {
                string suffix = DirectionSuffixes[directionIndex];
                string path = RunRoot + "/player_salvager_latch_9_run_aim_" + suffix + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!ValidateSheet(texture, RunFrameCount, "Run", path))
                {
                    poses = null;
                    return false;
                }

                poses[directionIndex] = new Latch9RunAimPose(
                    DirectionAngles[directionIndex],
                    CreatePreviewSprite(texture, 0, suffix, "run"),
                    CreatePreviewSprite(texture, 1, suffix, "run"),
                    CreatePreviewSprite(texture, 2, suffix, "run"),
                    CreatePreviewSprite(texture, 3, suffix, "run"),
                    CreatePreviewSprite(texture, 4, suffix, "run"),
                    CreatePreviewSprite(texture, 5, suffix, "run"));
            }

            return true;
        }

        private static bool TryBuildBackpedalPoses(out Latch9BackpedalAimPose[] poses)
        {
            poses = new Latch9BackpedalAimPose[DirectionSuffixes.Length];
            for (int directionIndex = 0; directionIndex < DirectionSuffixes.Length; directionIndex++)
            {
                string suffix = DirectionSuffixes[directionIndex];
                string path = BackpedalRoot +
                    "/player_salvager_latch_9_backpedal_aim_" + suffix + ".png";
                Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                if (!ValidateSheet(texture, BackpedalFrameCount, "Backpedal", path))
                {
                    poses = null;
                    return false;
                }

                poses[directionIndex] = new Latch9BackpedalAimPose(
                    DirectionAngles[directionIndex],
                    CreatePreviewSprite(texture, 0, suffix, "backpedal"),
                    CreatePreviewSprite(texture, 1, suffix, "backpedal"),
                    CreatePreviewSprite(texture, 2, suffix, "backpedal"),
                    CreatePreviewSprite(texture, 3, suffix, "backpedal"));
            }

            return true;
        }

        private static bool TryLoadBodyFrames(
            string path,
            int expectedFrameCount,
            string stateName,
            out Sprite[] frames)
        {
            frames = AssetDatabase.LoadAllAssetsAtPath(path)
                .OfType<Sprite>()
                .OrderBy(sprite => ParseTrailingIndex(sprite.name))
                .ToArray();

            if (frames.Length == expectedFrameCount)
            {
                return true;
            }

            Debug.LogError(
                $"Latch-9 {stateName} preview requires exactly {expectedFrameCount} canonical body frames: {path}");
            frames = null;
            return false;
        }

        private static bool ValidateSheet(Texture2D texture, int frameCount, string stateName, string path)
        {
            int expectedWidth = CellWidth * frameCount;
            if (texture != null && texture.width == expectedWidth && texture.height == CellHeight)
            {
                return true;
            }

            Debug.LogError(
                $"Latch-9 {stateName} sheet must be exactly {expectedWidth}x{CellHeight}: {path}");
            return false;
        }

        private static Sprite CreatePreviewSprite(
            Texture2D texture,
            int frameIndex,
            string suffix,
            string stateId)
        {
            Sprite sprite = Sprite.Create(
                texture,
                new Rect(frameIndex * CellWidth, 0f, CellWidth, CellHeight),
                new Vector2(24f / CellWidth, 8f / CellHeight),
                PixelsPerUnit,
                0,
                SpriteMeshType.FullRect);
            sprite.name = $"latch_9_{stateId}_aim_{suffix}_{frameIndex}_preview";
            sprite.hideFlags = HideFlags.HideAndDontSave;
            return sprite;
        }

        private static int ParseTrailingIndex(string spriteName)
        {
            int separatorIndex = spriteName.LastIndexOf('_');
            return separatorIndex >= 0 &&
                   int.TryParse(spriteName.Substring(separatorIndex + 1), out int frameIndex)
                ? frameIndex
                : int.MaxValue;
        }
    }
}
