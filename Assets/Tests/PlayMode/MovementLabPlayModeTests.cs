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
    public sealed class MovementLabPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator Player_LayeredPresentationStaysSynchronizedAcrossLocomotionStates()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            GameObject player = motor.gameObject;
            Rigidbody2D body = player.GetComponent<Rigidbody2D>();
            CapsuleCollider2D collider = player.GetComponent<CapsuleCollider2D>();
            PlayerUnarmedArmsPresenter2D presenter = player.GetComponent<PlayerUnarmedArmsPresenter2D>();
            PlayerAnimator2D playerAnimator = player.GetComponent<PlayerAnimator2D>();
            Transform visual = player.transform.Find("Visual - 48x64 Full Cell");
            Transform bodyVisual = visual?.Find("BodySpriteRenderer");
            Transform armsVisual = visual?.Find("ArmsWeaponSpriteRenderer");
            SpriteRenderer bodyRenderer = bodyVisual?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsVisual?.GetComponent<SpriteRenderer>();
            Animator animator = bodyVisual?.GetComponent<Animator>();

            Assert.That(body, Is.Not.Null);
            Assert.That(body.gravityScale, Is.EqualTo(0f));
            Assert.That(collider, Is.Not.Null);
            Assert.That(collider.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
            Assert.That(collider.offset, Is.EqualTo(new Vector2(0f, 1.375f)));
            Assert.That(playerAnimator, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(visual, Is.Not.Null);
            Assert.That(bodyVisual, Is.Not.Null);
            Assert.That(armsVisual, Is.Not.Null);
            Assert.That(visual.localPosition, Is.EqualTo(new Vector3(0f, -0.25f, 0f)));
            Assert.That(bodyVisual.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(armsVisual.localPosition, Is.EqualTo(Vector3.zero));
            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);
            Assert.That(bodyRenderer.sortingOrder, Is.EqualTo(10));
            Assert.That(armsRenderer.sortingOrder, Is.EqualTo(11));
            Assert.That(player.GetComponentsInChildren<Animator>(true), Has.Length.EqualTo(1));
            Assert.That(animator, Is.Not.Null);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return null;
                AssertStateAndLayers(animator, "Idle", presenter, bodyRenderer, armsRenderer);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForState(animator, "Run", presenter, bodyRenderer, armsRenderer, 90);
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                for (int index = 0; index < 90 && !bodyRenderer.flipX; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    AssertLayers(presenter, bodyRenderer, armsRenderer);
                }
                Assert.That(bodyRenderer.flipX, Is.True);
                Assert.That(armsRenderer.flipX, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.Space));
                InputSystem.Update();
                bool sawJump = false;
                bool sawFall = false;
                bool sawLand = false;
                for (int index = 0; index < 300 && !sawLand; index++)
                {
                    if (index == 8)
                    {
                        InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                        InputSystem.Update();
                    }

                    yield return new WaitForFixedUpdate();
                    yield return null;
                    AssertLayers(presenter, bodyRenderer, armsRenderer);
                    sawJump |= animator.GetCurrentAnimatorStateInfo(0).IsName("Jump");
                    sawFall |= animator.GetCurrentAnimatorStateInfo(0).IsName("Fall");
                    sawLand |= animator.GetCurrentAnimatorStateInfo(0).IsName("Land");
                }

                Assert.That(sawJump, Is.True, "Jump presentation state was not observed.");
                Assert.That(sawFall, Is.True, "Fall presentation state was not observed.");
                Assert.That(sawLand, Is.True, "Land presentation state was not observed.");
                Assert.That(bodyRenderer.flipX, Is.EqualTo(armsRenderer.flipX));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Player_JumpTakeoffHoldsFinalLayeredFrameUntilFall()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            Transform visual = motor.transform.Find("Visual - 48x64 Full Cell");
            Transform bodyVisual = visual?.Find("BodySpriteRenderer");
            Transform armsVisual = visual?.Find("ArmsWeaponSpriteRenderer");
            SpriteRenderer bodyRenderer = bodyVisual?.GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = armsVisual?.GetComponent<SpriteRenderer>();
            Animator animator = bodyVisual?.GetComponent<Animator>();
            PlayerUnarmedArmsPresenter2D presenter = motor.GetComponent<PlayerUnarmedArmsPresenter2D>();

            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(motor.IsGrounded, Is.True, "Player did not settle before the jump test.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();

                Sprite heldBodySprite = null;
                Sprite heldArmsSprite = null;
                int heldAscendingFrames = 0;
                bool sawJump = false;
                bool sawFall = false;
                for (int index = 0; index < 300 && !sawFall; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;

                    AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);
                    if (state.IsName("Jump"))
                    {
                        sawJump = true;
                        AssertLayers(presenter, bodyRenderer, armsRenderer);
                        if (bodyRenderer.sprite.name == "player_salvager_body_jump_2")
                        {
                            heldBodySprite ??= bodyRenderer.sprite;
                            heldArmsSprite ??= armsRenderer.sprite;
                            Assert.That(bodyRenderer.sprite, Is.SameAs(heldBodySprite),
                                "Jump presentation regressed after reaching its final takeoff frame.");
                            Assert.That(armsRenderer.sprite, Is.SameAs(heldArmsSprite),
                                "Arms presentation changed while the final takeoff frame was held.");
                            if (motor.Velocity.y > 0.15f)
                            {
                                heldAscendingFrames++;
                            }
                        }
                        else if (heldBodySprite != null)
                        {
                            Assert.Fail("Jump presentation left its final frame before entering Fall.");
                        }
                    }
                    else if (state.IsName("Fall"))
                    {
                        sawFall = true;
                        AssertLayers(presenter, bodyRenderer, armsRenderer);
                        Assert.That(bodyRenderer.sprite.name, Is.EqualTo("player_salvager_body_fall_0"));
                    }
                }

                Assert.That(sawJump, Is.True, "Jump presentation state was not observed.");
                Assert.That(heldBodySprite, Is.Not.Null, "The final jump takeoff frame was not observed.");
                Assert.That(heldAscendingFrames, Is.GreaterThanOrEqualTo(4),
                    "The final layered takeoff frame was not held through sustained ascent.");
                Assert.That(sawFall, Is.True, "Fall did not take ownership after ascent ended.");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Player_MovesJumpsAndRespawnsInMovementLab()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            PlayerInputReader input = motor.GetComponent<PlayerInputReader>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();

            try
            {
                for (int index = 0; index < 20; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(motor.IsGrounded, Is.True, "Player did not settle on the starting platform.");
                float initialX = body.position.x;

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return null;
                Assert.That(input.MoveX, Is.GreaterThan(0.5f),
                    "The MovementLab input map did not receive keyboard movement.");
                for (int index = 0; index < 20; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(body.position.x, Is.GreaterThan(initialX + 0.5f),
                    "Horizontal input did not move the player.");

                float groundedY = body.position.y;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
                InputSystem.Update();
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(body.position.y, Is.GreaterThan(groundedY + 0.25f),
                    "Jump input did not launch the player.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                body.position = new Vector2(body.position.x, -20f);
                body.linearVelocity = Vector2.zero;
                for (int index = 0; index < 3; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(body.position.x, Is.EqualTo(-27f).Within(0.1f));
                Assert.That(body.position.y, Is.GreaterThan(-1f), "Failsafe respawn did not recover the player.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        private static IEnumerator WaitForState(
            Animator animator,
            string state,
            PlayerUnarmedArmsPresenter2D presenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                AssertLayers(presenter, bodyRenderer, armsRenderer);
                if (animator.GetCurrentAnimatorStateInfo(0).IsName(state))
                {
                    yield break;
                }
            }

            Assert.Fail("Animator did not enter state " + state + ".");
        }

        private static void AssertStateAndLayers(
            Animator animator,
            string state,
            PlayerUnarmedArmsPresenter2D presenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state), Is.True);
            AssertLayers(presenter, bodyRenderer, armsRenderer);
        }

        private static void AssertLayers(
            PlayerUnarmedArmsPresenter2D presenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            Assert.That(bodyRenderer.flipX, Is.EqualTo(armsRenderer.flipX), "Layer facing diverged.");
            Assert.That(presenter.TryGetArmsSprite(bodyRenderer.sprite, out Sprite expectedArms), Is.True,
                "Displayed Body sprite is missing from the explicit arms mapping.");
            Assert.That(armsRenderer.sprite, Is.SameAs(expectedArms),
                "Arms overlay lagged or diverged from the displayed Body frame.");
        }
    }
}
