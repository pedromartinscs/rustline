using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Diagnostics
{
    [DisallowMultipleComponent]
    public sealed class MovementLabBombardierEncounter2D : MonoBehaviour
    {
        public const float DefaultResetDelay = 1.25f;

        [SerializeField] private Bombardier2D enemy;
        [SerializeField, Min(0f)] private float resetDelay = DefaultResetDelay;

        private float _resetTimeRemaining;

        public Bombardier2D Enemy => enemy;
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
            // A death does not recall a launched bomb. Automatic reuse waits for
            // every owned bomb to resolve; explicit encounter reset still clears it.
            if (_resetTimeRemaining <= 0f && (enemy == null || enemy.ActiveBombCount == 0))
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
