using Rustline.Gameplay.Player;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace Rustline.Diagnostics
{
    [RequireComponent(typeof(Rigidbody2D), typeof(PlayerMotor2D))]
    public sealed class MovementLabRespawn : MonoBehaviour
    {
        [SerializeField] private Transform spawnPoint;
        [SerializeField] private float failureHeight = -12f;

        private Rigidbody2D _body;
        private PlayerMotor2D _motor;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _motor = GetComponent<PlayerMotor2D>();

#if !UNITY_EDITOR
            ForceReleaseCompositeGeometry();
#endif
        }

        private void FixedUpdate()
        {
            if (spawnPoint == null || transform.position.y >= failureHeight)
            {
                return;
            }

            _body.position = spawnPoint.position;
            _body.rotation = 0f;
            _motor.ResetAfterRespawn();
        }

#if !UNITY_EDITOR
        private static void ForceReleaseCompositeGeometry()
        {
            TilemapCollider2D tilemapCollider = Object.FindAnyObjectByType<TilemapCollider2D>();
            if (tilemapCollider == null)
            {
                Debug.LogWarning(
                    "TEMP RELEASE COLLISION DIAGNOSTIC: no TilemapCollider2D was found.");
                return;
            }

            Tilemap tilemap = tilemapCollider.GetComponent<Tilemap>();
            CompositeCollider2D composite = tilemapCollider.GetComponent<CompositeCollider2D>();
            if (composite == null)
            {
                Debug.LogWarning(
                    "TEMP RELEASE COLLISION DIAGNOSTIC: TilemapCollider2D has no CompositeCollider2D.");
                return;
            }

            int tileCount = tilemap != null ? tilemap.GetUsedTilesCount() : -1;
            int tilemapShapesBefore = tilemapCollider.shapeCount;
            int compositeShapesBefore = composite.shapeCount;
            int compositePathsBefore = composite.pathCount;
            int compositePointsBefore = composite.pointCount;
            bool pendingBefore = tilemapCollider.hasTilemapChanges;

            composite.enabled = true;
            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

            tilemapCollider.ProcessTilemapChanges();
            composite.GenerateGeometry();
            Physics2D.SyncTransforms();

            Debug.Log(
                "TEMP RELEASE COLLISION DIAGNOSTIC: forced composite geometry. " +
                $"tiles={tileCount}, pendingBefore={pendingBefore}, " +
                $"tilemapShapesBefore={tilemapShapesBefore}, tilemapShapesAfter={tilemapCollider.shapeCount}, " +
                $"compositeShapesBefore={compositeShapesBefore}, compositeShapesAfter={composite.shapeCount}, " +
                $"compositePathsBefore={compositePathsBefore}, compositePathsAfter={composite.pathCount}, " +
                $"compositePointsBefore={compositePointsBefore}, compositePointsAfter={composite.pointCount}, " +
                $"pendingAfter={tilemapCollider.hasTilemapChanges}.");
        }
#endif
    }
}
