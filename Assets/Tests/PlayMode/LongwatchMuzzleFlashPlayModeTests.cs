using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class LongwatchMuzzleFlashPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator SuccessfulShot_ShowsF0ThenSameVariantF1ThenHides_AndRejectedShotDoesNotRestart()
        {
            yield return LoadFixture();
            GetFixture(out PlayerWeaponController2D weapon, out PlayerAim2D aim,
                out PlayerAnimator2D animator, out PlayerLongwatchAimPresenter2D longwatch,
                out LongwatchMuzzleFlashPresenter2D flash);

            Vector2 exactDirection = Direction(27f, false);
            ForcePose(animator, aim, longwatch, PlayerAnimationState.Idle,
                longwatch.GetBodyIdleFrame(0), exactDirection, false);
            yield return null;
            Assert.That(longwatch.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D pose), Is.True);
            Assert.That(pose.AuthoredAngleDegrees, Is.EqualTo(30));

            Vector2 expectedOrigin = aim.AimOriginWorld;
            int eventCountBefore = flash.ShotEventCount;
            Assert.That(weapon.TryFire(100f), Is.True);
            Assert.That(weapon.LastShotResult.Origin, Is.EqualTo(expectedOrigin));
            Assert.That(weapon.LastShotResult.Direction, Is.EqualTo(exactDirection));
            Assert.That(weapon.LastShotResult.Damage, Is.EqualTo(40));
            Assert.That(weapon.TryFire(100.1f), Is.False);
            Assert.That(flash.ShotEventCount, Is.EqualTo(eventCountBefore + 1));

            yield return null;
            int variant = flash.ActiveVariant;
            Assert.That(flash.IsVisible, Is.True);
            Assert.That(flash.ActiveFrame, Is.Zero);
            Assert.That(flash.FlashRenderer.sprite,
                Is.SameAs(flash.GetSprite(variant * 2)));

            yield return null;
            Assert.That(flash.IsVisible, Is.True);
            Assert.That(flash.ActiveVariant, Is.EqualTo(variant));
            Assert.That(flash.ActiveFrame, Is.EqualTo(1));
            Assert.That(flash.FlashRenderer.sprite,
                Is.SameAs(flash.GetSprite(variant * 2 + 1)));

            yield return null;
            Assert.That(flash.IsVisible, Is.False);
            Assert.That(flash.ActiveVariant, Is.EqualTo(-1));
            Assert.That(flash.ActiveFrame, Is.EqualTo(-1));
        }

        [UnityTest]
        public IEnumerator TenSuccessfulShots_VisitEveryVariantBeforeRepeating()
        {
            yield return LoadFixture();
            GetFixture(out PlayerWeaponController2D weapon, out PlayerAim2D aim,
                out PlayerAnimator2D animator, out PlayerLongwatchAimPresenter2D longwatch,
                out LongwatchMuzzleFlashPresenter2D flash);
            ForcePose(animator, aim, longwatch, PlayerAnimationState.Idle,
                longwatch.GetBodyIdleFrame(0), Vector2.right, false);
            yield return null;

            var variants = new HashSet<int>();
            int firstVariant = -1;
            for (int shot = 0; shot < 10; shot++)
            {
                Assert.That(weapon.TryFire(200f + shot * 0.25f), Is.True);
                yield return null;
                if (shot == 0)
                {
                    firstVariant = flash.ActiveVariant;
                }
                variants.Add(flash.ActiveVariant);
                yield return null;
                yield return null;
            }

            Assert.That(variants, Is.EquivalentTo(new[] { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }));
            Assert.That(flash.ShotEventCount, Is.EqualTo(10));
            Assert.That(flash.ShownFlashCount, Is.EqualTo(10));

            Assert.That(weapon.TryFire(202.5f), Is.True);
            yield return null;
            Assert.That(flash.ActiveVariant, Is.EqualTo(firstVariant));
        }

        [UnityTest]
        public IEnumerator RunAndCrouchShots_UseDisplayedBodyFrameAndMirrorLeftPose()
        {
            yield return LoadFixture();
            GetFixture(out PlayerWeaponController2D weapon, out PlayerAim2D aim,
                out PlayerAnimator2D animator, out PlayerLongwatchAimPresenter2D longwatch,
                out LongwatchMuzzleFlashPresenter2D flash);

            ForcePose(animator, aim, longwatch, PlayerAnimationState.Run,
                longwatch.GetBodyRunFrame(4), Direction(30f, false), false);
            yield return null;
            Assert.That(longwatch.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D runPose), Is.True);
            Assert.That(runPose.State, Is.EqualTo(LongwatchMuzzleState2D.Run));
            Assert.That(runPose.FrameIndex, Is.EqualTo(4));
            Assert.That(weapon.TryFire(300f), Is.True);
            yield return null;
            AssertFlashTransform(flash, runPose);
            yield return null;
            yield return null;

            ForcePose(animator, aim, longwatch, PlayerAnimationState.CrouchMove,
                longwatch.GetBodyCrouchFrame(5), Direction(-60f, true), true);
            yield return null;
            Assert.That(longwatch.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D crouchPose), Is.True);
            Assert.That(crouchPose.State, Is.EqualTo(LongwatchMuzzleState2D.Crouch));
            Assert.That(crouchPose.FrameIndex, Is.EqualTo(5));
            Assert.That(crouchPose.FacingLeft, Is.True);
            Assert.That(weapon.TryFire(300.25f), Is.True);
            yield return null;
            AssertFlashTransform(flash, crouchPose);
            Assert.That(Mathf.DeltaAngle(0f, flash.transform.localEulerAngles.z),
                Is.EqualTo(-120f).Within(0.001f));
        }

        [UnityTest]
        public IEnumerator UnsupportedCrouchShot_ResolvesGameplayButDoesNotDisplayFakeFlash()
        {
            yield return LoadFixture();
            GetFixture(out PlayerWeaponController2D weapon, out PlayerAim2D aim,
                out PlayerAnimator2D animator, out PlayerLongwatchAimPresenter2D longwatch,
                out LongwatchMuzzleFlashPresenter2D flash);

            Vector2 exactDirection = Direction(-72f, false);
            ForcePose(animator, aim, longwatch, PlayerAnimationState.CrouchMove,
                longwatch.GetBodyCrouchFrame(0), exactDirection, false);
            yield return null;
            Assert.That(longwatch.TryGetCurrentRenderedPose(out LongwatchRenderedPose2D pose), Is.True);
            Assert.That(pose.State, Is.EqualTo(LongwatchMuzzleState2D.Crouch));
            Assert.That(pose.DirectionIndex, Is.EqualTo(16));
            Assert.That(pose.AuthoredAngleDegrees, Is.EqualTo(-70));

            int shownBefore = flash.ShownFlashCount;
            int shotsBefore = weapon.ShotCount;
            Assert.That(weapon.TryFire(400f), Is.True);
            Assert.That(weapon.ShotCount, Is.EqualTo(shotsBefore + 1));
            Assert.That(weapon.LastShotResult.Direction, Is.EqualTo(exactDirection));
            yield return null;
            Assert.That(flash.IsVisible, Is.False);
            Assert.That(flash.ShownFlashCount, Is.EqualTo(shownBefore));
            Assert.That(flash.ShotEventCount, Is.EqualTo(1));
        }

        private static IEnumerator LoadFixture()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;
        }

        private static void GetFixture(
            out PlayerWeaponController2D weapon,
            out PlayerAim2D aim,
            out PlayerAnimator2D animator,
            out PlayerLongwatchAimPresenter2D longwatch,
            out LongwatchMuzzleFlashPresenter2D flash)
        {
            weapon = Object.FindAnyObjectByType<PlayerWeaponController2D>();
            aim = weapon?.GetComponent<PlayerAim2D>();
            animator = weapon?.GetComponent<PlayerAnimator2D>();
            longwatch = weapon?.GetComponent<PlayerLongwatchAimPresenter2D>();
            flash = weapon?.GetComponentInChildren<LongwatchMuzzleFlashPresenter2D>(true);
            Assert.That(weapon, Is.Not.Null);
            Assert.That(aim, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(longwatch, Is.Not.Null);
            Assert.That(flash, Is.Not.Null);
            weapon.ResetTransientState();
        }

        private static void ForcePose(
            PlayerAnimator2D animator,
            PlayerAim2D aim,
            PlayerLongwatchAimPresenter2D longwatch,
            PlayerAnimationState state,
            Sprite bodySprite,
            Vector2 aimDirection,
            bool facingLeft)
        {
            animator.enabled = false;
            Animator bodyAnimator = longwatch.BodySpriteRenderer.GetComponent<Animator>();
            if (bodyAnimator != null)
            {
                bodyAnimator.enabled = false;
            }
            FieldInfo stateField = typeof(PlayerAnimator2D).GetField(
                "_currentState", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(stateField, Is.Not.Null);
            stateField.SetValue(animator, (PlayerAnimationState?)state);
            aim.enabled = false;
            Assert.That(aim.ApplyWorldAimVector(aimDirection), Is.True);
            longwatch.BodySpriteRenderer.sprite = bodySprite;
            longwatch.BodySpriteRenderer.flipX = facingLeft;
            longwatch.ArmsWeaponSpriteRenderer.flipX = facingLeft;
        }

        private static void AssertFlashTransform(
            LongwatchMuzzleFlashPresenter2D flash,
            in LongwatchRenderedPose2D pose)
        {
            Assert.That(LongwatchMuzzleFlashMath.TryResolve(
                flash.Metadata, in pose, out Vector2 position, out float angle), Is.True);
            Assert.That((Vector2)flash.transform.localPosition, Is.EqualTo(position));
            Assert.That(Mathf.DeltaAngle(angle, flash.transform.localEulerAngles.z),
                Is.Zero.Within(0.001f));
            Assert.That(flash.transform.parent,
                Is.SameAs(flash.LongwatchPresenter.ArmsWeaponSpriteRenderer.transform));
            Assert.That(flash.IsVisible, Is.True);
        }

        private static Vector2 Direction(float angleDegrees, bool facingLeft)
        {
            float radians = angleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians);
            return new Vector2(facingLeft ? -x : x, Mathf.Sin(radians)).normalized;
        }
    }
}
