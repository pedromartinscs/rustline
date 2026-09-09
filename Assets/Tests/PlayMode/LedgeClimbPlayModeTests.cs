using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class LedgeClimbPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator LeftPlatformEdge_CompletesCommittedRightFacingClimbWithoutLandOrFire()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = motor.GetComponent<CapsuleCollider2D>();
            PlayerAim2D aim = motor.GetComponent<PlayerAim2D>();
            PlayerAnimator2D playerAnimator = motor.GetComponent<PlayerAnimator2D>();
            PlayerUnarmedArmsPresenter2D unarmed = motor.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerLongwatchAimPresenter2D longwatch = motor.GetComponent<PlayerLongwatchAimPresenter2D>();
            PlayerWeaponController2D weapon = motor.GetComponent<PlayerWeaponController2D>();
            Transform visual = motor.transform.Find("Visual - 48x64 Full Cell");
            SpriteRenderer bodyRenderer = visual.Find("BodySpriteRenderer").GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = visual.Find("ArmsWeaponSpriteRenderer").GetComponent<SpriteRenderer>();
            Animator animator = bodyRenderer.GetComponent<Animator>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            int landEvents = 0;
            int climbEvents = 0;
            motor.Landed += CountLand;
            motor.LedgeClimbStarted += CountClimb;
            try
            {
                aim.enabled = false;
                Assert.That(aim.ApplyWorldAimVector(Vector2.right), Is.True);
                body.position = new Vector2(9f - 16.5f / 16f, 2f - 40.5f / 16f);
                body.linearVelocity = new Vector2(2f, -1f);
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();

                yield return WaitForClimb(motor, 1, 4);
                yield return null;
                Assert.That(climbEvents, Is.EqualTo(1));
                Assert.That(motor.IsWallBraced, Is.False);
                Assert.That(collider.enabled, Is.False);
                Assert.That(playerAnimator.CurrentState, Is.EqualTo(PlayerAnimationState.LedgeClimb));
                Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName("LedgeClimb"), Is.True);
                Assert.That(bodyRenderer.sprite.name, Does.StartWith("player_salvager_body_ledge_climb_"));
                Assert.That(unarmed.TryGetArmsSprite(bodyRenderer.sprite, out Sprite expectedArms), Is.True);
                Assert.That(armsRenderer.sprite, Is.SameAs(expectedArms));
                Assert.That(longwatch.OwnsRenderer, Is.False);
                Assert.That(unarmed.OwnsRenderer, Is.True);
                Assert.That(weapon.TryFire(Time.time), Is.False);

                Vector2 capture = motor.LedgeCaptureRootPosition;
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.LedgeClimbFrameIndex, Is.EqualTo(1));
                AssertVector(body.position, capture + new Vector2(0.5f, 0.5f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                Assert.That(aim.ApplyWorldAimVector(Vector2.left), Is.True);
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(motor.IsLedgeClimbing, Is.True,
                    "Released movement input canceled the committed climb.");
                Assert.That(bodyRenderer.flipX, Is.False,
                    "Mouse aim changed the captured right-side climb facing.");

                yield return WaitForClimbCompletion(motor, 40);
                yield return null;
                AssertVector(body.position, new Vector2(9f + 1.05f * 0.5f + 1f / 16f, 2f + 1f / 16f));
                Assert.That(collider.enabled, Is.True);
                Assert.That(collider.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
                Assert.That(collider.offset, Is.EqualTo(new Vector2(0f, 1.375f)));
                Assert.That(body.linearVelocity, Is.EqualTo(Vector2.zero));
                Assert.That(motor.IsGrounded, Is.True);
                Assert.That(motor.IsLedgeClimbing, Is.False);
                Assert.That(landEvents, Is.Zero);
                Assert.That(playerAnimator.CurrentState,
                    Is.EqualTo(PlayerAnimationState.Idle).Or.EqualTo(PlayerAnimationState.Run));
            }
            finally
            {
                motor.Landed -= CountLand;
                motor.LedgeClimbStarted -= CountClimb;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }

            void CountLand() => landEvents++;
            void CountClimb() => climbEvents++;
        }

        [UnityTest]
        public IEnumerator RightPlatformEdge_MirrorsOnlyHorizontalTrajectoryAndFacing()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            PlayerAim2D aim = motor.GetComponent<PlayerAim2D>();
            SpriteRenderer bodyRenderer = motor.transform.Find(
                "Visual - 48x64 Full Cell/BodySpriteRenderer").GetComponent<SpriteRenderer>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                aim.enabled = false;
                Assert.That(aim.ApplyWorldAimVector(Vector2.left), Is.True);
                body.position = new Vector2(18f + 16.5f / 16f, 2f - 40.5f / 16f);
                body.linearVelocity = new Vector2(-2f, -1f);
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();

                yield return WaitForClimb(motor, -1, 4);
                yield return null;
                Vector2 capture = motor.LedgeCaptureRootPosition;
                Assert.That(bodyRenderer.flipX, Is.True);
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                AssertVector(body.position, capture + new Vector2(-0.5f, 0.5f));

                yield return WaitForClimbCompletion(motor, 40);
                AssertVector(body.position, new Vector2(18f - 1.05f * 0.5f - 1f / 16f, 2f + 1f / 16f));
                Assert.That(motor.IsGrounded, Is.True);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator BackpedalOffRightEdge_RequiresReverseInputButNotNegativeVelocity()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            PlayerAim2D aim = motor.GetComponent<PlayerAim2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                aim.enabled = false;
                Assert.That(aim.ApplyWorldAimVector(Vector2.left), Is.True);
                body.position = new Vector2(17.2f, 2.0625f);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();

                for (int index = 0; index < 120 && (motor.IsGrounded || body.linearVelocity.y >= -0.1f); index++)
                {
                    yield return new WaitForFixedUpdate();
                    Assert.That(motor.IsLedgeClimbing, Is.False,
                        "Input away from the left-side ledge incorrectly started a climb.");
                }
                Assert.That(motor.IsGrounded, Is.False);
                Assert.That(body.linearVelocity.y, Is.LessThan(-0.1f));
                Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsLedgeClimbing, Is.False,
                    "Neutral input incorrectly magnetized the player to the ledge.");
                Assert.That(body.linearVelocity.x, Is.GreaterThan(0f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                yield return WaitForClimb(motor, -1, 80);
                Assert.That(body.linearVelocity.x, Is.Zero,
                    "Committed climb did not zero the still-positive backpedal momentum.");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator FacingMismatchAndBlockedDestination_RejectWithoutDisablingCollider()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = motor.GetComponent<CapsuleCollider2D>();
            PlayerAim2D aim = motor.GetComponent<PlayerAim2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            GameObject obstruction = null;
            try
            {
                aim.enabled = false;
                Assert.That(aim.ApplyWorldAimVector(Vector2.left), Is.True);
                body.position = new Vector2(9f - 16.5f / 16f, 2f - 40.5f / 16f);
                body.linearVelocity = new Vector2(0f, -1f);
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsLedgeClimbing, Is.False);
                Assert.That(collider.enabled, Is.True);

                Assert.That(aim.ApplyWorldAimVector(Vector2.right), Is.True);
                obstruction = new GameObject("Ledge destination obstruction - Test");
                obstruction.layer = 6;
                BoxCollider2D blocker = obstruction.AddComponent<BoxCollider2D>();
                blocker.size = new Vector2(1f, 1f);
                obstruction.transform.position = new Vector3(9.6f, 3.5f, 0f);
                body.position = new Vector2(9f - 16.5f / 16f, 2f - 40.5f / 16f);
                body.linearVelocity = new Vector2(0f, -1f);
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsLedgeClimbing, Is.False);
                Assert.That(collider.enabled, Is.True);
            }
            finally
            {
                if (obstruction != null)
                {
                    Object.Destroy(obstruction);
                }
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator ResetAfterRespawn_CancelsClimbAndRestoresStandingCollider()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = motor.GetComponent<CapsuleCollider2D>();
            PlayerAim2D aim = motor.GetComponent<PlayerAim2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                aim.enabled = false;
                Assert.That(aim.ApplyWorldAimVector(Vector2.right), Is.True);
                body.position = new Vector2(9f - 16.5f / 16f, 2f - 40.5f / 16f);
                body.linearVelocity = new Vector2(0f, -1f);
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForClimb(motor, 1, 4);

                motor.ResetAfterRespawn();
                Assert.That(motor.IsLedgeClimbing, Is.False);
                Assert.That(motor.IsWallBraced, Is.False);
                Assert.That(motor.WallKickLockRemaining, Is.Zero);
                Assert.That(collider.enabled, Is.True);
                Assert.That(collider.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
                Assert.That(collider.offset, Is.EqualTo(new Vector2(0f, 1.375f)));
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private static IEnumerator WaitForClimb(
            PlayerMotor2D motor,
            int expectedSide,
            int maximumFixedFrames)
        {
            for (int index = 0; index < maximumFixedFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                if (motor.IsLedgeClimbing && motor.LedgeSide == expectedSide)
                {
                    yield break;
                }
            }

            Assert.Fail(
                $"Ledge climb side {expectedSide} did not start. " +
                $"position={motor.transform.position}, velocity={motor.Velocity}, " +
                $"grounded={motor.IsGrounded}, braced={motor.IsWallBraced}.");
        }

        private static IEnumerator WaitForClimbCompletion(PlayerMotor2D motor, int maximumFixedFrames)
        {
            for (int index = 0; index < maximumFixedFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                if (!motor.IsLedgeClimbing)
                {
                    yield break;
                }
            }

            Assert.Fail("Committed ledge climb did not complete automatically.");
        }

        private static void AssertVector(Vector2 actual, Vector2 expected)
        {
            Assert.That(actual.x, Is.EqualTo(expected.x).Within(0.002f));
            Assert.That(actual.y, Is.EqualTo(expected.y).Within(0.002f));
        }
    }
}
