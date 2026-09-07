using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    public readonly struct DamageInfo2D
    {
        public DamageInfo2D(int amount, Vector2 point, Vector2 direction, Object source)
        {
            Amount = amount;
            Point = point;
            Direction = direction;
            Source = source;
        }

        public int Amount { get; }
        public Vector2 Point { get; }
        public Vector2 Direction { get; }
        public Object Source { get; }
    }

    public readonly struct DamageResult2D
    {
        public DamageResult2D(
            in DamageInfo2D info,
            int previousHealth,
            int currentHealth,
            int appliedAmount,
            bool killed)
        {
            Info = info;
            PreviousHealth = previousHealth;
            CurrentHealth = currentHealth;
            AppliedAmount = appliedAmount;
            Killed = killed;
        }

        public DamageInfo2D Info { get; }
        public int PreviousHealth { get; }
        public int CurrentHealth { get; }
        public int AppliedAmount { get; }
        public bool DidApply => AppliedAmount > 0;
        public bool Killed { get; }
    }
}
