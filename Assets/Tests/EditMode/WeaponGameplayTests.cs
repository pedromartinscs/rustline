using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rustline.Tests
{
    public sealed class WeaponGameplayTests
    {
        private const string DefinitionPath = "Assets/Config/Weapons/LongwatchDMR.asset";

        [Test]
        public void LongwatchDefinition_HasExpectedPrototypeValues()
        {
            WeaponDefinition2D definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(DefinitionPath);

            Assert.That(definition, Is.Not.Null);
            Assert.That(definition.IsSane(out string reason), Is.True, reason);
            Assert.That(definition.WeaponId, Is.EqualTo("longwatch_dmr"));
            Assert.That(definition.DisplayName, Is.EqualTo("Longwatch DMR"));
            Assert.That(definition.FireMode, Is.EqualTo(WeaponFireMode2D.SemiAutomatic));
            Assert.That(definition.ShotInterval, Is.EqualTo(0.25f));
            Assert.That(definition.Range, Is.EqualTo(80f));
            Assert.That(definition.Damage, Is.EqualTo(40));
        }

        [Test]
        public void FireInput_IsMouseLeftSemiAutomaticPress()
        {
            InputActionAsset input = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
                "Assets/InputSystem_Actions.inputactions");
            InputAction fire = input?.FindActionMap("Player", false)?.FindAction("Fire", false);

            Assert.That(fire, Is.Not.Null);
            Assert.That(fire.type, Is.EqualTo(InputActionType.Button));
            Assert.That(fire.expectedControlType, Is.EqualTo("Button"));
            Assert.That(fire.bindings.Count, Is.EqualTo(1));
            Assert.That(fire.bindings[0].path, Is.EqualTo("<Mouse>/leftButton"));
            Assert.That(fire.bindings[0].interactions, Is.EqualTo("Press"));
        }

        [TestCase(PlayerAnimationState.Idle)]
        [TestCase(PlayerAnimationState.Run)]
        [TestCase(PlayerAnimationState.Backpedal)]
        [TestCase(PlayerAnimationState.CrouchIdle)]
        [TestCase(PlayerAnimationState.CrouchMove)]
        [TestCase(PlayerAnimationState.Fall)]
        public void FirePolicy_AllowsAuthoredGroundedLongwatchStates(PlayerAnimationState state)
        {
            Assert.That(WeaponFirePolicy2D.CanFire(state, false, false), Is.True);
        }

        [TestCase(PlayerAnimationState.Jump)]
        [TestCase(PlayerAnimationState.Land)]
        [TestCase(PlayerAnimationState.WallBrace)]
        [TestCase(PlayerAnimationState.LedgeClimb)]
        public void FirePolicy_BlocksCarryAndUnarmedTraversalStates(PlayerAnimationState state)
        {
            Assert.That(WeaponFirePolicy2D.CanFire(state, false, false), Is.False);
        }

        [Test]
        public void FirePolicy_BlocksMissingStateBraceAndKick()
        {
            Assert.That(WeaponFirePolicy2D.CanFire(null, false, false), Is.False);
            Assert.That(WeaponFirePolicy2D.CanFire(PlayerAnimationState.Idle, true, false), Is.False);
            Assert.That(WeaponFirePolicy2D.CanFire(PlayerAnimationState.Idle, false, true), Is.False);
        }

        [Test]
        public void Cooldown_FiresImmediatelyBlocksThenBecomesReadyWithoutBuffering()
        {
            var cooldown = new SemiAutomaticWeaponCooldown2D();

            Assert.That(cooldown.TryConsume(10f, 0.25f), Is.True);
            Assert.That(cooldown.TryConsume(10.249f, 0.25f), Is.False);
            Assert.That(cooldown.ReadyTime, Is.EqualTo(10.25f));
            Assert.That(cooldown.TryConsume(10.25f, 0.25f), Is.True);
            Assert.That(cooldown.ReadyTime, Is.EqualTo(10.5f));
        }

        [Test]
        public void ShotResult_PreservesContinuousDirectionAndConfiguredHitData()
        {
            WeaponDefinition2D definition = AssetDatabase.LoadAssetAtPath<WeaponDefinition2D>(DefinitionPath);
            Vector2 continuousDirection = new Vector2(0.99254614f, 0.12186934f);
            var result = new WeaponShotResult2D(
                definition,
                new Vector2(1f, 2f),
                continuousDirection,
                new Vector2(9f, 3f),
                true,
                null,
                Vector2.left,
                8f,
                40,
                false);

            Assert.That(result.WeaponId, Is.EqualTo("longwatch_dmr"));
            Assert.That(result.Direction, Is.EqualTo(continuousDirection));
            Assert.That(result.EndPoint, Is.EqualTo(new Vector2(9f, 3f)));
            Assert.That(result.HitDistance, Is.EqualTo(8f));
            Assert.That(result.Damage, Is.EqualTo(40));
        }

        [Test]
        public void LongwatchRecoilCurve_UsesContinuousDirectionAndReturnsExactlyToZero()
        {
            Vector2 direction = new Vector2(
                Mathf.Cos(7f * Mathf.Deg2Rad),
                Mathf.Sin(7f * Mathf.Deg2Rad));

            Vector2 initial = LongwatchRecoilPresenter2D.EvaluateOffset(
                direction,
                0f,
                LongwatchRecoilPresenter2D.RecoveryDuration,
                LongwatchRecoilPresenter2D.RecoilDistanceWorldUnits);
            Vector2 recovered = LongwatchRecoilPresenter2D.EvaluateOffset(
                direction,
                LongwatchRecoilPresenter2D.RecoveryDuration,
                LongwatchRecoilPresenter2D.RecoveryDuration,
                LongwatchRecoilPresenter2D.RecoilDistanceWorldUnits);

            Assert.That(Vector2.Dot(initial, direction), Is.LessThan(0f));
            Assert.That(initial.magnitude,
                Is.EqualTo(LongwatchRecoilPresenter2D.RecoilDistanceWorldUnits).Within(0.000001f));
            Assert.That(recovered, Is.EqualTo(Vector2.zero));
        }

        [Test]
        public void ActiveMuzzleFlash_HidesWhenLongwatchOwnsCarryWithoutMuzzlePose()
        {
            GameObject root = new GameObject("Longwatch Carry Flash Test");
            try
            {
                PlayerLongwatchAimPresenter2D longwatch = root.AddComponent<PlayerLongwatchAimPresenter2D>();
                FieldInfo ownsRenderer = typeof(PlayerLongwatchAimPresenter2D).GetField(
                    "_ownsRenderer", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo hasRenderedPose = typeof(PlayerLongwatchAimPresenter2D).GetField(
                    "_hasRenderedPose", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(ownsRenderer, Is.Not.Null);
                Assert.That(hasRenderedPose, Is.Not.Null);
                ownsRenderer.SetValue(longwatch, true);
                hasRenderedPose.SetValue(longwatch, false);

                GameObject flashObject = new GameObject("Longwatch Muzzle Flash");
                flashObject.transform.SetParent(root.transform, false);
                SpriteRenderer renderer = flashObject.AddComponent<SpriteRenderer>();
                LongwatchMuzzleFlashPresenter2D flash =
                    flashObject.AddComponent<LongwatchMuzzleFlashPresenter2D>();

                SerializedObject serialized = new SerializedObject(flash);
                serialized.FindProperty("longwatchPresenter").objectReferenceValue = longwatch;
                serialized.FindProperty("flashRenderer").objectReferenceValue = renderer;
                serialized.ApplyModifiedPropertiesWithoutUndo();

                FieldInfo activeVariant = typeof(LongwatchMuzzleFlashPresenter2D).GetField(
                    "_activeVariant", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo activeFrame = typeof(LongwatchMuzzleFlashPresenter2D).GetField(
                    "_activeFrame", BindingFlags.Instance | BindingFlags.NonPublic);
                FieldInfo activationFrame = typeof(LongwatchMuzzleFlashPresenter2D).GetField(
                    "_activationFrame", BindingFlags.Instance | BindingFlags.NonPublic);
                MethodInfo lateUpdate = typeof(LongwatchMuzzleFlashPresenter2D).GetMethod(
                    "LateUpdate", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(activeVariant, Is.Not.Null);
                Assert.That(activeFrame, Is.Not.Null);
                Assert.That(activationFrame, Is.Not.Null);
                Assert.That(lateUpdate, Is.Not.Null);

                activeVariant.SetValue(flash, 0);
                activeFrame.SetValue(flash, 0);
                activationFrame.SetValue(flash, Time.frameCount);
                renderer.enabled = true;

                lateUpdate.Invoke(flash, null);

                Assert.That(flash.IsVisible, Is.False);
                Assert.That(flash.ActiveVariant, Is.EqualTo(-1));
                Assert.That(flash.ActiveFrame, Is.EqualTo(-1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
