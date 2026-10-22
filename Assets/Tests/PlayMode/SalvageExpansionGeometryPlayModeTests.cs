using System.Collections;
using NUnit.Framework;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using Rustline.Physics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.Tilemaps;

namespace Rustline.Tests
{
    public sealed class SalvageExpansionGeometryPlayModeTests
    {
        private PlayerMotor2D _motor;
        private PlayerAim2D _aim;
        private Rigidbody2D _body;
        private Keyboard _keyboard;
        private InputSettings.BackgroundBehavior _background;
#if UNITY_EDITOR
        private InputSettings.EditorInputBehaviorInPlayMode _editorInput;
#endif

        private IEnumerator LoadRoute()
        {
            SceneManager.LoadScene("SalvageIntake");
            yield return null;
            var director = Object.FindAnyObjectByType<FirstPlayableDirector2D>();
            director.enabled = false; // Isolate geometry from combat, without changing terrain or movement.
            foreach (FirstPlayableEncounter2D encounter in director.Encounters) encounter.ResetEncounter();
            _motor = director.Player;
            _aim = _motor.GetComponent<PlayerAim2D>();
            _aim.enabled = false;
            _aim.ApplyWorldAimVector(Vector2.right);
            _body = _motor.GetComponent<Rigidbody2D>();
            _background = InputSystem.settings.backgroundBehavior;
            InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
#if UNITY_EDITOR
            _editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
            InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
            _keyboard = InputSystem.AddDevice<Keyboard>();
            Assert.That(_motor.enabled && _body.simulated && _motor.GetComponent<PlayerInputReader>().enabled, Is.True,
                "Geometry fixture must retain live player simulation/input.");
        }

        [UnityTearDown]
        public IEnumerator RestoreInputRuntime()
        {
            if (_keyboard != null && _keyboard.added)
            {
                Keys();
                InputSystem.RemoveDevice(_keyboard);
            }
            InputSystem.settings.backgroundBehavior = _background;
#if UNITY_EDITOR
            InputSystem.settings.editorInputBehaviorInPlayMode = _editorInput;
#endif
            yield return null;
        }

        [UnityTest]
        public IEnumerator SavedTerrain_HasContinuousIntroConnectionTwoBakedCompositesAndWideShaft()
        {
            yield return LoadRoute();
            Tilemap collision = GameObject.Find("Ground Collision - Hidden").GetComponent<Tilemap>();
            Assert.That(collision.cellBounds.xMax, Is.EqualTo(196));
            Assert.That(collision.HasTile(new Vector3Int(45,16,0)), Is.True);
            Assert.That(collision.HasTile(new Vector3Int(46,16,0)), Is.True);
            Assert.That(collision.HasTile(new Vector3Int(193,28,0)), Is.True);
            var initializers = Object.FindObjectsByType<TilemapCompositeColliderInitializer2D>();
            Assert.That(initializers.Length, Is.EqualTo(2));
            foreach (var initializer in initializers)
            {
                Assert.That(initializer.gameObject.layer, Is.EqualTo(6));
                Assert.That(initializer.GetComponent<Rigidbody2D>().bodyType, Is.EqualTo(RigidbodyType2D.Static));
                Assert.That(initializer.GetComponent<TilemapCollider2D>().compositeOperation, Is.EqualTo(Collider2D.CompositeOperation.Merge));
                Assert.That(initializer.GetComponent<CompositeCollider2D>().geometryType, Is.EqualTo(CompositeCollider2D.GeometryType.Polygons));
                Assert.That(initializer.HasGeneratedGeometry, Is.True);
            }
            for (int x = 122; x < 126; x++)
                for (int y = 17; y < 29; y++) Assert.That(collision.HasTile(new Vector3Int(x,y,0)), Is.False);
            RaycastHit2D underside = Physics2D.Raycast(new Vector2(105f,18f), Vector2.up, 5f, 1 << 6);
            Assert.That(underside.point.y, Is.EqualTo(19.625f).Within(0.01f));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RealInput_WalksFromFormerExitAcrossNewGalleryWithoutSeamLand()
        {
            yield return LoadRoute();
            Place(40f,17.05f);
            yield return Steps(25);
            int lands = 0;
            _motor.Landed += () => lands++;
            Keys(Key.D);
            yield return null;
            Assert.That(_keyboard.dKey.isPressed, Is.True, "Synthetic keyboard did not receive the event.");
            Assert.That(_motor.GetComponent<PlayerInputReader>().MoveX, Is.GreaterThan(0f), "Keyboard action did not reach player input.");
            for (int i = 0; i < 170; i++)
            {
                yield return new WaitForFixedUpdate();
                Assert.That(_body.position.y, Is.GreaterThan(16.95f));
            }
            Assert.That(_body.position.x, Is.GreaterThan(62f));
            Assert.That(_motor.IsGrounded, Is.True);
            Assert.That(lands, Is.Zero);
        }

        [UnityTest]
        public IEnumerator RealInput_JumpsProcessingCoverAndRaisedBatteryApproach()
        {
            yield return LoadRoute();
            Place(63.6f,17.05f); yield return Steps(20);
            Keys(Key.D,Key.Space); yield return Steps(38);
            Keys(); yield return Steps(22);
            Assert.That(_body.position.x, Is.GreaterThan(67f));
            Assert.That(_body.position.y, Is.GreaterThan(17.9f));
            Keys(Key.D); yield return Steps(28);
            Keys(); yield return Steps(20);
            Keys(Key.D,Key.Space); yield return Steps(23);
            Keys(Key.Space); yield return Steps(25);
            Assert.That(_body.position.x, Is.InRange(75f,77.6f));
            Assert.That(_body.position.y, Is.GreaterThan(17.9f));
            Keys(); yield return Steps(2);
            Keys(Key.D,Key.Space); yield return Steps(36);
            Keys(); yield return Steps(20);
            Assert.That(_body.position.x, Is.GreaterThan(79f));
            Assert.That(_body.position.y, Is.GreaterThan(19.9f));
            Assert.That(_motor.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator RealInput_CrouchesThrough42PixelMaintenanceUnderpassAndStandsAfterExit()
        {
            yield return LoadRoute();
            Place(99f,17.05f); yield return Steps(20);
            Keys(Key.D); yield return Steps(40);
            Assert.That(_body.position.x, Is.LessThan(102f), "Standing capsule crossed the underpass entrance.");
            Assert.That(_motor.IsCrouched, Is.False);
            Keys(Key.D,Key.S); yield return Steps(50);
            Assert.That(_body.position.x, Is.GreaterThan(103f));
            Assert.That(_motor.IsCrouched, Is.True);
            Keys(Key.D); yield return Steps(100);
            Assert.That(_body.position.x, Is.GreaterThan(108.6f));
            Assert.That(_motor.IsCrouched, Is.False);
        }

        [UnityTest]
        public IEnumerator RealInput_JumpsFourUnitRailGapOntoTerminalDeck()
        {
            yield return LoadRoute();
            Place(139.5f,29.05f); yield return Steps(20);
            Keys(Key.D);
            for (int i = 0; i < 45 && _body.position.x < 142.8f; i++) yield return new WaitForFixedUpdate();
            Keys(Key.D,Key.Space);
            float minimumY = _body.position.y;
            for (int i = 0; i < 55; i++)
            {
                yield return new WaitForFixedUpdate();
                minimumY = Mathf.Min(minimumY,_body.position.y);
            }
            Assert.That(_body.position.x, Is.GreaterThan(149f));
            Assert.That(minimumY, Is.GreaterThan(28.9f), "Jump fell into the recovery well.");
            Assert.That(_motor.IsGrounded, Is.True);
        }

        [UnityTest]
        public IEnumerator RealInput_ClimbsSecondFourUnitShaftUsingWallKicksAndLedgeExit()
        {
            yield return LoadRoute();
            yield return ClimbWell(123f,17.05f,128.2f);
        }

        [UnityTest]
        public IEnumerator RealInput_ReturnsFromRailRecoveryWell()
        {
            yield return LoadRoute();
            yield return ClimbWell(145f,21.05f,150.2f);
        }

        [UnityTest]
        public IEnumerator RealInput_CanLedgeClimbOntoAlternateMaintenanceRoof()
        {
            yield return LoadRoute();
            Place(100f,17.05f); yield return Steps(20);
            bool climbed=false;
            _motor.LedgeClimbStarted += () => climbed=true;
            Keys(Key.D,Key.Space);
            for(int i=0;i<100 && _body.position.x < 104f;i++) yield return new WaitForFixedUpdate();
            Keys(); yield return Steps(20);
            Assert.That(climbed,Is.True);
            Assert.That(_body.position.x,Is.GreaterThan(102.5f));
            Assert.That(_body.position.y,Is.GreaterThan(20.5f));
        }

        private IEnumerator ClimbWell(float spawnX,float spawnY,float exitX)
        {
            Place(spawnX,spawnY); yield return Steps(20);
            int direction = 1;
            bool held = true;
            int braces = 0;
            Keys(Key.D,Key.Space);
            for (int i = 0; i < 650 && !(_body.position.x > exitX && _body.position.y > 28.9f); i++)
            {
                yield return new WaitForFixedUpdate();
                if (_motor.IsLedgeClimbing) { Keys(Key.D); held = false; continue; }
                if (_motor.IsWallBraced)
                {
                    braces++;
                    if (held) { direction = _motor.WallSide; held = false; }
                    else { held = true; direction = _motor.WallSide; }
                    Keys(held ? new[] { direction > 0 ? Key.D : Key.A, Key.Space } : new[] { direction > 0 ? Key.D : Key.A });
                    if (held) direction = -direction;
                }
                else
                {
                    if (_body.position.y > 29f) direction = 1;
                    _aim.ApplyWorldAimVector(Vector2.right * direction);
                    Keys(held ? new[] { direction > 0 ? Key.D : Key.A, Key.Space } : new[] { direction > 0 ? Key.D : Key.A });
                }
            }
            Assert.That(braces, Is.GreaterThan(1));
            Assert.That(_body.position.x, Is.GreaterThan(exitX), "Shaft traversal stalled at " + _body.position);
            Assert.That(_body.position.y, Is.GreaterThan(28.9f));
        }

        private void Place(float x,float y)
        {
            _motor.transform.position = new Vector2(x,y);
            _body.position = new Vector2(x,y);
            _body.linearVelocity = Vector2.zero;
            _motor.ResetAfterRespawn();
            Physics2D.SyncTransforms();
        }
        private void Keys(params Key[] keys)
        {
            // Exercise the game's real Input System runtime (also used by the Release smoke).
            // The isolated InputTestFixture has no Editor state buffers in this batch context.
            InputSystem.QueueStateEvent(_keyboard,new KeyboardState(keys));
            InputSystem.Update();
        }
        private static IEnumerator Steps(int count)
        {
            for(int i=0;i<count;i++) yield return new WaitForFixedUpdate();
        }
    }
}
