using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public sealed class FirstPlayableEncounter2D : MonoBehaviour
    {
        [SerializeField] private CombatHealth2D[] members;
        [SerializeField] private Rect activationArea;
        [SerializeField] private string objective;
        private Sentry2D[] _sentries;
        private Bombardier2D[] _ranged;
        public CombatHealth2D[] Members => members;
        public Rect ActivationArea => activationArea;
        public string Objective => objective;
        public bool IsActive { get; private set; }
        public bool IsDefeated
        {
            get
            {
                if (members == null || members.Length == 0) return false;
                foreach (CombatHealth2D member in members)
                    if (member == null || member.IsAlive) return false;
                return true;
            }
        }

        private void Awake()
        {
            _sentries = new Sentry2D[members.Length];
            _ranged = new Bombardier2D[members.Length];
            for (int i = 0; i < members.Length; i++)
            {
                members[i].TryGetComponent(out _sentries[i]);
                members[i].TryGetComponent(out _ranged[i]);
                if (_sentries[i] != null) _sentries[i].RequireVisibleDetection = true;
                if (_ranged[i] != null) _ranged[i].RequireVisibleDetection = true;
            }
        }

        private void OnEnable()
        {
            foreach (CombatHealth2D member in members) member.Damaged += OnDamaged;
            foreach (Sentry2D sentry in _sentries) if (sentry != null) sentry.PlayerDetected += Activate;
            foreach (Bombardier2D ranged in _ranged) if (ranged != null) ranged.PlayerDetected += Activate;
        }

        private void OnDisable()
        {
            foreach (CombatHealth2D member in members)
                if (member != null) member.Damaged -= OnDamaged;
            foreach (Sentry2D sentry in _sentries) if (sentry != null) sentry.PlayerDetected -= Activate;
            foreach (Bombardier2D ranged in _ranged) if (ranged != null) ranged.PlayerDetected -= Activate;
        }

        private void OnDamaged(DamageResult2D result)
        {
            if (result.DidApply && !IsActive) Activate();
        }

        public void Activate()
        {
            IsActive = true;
            for (int i = 0; i < members.Length; i++)
            {
                SetCombatActive(i, members[i].IsAlive);
                _sentries[i]?.SetCombatEngaged(members[i].IsAlive);
                _ranged[i]?.SetCombatEngaged(members[i].IsAlive);
            }
        }

        public void ResetEncounter()
        {
            for (int i = 0; i < members.Length; i++)
            {
                _ranged[i]?.ResetForEncounter();
                _sentries[i]?.ResetForEncounter();
                SetCombatActive(i, true);
                _sentries[i]?.SetCombatEngaged(false);
                _ranged[i]?.SetCombatEngaged(false);
            }
            IsActive = false;
        }

        public void ClearBombs()
        {
            foreach (Bombardier2D ranged in _ranged) ranged?.ClearActiveBombs();
        }

        private void SetCombatActive(int index, bool active)
        {
            _sentries[index]?.SetCombatActive(active);
            _ranged[index]?.SetCombatActive(active);
        }
    }
}
