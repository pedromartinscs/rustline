using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Physics
{
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TilemapCollider2D), typeof(CompositeCollider2D))]
    public sealed class TilemapCompositeColliderInitializer2D : MonoBehaviour
    {
        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _compositeCollider;

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
