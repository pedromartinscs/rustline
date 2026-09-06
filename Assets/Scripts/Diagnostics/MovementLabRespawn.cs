using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
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
        private PlayerWeaponController2D _weapon;

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _motor = GetComponent<PlayerMotor2D>();
            _weapon = GetComponent<PlayerWeaponController2D>();
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
            _weapon?.ResetTransientState();
        }
    }
}
