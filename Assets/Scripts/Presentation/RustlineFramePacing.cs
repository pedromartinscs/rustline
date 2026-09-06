using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>
    /// Global runtime frame-pacing policy. Rustline intentionally caps rendering at 60 FPS
    /// to preserve CPU/GPU headroom and stable pacing; slower hardware may naturally run below it.
    /// </summary>
    public static class RustlineFramePacing
    {
        public const int MaximumFrameRate = 60;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void ApplyRuntimePolicy()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = MaximumFrameRate;
        }
    }
}
