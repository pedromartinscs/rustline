using System.Collections;
using System.Reflection;
using NUnit.Framework;
using Rustline.Presentation;
using UnityEngine;
using UnityEngine.TestTools;

namespace Rustline.Tests
{
    public sealed class PixelCameraFollowPlayModeTests
    {
        [UnityTest]
        public IEnumerator Follow_PreservesContinuousSmoothingAndRecoilWithoutDirtyingUnchangedPixels()
        {
            GameObject cameraObject = new GameObject("Camera follow test");
            GameObject targetObject = new GameObject("Follow target test");
            cameraObject.transform.position = new Vector3(0f, 2f, -10f);
            PixelCameraFollow2D follow = cameraObject.AddComponent<PixelCameraFollow2D>();
            typeof(PixelCameraFollow2D).GetField("target", BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(follow, targetObject.transform);
            try
            {
                cameraObject.transform.hasChanged = false;
                yield return null;
                yield return null;
                Assert.That(cameraObject.transform.hasChanged, Is.False,
                    "Stationary follow should not dirty the Transform each frame.");

                // Tiny motion must still accumulate in the continuous state while the
                // rendered position is unchanged; stopping smoothing here loses motion.
                targetObject.transform.position = new Vector3(0.001f, 0f, 0f);
                yield return null;
                yield return null;
                Assert.That(follow.ContinuousFollowPosition.x, Is.GreaterThan(0f));
                Assert.That(cameraObject.transform.position.x, Is.Zero);
                Assert.That(cameraObject.transform.hasChanged, Is.False);

                Vector3 continuous = follow.ContinuousFollowPosition;
                follow.SetPresentationOffset(new Vector2(0.0625f, -0.0625f));
                yield return null;
                yield return null;
                Assert.That(cameraObject.transform.position.x, Is.EqualTo(0.0625f));
                Assert.That(cameraObject.transform.position.y, Is.EqualTo(1.9375f));
                Assert.That(follow.ContinuousFollowPosition.x, Is.GreaterThanOrEqualTo(continuous.x));
                Assert.That(follow.ContinuousFollowPosition.y, Is.EqualTo(2f));

                follow.SetPresentationOffset(Vector2.zero);
                yield return null;
                yield return null;
                Assert.That(cameraObject.transform.position, Is.EqualTo(new Vector3(0f, 2f, -10f)));

                follow.enabled = false;
                cameraObject.transform.position = new Vector3(5f, 8f, -12f);
                follow.enabled = true;
                Assert.That(follow.ContinuousFollowPosition, Is.EqualTo(cameraObject.transform.position),
                    "Re-enabling must restart from the actual Transform, including an external Z change.");
                yield return null;
                yield return null;
                Assert.That(cameraObject.transform.position.z, Is.EqualTo(-12f));
                Assert.That(cameraObject.transform.position.x, Is.LessThan(5f));
            }
            finally
            {
                Object.Destroy(cameraObject);
                Object.Destroy(targetObject);
            }
        }
    }
}
