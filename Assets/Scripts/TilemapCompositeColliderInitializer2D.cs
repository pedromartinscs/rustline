using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Physics
{
    /// <summary>
    /// Runtime safety net for the release-critical TilemapCollider2D -> CompositeCollider2D path.
    /// The original Windows Release fix that was human-verified in commit 047c49e is intentionally
    /// preserved: process pending Tilemap collider work, regenerate the Composite, then sync 2D
    /// transforms from Awake. Build-time baking/validation is handled separately in the Editor.
    /// See docs/RELEASE_COLLISION.md before changing this component.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TilemapCollider2D), typeof(CompositeCollider2D))]
    public sealed class TilemapCompositeColliderInitializer2D : MonoBehaviour
    {
        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _compositeCollider;

        public bool HasGeneratedGeometry =>
            _compositeCollider != null &&
            _compositeCollider.pathCount > 0 &&
            _compositeCollider.pointCount > 0;

        private void Awake()
        {
            _tilemapCollider = GetComponent<TilemapCollider2D>();
            _compositeCollider = GetComponent<CompositeCollider2D>();
            EnsureGeometry();
        }

        public void EnsureGeometry()
        {
            if (_tilemapCollider == null)
            {
                _tilemapCollider = GetComponent<TilemapCollider2D>();
            }

            if (_compositeCollider == null)
            {
                _compositeCollider = GetComponent<CompositeCollider2D>();
            }

            if (_tilemapCollider == null ||
                _compositeCollider == null ||
                !_tilemapCollider.enabled ||
                !_compositeCollider.enabled ||
                _tilemapCollider.compositeOperation == Collider2D.CompositeOperation.None)
            {
                return;
            }

            _tilemapCollider.ProcessTilemapChanges();
            _compositeCollider.GenerateGeometry();
            Physics2D.SyncTransforms();
        }
    }
}
