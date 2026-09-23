using System;
using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    /// <summary>Player-owned magazine state, retained while equipment and modes change.</summary>
    [DisallowMultipleComponent]
    public sealed class PlayerWeaponAmmo2D : MonoBehaviour
    {
        public readonly struct Snapshot
        {
            public readonly WeaponAmmoPolicy2D Policy;
            public readonly int Rounds;
            public readonly int Capacity;
            public readonly int ReserveMagazines;

            public Snapshot(WeaponAmmoPolicy2D policy, int rounds, int capacity, int reserveMagazines)
            {
                Policy = policy;
                Rounds = rounds;
                Capacity = capacity;
                ReserveMagazines = reserveMagazines;
            }
        }

        private struct MagazineState
        {
            internal int Rounds;
            internal int ReserveMagazines;
        }

        private readonly Dictionary<WeaponDefinition2D, MagazineState> _magazines =
            new Dictionary<WeaponDefinition2D, MagazineState>();

        public event Action<WeaponDefinition2D> AmmoChanged;

        public Snapshot GetSnapshot(WeaponDefinition2D definition)
        {
            if (definition == null)
            {
                return new Snapshot(WeaponAmmoPolicy2D.Untracked, 0, 0, 0);
            }
            if (definition.AmmoPolicy != WeaponAmmoPolicy2D.Magazine)
            {
                return new Snapshot(definition.AmmoPolicy, 0, 0, 0);
            }

            MagazineState state = GetOrInitialize(definition);
            return new Snapshot(definition.AmmoPolicy, state.Rounds,
                definition.MagazineCapacity, state.ReserveMagazines);
        }

        public bool CanFire(WeaponDefinition2D definition)
        {
            return definition != null &&
                (definition.AmmoPolicy != WeaponAmmoPolicy2D.Magazine ||
                 GetOrInitialize(definition).Rounds > 0);
        }

        /// <summary>Call only after hitscan acceptance or a successful projectile queue.</summary>
        public void ConsumeAcceptedShot(WeaponDefinition2D definition)
        {
            if (definition == null || definition.AmmoPolicy != WeaponAmmoPolicy2D.Magazine)
            {
                return;
            }

            MagazineState state = GetOrInitialize(definition);
            if (state.Rounds <= 0)
            {
                throw new InvalidOperationException("Accepted shot has no magazine round.");
            }
            state.Rounds--;
            _magazines[definition] = state;
            AmmoChanged?.Invoke(definition);
        }

        public bool TryReload(WeaponDefinition2D definition)
        {
            if (definition == null || definition.AmmoPolicy != WeaponAmmoPolicy2D.Magazine)
            {
                return false;
            }

            MagazineState state = GetOrInitialize(definition);
            if (state.Rounds >= definition.MagazineCapacity || state.ReserveMagazines <= 0)
            {
                return false;
            }

            state.Rounds = definition.MagazineCapacity;
            state.ReserveMagazines--;
            _magazines[definition] = state;
            AmmoChanged?.Invoke(definition);
            return true;
        }

        private MagazineState GetOrInitialize(WeaponDefinition2D definition)
        {
            if (!_magazines.TryGetValue(definition, out MagazineState state))
            {
                state = new MagazineState
                {
                    Rounds = definition.MagazineCapacity,
                    ReserveMagazines = definition.InitialReserveMagazines
                };
                _magazines.Add(definition, state);
            }
            return state;
        }
    }
}
