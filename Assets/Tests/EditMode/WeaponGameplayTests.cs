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
        public void FirePolicy_AllowsAuthoredGroundedLongwatchStates(PlayerAnimationState state)
        {
            Assert.That(WeaponFirePolicy2D.CanFire(state, false, false), Is.True);
        }

        [TestCase(PlayerAnimationState.Jump)]
        [TestCase(PlayerAnimationState.Fall)]
        [TestCase(PlayerAnimationState.Land)]
        [TestCase(PlayerAnimationState.CrouchIdle)]
        [TestCase(PlayerAnimationState.CrouchMove)]
        public void FirePolicy_BlocksStatesWithoutAuthoredLongwatchPresentation(PlayerAnimationState state)
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
    }
}
