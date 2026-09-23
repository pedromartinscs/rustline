using System;
using System.Collections.Generic;
using Rustline.Gameplay.Player;
using UnityEngine;

namespace Rustline.Gameplay.Weapons
{
    [Serializable]
    public struct WeaponLoadoutEntry2D
    {
        [Range(0, 9)] [SerializeField] private int slot;
        [SerializeField] private bool available;
        [SerializeField] private Sprite carouselSprite;
        [SerializeField] private WeaponDefinition2D weaponDefinition;
        [SerializeField] private Behaviour[] activePresentationComponents;

        public int Slot => slot;
        public bool Available => available;
        public Sprite CarouselSprite => carouselSprite;
        public WeaponDefinition2D WeaponDefinition => weaponDefinition;
        public Behaviour[] ActivePresentationComponents => activePresentationComponents;
    }

    /// <summary>
    /// The sole owner of equipped weapon state. The carousel observes its discrete steps;
    /// its animation never becomes an independent gameplay selection source.
    /// </summary>
    [DefaultExecutionOrder(-100)]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(PlayerWeaponController2D))]
    public sealed class PlayerWeaponEquipment2D : MonoBehaviour
    {
        [SerializeField] private PlayerInputReader input;
        [SerializeField] private PlayerWeaponController2D weaponController;
        [SerializeField] private WeaponLoadoutEntry2D[] loadout = Array.Empty<WeaponLoadoutEntry2D>();
        [SerializeField, Range(0, 9)] private int initialSlot = 2;
        [SerializeField, Min(0.01f)] private float carouselStepDuration = 0.14f;

        private readonly List<int> _availableSlots = new List<int>(10);
        private int _selectedSlot = -1;
        private int _requestedSlot = -1;
        private bool _stepActive;
        private float _stepStartedAt;
        private WeaponCarouselDirection2D _stepDirection;
        private int _stepIncomingSlot = -1;

        public event Action<int, int, WeaponCarouselDirection2D> StepStarted;
        public event Action<WeaponLoadoutEntry2D> EquipmentChanged;

        public int SelectedSlot => _selectedSlot;
        public int RequestedSlot => _requestedSlot;
        public bool IsStepActive => _stepActive;
        public float StepProgress => !_stepActive ? 0f : Mathf.Clamp01((Time.unscaledTime - _stepStartedAt) / carouselStepDuration);
        public WeaponCarouselDirection2D StepDirection => _stepDirection;
        public int StepIncomingSlot => _stepIncomingSlot;
        public float CarouselStepDuration => carouselStepDuration;
        public IReadOnlyList<int> AvailableSlots => _availableSlots;

        private void Awake()
        {
            if (weaponController == null)
            {
                weaponController = GetComponent<PlayerWeaponController2D>();
            }
            if (input == null)
            {
                input = GetComponent<PlayerInputReader>();
            }

            RebuildAvailableSlots();
            int firstSlot = IsAvailable(initialSlot) ? initialSlot : (_availableSlots.Count > 0 ? _availableSlots[0] : -1);
            ApplyEquippedSlot(firstSlot);
        }

        private void Update()
        {
            ConsumeSelectionInput();
            if (!_stepActive || Time.unscaledTime - _stepStartedAt < carouselStepDuration)
            {
                return;
            }

            ApplyEquippedSlot(_stepIncomingSlot);
            _stepActive = false;
            if (_requestedSlot != _selectedSlot)
            {
                BeginNextStep();
            }
        }

        public bool RequestSlot(int slot)
        {
            if (!IsAvailable(slot) || slot == _requestedSlot)
            {
                return false;
            }

            _requestedSlot = slot;
            if (!_stepActive && _requestedSlot != _selectedSlot)
            {
                BeginNextStep();
            }

            return true;
        }

        public bool RequestAdjacent(WeaponCarouselDirection2D direction)
        {
            if (_selectedSlot < 0 || _availableSlots.Count < 2)
            {
                return false;
            }

            return RequestSlot(WeaponSelectionNavigation2D.GetAdjacentSlot(_availableSlots, _selectedSlot, direction));
        }

        public bool TryGetEntry(int slot, out WeaponLoadoutEntry2D entry)
        {
            for (int index = 0; index < loadout.Length; index++)
            {
                if (loadout[index].Slot == slot)
                {
                    entry = loadout[index];
                    return true;
                }
            }

            entry = default;
            return false;
        }

        private void ConsumeSelectionInput()
        {
            if (input == null)
            {
                return;
            }

            int direction = input.ConsumeWeaponCycleDirection();
            if (direction > 0)
            {
                RequestAdjacent(WeaponCarouselDirection2D.Successor);
            }
            else if (direction < 0)
            {
                RequestAdjacent(WeaponCarouselDirection2D.Predecessor);
            }

            int slot = input.ConsumeWeaponSlotPressed();
            if (slot >= 0)
            {
                RequestSlot(slot);
            }
        }

        private void BeginNextStep()
        {
            if (!IsAvailable(_selectedSlot) || !IsAvailable(_requestedSlot))
            {
                return;
            }

            _stepDirection = WeaponSelectionNavigation2D.GetShortestDirection(
                _availableSlots, _selectedSlot, _requestedSlot);
            _stepIncomingSlot = WeaponSelectionNavigation2D.GetAdjacentSlot(
                _availableSlots, _selectedSlot, _stepDirection);
            if (_stepIncomingSlot == _selectedSlot)
            {
                return;
            }

            _stepActive = true;
            _stepStartedAt = Time.unscaledTime;
            StepStarted?.Invoke(_selectedSlot, _stepIncomingSlot, _stepDirection);
        }

        private void ApplyEquippedSlot(int slot)
        {
            _selectedSlot = slot;
            _requestedSlot = _requestedSlot < 0 ? slot : _requestedSlot;
            bool found = TryGetEntry(slot, out WeaponLoadoutEntry2D selected);
            for (int entryIndex = 0; entryIndex < loadout.Length; entryIndex++)
            {
                Behaviour[] components = loadout[entryIndex].ActivePresentationComponents;
                if (components == null)
                {
                    continue;
                }

                bool active = found && loadout[entryIndex].Slot == selected.Slot;
                for (int componentIndex = 0; componentIndex < components.Length; componentIndex++)
                {
                    if (components[componentIndex] != null)
                    {
                        components[componentIndex].enabled = active;
                    }
                }
            }

            // A null definition is the explicit, real Unarmed equipment state.
            weaponController?.EquipWeapon(found ? selected.WeaponDefinition : null);
            if (found)
            {
                EquipmentChanged?.Invoke(selected);
            }
        }

        private void RebuildAvailableSlots()
        {
            _availableSlots.Clear();
            for (int index = 0; index < loadout.Length; index++)
            {
                WeaponLoadoutEntry2D entry = loadout[index];
                if (!entry.Available || entry.Slot < 0 || entry.Slot > 9 ||
                    WeaponSelectionNavigation2D.FindAvailableIndex(_availableSlots, entry.Slot) >= 0)
                {
                    continue;
                }

                int insertIndex = _availableSlots.Count;
                while (insertIndex > 0 && _availableSlots[insertIndex - 1] > entry.Slot)
                {
                    insertIndex--;
                }
                _availableSlots.Insert(insertIndex, entry.Slot);
            }
        }

        private bool IsAvailable(int slot)
        {
            return WeaponSelectionNavigation2D.FindAvailableIndex(_availableSlots, slot) >= 0;
        }
    }
}
