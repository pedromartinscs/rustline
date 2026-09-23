using System;
using System.Collections.Generic;

namespace Rustline.Gameplay.Weapons
{
    public enum WeaponCarouselDirection2D
    {
        Predecessor = -1,
        Successor = 1,
    }

    /// <summary>Pure cyclic navigation used by the equipment authority and carousel view.</summary>
    public static class WeaponSelectionNavigation2D
    {
        public static int FindAvailableIndex(IReadOnlyList<int> availableSlots, int slot)
        {
            if (availableSlots == null)
            {
                return -1;
            }

            for (int index = 0; index < availableSlots.Count; index++)
            {
                if (availableSlots[index] == slot)
                {
                    return index;
                }
            }

            return -1;
        }

        public static int GetAdjacentSlot(
            IReadOnlyList<int> availableSlots,
            int currentSlot,
            WeaponCarouselDirection2D direction)
        {
            int currentIndex = FindAvailableIndex(availableSlots, currentSlot);
            if (currentIndex < 0 || availableSlots.Count < 2)
            {
                return currentSlot;
            }

            int nextIndex = Mod(currentIndex + (int)direction, availableSlots.Count);
            return availableSlots[nextIndex];
        }

        public static WeaponCarouselDirection2D GetShortestDirection(
            IReadOnlyList<int> availableSlots,
            int currentSlot,
            int targetSlot)
        {
            int currentIndex = FindAvailableIndex(availableSlots, currentSlot);
            int targetIndex = FindAvailableIndex(availableSlots, targetSlot);
            if (currentIndex < 0 || targetIndex < 0 || currentIndex == targetIndex || availableSlots.Count < 2)
            {
                return WeaponCarouselDirection2D.Successor;
            }

            int forward = Mod(targetIndex - currentIndex, availableSlots.Count);
            int backward = Mod(currentIndex - targetIndex, availableSlots.Count);
            return forward <= backward
                ? WeaponCarouselDirection2D.Successor
                : WeaponCarouselDirection2D.Predecessor;
        }

        public static int Mod(int value, int modulus)
        {
            if (modulus <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(modulus));
            }

            int result = value % modulus;
            return result < 0 ? result + modulus : result;
        }
    }

    public readonly struct WeaponCarouselTransitionSlots2D
    {
        public WeaponCarouselTransitionSlots2D(int outgoingNeighbor, int movingCenter, int movingNeighbor, int incomingNeighbor)
        {
            OutgoingNeighbor = outgoingNeighbor;
            MovingCenter = movingCenter;
            MovingNeighbor = movingNeighbor;
            IncomingNeighbor = incomingNeighbor;
        }
        public int OutgoingNeighbor { get; }
        public int MovingCenter { get; }
        public int MovingNeighbor { get; }
        public int IncomingNeighbor { get; }
    }

    public static class WeaponCarouselTransition2D
    {
        public static WeaponCarouselTransitionSlots2D GetSlots(
            IReadOnlyList<int> availableSlots,
            int centerSlot,
            WeaponCarouselDirection2D direction)
        {
            int upper = WeaponSelectionNavigation2D.GetAdjacentSlot(availableSlots, centerSlot, WeaponCarouselDirection2D.Successor);
            int lower = WeaponSelectionNavigation2D.GetAdjacentSlot(availableSlots, centerSlot, WeaponCarouselDirection2D.Predecessor);
            if (direction == WeaponCarouselDirection2D.Successor)
            {
                return new WeaponCarouselTransitionSlots2D(
                    lower, centerSlot, upper,
                    WeaponSelectionNavigation2D.GetAdjacentSlot(availableSlots, upper, direction));
            }
            return new WeaponCarouselTransitionSlots2D(
                upper, centerSlot, lower,
                WeaponSelectionNavigation2D.GetAdjacentSlot(availableSlots, lower, direction));
        }
    }
}
