using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class WeaponSelectionNavigationTests
    {
        private static readonly List<int> Initial = new List<int> { 0, 1, 2 };

        [Test]
        public void CyclicNextPrevious_WrapsInitialArsenal()
        {
            Assert.That(WeaponSelectionNavigation2D.GetAdjacentSlot(Initial, 2, WeaponCarouselDirection2D.Successor), Is.EqualTo(0));
            Assert.That(WeaponSelectionNavigation2D.GetAdjacentSlot(Initial, 0, WeaponCarouselDirection2D.Predecessor), Is.EqualTo(2));
        }

        [Test]
        public void MissingSlots_AreNotNavigable()
        {
            List<int> available = new List<int> { 0, 2, 7 };
            Assert.That(WeaponSelectionNavigation2D.FindAvailableIndex(available, 1), Is.EqualTo(-1));
            Assert.That(WeaponSelectionNavigation2D.GetAdjacentSlot(available, 2, WeaponCarouselDirection2D.Successor), Is.EqualTo(7));
        }

        [Test]
        public void DirectSelection_UsesShortestRouteBothDirections()
        {
            List<int> slots = new List<int> { 0, 1, 2, 3, 4, 5 };
            Assert.That(WeaponSelectionNavigation2D.GetShortestDirection(slots, 4, 0), Is.EqualTo(WeaponCarouselDirection2D.Successor));
            Assert.That(WeaponSelectionNavigation2D.GetShortestDirection(slots, 1, 5), Is.EqualTo(WeaponCarouselDirection2D.Predecessor));
        }

        [Test]
        public void DirectSelection_EqualDistanceChoosesSuccessor()
        {
            List<int> slots = new List<int> { 0, 1, 2, 3, 4, 5 };
            Assert.That(WeaponSelectionNavigation2D.GetShortestDirection(slots, 0, 3), Is.EqualTo(WeaponCarouselDirection2D.Successor));
        }

        [Test]
        public void FourViewSuccessorTransition_UsesOutgoingLowerAndIncomingUpper()
        {
            WeaponCarouselTransitionSlots2D transition = WeaponCarouselTransition2D.GetSlots(
                new List<int> { 0, 1, 2, 3, 4, 5 }, 4, WeaponCarouselDirection2D.Successor);
            Assert.That(transition.OutgoingNeighbor, Is.EqualTo(3));
            Assert.That(transition.MovingCenter, Is.EqualTo(4));
            Assert.That(transition.MovingNeighbor, Is.EqualTo(5));
            Assert.That(transition.IncomingNeighbor, Is.EqualTo(0));
        }

        [Test]
        public void FourViewPredecessorTransition_IsExactMirror()
        {
            WeaponCarouselTransitionSlots2D transition = WeaponCarouselTransition2D.GetSlots(
                new List<int> { 0, 1, 2, 3, 4, 5 }, 4, WeaponCarouselDirection2D.Predecessor);
            Assert.That(transition.OutgoingNeighbor, Is.EqualTo(5));
            Assert.That(transition.MovingCenter, Is.EqualTo(4));
            Assert.That(transition.MovingNeighbor, Is.EqualTo(3));
            Assert.That(transition.IncomingNeighbor, Is.EqualTo(2));
        }

        [Test]
        public void CarouselLayout_KeepsAllInitialCardsInsideRepresentativeLogicalViewport()
        {
            const int logicalWidth = 800;
            const int logicalHeight = 600;
            for (int level = -1; level <= 1; level++)
            {
                Rect bounds = WeaponCarouselHud2D.CalculateCardBounds(level, 0.60f, 0.48f, 16, 86);
                Assert.That(bounds.xMin, Is.EqualTo(16f));
                Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(16f));
                Assert.That(bounds.xMax, Is.LessThanOrEqualTo(logicalWidth));
                Assert.That(bounds.yMax, Is.LessThanOrEqualTo(logicalHeight));
            }
        }

        [Test]
        public void CarouselFadeShader_CompilesWithoutErrors()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Shaders/RustlineWeaponCarouselFade.shader");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
        }

        [TestCase(0, 0)]
        [TestCase(1, 0)]
        [TestCase(2, 1)]
        public void SmallArsenals_DoNotInventAdditionalEntries(int count, int expectedNeighbor)
        {
            var slots = new List<int>();
            for (int index = 0; index < count; index++) slots.Add(index);
            int current = count == 0 ? 0 : 0;
            Assert.That(WeaponSelectionNavigation2D.GetAdjacentSlot(slots, current, WeaponCarouselDirection2D.Successor), Is.EqualTo(expectedNeighbor));
        }

        [Test]
        public void PlayerPrefab_PersistsFullLoadoutAndLatchRuntimePackage()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            Assert.That(prefab.GetComponent<PlayerWeaponEquipment2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<PlayerLatch9AimPresenter2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<Latch9ProjectileEmitter2D>(), Is.Not.Null);
            Assert.That(prefab.GetComponent<WeaponCarouselHud2D>(), Is.Not.Null);
        }

        [Test]
        public void EquipmentAuthority_RetargetsDuringActiveStepAndSwitchesRuntimePackages()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                PlayerWeaponEquipment2D equipment = instance.GetComponent<PlayerWeaponEquipment2D>();
                PlayerWeaponController2D controller = instance.GetComponent<PlayerWeaponController2D>();
                PlayerLatch9AimPresenter2D latch = instance.GetComponent<PlayerLatch9AimPresenter2D>();
                PlayerLongwatchAimPresenter2D longwatch = instance.GetComponent<PlayerLongwatchAimPresenter2D>();
                MethodInfo awake = typeof(PlayerWeaponEquipment2D).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo apply = typeof(PlayerWeaponEquipment2D).GetMethod("ApplyEquippedSlot", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(awake, Is.Not.Null); Assert.That(apply, Is.Not.Null);
                awake.Invoke(equipment, null);

                Assert.That(equipment.RequestSlot(0), Is.True);
                Assert.That(equipment.IsStepActive, Is.True);
                Assert.That(equipment.RequestSlot(1), Is.True);
                Assert.That(equipment.RequestedSlot, Is.EqualTo(1));

                apply.Invoke(equipment, new object[] { 0 });
                Assert.That(controller.WeaponDefinition, Is.Null);
                Assert.That(latch.enabled, Is.False);
                Assert.That(longwatch.enabled, Is.False);

                apply.Invoke(equipment, new object[] { 1 });
                Assert.That(controller.WeaponDefinition.WeaponId, Is.EqualTo("latch_9"));
                Assert.That(latch.enabled, Is.True);
                Assert.That(longwatch.enabled, Is.False);

                apply.Invoke(equipment, new object[] { 2 });
                Assert.That(controller.WeaponDefinition.WeaponId, Is.EqualTo("longwatch_dmr"));
                Assert.That(latch.enabled, Is.False);
                Assert.That(longwatch.enabled, Is.True);
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void EquipmentAuthority_RepeatedAdjacentRequestsAdvanceLatestTargetDuringStep()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            GameObject instance = Object.Instantiate(prefab);
            try
            {
                PlayerWeaponEquipment2D equipment = instance.GetComponent<PlayerWeaponEquipment2D>();
                MethodInfo awake = typeof(PlayerWeaponEquipment2D).GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic);
                awake.Invoke(equipment, null);

                Assert.That(equipment.SelectedSlot, Is.EqualTo(2));
                Assert.That(equipment.RequestAdjacent(WeaponCarouselDirection2D.Successor), Is.True);
                Assert.That(equipment.RequestedSlot, Is.EqualTo(0));
                Assert.That(equipment.RequestAdjacent(WeaponCarouselDirection2D.Successor), Is.True);
                Assert.That(equipment.RequestedSlot, Is.EqualTo(1));

                Assert.That(equipment.RequestAdjacent(WeaponCarouselDirection2D.Predecessor), Is.True);
                Assert.That(equipment.RequestedSlot, Is.EqualTo(0));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void Unarmed_IsARealNoFireEquipmentState()
        {
            GameObject root = new GameObject("Unarmed Selection Test");
            try
            {
                root.AddComponent<BoxCollider2D>();
                PlayerWeaponController2D controller = root.AddComponent<PlayerWeaponController2D>();
                controller.EquipWeapon(null);
                Assert.That(controller.WeaponDefinition, Is.Null);
                Assert.That(controller.TryFire(0f), Is.False);
            }
            finally { Object.DestroyImmediate(root); }
        }
    }
}
