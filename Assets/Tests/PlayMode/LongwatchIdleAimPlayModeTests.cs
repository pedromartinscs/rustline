using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class LongwatchIdleAimPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator IdleLongwatch_FollowsMouseAcrossAnglesAndBodyFramesWithoutDrift()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation nativePresentation =
                Object.FindAnyObjectByType<NativePixelPresentation>();
            Assert.That(motor, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);

            PlayerLongwatchIdleAimPresenter2D armed =
                motor.GetComponent<PlayerLongwatchIdleAimPresenter2D>();
            PlayerUnarmedArmsPresenter2D unarmed = motor.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerAnimator2D playerAnimator = motor.GetComponent<PlayerAnimator2D>();
            Transform visual = motor.transform.Find("Visual - 48x64 Full Cell");
            Transform bodyTransform = visual?.Find("BodySpriteRenderer");
            Transform armsTransform = visual?.Find("ArmsWeaponSpriteRenderer");
            SpriteRenderer bodyRenderer = bodyTransform?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsTransform?.GetComponent<SpriteRenderer>();
            Animator animator = bodyTransform?.GetComponent<Animator>();

            Assert.That(armed, Is.Not.Null);
            Assert.That(unarmed, Is.Not.Null);
            Assert.That(playerAnimator, Is.Not.Null);
            Assert.That(armed.NativePixelPresentation, Is.SameAs(nativePresentation));
            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(motor.GetComponentsInChildren<Animator>(true), Has.Length.EqualTo(1));

            Vector3 bodyLocalPosition = bodyTransform.localPosition;
            Vector3 armsLocalPosition = armsTransform.localPosition;
            Quaternion bodyLocalRotation = bodyTransform.localRotation;
            Quaternion armsLocalRotation = armsTransform.localRotation;
            Vector3 bodyLocalScale = bodyTransform.localScale;
            Vector3 armsLocalScale = armsTransform.localScale;

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return SettlePlayer();
                QueueWorldAim(mouse, nativePresentation, bodyTransform.position, Vector2.right);
                yield return WaitForArmedIdle(armed, 20);
                Assert.That(armed.OwnsRenderer, Is.True);
                Assert.That(unarmed.OwnsRenderer, Is.False);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(0));
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);

                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
                yield return null;
                AssertArmedMatchesBodyFrame(armed, bodyRenderer, armsRenderer, 0);

                animator.Play("Idle", 0, 0.6f);
                animator.Update(0f);
                yield return null;
                AssertArmedMatchesBodyFrame(armed, bodyRenderer, armsRenderer, 1);

                QueueWorldAim(mouse, nativePresentation, bodyTransform.position,
                    DirectionAtDegrees(34f, false));
                yield return null;
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(30));
                AssertArmedMatchesBodyFrame(armed, bodyRenderer, armsRenderer, 1);

                QueueWorldAim(mouse, nativePresentation, bodyTransform.position,
                    DirectionAtDegrees(47f, true));
                yield return null;
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(50));
                Assert.That(bodyRenderer.flipX, Is.True);
                Assert.That(armsRenderer.flipX, Is.True);
                AssertArmedMatchesBodyFrame(armed, bodyRenderer, armsRenderer, 1);

                QueueWorldAim(mouse, nativePresentation, bodyTransform.position,
                    DirectionAtDegrees(-74f, true));
                yield return null;
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-70));
                Assert.That(bodyRenderer.flipX, Is.True);
                Assert.That(armsRenderer.flipX, Is.True);
                AssertArmedMatchesBodyFrame(armed, bodyRenderer, armsRenderer, 1);

                Assert.That(bodyTransform.localPosition, Is.EqualTo(bodyLocalPosition));
                Assert.That(armsTransform.localPosition, Is.EqualTo(armsLocalPosition));
                Assert.That(bodyTransform.localRotation, Is.EqualTo(bodyLocalRotation));
                Assert.That(armsTransform.localRotation, Is.EqualTo(armsLocalRotation));
                Assert.That(bodyTransform.localScale, Is.EqualTo(bodyLocalScale));
                Assert.That(armsTransform.localScale, Is.EqualTo(armsLocalScale));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator Longwatch_ReleasesOutsideIdleAndReacquiresWithCurrentMouseAim()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation nativePresentation =
                Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerLongwatchIdleAimPresenter2D armed =
                motor?.GetComponent<PlayerLongwatchIdleAimPresenter2D>();
            PlayerUnarmedArmsPresenter2D unarmed = motor?.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerAnimator2D playerAnimator = motor?.GetComponent<PlayerAnimator2D>();
            Transform bodyTransform = motor?.transform.Find("Visual - 48x64 Full Cell/BodySpriteRenderer");
            SpriteRenderer bodyRenderer = bodyTransform?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = motor?.transform
                .Find("Visual - 48x64 Full Cell/ArmsWeaponSpriteRenderer")?.GetComponent<SpriteRenderer>();

            Assert.That(motor, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(armed, Is.Not.Null);
            Assert.That(unarmed, Is.Not.Null);
            Assert.That(playerAnimator, Is.Not.Null);
            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SettlePlayer();
                QueueWorldAim(mouse, nativePresentation, bodyTransform.position,
                    DirectionAtDegrees(62f, true));
                yield return WaitForArmedIdle(armed, 20);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(60));
                Assert.That(armed.Selection.FlipX, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForAnimationState(playerAnimator, PlayerAnimationState.Run, 90);
                AssertUnarmedOwnership(armed, unarmed, bodyRenderer, armsRenderer);
                Assert.That(bodyRenderer.flipX, Is.False,
                    "Movement-facing did not resume immediately after armed Idle released.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                QueueWorldAim(mouse, nativePresentation, bodyTransform.position,
                    DirectionAtDegrees(-28f, true));
                yield return WaitForArmedIdle(armed, 120);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-30));
                Assert.That(armed.Selection.FlipX, Is.True);
                Assert.That(bodyRenderer.flipX, Is.True);
                Assert.That(armsRenderer.flipX, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                yield return WaitForAnimationState(playerAnimator, PlayerAnimationState.Jump, 60);
                AssertUnarmedOwnership(armed, unarmed, bodyRenderer, armsRenderer);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return WaitForAnimationState(playerAnimator, PlayerAnimationState.Fall, 240);
                AssertUnarmedOwnership(armed, unarmed, bodyRenderer, armsRenderer);
                yield return WaitForAnimationState(playerAnimator, PlayerAnimationState.Land, 240);
                AssertUnarmedOwnership(armed, unarmed, bodyRenderer, armsRenderer);
                yield return WaitForArmedIdle(armed, 120);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-30));
                Assert.That(armed.Selection.FlipX, Is.True);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
            }
        }

        private static IEnumerator SettlePlayer()
        {
            for (int index = 0; index < 30; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return null;
        }

        private static IEnumerator WaitForArmedIdle(
            PlayerLongwatchIdleAimPresenter2D armed,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                if (armed.OwnsRenderer)
                {
                    yield break;
                }
            }

            Assert.Fail("Longwatch presenter did not acquire the Idle overlay renderer.");
        }

        private static IEnumerator WaitForAnimationState(
            PlayerAnimator2D playerAnimator,
            PlayerAnimationState expected,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                if (playerAnimator.CurrentState == expected)
                {
                    yield break;
                }
            }

            Assert.Fail("Player presentation did not enter " + expected + ".");
        }

        private static void QueueWorldAim(
            Mouse mouse,
            NativePixelPresentation presentation,
            Vector3 bodyPivotWorld,
            Vector2 worldDirection)
        {
            Vector3 targetViewport = presentation.WorldCamera.WorldToViewportPoint(
                bodyPivotWorld + (Vector3)(worldDirection.normalized * 8f));
            NativePixelViewport viewport = presentation.Viewport;
            Vector2 physicalPosition = new Vector2(
                viewport.OutputOffsetX + targetViewport.x * viewport.LogicalWidth * viewport.IntegerScale,
                viewport.OutputOffsetY + targetViewport.y * viewport.LogicalHeight * viewport.IntegerScale);
            InputSystem.QueueStateEvent(mouse, new MouseState { position = physicalPosition });
            InputSystem.Update();
        }

        private static Vector2 DirectionAtDegrees(float authoredAngleDegrees, bool left)
        {
            float radians = authoredAngleDegrees * Mathf.Deg2Rad;
            float x = Mathf.Cos(radians);
            return new Vector2(left ? -x : x, Mathf.Sin(radians));
        }

        private static void AssertArmedMatchesBodyFrame(
            PlayerLongwatchIdleAimPresenter2D armed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int expectedBodyFrame)
        {
            Assert.That(bodyRenderer.sprite, Is.SameAs(armed.GetBodyIdleFrame(expectedBodyFrame)));
            LongwatchIdleAimPose pose = armed.GetAimPose(armed.Selection.DirectionIndex);
            Assert.That(armsRenderer.sprite, Is.SameAs(pose.GetFrame(expectedBodyFrame)),
                "Longwatch overlay did not match the final displayed Body Idle frame.");
        }

        private static void AssertUnarmedOwnership(
            PlayerLongwatchIdleAimPresenter2D armed,
            PlayerUnarmedArmsPresenter2D unarmed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            Assert.That(armed.OwnsRenderer, Is.False);
            Assert.That(unarmed.OwnsRenderer, Is.True);
            Assert.That(unarmed.TryGetArmsSprite(bodyRenderer.sprite, out Sprite expectedArms), Is.True);
            Assert.That(armsRenderer.sprite, Is.SameAs(expectedArms));
            Assert.That(bodyRenderer.flipX, Is.EqualTo(armsRenderer.flipX));
        }
    }
}
