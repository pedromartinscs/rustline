using System.Collections;
using NUnit.Framework;
using Rustline.Diagnostics;
using Rustline.Gameplay.Player;
using Rustline.Physics;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class MovementLabPlayModeTests : InputTestFixture
    {
        [UnityTest]
        public IEnumerator PerformanceHud_IsOptInAndPenumbraToggleWorksWhileHidden()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            NativePixelPresentation presentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            Assert.That(presentation, Is.Not.Null);
            MovementLabPerformanceHud hud = Object.FindAnyObjectByType<MovementLabPerformanceHud>();
            GameObject temporaryHudObject = null;
            if (hud == null)
            {
                temporaryHudObject = new GameObject("Performance HUD - Test");
                hud = temporaryHudObject.AddComponent<MovementLabPerformanceHud>();
            }

            Assert.That(hud.IsVisible, Is.False);

            yield return null;
            yield return null;
            Assert.That(hud.SampleFrameCount, Is.EqualTo(0),
                "Hidden HUD performed unused frame-window sampling.");

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                bool initialPenumbraState = presentation.PenumbraEnabled;
                Press(keyboard.pKey);
                yield return null;
                Release(keyboard.pKey);
                Assert.That(presentation.PenumbraEnabled, Is.Not.EqualTo(initialPenumbraState),
                    "P did not toggle Penumbra while the HUD was hidden.");

                Press(keyboard.hKey);
                yield return null;
                Release(keyboard.hKey);
                Assert.That(hud.IsVisible, Is.True);
                yield return null;
                Assert.That(hud.SampleFrameCount, Is.GreaterThan(0));

                Press(keyboard.hKey);
                yield return null;
                Release(keyboard.hKey);
                Assert.That(hud.IsVisible, Is.False);
                Assert.That(hud.SampleFrameCount, Is.EqualTo(0));
                yield return null;
                Assert.That(hud.SampleFrameCount, Is.EqualTo(0));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                if (temporaryHudObject != null)
                {
                    Object.Destroy(temporaryHudObject);
                }
            }
        }

        [UnityTest]
        public IEnumerator GroundCompositeGeometry_IsInitializedOnSceneLoad()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            TilemapCompositeColliderInitializer2D initializer =
                Object.FindAnyObjectByType<TilemapCompositeColliderInitializer2D>();
            Assert.That(initializer, Is.Not.Null);

            TilemapCollider2D tilemapCollider = initializer.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = initializer.GetComponent<CompositeCollider2D>();
            Assert.That(tilemapCollider, Is.Not.Null);
            Assert.That(composite, Is.Not.Null);
            Assert.That(tilemapCollider.compositeOperation, Is.EqualTo(Collider2D.CompositeOperation.Merge));
            Assert.That(composite.pathCount, Is.GreaterThan(0),
                "CompositeCollider2D has no generated paths after scene initialization.");
            Assert.That(composite.pointCount, Is.GreaterThan(0),
                "CompositeCollider2D has no generated points after scene initialization.");
        }

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
            PlayerLongwatchAimPresenter2D longwatchPresenter =
                player.GetComponent<PlayerLongwatchAimPresenter2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            PlayerAnimator2D playerAnimator = player.GetComponent<PlayerAnimator2D>();
            PlayerJumpPresentation2D jumpPresentation = player.GetComponent<PlayerJumpPresentation2D>();
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
            Assert.That(longwatchPresenter, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(jumpPresentation, Is.Not.Null);
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
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return null;
                QueueWorldAim(mouse, nativePresentation, longwatchPresenter.AimOriginWorld, Vector2.right);
                yield return null;
                AssertStateAndLayers(
                    animator, "Idle", presenter, longwatchPresenter, bodyRenderer, armsRenderer);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForState(
                    animator, "Run", presenter, longwatchPresenter, bodyRenderer, armsRenderer, 90);
                Assert.That(bodyRenderer.flipX, Is.False);
                Assert.That(armsRenderer.flipX, Is.False);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                QueueWorldAim(mouse, nativePresentation, longwatchPresenter.AimOriginWorld, Vector2.left);
                for (int index = 0; index < 90 && !bodyRenderer.flipX; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    AssertOwnedLayers(presenter, longwatchPresenter, bodyRenderer, armsRenderer);
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
                    AssertOwnedLayers(presenter, longwatchPresenter, bodyRenderer, armsRenderer);
                    sawJump |= animator.GetCurrentAnimatorStateInfo(0).IsName("Jump");
                    sawFall |= animator.GetCurrentAnimatorStateInfo(0).IsName("Fall");
                    sawLand |= animator.GetCurrentAnimatorStateInfo(0).IsName("Land");
                }

                Assert.That(sawJump, Is.True, "Jump presentation state was not observed.");
                Assert.That(sawFall, Is.True, "Fall presentation state was not observed.");
                Assert.That(sawLand, Is.True, "Land presentation state was not observed.");
                Assert.That(bodyRenderer.flipX, Is.EqualTo(armsRenderer.flipX));
                Assert.That(jumpPresentation.TakeoffActive, Is.False,
                    "Short-hop takeoff compensation remained active after landing.");
                Assert.That(visual.localPosition, Is.EqualTo(jumpPresentation.BaselineLocalPosition),
                    "Short hop left a residual Visual offset.");
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator Player_GroundedRunningJumpAnchorsOnlyYThenEasesExactlyToBaseline()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            PlayerJumpPresentation2D presentation = motor.GetComponent<PlayerJumpPresentation2D>();
            Transform visual = motor.transform.Find("Visual - 48x64 Full Cell");
            Assert.That(presentation, Is.Not.Null);
            Assert.That(visual, Is.Not.Null);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 10; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(motor.IsGrounded, Is.True);
                Vector3 baseline = visual.localPosition;
                Vector3 startingRootPosition = motor.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
                InputSystem.Update();

                bool observedAnchoredVerticalMotion = false;
                bool observedCatchUp = false;
                bool restoredExactly = false;
                float earlyCatchUpCompensation = 0f;
                float lateCatchUpCompensation = float.MaxValue;
                for (int index = 0; index < 90; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;

                    if (!presentation.TakeoffActive)
                    {
                        if (observedCatchUp)
                        {
                            restoredExactly = visual.localPosition == baseline;
                            break;
                        }

                        continue;
                    }

                    float elapsed = presentation.TakeoffElapsed;
                    float normalTargetWorldY = motor.transform.TransformPoint(baseline).y;
                    float remainingCompensation = Mathf.Abs(normalTargetWorldY - visual.position.y);
                    Assert.That(visual.localPosition.x, Is.EqualTo(baseline.x).Within(0.0001f),
                        "Jump takeoff presentation anchored X instead of following the physical root.");

                    if (elapsed < 0.095f && motor.transform.position.y > startingRootPosition.y + 0.02f)
                    {
                        observedAnchoredVerticalMotion = true;
                        Assert.That(visual.position.y,
                            Is.EqualTo(presentation.TakeoffAnchorWorldY).Within(0.005f),
                            "Visual Y moved during the full-anchor phase.");
                    }

                    if (elapsed >= 0.105f && elapsed <= 0.16f)
                    {
                        observedCatchUp = true;
                        earlyCatchUpCompensation = Mathf.Max(earlyCatchUpCompensation, remainingCompensation);
                    }
                    else if (elapsed >= 0.20f && elapsed < 0.255f)
                    {
                        lateCatchUpCompensation = Mathf.Min(lateCatchUpCompensation, remainingCompensation);
                    }
                }

                Assert.That(observedAnchoredVerticalMotion, Is.True,
                    "The root did not rise while Visual Y remained anchored during the first 100 ms.");
                Assert.That(motor.transform.position.x, Is.GreaterThan(startingRootPosition.x + 0.1f),
                    "Running-jump X motion was frozen by presentation anchoring.");
                Assert.That(observedCatchUp, Is.True, "The 100-260 ms catch-up phase was not observed.");
                Assert.That(earlyCatchUpCompensation, Is.GreaterThan(0.01f));
                Assert.That(lateCatchUpCompensation, Is.LessThan(earlyCatchUpCompensation * 0.5f),
                    "Remaining Y compensation did not decrease substantially during cubic catch-up.");
                Assert.That(restoredExactly, Is.True,
                    "Visual did not return to its exact configured baseline after catch-up.");
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
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
            PlayerJumpPresentation2D jumpPresentation = motor.GetComponent<PlayerJumpPresentation2D>();

            Assert.That(bodyRenderer, Is.Not.Null);
            Assert.That(armsRenderer, Is.Not.Null);
            Assert.That(animator, Is.Not.Null);
            Assert.That(presenter, Is.Not.Null);
            Assert.That(jumpPresentation, Is.Not.Null);

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
                            Assert.That(visual.localPosition, Is.EqualTo(jumpPresentation.BaselineLocalPosition),
                                "Frame 3 began before takeoff compensation returned to baseline.");
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
        public IEnumerator Player_RunningJumpDustStaysInWorldAndPlaysExactlyOnce()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            PlayerJumpPresentation2D presentation = motor.GetComponent<PlayerJumpPresentation2D>();
            PlayerLongwatchAimPresenter2D longwatchPresenter = motor.GetComponent<PlayerLongwatchAimPresenter2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            Transform bodyVisual = motor.transform.Find("Visual - 48x64 Full Cell/BodySpriteRenderer");
            SpriteRenderer bodyRenderer = bodyVisual?.GetComponent<SpriteRenderer>();
            Assert.That(presentation, Is.Not.Null);
            Assert.That(longwatchPresenter, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(bodyRenderer, Is.Not.Null);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                QueueWorldAim(mouse, nativePresentation, longwatchPresenter.AimOriginWorld, Vector2.right);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 10; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(bodyRenderer.flipX, Is.False);
                Vector3 playerPositionAtInput = motor.transform.position;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
                InputSystem.Update();

                PlayerJumpDustFx2D dust = null;
                for (int index = 0; index < 12 && dust == null; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    dust = Object.FindAnyObjectByType<PlayerJumpDustFx2D>();
                }

                Assert.That(dust, Is.Not.Null, "Grounded jump did not spawn jump dust.");
                Assert.That(dust.transform.parent, Is.Null, "Jump dust was parented beneath the moving player.");
                Assert.That(dust.SpriteRenderer.flipX, Is.False,
                    "Right-facing takeoff did not snapshot right-facing dust.");
                Vector3 dustSpawnPosition = dust.transform.position;
                Assert.That(dustSpawnPosition, Is.EqualTo(presentation.TakeoffWorldPosition),
                    "Dust did not spawn at the authored full-cell takeoff pivot.");
                bool[] observedFrames = new bool[3];
                for (int index = 0; index < 90 && dust != null; index++)
                {
                    observedFrames[dust.CurrentFrameIndex] = true;
                    Assert.That(dust.transform.position, Is.EqualTo(dustSpawnPosition),
                        "World-space dust moved after takeoff.");
                    Assert.That(Object.FindObjectsByType<PlayerJumpDustFx2D>(FindObjectsSortMode.None), Has.Length.EqualTo(1),
                        "One successful jump spawned more than one dust object.");
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                yield return null;
                Assert.That(observedFrames, Is.All.True, "Dust did not visibly progress through all three frames.");
                Assert.That(Object.FindAnyObjectByType<PlayerJumpDustFx2D>(), Is.Null,
                    "Jump dust persisted after its one-shot duration.");
                Assert.That(motor.transform.position.x, Is.GreaterThan(playerPositionAtInput.x + 0.2f));
                Assert.That(motor.transform.position.y, Is.GreaterThan(playerPositionAtInput.y + 0.2f));
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
        public IEnumerator Player_LeftFacingDustKeepsTakeoffFacingAfterPlayerTurns()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            PlayerLongwatchAimPresenter2D longwatchPresenter = motor.GetComponent<PlayerLongwatchAimPresenter2D>();
            NativePixelPresentation nativePresentation = Object.FindAnyObjectByType<NativePixelPresentation>();
            SpriteRenderer bodyRenderer = motor.transform
                .Find("Visual - 48x64 Full Cell/BodySpriteRenderer")?.GetComponent<SpriteRenderer>();
            Assert.That(longwatchPresenter, Is.Not.Null);
            Assert.That(nativePresentation, Is.Not.Null);
            Assert.That(bodyRenderer, Is.Not.Null);

            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                QueueWorldAim(mouse, nativePresentation, longwatchPresenter.AimOriginWorld, Vector2.left);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                for (int index = 0; index < 30 && !bodyRenderer.flipX; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(bodyRenderer.flipX, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.Space));
                InputSystem.Update();
                PlayerJumpDustFx2D dust = null;
                for (int index = 0; index < 12 && dust == null; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    dust = Object.FindAnyObjectByType<PlayerJumpDustFx2D>();
                }

                Assert.That(dust, Is.Not.Null);
                Assert.That(dust.SpriteRenderer.flipX, Is.True,
                    "Left-facing takeoff did not snapshot mirrored dust.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                QueueWorldAim(mouse, nativePresentation, longwatchPresenter.AimOriginWorld, Vector2.right);
                for (int index = 0; index < 12 && bodyRenderer.flipX && dust != null; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(bodyRenderer.flipX, Is.False, "Player did not turn after takeoff.");
                Assert.That(dust, Is.Not.Null, "Dust expired before facing snapshot could be verified.");
                Assert.That(dust.SpriteRenderer.flipX, Is.True,
                    "Existing world-space dust changed facing with the airborne player.");
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
        public IEnumerator Player_CoyoteJumpKeepsImpulseWithoutSpawningFloatingDust()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Assert.That(motor, Is.Not.Null);
            PlayerJumpPresentation2D presentation = motor.GetComponent<PlayerJumpPresentation2D>();
            Assert.That(presentation, Is.Not.Null);
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                for (int index = 0; index < 30; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(motor.IsGrounded, Is.True);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                for (int index = 0; index < 180 && motor.IsGrounded; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                }

                Assert.That(motor.IsGrounded, Is.False, "Player did not leave the platform for the coyote test.");
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A, Key.Space));
                InputSystem.Update();
                bool receivedCoyoteImpulse = false;
                for (int index = 0; index < 8 && !receivedCoyoteImpulse; index++)
                {
                    yield return new WaitForFixedUpdate();
                    yield return null;
                    receivedCoyoteImpulse = motor.Velocity.y > 5f;
                }

                Assert.That(receivedCoyoteImpulse, Is.True, "Coyote jump no longer received its normal impulse.");
                Assert.That(presentation.TakeoffActive, Is.True,
                    "Coyote jump did not receive takeoff presentation from its current visual position.");
                Assert.That(Object.FindAnyObjectByType<PlayerJumpDustFx2D>(), Is.Null,
                    "Coyote jump spawned floating dust while already airborne.");
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
                Assert.That(motor.IsCrouched, Is.False);
                Assert.That(motor.IsWallBraced, Is.False);
                Assert.That(motor.WallKickLockRemaining, Is.Zero);
                CapsuleCollider2D capsule = motor.GetComponent<CapsuleCollider2D>();
                Assert.That(capsule.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
                Assert.That(capsule.offset, Is.EqualTo(new Vector2(0f, 1.375f)));
            }
            finally
            {
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Player_CrouchPreservesFeetBlocksStandAndAutoStandsAfterTunnel()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D capsule = motor.GetComponent<CapsuleCollider2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                body.position = new Vector2(67f, 0.02f);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                float standingBottom = capsule.offset.y - capsule.size.y * 0.5f;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsCrouched, Is.True);
                Assert.That(capsule.size, Is.EqualTo(new Vector2(1.05f, 1.75f)));
                Assert.That(capsule.offset.y - capsule.size.y * 0.5f,
                    Is.EqualTo(standingBottom).Within(0.0001f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.D));
                InputSystem.Update();
                for (int index = 0; index < 180 && body.position.x < 72f; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(body.position.x, Is.GreaterThanOrEqualTo(72f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 8; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.IsCrouched, Is.True,
                    "Releasing crouch beneath the tunnel forced the standing capsule into its ceiling.");

                for (int index = 0; index < 180 && motor.IsCrouched; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(body.position.x, Is.GreaterThan(77.5f));
                Assert.That(motor.IsCrouched, Is.False,
                    "The player did not automatically stand after leaving the low ceiling.");
                Assert.That(capsule.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Player_CrouchJumpRequiresClearanceAndAirborneInputDoesNotShrink()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            CapsuleCollider2D capsule = motor.GetComponent<CapsuleCollider2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                body.position = new Vector2(67f, 0.02f);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                InputSystem.Update();
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.IsCrouched, Is.True);
                body.position = new Vector2(72f, body.position.y);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsCrouched, Is.True);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.Space));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsCrouched, Is.True);
                Assert.That(body.linearVelocity.y, Is.LessThan(1f),
                    "A blocked crouch jump expanded or launched through the tunnel ceiling.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                for (int index = 0; index < 8; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.IsCrouched, Is.True);
                body.position = new Vector2(80f, 0.02f);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                for (int index = 0; index < 5; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.IsCrouched, Is.False);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S, Key.Space));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsCrouched, Is.False);
                Assert.That(body.linearVelocity.y, Is.GreaterThan(10f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsCrouched, Is.False,
                    "Airborne crouch input shrank the standing collider.");
                Assert.That(capsule.size, Is.EqualTo(new Vector2(1.05f, 2.75f)));
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator Player_WallBraceAndKickUseActualWallSideAndPreserveImpulseLock()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;

            PlayerMotor2D motor = Object.FindAnyObjectByType<PlayerMotor2D>();
            Rigidbody2D body = motor.GetComponent<Rigidbody2D>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            int landEvents = 0;
            motor.Landed += CountLand;
            try
            {
                body.position = new Vector2(99.4f, -1f);
                body.linearVelocity = new Vector2(0f, -9f);
                Physics2D.SyncTransforms();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 3; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(motor.IsGrounded, Is.False);
                Assert.That(motor.IsWallBraced, Is.True);
                Assert.That(motor.WallSide, Is.EqualTo(1));
                Assert.That(body.linearVelocity.y, Is.InRange(-4.01f, -0.01f));

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                Assert.That(motor.IsWallBraced, Is.False);
                Assert.That(body.linearVelocity.x, Is.EqualTo(-8f).Within(0.05f));
                Assert.That(body.linearVelocity.y, Is.EqualTo(11.5f).Within(0.05f));
                Assert.That(motor.WallKickLockRemaining, Is.GreaterThan(0f));

                for (int index = 0; index < 3; index++)
                {
                    yield return new WaitForFixedUpdate();
                    Assert.That(body.linearVelocity.x, Is.EqualTo(-8f).Within(0.05f),
                        "Held input canceled the away impulse during the wall-kick lock.");
                    Assert.That(motor.IsWallBraced, Is.False,
                        "The player immediately reattached to the same wall during lock.");
                }
                for (int index = 0; index < 8 && motor.IsWallKicking; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                Assert.That(motor.IsWallKicking, Is.False);
                Assert.That(body.linearVelocity.x, Is.GreaterThan(-8f),
                    "Normal air control did not return after the authored wall-kick lock.");
                Assert.That(landEvents, Is.EqualTo(0), "Wall contact emitted a false Land event.");
            }
            finally
            {
                motor.Landed -= CountLand;
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }

            void CountLand() => landEvents++;
        }

        private static IEnumerator WaitForState(
            Animator animator,
            string state,
            PlayerUnarmedArmsPresenter2D presenter,
            PlayerLongwatchAimPresenter2D longwatchPresenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer,
            int maximumFrames)
        {
            for (int index = 0; index < maximumFrames; index++)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
                AssertOwnedLayers(presenter, longwatchPresenter, bodyRenderer, armsRenderer);
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
            PlayerLongwatchAimPresenter2D longwatchPresenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).IsName(state), Is.True);
            Assert.That(longwatchPresenter.OwnsRenderer, Is.True);
            AssertOwnedLayers(presenter, longwatchPresenter, bodyRenderer, armsRenderer);
        }

        private static void AssertOwnedLayers(
            PlayerUnarmedArmsPresenter2D unarmedPresenter,
            PlayerLongwatchAimPresenter2D longwatchPresenter,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            if (!longwatchPresenter.OwnsRenderer)
            {
                AssertLayers(unarmedPresenter, bodyRenderer, armsRenderer);
                return;
            }

            Assert.That(unarmedPresenter.OwnsRenderer, Is.False);
            Assert.That(bodyRenderer.flipX, Is.EqualTo(armsRenderer.flipX), "Layer facing diverged.");
            for (int frameIndex = 0; frameIndex < longwatchPresenter.BodyIdleFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == longwatchPresenter.GetBodyIdleFrame(frameIndex))
                {
                    LongwatchIdleAimPose idlePose = longwatchPresenter.GetIdleAimPose(
                        longwatchPresenter.Selection.DirectionIndex);
                    Assert.That(armsRenderer.sprite, Is.SameAs(idlePose.GetFrame(frameIndex)),
                        "Longwatch overlay lagged or diverged from the displayed Body Idle frame.");
                    return;
                }
            }

            for (int frameIndex = 0; frameIndex < longwatchPresenter.BodyRunFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == longwatchPresenter.GetBodyRunFrame(frameIndex))
                {
                    LongwatchRunAimPose runPose = longwatchPresenter.GetRunAimPose(
                        longwatchPresenter.Selection.DirectionIndex);
                    Assert.That(armsRenderer.sprite, Is.SameAs(runPose.GetFrame(frameIndex)),
                        "Longwatch overlay lagged or diverged from the displayed Body Run frame.");
                    return;
                }
            }

            for (int frameIndex = 0; frameIndex < longwatchPresenter.BodyBackpedalFrameCount; frameIndex++)
            {
                if (bodyRenderer.sprite == longwatchPresenter.GetBodyBackpedalFrame(frameIndex))
                {
                    LongwatchBackpedalAimPose backpedalPose = longwatchPresenter.GetBackpedalAimPose(
                        longwatchPresenter.Selection.DirectionIndex);
                    Assert.That(armsRenderer.sprite, Is.SameAs(backpedalPose.GetFrame(frameIndex)),
                        "Longwatch overlay lagged or diverged from the displayed Body Backpedal frame.");
                    return;
                }
            }

            Assert.Fail("Armed Longwatch owns the overlay for an unsupported Body sprite.");
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
