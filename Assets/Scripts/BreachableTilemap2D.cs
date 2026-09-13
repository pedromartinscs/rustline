using System;
using System.Collections.Generic;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Gameplay.Environment
{
    /// <summary>
    /// Runtime authority for breaching the modular industrial structural Tilemap.
    ///
    /// A valid hit removes the impacted structural visual immediately. The matching hidden
    /// collision normally disappears with it. Collision is retained only as a temporary movement
    /// safety seal when the destroyed cells form a genuinely bounded aperture: fewer than two
    /// contiguous cells across a horizontal floor/ceiling opening, or fewer than three contiguous
    /// cells across a vertical wall opening. Corners, ledges, steps and other openings that are not
    /// bounded by solid collision at both ends can never leave ghost collision behind.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap), typeof(TilemapCollider2D), typeof(CompositeCollider2D))]
    public sealed class BreachableTilemap2D : MonoBehaviour, IWeaponHitReceiver2D
    {
        private const float InwardSampleDistance = 1f / 32f;
        private const int HorizontalReleaseThreshold = 2;
        private const int VerticalReleaseThreshold = 3;

        [Flags]
        private enum BreachAxis
        {
            None = 0,
            Horizontal = 1 << 0,
            Vertical = 1 << 1,
        }

        private struct BreachState
        {
            internal BreachState(BreachAxis axes)
            {
                Axes = axes;
                CollisionReleased = false;
            }

            internal BreachAxis Axes;
            internal bool CollisionReleased;
        }

        [SerializeField] private Tilemap structuralVisualTilemap;
        [SerializeField] private WeaponDefinition2D[] breachingWeapons = Array.Empty<WeaponDefinition2D>();

        private readonly Dictionary<Vector3Int, BreachState> _breachedCells =
            new Dictionary<Vector3Int, BreachState>();
        private readonly List<Vector3Int> _reconcileBuffer = new List<Vector3Int>();

        private Tilemap _collisionTilemap;
        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _compositeCollider;

        public Tilemap StructuralVisualTilemap => structuralVisualTilemap;
        public int BreachingWeaponCount => breachingWeapons?.Length ?? 0;

        private void Awake()
        {
            CacheRequiredComponents();
        }

        public void Configure(Tilemap visualTilemap, params WeaponDefinition2D[] allowedWeapons)
        {
            structuralVisualTilemap = visualTilemap;
            breachingWeapons = allowedWeapons ?? Array.Empty<WeaponDefinition2D>();
        }

        public bool AllowsWeapon(WeaponDefinition2D weapon)
        {
            if (weapon == null || breachingWeapons == null)
            {
                return false;
            }

            for (int i = 0; i < breachingWeapons.Length; i++)
            {
                if (breachingWeapons[i] == weapon)
                {
                    return true;
                }
            }

            return false;
        }

        public void ReceiveHit(in WeaponHitInfo2D hit)
        {
            if (!AllowsWeapon(hit.Weapon) || structuralVisualTilemap == null)
            {
                return;
            }

            CacheRequiredComponents();
            if (_collisionTilemap == null || _tilemapCollider == null || _compositeCollider == null)
            {
                return;
            }

            Vector3Int cell = ResolveImpactedCell(hit.Point, hit.Normal, hit.Direction);
            BreachAxis axis = ClassifyAxis(hit.Normal, hit.Direction);

            bool hadRuntimeState = _breachedCells.TryGetValue(cell, out BreachState state);
            if (!hadRuntimeState)
            {
                // Initial eligibility is intentionally strict. Both the visible structural tile and
                // its hidden collision must exist. This excludes authored production architecture
                // whose collision remains after its generic RuleTile skin was deliberately removed.
                if (!structuralVisualTilemap.HasTile(cell) || !_collisionTilemap.HasTile(cell))
                {
                    return;
                }

                state = new BreachState(axis);
                _breachedCells.Add(cell, state);
                structuralVisualTilemap.SetTile(cell, null);
                structuralVisualTilemap.RefreshAllTiles();
            }
            else
            {
                state.Axes |= axis;
                _breachedCells[cell] = state;
            }

            // Reconcile every pending breach, not only the latest one. Destroying a neighboring
            // cell can turn an earlier bounded micro-aperture into an exposed edge, in which case
            // its retained collision must disappear immediately as well.
            if (ReconcilePendingCollision())
            {
                RebuildCollisionGeometry();
            }
        }

        private Vector3Int ResolveImpactedCell(Vector2 point, Vector2 normal, Vector2 direction)
        {
            Vector2 inward;
            if (normal.sqrMagnitude > 0.000001f)
            {
                inward = -normal.normalized;
            }
            else if (direction.sqrMagnitude > 0.000001f)
            {
                inward = direction.normalized;
            }
            else
            {
                inward = Vector2.zero;
            }

            return _collisionTilemap.WorldToCell(point + inward * InwardSampleDistance);
        }

        private static BreachAxis ClassifyAxis(Vector2 normal, Vector2 direction)
        {
            Vector2 basis = normal.sqrMagnitude > 0.000001f ? normal : -direction;
            return Mathf.Abs(basis.y) >= Mathf.Abs(basis.x)
                ? BreachAxis.Horizontal
                : BreachAxis.Vertical;
        }

        private bool ReconcilePendingCollision()
        {
            _reconcileBuffer.Clear();
            foreach (KeyValuePair<Vector3Int, BreachState> pair in _breachedCells)
            {
                if (!pair.Value.CollisionReleased)
                {
                    _reconcileBuffer.Add(pair.Key);
                }
            }

            bool collisionChanged = false;
            for (int i = 0; i < _reconcileBuffer.Count; i++)
            {
                Vector3Int cell = _reconcileBuffer[i];
                BreachState state = _breachedCells[cell];

                if (ShouldRetainSafetyCollision(cell, state))
                {
                    continue;
                }

                if (_collisionTilemap.HasTile(cell))
                {
                    _collisionTilemap.SetTile(cell, null);
                    collisionChanged = true;
                }

                state.CollisionReleased = true;
                _breachedCells[cell] = state;
            }

            return collisionChanged;
        }

        private bool ShouldRetainSafetyCollision(Vector3Int cell, BreachState state)
        {
            if ((state.Axes & BreachAxis.Horizontal) != 0 &&
                IsBoundedNarrowRun(cell, BreachAxis.Horizontal))
            {
                return true;
            }

            if ((state.Axes & BreachAxis.Vertical) != 0 &&
                IsBoundedNarrowRun(cell, BreachAxis.Vertical))
            {
                return true;
            }

            return false;
        }

        private bool IsBoundedNarrowRun(Vector3Int origin, BreachAxis axis)
        {
            Vector3Int step = axis == BreachAxis.Horizontal ? Vector3Int.right : Vector3Int.up;
            int threshold = axis == BreachAxis.Horizontal
                ? HorizontalReleaseThreshold
                : VerticalReleaseThreshold;

            Vector3Int first = origin;
            while (QualifiesForAxis(first - step, axis))
            {
                first -= step;
            }

            int runLength = 0;
            Vector3Int cursor = first;
            while (QualifiesForAxis(cursor, axis))
            {
                runLength++;
                cursor += step;
            }

            if (runLength >= threshold)
            {
                return false;
            }

            Vector3Int before = first - step;
            Vector3Int after = cursor;
            return IsSolidBoundary(before) && IsSolidBoundary(after);
        }

        private bool QualifiesForAxis(Vector3Int cell, BreachAxis axis)
        {
            return _breachedCells.TryGetValue(cell, out BreachState state) &&
                (state.Axes & axis) != 0;
        }

        private bool IsSolidBoundary(Vector3Int cell)
        {
            // A breached cell is never allowed to serve as an aperture support, even if its
            // temporary collision has not yet been reconciled away. Collision-only production
            // architecture, however, is a valid physical boundary and may safely bound a seal.
            return !_breachedCells.ContainsKey(cell) && _collisionTilemap.HasTile(cell);
        }

        private void CacheRequiredComponents()
        {
            if (_collisionTilemap == null)
            {
                _collisionTilemap = GetComponent<Tilemap>();
            }

            if (_tilemapCollider == null)
            {
                _tilemapCollider = GetComponent<TilemapCollider2D>();
            }

            if (_compositeCollider == null)
            {
                _compositeCollider = GetComponent<CompositeCollider2D>();
            }
        }

        private void RebuildCollisionGeometry()
        {
            _collisionTilemap.RefreshAllTiles();
            _tilemapCollider.ProcessTilemapChanges();
            _compositeCollider.GenerateGeometry();
            Physics2D.SyncTransforms();
        }
    }
}
