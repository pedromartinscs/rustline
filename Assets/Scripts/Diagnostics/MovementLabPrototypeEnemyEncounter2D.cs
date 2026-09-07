using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class MovementLabPrototypeEnemyEncounter2D : MonoBehaviour
    {
        public const float DefaultResetDelay = 1.25f;

        [SerializeField] private PrototypeGroundEnemy2D enemy;
        [SerializeField, Min(0f)] private float resetDelay = DefaultResetDelay;

        private float _resetTimeRemaining;

        public PrototypeGroundEnemy2D Enemy => enemy;
        public float ResetDelay => resetDelay;
        public float ResetTimeRemaining => _resetTimeRemaining;
        public bool IsResetPending { get; private set; }
        public int DeathCount { get; private set; }
        public int ResetCount { get; private set; }

        private void OnEnable()
        {
            if (enemy != null)
            {
                enemy.Died += OnEnemyDied;
            }
        }

        private void OnDisable()
        {
            if (enemy != null)
            {
                enemy.Died -= OnEnemyDied;
            }
        }

        private void Update()
        {
            if (!IsResetPending)
            {
                return;
            }

            _resetTimeRemaining -= Time.deltaTime;
            if (_resetTimeRemaining <= 0f)
            {
                ResetNow();
            }
        }

        public void ResetNow()
        {
            IsResetPending = false;
            _resetTimeRemaining = 0f;
            if (enemy == null)
            {
                return;
            }

            enemy.ResetForEncounter();
            ResetCount++;
        }

        private void OnEnemyDied()
        {
            DeathCount++;
            IsResetPending = true;
            _resetTimeRemaining = resetDelay;
        }
    }
}
