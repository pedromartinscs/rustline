using Unity.Profiling;
using UnityEngine;

namespace Rustline.Gameplay.Player
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class PlayerGroundProbe2D : MonoBehaviour
    {
        private static readonly ProfilerMarker GroundProbeMarker =
            new ProfilerMarker("Rustline.Player.GroundProbe");

        [SerializeField] private PlayerMovementConfig config;
        [SerializeField] private LayerMask groundLayers;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private Collider2D _collider;
        private ContactFilter2D _filter;

        public LayerMask GroundLayers => groundLayers;

        private void Awake()
        {
            _collider = GetComponent<Collider2D>();
            RebuildFilter();
        }

        private void OnValidate()
        {
            RebuildFilter();
        }

        public bool CheckGrounded(float verticalVelocity)
        {
            using (GroundProbeMarker.Auto())
            {
                if (_collider == null || config == null || verticalVelocity > config.MaximumGroundingUpwardSpeed)
                {
                    return false;
                }

                int hitCount = _collider.Cast(Vector2.down, _filter, _hits, config.GroundCheckDistance);
                for (int index = 0; index < hitCount; index++)
                {
                    RaycastHit2D hit = _hits[index];
                    if (hit.collider != null && hit.normal.y >= config.MinimumGroundNormalY)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        private void RebuildFilter()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = groundLayers,
                useTriggers = false,
            };
        }
    }
}
