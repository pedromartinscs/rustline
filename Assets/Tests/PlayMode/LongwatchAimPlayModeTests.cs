using System.Collections;
using System.Reflection;
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
    public sealed class LongwatchAimPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator AimDirtyState_RefreshesForEveryExactConversionInput()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerAim2D aim = Object.FindAnyObjectByType<PlayerAim2D>();
            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            Assert.That(aim, Is.Not.Null);
            Assert.That(presentation, Is.Not.Null);

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return SettlePlayer();
                QueueWorldAim(mouse, presentation, aim.AimOriginWorld, DirectionAtDegrees(27f, false));
                yield return null;

                Vector2 stationaryDirection = aim.ContinuousAimDirection;
                bool stationaryFacing = aim.FacingLeft;
                uint stationaryRevision = aim.AimRevision;
                aim.RefreshAim();
                aim.RefreshAim();
                Assert.That(aim.ContinuousAimDirection, Is.EqualTo(stationaryDirection));
                Assert.That(aim.FacingLeft, Is.EqualTo(stationaryFacing));
                Assert.That(aim.AimRevision, Is.EqualTo(stationaryRevision));

                QueueWorldAim(mouse, presentation, aim.AimOriginWorld, DirectionAtDegrees(-31f, false));
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(stationaryRevision),
                    "Pointer movement did not refresh aim immediately.");

                uint pointerRevision = aim.AimRevision;
                Vector3 originalPlayerPosition = aim.transform.position;
                aim.transform.position = originalPlayerPosition + new Vector3(0.125f, 0f, 0f);
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(pointerRevision),
                    "Player movement did not refresh aim.");
                aim.transform.position = originalPlayerPosition;
                aim.RefreshAim();

                Camera worldCamera = presentation.WorldCamera;
                uint originRevision = aim.AimRevision;
                Vector3 originalCameraPosition = worldCamera.transform.position;
                worldCamera.transform.position = originalCameraPosition + new Vector3(0.125f, 0f, 0f);
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(originRevision),
                    "Camera movement did not refresh aim.");
                worldCamera.transform.position = originalCameraPosition;
                aim.RefreshAim();

                uint cameraPositionRevision = aim.AimRevision;
                Quaternion originalCameraRotation = worldCamera.transform.rotation;
                worldCamera.transform.rotation = Quaternion.Euler(0f, 0f, 0.125f) * originalCameraRotation;
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(cameraPositionRevision),
                    "Camera rotation did not refresh aim.");
                worldCamera.transform.rotation = originalCameraRotation;
                aim.RefreshAim();

                uint cameraRevision = aim.AimRevision;
                float originalOrthographicSize = worldCamera.orthographicSize;
                worldCamera.orthographicSize = originalOrthographicSize + 0.125f;
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(cameraRevision),
                    "Camera projection changes did not refresh aim.");
                worldCamera.orthographicSize = originalOrthographicSize;
                aim.RefreshAim();

                FieldInfo viewportField = typeof(NativePixelPresentation).GetField(
                    "_viewport",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(viewportField, Is.Not.Null);
                NativePixelViewport originalViewport = presentation.Viewport;
                NativePixelViewport shiftedViewport = new NativePixelViewport(
                    originalViewport.PhysicalWidth,
                    originalViewport.PhysicalHeight,
                    originalViewport.IntegerScale,
                    originalViewport.LogicalWidth,
                    originalViewport.LogicalHeight,
                    originalViewport.OutputWidth,
                    originalViewport.OutputHeight,
                    originalViewport.OutputOffsetX + 1,
                    originalViewport.OutputOffsetY);
                uint projectionRevision = aim.AimRevision;
                viewportField.SetValue(presentation, shiftedViewport);
                aim.RefreshAim();
                Assert.That(aim.AimRevision, Is.GreaterThan(projectionRevision),
                    "Native viewport changes did not refresh aim.");
                viewportField.SetValue(presentation, originalViewport);
                aim.RefreshAim();
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator IdleLongwatch_UsesCorrectedAimOriginAndFollowsBodyWithoutDrift()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerLongwatchAimPresenter2D armed = motor?.GetComponent<PlayerLongwatchAimPresenter2D>();
            PlayerAim2D playerAim = motor?.GetComponent<PlayerAim2D>();
            PlayerUnarmedArmsPresenter2D unarmed = motor?.GetComponent<PlayerUnarmedArmsPresenter2D>();
            Transform bodyTransform = motor?.transform.Find("Visual - 48x64 Full Cell/BodySpriteRenderer");
            Transform armsTransform = motor?.transform.Find("Visual - 48x64 Full Cell/ArmsWeaponSpriteRenderer");
            SpriteRenderer bodyRenderer = bodyTransform?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsTransform?.GetComponent<SpriteRenderer>();
            Animator animator = bodyTransform?.GetComponent<Animator>();

            Assert.That(motor, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(armed, Is.Not.Null);
            Assert.That(playerAim, Is.Not.Null);
            Assert.That(unarmed, Is.Not.Null);
            Assert.That(armed.PlayerAim, Is.SameAs(playerAim));
            Assert.That(playerAim.NativePixelPresentation, Is.SameAs(nativePresentation));
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
                Assert.That(armed.AimOriginWorld - bodyTransform.position,
                    Is.EqualTo(Vector3.up * 2.375f));

                // A horizontal target from the corrected origin would select roughly +20 degrees
                // if the old Body pivot were still being used.
                QueueWorldAim(mouse, nativePresentation, armed.AimOriginWorld, Vector2.right);
                yield return WaitForArmedState(armed, PlayerAnimationState.Idle, 20);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(0));
                Assert.That(armed.ContinuousAimDirection.y, Is.EqualTo(0f).Within(0.001f));
                Assert.That(unarmed.OwnsRenderer, Is.False);
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);

                animator.Play("Idle", 0, 0f);
                animator.Update(0f);
                yield return null;
                AssertArmedMatchesIdleBodyFrame(armed, bodyRenderer, armsRenderer, 0);

                animator.Play("Idle", 0, 0.6f);
                animator.Update(0f);
                yield return null;
                AssertArmedMatchesIdleBodyFrame(armed, bodyRenderer, armsRenderer, 1);

                QueueWorldAim(mouse, nativePresentation, armed.AimOriginWorld,
                    DirectionAtDegrees(47f, true));
                yield return null;
                yield return null;
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(50));
                Assert.That(bodyRenderer.flipX, Is.True);
                Assert.That(armsRenderer.flipX, Is.True);
                AssertArmedMatchesIdleBodyFrame(armed, bodyRenderer, armsRenderer, 1);

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
        public IEnumerator RunLongwatch_UsesBodyClockAndAimFacingIndependentOfMovement()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerLongwatchAimPresenter2D armed = motor?.GetComponent<PlayerLongwatchAimPresenter2D>();
            PlayerUnarmedArmsPresenter2D unarmed = motor?.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerAnimator2D playerAnimator = motor?.GetComponent<PlayerAnimator2D>();
            Transform bodyTransform = motor?.transform.Find("Visual - 48x64 Full Cell/BodySpriteRenderer");
            SpriteRenderer bodyRenderer = bodyTransform?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = motor?.transform
                .Find("Visual - 48x64 Full Cell/ArmsWeaponSpriteRenderer")?.GetComponent<SpriteRenderer>();
            Animator animator = bodyTransform?.GetComponent<Animator>();

            Assert.That(motor, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(armed, Is.Not.Null);
            Assert.That(unarmed, Is.Not.Null);
            Assert.That(playerAnimator, Is.Not.Null);
            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SettlePlayer();
                QueueWorldAim(mouse, nativePresentation, armed.AimOriginWorld,
                    DirectionAtDegrees(34f, false));
                yield return WaitForArmedState(armed, PlayerAnimationState.Idle, 20);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(30));

                // Physically run right while the aim-facing remains right.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForArmedState(armed, PlayerAnimationState.Run, 90);
                Assert.That(motor.Velocity.x, Is.GreaterThan(0f));
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);
                Assert.That(armed.OwnsRenderer, Is.True);
                Assert.That(unarmed.OwnsRenderer, Is.False);

                animator.speed = 0f;
                for (int frameIndex = 0; frameIndex < 6; frameIndex++)
                {
                    SetAnimatorFrame(animator, "Run", frameIndex, 6);
                    yield return null;
                    AssertArmedMatchesRunBodyFrame(armed, bodyRenderer, armsRenderer, frameIndex);
                }

                SetAnimatorFrame(animator, "Run", 3, 6);
                yield return null;
                Sprite bodyFrame3 = bodyRenderer.sprite;
                QueueWorldAim(mouse, nativePresentation, armed.AimOriginWorld,
                    DirectionAtDegrees(-47f, false));
                yield return null;
                yield return null;
                Assert.That(bodyRenderer.sprite, Is.SameAs(bodyFrame3),
                    "Changing aim must not change the Body animation frame.");
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-50));
                AssertArmedMatchesRunBodyFrame(armed, bodyRenderer, armsRenderer, 3);

                // This portion verifies locomotion independence, not stationary-screen-pointer
                // parallax while both player and camera continue moving. Freeze the already
                // resolved continuous world aim so frame scheduling cannot move it across a
                // quantization boundary during the later deceleration waits.
                armed.PlayerAim.enabled = false;

                SetAnimatorFrame(animator, "Run", 5, 6);
                yield return null;
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-50));
                AssertArmedMatchesRunBodyFrame(armed, bodyRenderer, armsRenderer, 5);

                // Reverse physical movement while keeping right aim-facing.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                yield return WaitForArmedState(armed, PlayerAnimationState.Backpedal, 90);
                Assert.That(motor.Velocity.x, Is.LessThan(0f));
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-50));
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);

                animator.speed = 0f;
                for (int frameIndex = 0; frameIndex < 4; frameIndex++)
                {
                    SetAnimatorFrame(animator, "Backpedal", frameIndex, 4);
                    yield return null;
                    AssertArmedMatchesBackpedalBodyFrame(armed, bodyRenderer, armsRenderer, frameIndex);
                }

                // Idle and Run share the same continuous selection without resetting it.
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return WaitForArmedState(armed, PlayerAnimationState.Idle, 120);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-50));
                Assert.That(armed.Selection.FlipX, Is.False);
                AssertArmedMatchesCurrentBody(armed, bodyRenderer, armsRenderer);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForArmedState(armed, PlayerAnimationState.Run, 90);
                Assert.That(armed.Selection.AuthoredAngleDegrees, Is.EqualTo(-50));
                Assert.That(armed.Selection.FlipX, Is.False);
                AssertArmedMatchesCurrentBody(armed, bodyRenderer, armsRenderer);
            }
            finally
            {
                armed.PlayerAim.enabled = true;
                animator.speed = 1f;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator Longwatch_ReleasesForJumpFallAndLandThenReacquires()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerLongwatchAimPresenter2D armed = motor?.GetComponent<PlayerLongwatchAimPresenter2D>();
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

            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SettlePlayer();
                QueueWorldAim(mouse, nativePresentation, armed.AimOriginWorld,
                    DirectionAtDegrees(-28f, true));
                yield return WaitForArmedState(armed, PlayerAnimationState.Idle, 20);

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
                yield return WaitForArmedState(armed, PlayerAnimationState.Idle, 120);
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

        [UnityTest]
        public IEnumerator GroundedAimRelativeLocomotion_UsesSevenForwardAndFourBackpedal()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerLongwatchAimPresenter2D armed = motor?.GetComponent<PlayerLongwatchAimPresenter2D>();
            Rigidbody2D body = motor?.GetComponent<Rigidbody2D>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SettlePlayer();
                yield return AssertGroundedLocomotionCase(
                    motor, body, armed, presentation, mouse, keyboard,
                    Vector2.right, Key.D, 7f, PlayerAnimationState.Run);
                yield return AssertGroundedLocomotionCase(
                    motor, body, armed, presentation, mouse, keyboard,
                    Vector2.right, Key.A, -4f, PlayerAnimationState.Backpedal);
                yield return AssertGroundedLocomotionCase(
                    motor, body, armed, presentation, mouse, keyboard,
                    Vector2.left, Key.A, -7f, PlayerAnimationState.Run);
                yield return AssertGroundedLocomotionCase(
                    motor, body, armed, presentation, mouse, keyboard,
                    Vector2.left, Key.D, 4f, PlayerAnimationState.Backpedal);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator VerticalAimHysteresis_DoesNotChatterAcrossUpOrDownAxis()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerAim2D aim = Object.FindAnyObjectByType<PlayerAim2D>();
            PlayerAnimator2D playerAnimator = aim?.GetComponent<PlayerAnimator2D>();
            Assert.That(aim, Is.Not.Null);
            Assert.That(playerAnimator, Is.Not.Null);
            yield return SettlePlayer();
            aim.enabled = false;

            Vector2 rightOfUp = DirectionAtDegrees(84f, false);
            Vector2 leftNearUp = DirectionAtDegrees(88f, true);
            aim.ApplyWorldAimVector(rightOfUp);
            yield return null;
            Assert.That(aim.FacingLeft, Is.False);
            aim.ApplyWorldAimVector(leftNearUp);
            yield return null;
            Assert.That(aim.FacingLeft, Is.False, "Facing chattered inside the +90 degree zone.");
            Assert.That(aim.ContinuousAimDirection, Is.EqualTo(leftNearUp.normalized));
            aim.ApplyWorldAimVector(DirectionAtDegrees(84f, true));
            yield return null;
            Assert.That(aim.FacingLeft, Is.True);

            Vector2 rightNearDown = DirectionAtDegrees(-88f, false);
            aim.ApplyWorldAimVector(rightNearDown);
            yield return null;
            Assert.That(aim.FacingLeft, Is.True, "Facing chattered inside the -90 degree zone.");
            Assert.That(aim.ContinuousAimDirection, Is.EqualTo(rightNearDown.normalized));
            aim.ApplyWorldAimVector(DirectionAtDegrees(-84f, false));
            yield return null;
            Assert.That(aim.FacingLeft, Is.False);
        }

        [UnityTest]
        public IEnumerator HemisphereChangeWhileMoving_TransitionsNaturallyToBackpedalCap()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            PlayerAim2D aim = motor?.GetComponent<PlayerAim2D>();
            PlayerAnimator2D playerAnimator = motor?.GetComponent<PlayerAnimator2D>();
            Rigidbody2D body = motor?.GetComponent<Rigidbody2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return SettlePlayer();
                aim.enabled = false;
                aim.ApplyWorldAimVector(Vector2.right);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 40; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.Velocity.x, Is.EqualTo(7f).Within(0.05f));

                body.position = new Vector2(-22f, 0.05f);
                Physics2D.SyncTransforms();
                aim.ApplyWorldAimVector(Vector2.left);
                bool sawIntermediateVelocity = false;
                for (int index = 0; index < 20; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    sawIntermediateVelocity |= motor.Velocity.x > 4.05f && motor.Velocity.x < 6.95f;
                }

                Assert.That(sawIntermediateVelocity, Is.True,
                    "Facing change snapped velocity directly to the Backpedal cap.");
                Assert.That(motor.Velocity.x, Is.EqualTo(4f).Within(0.05f));
                Assert.That(playerAnimator.CurrentState, Is.EqualTo(PlayerAnimationState.Backpedal));
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
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

        private static IEnumerator AssertGroundedLocomotionCase(
            PlayerMotor2D motor,
            Rigidbody2D body,
            PlayerLongwatchAimPresenter2D armed,
            NativePixelPresentation presentation,
            Mouse mouse,
            Keyboard keyboard,
            Vector2 aimDirection,
            Key movementKey,
            float expectedVelocity,
            PlayerAnimationState expectedState)
        {
            body.position = new Vector2(-22f, 0.05f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            InputSystem.QueueStateEvent(keyboard, new KeyboardState());
            InputSystem.Update();
            yield return new WaitForFixedUpdate();
            QueueWorldAim(mouse, presentation, armed.AimOriginWorld, aimDirection);
            yield return null;
            yield return null;
            InputSystem.QueueStateEvent(keyboard, new KeyboardState(movementKey));
            InputSystem.Update();
            for (int index = 0; index < 50; index++)
            {
                yield return new WaitForFixedUpdate();
            }
            yield return null;

            Assert.That(motor.IsGrounded, Is.True);
            Assert.That(motor.Velocity.x, Is.EqualTo(expectedVelocity).Within(0.08f));
            Assert.That(armed.PlayerAnimator.CurrentState, Is.EqualTo(expectedState));
            Assert.That(armed.OwnsRenderer, Is.True);
        }

        private static IEnumerator WaitForArmedState(
            PlayerLongwatchAimPresenter2D armed,
            PlayerAnimationState expectedState,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                if (armed.OwnsRenderer && armed.PlayerAnimator.CurrentState == expectedState)
                {
                    yield break;
                }
            }

            Assert.Fail("Longwatch presenter did not acquire the overlay renderer for " + expectedState + ".");
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

        private static void SetAnimatorFrame(Animator animator, string state, int frameIndex, int frameCount)
        {
            float normalizedTime = (frameIndex + 0.01f) / frameCount;
            animator.Play(state, 0, normalizedTime);
            animator.Update(0f);
        }

        private static void QueueWorldAim(
            Mouse mouse,
            NativePixelPresentation presentation,
            Vector3 aimOriginWorld,
            Vector2 worldDirection)
        {
            Vector3 targetViewport = presentation.WorldCamera.WorldToViewportPoint(
                aimOriginWorld + (Vector3)(worldDirection.normalized * 8f));
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

        private static void AssertArmedMatchesIdleBodyFrame(
            PlayerLongwatchAimPresenter2D armed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int expectedBodyFrame)
        {
            Assert.That(bodyRenderer.sprite, Is.SameAs(armed.GetBodyIdleFrame(expectedBodyFrame)));
            LongwatchIdleAimPose pose = armed.GetIdleAimPose(armed.Selection.DirectionIndex);
            Assert.That(armsRenderer.sprite, Is.SameAs(pose.GetFrame(expectedBodyFrame)),
                "Longwatch overlay did not match the final displayed Body Idle frame.");
        }

        private static void AssertArmedMatchesRunBodyFrame(
            PlayerLongwatchAimPresenter2D armed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int expectedBodyFrame)
        {
            Assert.That(bodyRenderer.sprite, Is.SameAs(armed.GetBodyRunFrame(expectedBodyFrame)));
            LongwatchRunAimPose pose = armed.GetRunAimPose(armed.Selection.DirectionIndex);
            Assert.That(armsRenderer.sprite, Is.SameAs(pose.GetFrame(expectedBodyFrame)),
                "Longwatch overlay did not match the final displayed Body Run frame.");
        }

        private static void AssertArmedMatchesBackpedalBodyFrame(
            PlayerLongwatchAimPresenter2D armed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int expectedBodyFrame)
        {
            Assert.That(bodyRenderer.sprite, Is.SameAs(armed.GetBodyBackpedalFrame(expectedBodyFrame)));
            LongwatchBackpedalAimPose pose = armed.GetBackpedalAimPose(armed.Selection.DirectionIndex);
            Assert.That(armsRenderer.sprite, Is.SameAs(pose.GetFrame(expectedBodyFrame)),
                "Longwatch overlay did not match the final displayed Body Backpedal frame.");
        }

        private static void AssertArmedMatchesCurrentBody(
            PlayerLongwatchAimPresenter2D armed,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            for (int frameIndex = 0; frameIndex < armed.BodyIdleFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == armed.GetBodyIdleFrame(frameIndex))
                {
                    AssertArmedMatchesIdleBodyFrame(armed, bodyRenderer, armsRenderer, frameIndex);
                    return;
                }
            }

            for (int frameIndex = 0; frameIndex < armed.BodyRunFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == armed.GetBodyRunFrame(frameIndex))
                {
                    AssertArmedMatchesRunBodyFrame(armed, bodyRenderer, armsRenderer, frameIndex);
                    return;
                }
            }

            for (int frameIndex = 0; frameIndex < armed.BodyBackpedalFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == armed.GetBodyBackpedalFrame(frameIndex))
                {
                    AssertArmedMatchesBackpedalBodyFrame(armed, bodyRenderer, armsRenderer, frameIndex);
                    return;
                }
            }

            Assert.Fail("Displayed Body sprite was not an authored Longwatch Idle or Run frame.");
        }

        private static void AssertUnarmedOwnership(
            PlayerLongwatchAimPresenter2D armed,
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
