using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Player;
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
    }
}
