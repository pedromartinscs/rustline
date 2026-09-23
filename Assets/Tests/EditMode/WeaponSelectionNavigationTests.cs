using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

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
        public void SelectorPresentation_UsesExactlyTwoReusableViews()
        {
            Assert.That(WeaponCarouselHud2D.ReusableViewCount, Is.EqualTo(2));
        }

        [Test]
        public void RestingCard_UsesOneFixedScaleInsideRepresentativeLogicalViewport()
        {
            const int logicalWidth = 800;
            const int logicalHeight = 600;
            Rect bounds = WeaponCarouselHud2D.CalculateRestingCardBounds(0.60f, 16);

            Assert.That(bounds.xMin, Is.EqualTo(16f));
            Assert.That(bounds.yMin, Is.EqualTo(16f));
            Assert.That(bounds.width, Is.EqualTo(216f).Within(0.001f));
            Assert.That(bounds.height, Is.EqualTo(105f).Within(0.001f));
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(logicalWidth));
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(logicalHeight));
        }

        [TestCase(WeaponCarouselDirection2D.Successor)]
        [TestCase(WeaponCarouselDirection2D.Predecessor)]
        public void TransitionCards_KeepConstantNonOverlappingSeparation(
            WeaponCarouselDirection2D direction)
        {
            const float scale = 0.60f;
            const int gap = 20;
            Rect resting = WeaponCarouselHud2D.CalculateRestingCardBounds(scale, 16);
            float distance = WeaponCarouselHud2D.CalculateStepDistance(scale, gap);
            float[] samples = { 0f, 0.25f, 0.5f, 0.75f, 1f };

            foreach (float progress in samples)
            {
                Vector2 centers = WeaponCarouselHud2D.CalculateTransitionCenterYs(
                    resting.center.y,
                    distance,
                    progress,
                    direction);
                float centerSeparation = Mathf.Abs(centers.y - centers.x);
                float clearGap = centerSeparation - resting.height;

                Assert.That(centerSeparation, Is.EqualTo(distance).Within(0.001f));
                Assert.That(clearGap, Is.GreaterThanOrEqualTo(gap - 0.001f));
            }
        }

        [Test]
        public void SpatialPenumbra_UsesFixedMirroredHudSpaceBands()
        {
            Rect resting = WeaponCarouselHud2D.CalculateRestingCardBounds(0.60f, 16);
            const float thickness = 20f;

            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.center.y, resting.yMin, resting.yMax, thickness), Is.EqualTo(0f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMax, resting.yMin, resting.yMax, thickness), Is.EqualTo(0f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMax + 10f, resting.yMin, resting.yMax, thickness), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMax + 20f, resting.yMin, resting.yMax, thickness), Is.EqualTo(1f).Within(0.001f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMax + 21f, resting.yMin, resting.yMax, thickness), Is.GreaterThan(1f));

            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMin - 10f, resting.yMin, resting.yMax, thickness), Is.EqualTo(0.5f).Within(0.001f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMin - 20f, resting.yMin, resting.yMax, thickness), Is.EqualTo(1f).Within(0.001f));
            Assert.That(WeaponCarouselHud2D.CalculateSpatialPenumbraDistance(
                resting.yMin - 21f, resting.yMin, resting.yMax, thickness), Is.GreaterThan(1f));
        }

        [Test]
        public void CarouselSpatialPenumbraShader_CompilesWithoutErrors()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(
                "Assets/Shaders/RustlineWeaponCarouselFade.shader");
            Assert.That(shader, Is.Not.Null);
            Assert.That(ShaderUtil.ShaderHasError(shader), Is.False);
        }

        [Test]
        public void RustlineHudLayer_UsesReservedIndexAndDedicatedCameraMask()
        {
            Assert.That(LayerMask.NameToLayer(WeaponCarouselHud2D.RustlineHudLayerName),
                Is.EqualTo(WeaponCarouselHud2D.RustlineHudLayerIndex));
            Assert.That(WeaponCarouselHud2D.DedicatedHudCullingMask,
                Is.EqualTo(1 << WeaponCarouselHud2D.RustlineHudLayerIndex));
        }

        [TestCase("Assets/Scenes/MovementLab.unity")]
        [TestCase("Assets/Scenes/Demo/SalvageIntake.unity")]
        public void ProductionWorldCameras_ExcludeRustlineHud(string scenePath)
        {
            Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Additive);
            try
            {
                NativePixelPresentation presentation = FindInScene<NativePixelPresentation>(scene);
                Assert.That(presentation, Is.Not.Null, scenePath + " is missing native-pixel presentation.");
                Assert.That(presentation.WorldCamera.cullingMask & WeaponCarouselHud2D.DedicatedHudCullingMask,
                    Is.EqualTo(0), scenePath + " world camera renders RustlineHUD.");
            }
            finally
            {
                EditorSceneManager.CloseScene(scene, true);
            }
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
        public void TwoWeaponWheelSuccessor_PreservesSuccessorStepDirection()
        {
            GameObject instance = InstantiateTwoWeaponEquipment();
            try
            {
                PlayerWeaponEquipment2D equipment = InitializeEquipment(instance);
                Assert.That(equipment.RequestAdjacent(WeaponCarouselDirection2D.Successor), Is.True);
                Assert.That(equipment.StepDirection, Is.EqualTo(WeaponCarouselDirection2D.Successor));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void TwoWeaponWheelPredecessor_PreservesPredecessorStepDirection()
        {
            GameObject instance = InstantiateTwoWeaponEquipment();
            try
            {
                PlayerWeaponEquipment2D equipment = InitializeEquipment(instance);
                Assert.That(equipment.RequestAdjacent(WeaponCarouselDirection2D.Predecessor), Is.True);
                Assert.That(equipment.StepDirection, Is.EqualTo(WeaponCarouselDirection2D.Predecessor));
            }
            finally { Object.DestroyImmediate(instance); }
        }

        [Test]
        public void TwoWeaponDirectSelection_EqualDistanceUsesSuccessorTieBreak()
        {
            GameObject instance = InstantiateTwoWeaponEquipment();
            try
            {
                PlayerWeaponEquipment2D equipment = InitializeEquipment(instance);
                Assert.That(equipment.RequestSlot(1), Is.True);
                Assert.That(equipment.StepDirection, Is.EqualTo(WeaponCarouselDirection2D.Successor));
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

        private static GameObject InstantiateTwoWeaponEquipment()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Player/Player.prefab");
            GameObject instance = Object.Instantiate(prefab);
            SerializedObject serialized = new SerializedObject(instance.GetComponent<PlayerWeaponEquipment2D>());
            serialized.FindProperty("loadout").GetArrayElementAtIndex(2)
                .FindPropertyRelative("available").boolValue = false;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return instance;
        }

        private static PlayerWeaponEquipment2D InitializeEquipment(GameObject instance)
        {
            PlayerWeaponEquipment2D equipment = instance.GetComponent<PlayerWeaponEquipment2D>();
            MethodInfo awake = typeof(PlayerWeaponEquipment2D).GetMethod(
                "Awake", BindingFlags.Instance | BindingFlags.NonPublic);
            awake.Invoke(equipment, null);
            return equipment;
        }

        private static T FindInScene<T>(Scene scene) where T : Component
        {
            foreach (GameObject root in scene.GetRootGameObjects())
            {
                T result = root.GetComponentInChildren<T>(true);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }
    }
}
