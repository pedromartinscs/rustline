using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using Rustline.Gameplay.Player;
using UnityEditor;
using UnityEngine;

namespace Rustline.Tests
{
    public sealed class PlayerWallBraceCoverageTests
    {
        private const int GroundLayer = 6;
        private const float WallThickness = 0.25f;

        private readonly List<GameObject> _objects = new List<GameObject>();
        private PlayerMovementConfig _config;
        private PlayerEnvironmentProbe2D _probe;

        [SetUp]
        public void SetUp()
        {
            _config = ScriptableObject.CreateInstance<PlayerMovementConfig>();

            GameObject player = Track(new GameObject("Wall Brace coverage player - Test"));
            player.SetActive(false);
            Rigidbody2D body = player.AddComponent<Rigidbody2D>();
            body.bodyType = RigidbodyType2D.Dynamic;
            body.gravityScale = 0f;
            body.freezeRotation = true;
            CapsuleCollider2D capsule = player.AddComponent<CapsuleCollider2D>();
            capsule.direction = CapsuleDirection2D.Vertical;
            capsule.size = _config.StandingColliderSize;
            capsule.offset = _config.StandingColliderOffset;

            PlayerGroundProbe2D groundProbe = player.AddComponent<PlayerGroundProbe2D>();
            _probe = player.AddComponent<PlayerEnvironmentProbe2D>();
            SetObjectReference(groundProbe, "config", _config);
            SetLayerMask(groundProbe, "groundLayers", 1 << GroundLayer);
            SetObjectReference(_probe, "config", _config);

            player.SetActive(true);
            typeof(PlayerEnvironmentProbe2D)
                .GetMethod("Awake", BindingFlags.Instance | BindingFlags.NonPublic)
                ?.Invoke(_probe, null);
            Physics2D.SyncTransforms();
        }

        [TearDown]
        public void TearDown()
        {
            for (int index = _objects.Count - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(_objects[index]);
            }

            Object.DestroyImmediate(_config);
            _objects.Clear();
        }

        [TestCase(0, 14.5f)]
        [TestCase(1, 19.5f)]
        [TestCase(2, 24.5f)]
        [TestCase(3, 29.5f)]
        [TestCase(4, 34.5f)]
        [TestCase(5, 39.5f)]
        [TestCase(6, 44.5f)]
        public void AuthoredCoverageSamples_MatchCalibratedPhysicalRootHeights(
            int sampleIndex,
            float expectedPixels)
        {
            Assert.That(PlayerWallBraceContact2D.GetSampleHeightPixels(sampleIndex),
                Is.EqualTo(expectedPixels));
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void FullCoverage_ReturnsRequestedWallSide(int side)
        {
            CreateWallSegment(side, CalibratedPlane(side), 0.75f, 3f);

            Assert.That(_probe.FindWallSide(side), Is.EqualTo(side));
        }

        [Test]
        public void TooShortAtTop_DoesNotReturnWallSide()
        {
            CreateWallSegment(1, CalibratedPlane(1), 0.75f, 2.70f);

            Assert.That(_probe.FindWallSide(1f), Is.Zero);
        }

        [Test]
        public void TooShortAtBottom_DoesNotReturnWallSide()
        {
            CreateWallSegment(-1, CalibratedPlane(-1), 0.96f, 3f);

            Assert.That(_probe.FindWallSide(-1f), Is.Zero);
        }

        [Test]
        public void MiddleGap_DoesNotReturnWallSide()
        {
            CreateWallSegment(1, CalibratedPlane(1), 0.75f, 1.70f);
            CreateWallSegment(1, CalibratedPlane(1), 2f, 3f);

            Assert.That(_probe.FindWallSide(1f), Is.Zero);
        }

        [TestCase(1)]
        [TestCase(-1)]
        public void MisalignedWallPlanes_DoesNotReturnWallSideEvenWhenEverySampleHits(int side)
        {
            float lowerPlane = side * 0.53f;
            float upperPlane = side * 0.599f;
            CreateWallSegment(side, lowerPlane, 0.75f, 1.70f);
            CreateWallSegment(side, upperPlane, 1.70f, 3f);
            Physics2D.SyncTransforms();

            AssertEverySampleHitsNearVerticalGround(side);
            Assert.That(Mathf.Abs(upperPlane - lowerPlane),
                Is.GreaterThan(PlayerWallBraceContact2D.WallPlaneTolerancePixels /
                    PlayerWallBraceContact2D.PixelsPerUnit));
            Assert.That(_probe.FindWallSide(side), Is.Zero);
        }

        private void AssertEverySampleHitsNearVerticalGround(int side)
        {
            float maximumReach = _config.StandingColliderSize.x * 0.5f +
                _config.WallCheckDistance;
            for (int sampleIndex = 0;
                 sampleIndex < PlayerWallBraceContact2D.SampleCount;
                 sampleIndex++)
            {
                Vector2 origin = Vector2.up *
                    (PlayerWallBraceContact2D.GetSampleHeightPixels(sampleIndex) /
                        PlayerWallBraceContact2D.PixelsPerUnit);
                RaycastHit2D hit = Physics2D.Raycast(
                    origin,
                    Vector2.right * side,
                    maximumReach,
                    1 << GroundLayer);
                Assert.That(hit.collider, Is.Not.Null, "Missing sample " + sampleIndex + ".");
                Assert.That(-hit.normal.x * side,
                    Is.GreaterThanOrEqualTo(_config.MinimumWallNormalX));
            }
        }

        private void CreateWallSegment(int side, float wallPlaneX, float bottomY, float topY)
        {
            GameObject wall = Track(new GameObject("Wall Brace coverage segment - Test"));
            wall.layer = GroundLayer;
            BoxCollider2D collider = wall.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(WallThickness, topY - bottomY);
            wall.transform.position = new Vector3(
                wallPlaneX + side * WallThickness * 0.5f,
                (bottomY + topY) * 0.5f,
                0f);
            Physics2D.SyncTransforms();
        }

        private static float CalibratedPlane(int side)
        {
            return side * PlayerWallBraceContact2D.RequiredWallPlaneXPixels /
                PlayerWallBraceContact2D.PixelsPerUnit;
        }

        private GameObject Track(GameObject value)
        {
            _objects.Add(value);
            return value;
        }

        private static void SetObjectReference(Object target, string propertyName, Object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLayerMask(Object target, string propertyName, int value)
        {
            SerializedObject serialized = new SerializedObject(target);
            serialized.FindProperty(propertyName).intValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
