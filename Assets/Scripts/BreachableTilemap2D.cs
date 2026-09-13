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
    /// Visual destruction happens one 16 px cell at a time. Collision is deliberately retained
    /// until the destroyed run is large enough to be physically traversable: two contiguous cells
    /// for a horizontal floor/ceiling aperture and three contiguous cells for a vertical wall
    /// aperture. Authored cells that start with hidden collision but no structural visual are never
    /// considered breached unless this component created their runtime breach state itself.
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

            BreachAxis axis = ClassifyAxis(hit.Normal, hit.Direction);
            Vector3Int cell = ResolveImpactedCell(hit.Point, hit.Normal, hit.Direction);

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
                structuralVisualTilemap.SetTile(cell, null);
                structuralVisualTilemap.RefreshAllTiles();
            }
            else
            {
                state.Axes |= axis;
            }

            _breachedCells[cell] = state;

            if (TryReleaseQualifiedRun(cell, axis))
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

        private bool TryReleaseQualifiedRun(Vector3Int origin, BreachAxis axis)
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

            if (runLength < threshold)
            {
                return false;
            }

            bool collisionChanged = false;
            cursor = first;
            for (int i = 0; i < runLength; i++, cursor += step)
            {
                BreachState state = _breachedCells[cursor];
                if (!state.CollisionReleased)
                {
                    if (_collisionTilemap.HasTile(cursor))
                    {
                        _collisionTilemap.SetTile(cursor, null);
                        collisionChanged = true;
                    }

                    state.CollisionReleased = true;
                    _breachedCells[cursor] = state;
                }
            }

            return collisionChanged;
        }

        private bool QualifiesForAxis(Vector3Int cell, BreachAxis axis)
        {
            return _breachedCells.TryGetValue(cell, out BreachState state) &&
                (state.Axes & axis) != 0;
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
