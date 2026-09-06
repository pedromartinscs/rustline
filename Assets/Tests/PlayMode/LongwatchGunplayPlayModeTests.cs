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
    public sealed class LongwatchGunplayPlayModeTests : InputTestFixture
    {
        private PlayerWeaponController2D _weapon;
        private PlayerMotor2D _motor;
        private PlayerAim2D _aim;
        private PlayerLongwatchAimPresenter2D _presenter;
        private Rigidbody2D _body;

        [UnityTest]
        public IEnumerator IdleClick_FiresHitsTargetAndShowsReusedTrace()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerMotor2D motor = _motor;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            DiagnosticCombatTarget2D target = FindTarget("Target - Clear Horizontal");
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return PlaceGrounded(body, 116f);
                SetAim(aim, Vector2.right);
                target.ResetDiagnostics();
                weapon.ResetTransientState();

                yield return Click(mouse);

                Assert.That(weapon.ShotCount, Is.EqualTo(1));
                Assert.That(weapon.LastShotResult.Hit, Is.True);
                Assert.That(weapon.LastShotResult.HitCollider, Is.SameAs(target.GetComponent<Collider2D>()));
                Assert.That(weapon.LastShotResult.HitReceiverNotified, Is.True);
                Assert.That(target.HitsTaken, Is.EqualTo(1));
                Assert.That(target.AccumulatedDamage, Is.EqualTo(40));
                Assert.That(target.LastHitDirection, Is.EqualTo(Vector2.right));
                Assert.That(weapon.ShotFeedback.IsVisible, Is.True);
                Assert.That(motor.IsGrounded, Is.True);
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator OffGridAim_UsesContinuousSevenDegreesWhileVisualUsesTen()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerAim2D aim = _aim;
            PlayerLongwatchAimPresenter2D presenter = _presenter;
            Rigidbody2D body = _body;
            DiagnosticCombatTarget2D target = FindTarget("Target - Continuous +7 Degrees");
            yield return PlaceGrounded(body, 130f);
            Vector2 direction = DirectionAtDegrees(7f);
            SetAim(aim, direction);
            yield return null;
            target.ResetDiagnostics();
            weapon.ResetTransientState();

            Assert.That(presenter.Selection.AuthoredAngleDegrees, Is.EqualTo(10));
            Assert.That(weapon.TryFire(Time.time), Is.True);

            Assert.That(weapon.LastShotResult.Direction.x, Is.EqualTo(direction.x).Within(0.000001f));
            Assert.That(weapon.LastShotResult.Direction.y, Is.EqualTo(direction.y).Within(0.000001f));
            Assert.That(weapon.LastShotResult.HitCollider, Is.SameAs(target.GetComponent<Collider2D>()));
            Assert.That(target.LastHitDirection.x, Is.EqualTo(direction.x).Within(0.000001f));
            Assert.That(target.LastHitDirection.y, Is.EqualTo(direction.y).Within(0.000001f));
        }

        [UnityTest]
        public IEnumerator GroundObstruction_StopsShotBeforeTarget()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            DiagnosticCombatTarget2D target = FindTarget("Target - Occluded By Ground");
            yield return PlaceGrounded(body, 145f);
            SetAim(aim, Vector2.right);
            target.ResetDiagnostics();
            weapon.ResetTransientState();

            Assert.That(weapon.TryFire(Time.time), Is.True);

            Assert.That(weapon.LastShotResult.Hit, Is.True);
            Assert.That(weapon.LastShotResult.HitCollider.gameObject.layer, Is.EqualTo(6));
            Assert.That(weapon.LastShotResult.HitReceiverNotified, Is.False);
            Assert.That(weapon.LastShotResult.EndPoint.x, Is.LessThan(151f));
            Assert.That(target.HitsTaken, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RunAndBackpedal_CanFireWithoutResettingAnimationOrOverlay()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerAim2D aim = _aim;
            PlayerLongwatchAimPresenter2D presenter = _presenter;
            Rigidbody2D body = _body;
            PlayerAnimator2D playerAnimator = weapon.GetComponent<PlayerAnimator2D>();
            Animator animator = weapon.GetComponentInChildren<Animator>();
            SpriteRenderer bodyRenderer = weapon.transform
                .Find("Visual - 48x64 Full Cell/BodySpriteRenderer").GetComponent<SpriteRenderer>();
            SpriteRenderer armsRenderer = weapon.transform
                .Find("Visual - 48x64 Full Cell/ArmsWeaponSpriteRenderer").GetComponent<SpriteRenderer>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return PlaceGrounded(body, 116f);
                SetAim(aim, Vector2.right);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                yield return WaitForState(playerAnimator, PlayerAnimationState.Run, 90);
                AssertFirePreservesPresentation(weapon, presenter, playerAnimator, animator, bodyRenderer, armsRenderer);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return PlaceGrounded(body, 116f);
                SetAim(aim, Vector2.right);
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.A));
                InputSystem.Update();
                yield return WaitForState(playerAnimator, PlayerAnimationState.Backpedal, 90);
                for (int index = 0; index < 8; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                yield return null;
                Assert.That(body.linearVelocity.x, Is.EqualTo(-4f).Within(0.08f));
                AssertFirePreservesPresentation(weapon, presenter, playerAnimator, animator, bodyRenderer, armsRenderer);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator JumpAndCrouch_ClicksAreDroppedAndNeverFireLater()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            PlayerAnimator2D playerAnimator = weapon.GetComponent<PlayerAnimator2D>();
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                yield return PlaceGrounded(body, 116f);
                SetAim(aim, Vector2.right);
                weapon.ResetTransientState();

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.Space));
                InputSystem.Update();
                yield return WaitForState(playerAnimator, PlayerAnimationState.Jump, 60);
                yield return Click(mouse);
                Assert.That(weapon.ShotCount, Is.Zero);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return PlaceGrounded(body, 116f);
                yield return null;
                Assert.That(weapon.ShotCount, Is.Zero, "Rejected jump click was buffered.");

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.S));
                InputSystem.Update();
                yield return WaitForState(playerAnimator, PlayerAnimationState.CrouchIdle, 30);
                yield return Click(mouse);
                Assert.That(weapon.ShotCount, Is.Zero);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                yield return WaitForState(playerAnimator, PlayerAnimationState.Idle, 30);
                yield return null;
                Assert.That(weapon.ShotCount, Is.Zero, "Rejected crouch click was buffered.");
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
        public IEnumerator WallBraceAndKick_CannotFire()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerMotor2D motor = _motor;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            Keyboard keyboard = InputSystem.AddDevice<Keyboard>();
            try
            {
                body.position = new Vector2(99.4f, -1f);
                body.linearVelocity = new Vector2(0f, -9f);
                Physics2D.SyncTransforms();
                SetAim(aim, Vector2.right);
                weapon.ResetTransientState();
                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D));
                InputSystem.Update();
                for (int index = 0; index < 3; index++)
                {
                    yield return new WaitForFixedUpdate();
                }

                Assert.That(motor.IsWallBraced, Is.True);
                Assert.That(weapon.TryFire(Time.time), Is.False);

                InputSystem.QueueStateEvent(keyboard, new KeyboardState(Key.D, Key.Space));
                InputSystem.Update();
                yield return new WaitForFixedUpdate();
                yield return null;
                Assert.That(motor.IsWallKicking, Is.True);
                Assert.That(weapon.TryFire(Time.time), Is.False);
                Assert.That(weapon.ShotCount, Is.Zero);
            }
            finally
            {
                InputSystem.QueueStateEvent(keyboard, new KeyboardState());
                InputSystem.Update();
                InputSystem.RemoveDevice(keyboard);
            }
        }

        [UnityTest]
        public IEnumerator SemiAutomaticInput_HoldAndCooldownDoNotCreateExtraShots()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return PlaceGrounded(body, 116f);
                SetAim(aim, Vector2.right);
                weapon.ResetTransientState();

                QueueMouse(mouse, true);
                yield return null;
                Assert.That(weapon.ShotCount, Is.EqualTo(1));
                yield return new WaitForSeconds(0.3f);
                Assert.That(weapon.ShotCount, Is.EqualTo(1), "Holding Fire produced automatic shots.");

                QueueMouse(mouse, false);
                yield return null;
                QueueMouse(mouse, true);
                yield return null;
                Assert.That(weapon.ShotCount, Is.EqualTo(2));

                QueueMouse(mouse, false);
                yield return null;
                QueueMouse(mouse, true);
                yield return null;
                Assert.That(weapon.ShotCount, Is.EqualTo(2), "Cooldown allowed a rapid re-press.");

                QueueMouse(mouse, false);
                yield return new WaitForSeconds(0.26f);
                QueueMouse(mouse, true);
                yield return null;
                Assert.That(weapon.ShotCount, Is.EqualTo(3));
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        [UnityTest]
        public IEnumerator CombatTargetTrigger_DoesNotAffectEnvironmentAndRespawnClearsPendingFire()
        {
            yield return LoadRange();
            PlayerWeaponController2D weapon = _weapon;
            PlayerMotor2D motor = _motor;
            PlayerAim2D aim = _aim;
            Rigidbody2D body = _body;
            Mouse mouse = InputSystem.AddDevice<Mouse>();
            try
            {
                yield return PlaceGrounded(body, 124f);
                SetAim(aim, Vector2.right);
                Assert.That(motor.IsGrounded, Is.True);
                Assert.That(motor.IsWallBraced, Is.False);

                weapon.ResetTransientState();
                // Hold weapon consumption while staging a pending input so this specifically
                // verifies that the respawn integration clears the transient press.
                weapon.enabled = false;
                QueueMouse(mouse, true);
                body.position = new Vector2(body.position.x, -20f);
                body.linearVelocity = Vector2.zero;
                Physics2D.SyncTransforms();
                for (int index = 0; index < 3; index++)
                {
                    yield return new WaitForFixedUpdate();
                }
                weapon.enabled = true;
                yield return null;

                Assert.That(body.position.x, Is.EqualTo(-27f).Within(0.1f));
                Assert.That(weapon.ShotCount, Is.Zero, "Respawn left a pending ghost Fire press.");
            }
            finally
            {
                InputSystem.RemoveDevice(mouse);
            }
        }

        private IEnumerator LoadRange()
        {
            SceneManager.LoadScene("MovementLab");
            yield return null;
            _weapon = Object.FindAnyObjectByType<PlayerWeaponController2D>();
            _motor = _weapon?.GetComponent<PlayerMotor2D>();
            _aim = _weapon?.GetComponent<PlayerAim2D>();
            _presenter = _weapon?.GetComponent<PlayerLongwatchAimPresenter2D>();
            _body = _weapon?.GetComponent<Rigidbody2D>();
            Assert.That(_weapon, Is.Not.Null);
            Assert.That(_motor, Is.Not.Null);
            Assert.That(_aim, Is.Not.Null);
            Assert.That(_presenter, Is.Not.Null);
            Assert.That(_body, Is.Not.Null);
        }

        private static IEnumerator PlaceGrounded(Rigidbody2D body, float x)
        {
            body.position = new Vector2(x, 0.02f);
            body.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
            for (int index = 0; index < 30; index++)
            {
                yield return new WaitForFixedUpdate();
            }

            yield return null;
        }

        private static void SetAim(PlayerAim2D aim, Vector2 direction)
        {
            aim.enabled = false;
            Assert.That(aim.ApplyWorldAimVector(direction), Is.True);
        }

        private static IEnumerator Click(Mouse mouse)
        {
            QueueMouse(mouse, true);
            yield return null;
            QueueMouse(mouse, false);
            yield return null;
        }

        private static void QueueMouse(Mouse mouse, bool pressed)
        {
            MouseState state = new MouseState();
            if (pressed)
            {
                state = state.WithButton(MouseButton.Left);
            }

            InputSystem.QueueStateEvent(mouse, state);
            InputSystem.Update();
        }

        private static DiagnosticCombatTarget2D FindTarget(string name)
        {
            DiagnosticCombatTarget2D[] targets = Object.FindObjectsByType<DiagnosticCombatTarget2D>();
            foreach (DiagnosticCombatTarget2D target in targets)
            {
                if (target.name == name)
                {
                    return target;
                }
            }

            Assert.Fail("Missing diagnostic target: " + name);
            return null;
        }

        private static IEnumerator WaitForState(
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

            Assert.Fail("Player did not enter " + expected + ".");
        }

        private static void AssertFirePreservesPresentation(
            PlayerWeaponController2D weapon,
            PlayerLongwatchAimPresenter2D presenter,
            PlayerAnimator2D playerAnimator,
            Animator animator,
            SpriteRenderer bodyRenderer,
            SpriteRenderer armsRenderer)
        {
            PlayerAnimationState? stateBefore = playerAnimator.CurrentState;
            float normalizedTimeBefore = animator.GetCurrentAnimatorStateInfo(0).normalizedTime;
            Sprite bodyBefore = bodyRenderer.sprite;
            Sprite armsBefore = armsRenderer.sprite;
            LongwatchAimSelection selectionBefore = presenter.Selection;
            weapon.ResetTransientState();

            Assert.That(weapon.TryFire(Time.time), Is.True);
            Assert.That(playerAnimator.CurrentState, Is.EqualTo(stateBefore));
            Assert.That(animator.GetCurrentAnimatorStateInfo(0).normalizedTime, Is.EqualTo(normalizedTimeBefore));
            Assert.That(bodyRenderer.sprite, Is.SameAs(bodyBefore));
            Assert.That(armsRenderer.sprite, Is.SameAs(armsBefore));
            Assert.That(presenter.Selection.DirectionIndex, Is.EqualTo(selectionBefore.DirectionIndex));
            Assert.That(presenter.Selection.FlipX, Is.EqualTo(selectionBefore.FlipX));
        }

        private static Vector2 DirectionAtDegrees(float degrees)
        {
            float radians = degrees * Mathf.Deg2Rad;
            return new Vector2(Mathf.Cos(radians), Mathf.Sin(radians));
        }
    }
}
