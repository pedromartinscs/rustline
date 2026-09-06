using Rustline.Gameplay.Player;
using UnityEngine;

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
            CreateReleaseCollisionControl();
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
        private void CreateReleaseCollisionControl()
        {
            if (spawnPoint == null)
            {
                Debug.LogWarning(
                    "TEMP RELEASE COLLISION DIAGNOSTIC: spawn point is missing; control collider was not created.");
                return;
            }

            const float controlWidth = 6f;
            const float controlHeight = 0.5f;
            const float spawnClearanceAbovePlatform = 0.08f;

            GameObject control = new GameObject("TEMP - Release Collision Control");
            int groundLayer = LayerMask.NameToLayer("Ground");
            if (groundLayer >= 0)
            {
                control.layer = groundLayer;
            }

            control.transform.position = new Vector3(
                spawnPoint.position.x,
                spawnPoint.position.y - spawnClearanceAbovePlatform - (controlHeight * 0.5f),
                0f);

            BoxCollider2D collider = control.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(controlWidth, controlHeight);

            Debug.Log(
                $"TEMP RELEASE COLLISION DIAGNOSTIC: created {controlWidth}x{controlHeight} Ground BoxCollider2D " +
                $"at {control.transform.position}. Walk beyond x={spawnPoint.position.x + controlWidth * 0.5f:F2} " +
                "to leave the control collider.");
        }
#endif
    }
}
