using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Production encounter perception additionally requires the actor to be in view.</summary>
    internal static class EnemyPerception2D
    {
        internal static bool InView(ref Camera camera, Vector2 sensor)
        {
            if (camera == null) camera = Camera.main;
            if (camera == null) return false;
            Vector3 point = camera.WorldToViewportPoint(sensor);
            return point.z > 0f && point.x >= 0f && point.x <= 1f && point.y >= 0f && point.y <= 1f;
        }
    }
}
