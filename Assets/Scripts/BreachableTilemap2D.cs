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
    /// A valid hit always removes both the impacted structural visual and its matching Tilemap
    /// collision cell. If the resulting opening is a genuinely bounded micro-aperture below the
    /// safe traversal threshold, movement safety is restored with a separate temporary BoxCollider2D
    /// marked as weapon-raycast passthrough. This keeps player traversal protection independent from
    /// ballistic occlusion: corners, ledges and valid open breaches never retain hidden Tilemap
    /// collision, while narrow enclosed apertures can still support the player without stopping fire.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Tilemap), typeof(TilemapCollider2D), typeof(CompositeCollider2D))]
    public sealed class BreachableTilemap2D : MonoBehaviour, IWeaponHitReceiver2D
    {
        private const float InwardSampleDistance = 1f / 32f;
        private const int HorizontalReleaseThreshold = 2;
        private const int VerticalReleaseThreshold = 3;
        private const string SafetySealRootName = "Breach Safety Seals - Runtime";

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
            }

            internal BreachAxis Axes;
        }

        [SerializeField] private Tilemap structuralVisualTilemap;
        [SerializeField] private WeaponDefinition2D[] breachingWeapons = Array.Empty<WeaponDefinition2D>();

        private readonly Dictionary<Vector3Int, BreachState> _breachedCells =
            new Dictionary<Vector3Int, BreachState>();
        private readonly Dictionary<Vector3Int, GameObject> _safetySeals =
            new Dictionary<Vector3Int, GameObject>();
        private readonly List<Vector3Int> _reconcileBuffer = new List<Vector3Int>();

        private Tilemap _collisionTilemap;
        private TilemapCollider2D _tilemapCollider;
        private CompositeCollider2D _compositeCollider;
        private Transform _safetySealRoot;

        public Tilemap StructuralVisualTilemap => structuralVisualTilemap;
        public int BreachingWeaponCount => breachingWeapons?.Length ?? 0;

        private void Awake()
        {
            CacheRequiredComponents();
        }

        private void OnDestroy()
        {
            if (_safetySealRoot != null)
            {
                Destroy(_safetySealRoot.gameObject);
            }
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

                // Tilemap collision and structural presentation always agree about destruction.
                // Any traversal-only protection is represented separately by a safety seal collider.
                structuralVisualTilemap.SetTile(cell, null);
                _collisionTilemap.SetTile(cell, null);
                structuralVisualTilemap.RefreshAllTiles();
                _collisionTilemap.RefreshAllTiles();
            }
            else
            {
                state.Axes |= axis;
                _breachedCells[cell] = state;
            }

            ReconcileSafetySeals();
            RebuildCollisionGeometry();
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

        private void ReconcileSafetySeals()
        {
            _reconcileBuffer.Clear();
            foreach (Vector3Int cell in _breachedCells.Keys)
            {
                _reconcileBuffer.Add(cell);
            }

            for (int i = 0; i < _reconcileBuffer.Count; i++)
            {
                Vector3Int cell = _reconcileBuffer[i];
                BreachState state = _breachedCells[cell];
                bool shouldSeal = ShouldRetainSafetyCollision(cell, state);
                bool hasSeal = _safetySeals.TryGetValue(cell, out GameObject seal) && seal != null;

                if (shouldSeal)
                {
                    if (!hasSeal)
                    {
                        CreateSafetySeal(cell);
                    }
                }
                else if (hasSeal)
                {
                    RemoveSafetySeal(cell, seal);
                }
            }
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
            // A breached cell never serves as an aperture support. Safety seals are intentionally
            // absent from this test because they protect traversal only; they are not structure.
            return !_breachedCells.ContainsKey(cell) && _collisionTilemap.HasTile(cell);
        }

        private void CreateSafetySeal(Vector3Int cell)
        {
            Transform root = GetOrCreateSafetySealRoot();
            var seal = new GameObject($"Safety Seal [{cell.x},{cell.y}]");
            seal.layer = _collisionTilemap.gameObject.layer;
            seal.transform.SetParent(root, true);
            seal.transform.position = _collisionTilemap.GetCellCenterWorld(cell);
            seal.transform.rotation = _collisionTilemap.transform.rotation;
            seal.transform.localScale = Vector3.one;

            Vector3 cellSize = _collisionTilemap.layoutGrid.cellSize;
            BoxCollider2D collider = seal.AddComponent<BoxCollider2D>();
            collider.isTrigger = false;
            collider.size = new Vector2(Mathf.Abs(cellSize.x), Mathf.Abs(cellSize.y));
            seal.AddComponent<WeaponRaycastPassthrough2D>();

            _safetySeals[cell] = seal;
        }

        private void RemoveSafetySeal(Vector3Int cell, GameObject seal)
        {
            _safetySeals.Remove(cell);
            if (seal == null)
            {
                return;
            }

            // Disable immediately so the current frame cannot observe stale movement collision;
            // Destroy then cleans up the runtime-only object normally at end of frame.
            seal.SetActive(false);
            Destroy(seal);
        }

        private Transform GetOrCreateSafetySealRoot()
        {
            if (_safetySealRoot != null)
            {
                return _safetySealRoot;
            }

            var root = new GameObject(SafetySealRootName);
            root.layer = _collisionTilemap.gameObject.layer;

            Transform gridParent = _collisionTilemap.transform.parent;
            if (gridParent != null)
            {
                root.transform.SetParent(gridParent, false);
            }

            _safetySealRoot = root.transform;
            return _safetySealRoot;
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
