using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public sealed class FirstPlayableEncounter2D : MonoBehaviour
    {
        [SerializeField] private CombatHealth2D[] members;
        [SerializeField] private Rect activationArea;
        public CombatHealth2D[] Members => members;
        public Rect ActivationArea => activationArea;
        public bool IsActive { get; private set; }
        public bool IsDefeated
        {
            get
            {
                if (!IsActive || members == null || members.Length == 0) return false;
                foreach (CombatHealth2D member in members)
                    if (member == null || member.IsAlive) return false;
                return true;
            }
        }

        public void Activate()
        {
            IsActive = true;
            foreach (CombatHealth2D member in members) member.gameObject.SetActive(true);
        }

        public void ResetEncounter()
        {
            foreach (CombatHealth2D member in members)
            {
                // Actors have all run Awake before the director's first Start.
                if (member.TryGetComponent(out Bombardier2D ranged)) ranged.ResetForEncounter();
                if (member.TryGetComponent(out Sentry2D sentry)) sentry.ResetForEncounter();
                member.gameObject.SetActive(false);
            }
            IsActive = false;
        }

        public void ClearBombs()
        {
            foreach (CombatHealth2D member in members)
                if (member.TryGetComponent(out Bombardier2D ranged)) ranged.ClearActiveBombs();
        }
    }
}
