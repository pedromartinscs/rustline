using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Authored station data; its Transform is the supported retry spawn.</summary>
    public sealed class FirstPlayableCheckpoint2D : MonoBehaviour
    {
        [SerializeField] private string label;
        [SerializeField, Min(1)] private int requiredClears;
        [SerializeField] private Rect area;
        [SerializeField] private LineRenderer beacon;
        public string Label => label;
        public int RequiredClears => requiredClears;
        public Rect Area => area;
        public LineRenderer Beacon => beacon;
    }
}
