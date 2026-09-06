using UnityEngine;

namespace Rustline.Gameplay.Player
{
    [RequireComponent(typeof(CapsuleCollider2D), typeof(PlayerGroundProbe2D))]
    public sealed class PlayerEnvironmentProbe2D : MonoBehaviour
    {
        [SerializeField] private PlayerMovementConfig config;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];
        private CapsuleCollider2D _collider;
        private PlayerGroundProbe2D _groundProbe;
        private ContactFilter2D _filter;

        private void Awake()
        {
            _collider = GetComponent<CapsuleCollider2D>();
            _groundProbe = GetComponent<PlayerGroundProbe2D>();
            RebuildFilter();
        }

        private void OnValidate()
        {
            if (_groundProbe == null)
            {
                _groundProbe = GetComponent<PlayerGroundProbe2D>();
            }

            RebuildFilter();
        }

        public bool HasStandingClearance()
        {
            if (_collider == null || config == null || _collider.size.y >= config.StandingColliderSize.y)
            {
                return true;
            }

            float expansionDistance = config.StandingColliderSize.y - _collider.size.y;
            int hitCount = _collider.Cast(Vector2.up, _filter, _hits, expansionDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && hit.normal.y < 0f)
                {
                    return false;
                }
            }

            return true;
        }

        public int FindWallSide(float horizontalInput)
        {
            if (_collider == null || config == null || Mathf.Abs(horizontalInput) < config.InputDeadZone)
            {
                return 0;
            }

            int side = horizontalInput < 0f ? -1 : 1;
            int hitCount = _collider.Cast(Vector2.right * side, _filter, _hits, config.WallCheckDistance);
            for (int index = 0; index < hitCount; index++)
            {
                RaycastHit2D hit = _hits[index];
                if (hit.collider != null && -hit.normal.x * side >= config.MinimumWallNormalX)
                {
                    return side;
                }
            }

            return 0;
        }

        private void RebuildFilter()
        {
            _filter = new ContactFilter2D
            {
                useLayerMask = true,
                layerMask = _groundProbe != null ? _groundProbe.GroundLayers : 0,
                useTriggers = false,
            };
        }
    }
}
