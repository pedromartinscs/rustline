using Rustline.Gameplay.Combat;
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
        private CombatHealth2D _health;
        private MovementLabBombardierEncounter2D _encounter;
        private bool _respawning;

        public Transform SpawnPoint => spawnPoint;
        public int RespawnCount { get; private set; }

        private void Awake()
        {
            _body = GetComponent<Rigidbody2D>();
            _motor = GetComponent<PlayerMotor2D>();
            _weapon = GetComponent<PlayerWeaponController2D>();
            _health = GetComponent<CombatHealth2D>();
            _encounter = FindAnyObjectByType<MovementLabBombardierEncounter2D>();
        }

        private void OnEnable()
        {
            if (_health == null) _health = GetComponent<CombatHealth2D>();
            if (_health != null) _health.Died += OnCombatDeath;
        }

        private void OnDisable()
        {
            if (_health != null) _health.Died -= OnCombatDeath;
        }

        private void FixedUpdate()
        {
            if (spawnPoint == null || transform.position.y >= failureHeight)
            {
                return;
            }

            RespawnNow();
        }

        public void RespawnNow()
        {
            if (_respawning || spawnPoint == null) return;
            _respawning = true;
            _encounter?.ResetNow();
            _body.position = spawnPoint.position;
            _body.rotation = 0f;
            _body.linearVelocity = Vector2.zero;
            _body.angularVelocity = 0f;
            _motor.ResetAfterRespawn();
            _weapon?.ResetTransientState();
            _health?.ResetHealth();
            Physics2D.SyncTransforms();
            RespawnCount++;
            _respawning = false;
        }

        private void OnCombatDeath(DamageResult2D result) => RespawnNow();
    }
}
