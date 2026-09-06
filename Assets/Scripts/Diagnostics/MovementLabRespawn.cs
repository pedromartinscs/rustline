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
            ConfigureReleaseTilemapCollisionWithoutComposite();
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
        private static void ConfigureReleaseTilemapCollisionWithoutComposite()
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

            int tileCount = tilemap != null ? tilemap.GetUsedTilesCount() : -1;
            int tilemapShapesBefore = tilemapCollider.shapeCount;
            int compositeShapesBefore = composite != null ? composite.shapeCount : -1;

            tilemapCollider.compositeOperation = Collider2D.CompositeOperation.None;
            if (composite != null)
            {
                composite.enabled = false;
            }

            if (tilemapCollider.hasTilemapChanges)
            {
                tilemapCollider.ProcessTilemapChanges();
            }

            Physics2D.SyncTransforms();

            Debug.Log(
                "TEMP RELEASE COLLISION DIAGNOSTIC: CompositeCollider2D bypassed. " +
                $"tiles={tileCount}, tilemapShapesBefore={tilemapShapesBefore}, " +
                $"compositeShapesBefore={compositeShapesBefore}, " +
                $"tilemapShapesAfter={tilemapCollider.shapeCount}, " +
                $"hasPendingTileChanges={tilemapCollider.hasTilemapChanges}.");
        }
#endif
    }
}
