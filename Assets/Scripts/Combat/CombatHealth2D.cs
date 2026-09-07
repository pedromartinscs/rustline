using System;
using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    [DisallowMultipleComponent]
    public sealed class CombatHealth2D : MonoBehaviour
    {
        [SerializeField, Min(1)] private int maximumHealth = 100;

        private int _currentHealth;
        private bool _initialized;
        private bool _deathEmitted;

        public event Action<DamageResult2D> Damaged;
        public event Action<DamageResult2D> Died;
        public event Action HealthReset;

        public int MaximumHealth => Mathf.Max(1, maximumHealth);
        public int CurrentHealth
        {
            get
            {
                EnsureInitialized();
                return _currentHealth;
            }
        }

        public bool IsAlive => CurrentHealth > 0;
        public bool IsDead => CurrentHealth == 0;
        public DamageResult2D LastDamageResult { get; private set; }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1, maximumHealth);
        }

        public DamageResult2D ApplyDamage(in DamageInfo2D info)
        {
            EnsureInitialized();
            if (info.Amount <= 0 || _currentHealth <= 0)
            {
                return new DamageResult2D(in info, _currentHealth, _currentHealth, 0, false);
            }

            int previousHealth = _currentHealth;
            _currentHealth = Mathf.Max(0, _currentHealth - info.Amount);
            int appliedAmount = previousHealth - _currentHealth;
            bool killed = _currentHealth == 0;
            var result = new DamageResult2D(
                in info,
                previousHealth,
                _currentHealth,
                appliedAmount,
                killed);
            LastDamageResult = result;
            Damaged?.Invoke(result);

            if (killed && !_deathEmitted)
            {
                _deathEmitted = true;
                Died?.Invoke(result);
            }

            return result;
        }

        public void ResetHealth()
        {
            _initialized = true;
            _currentHealth = MaximumHealth;
            _deathEmitted = false;
            LastDamageResult = default;
            HealthReset?.Invoke();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }

            _initialized = true;
            _currentHealth = MaximumHealth;
            _deathEmitted = false;
        }
    }
}
