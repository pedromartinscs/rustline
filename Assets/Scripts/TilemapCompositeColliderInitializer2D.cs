using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Physics
{
    /// <summary>
    /// Release-critical startup guard for TilemapCollider2D -> CompositeCollider2D geometry.
    ///
    /// Windows Player builds have repeatedly loaded MovementLab with the serialized Composite
    /// geometry empty even though the same scene is correct in Editor Play Mode. Do not replace
    /// this with a one-shot Awake-only call: the native Tilemap/Collider data can become ready
    /// later in the startup lifecycle, especially after the course grows or is rebuilt.
    ///
    /// The accepted contract is:
    /// ProcessTilemapChanges -> GenerateGeometry -> Physics2D.SyncTransforms
    /// in Awake, again in Start, and during the first two FixedUpdate ticks.
    /// See docs/RELEASE_COLLISION.md.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(TilemapCollider2D), typeof(CompositeCollider2D))]
    public sealed class TilemapCompositeColliderInitializer2D : MonoBehaviour
    {
        private const int PostLoadFixedPassCount = 2;

        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _compositeCollider;
        private int _postLoadFixedPassesRemaining;

        public bool HasGeneratedGeometry =>
            _compositeCollider != null &&
            _compositeCollider.pathCount > 0 &&
            _compositeCollider.pointCount > 0;

        private void Awake()
        {
            CacheComponents();
            _postLoadFixedPassesRemaining = PostLoadFixedPassCount;
            EnsureGeometry();
        }

        private void Start()
        {
            // Awake can be too early in Windows Player builds. Repeat after every scene object
            // has completed Awake/OnEnable and immediately before normal frame simulation.
            EnsureGeometry();
        }

        private void FixedUpdate()
        {
            if (_postLoadFixedPassesRemaining <= 0)
            {
                return;
            }

            // DefaultExecutionOrder keeps these defensive passes ahead of the player motor's
            // FixedUpdate. Physics2D then simulates against freshly generated Composite geometry.
            EnsureGeometry();
            _postLoadFixedPassesRemaining--;
        }

        public void EnsureGeometry()
        {
            CacheComponents();

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

        private void CacheComponents()
        {
            if (_tilemapCollider == null)
            {
                _tilemapCollider = GetComponent<TilemapCollider2D>();
            }

            if (_compositeCollider == null)
            {
                _compositeCollider = GetComponent<CompositeCollider2D>();
            }
        }
    }
}
