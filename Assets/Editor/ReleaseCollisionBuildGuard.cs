using Rustline.Physics;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Bakes and validates Tilemap -> Composite geometry in the exact Scene instance Unity is
    /// exporting to a Player. This prevents a Windows Release from being produced with empty
    /// Composite geometry even when Editor Play Mode happens to look correct.
    /// </summary>
    public sealed class ReleaseCollisionBuildGuard : IProcessSceneWithReport
    {
        public int callbackOrder => -10000;

        public void OnProcessScene(Scene scene, BuildReport report)
        {
            // Unity also invokes this callback while reloading scenes for Editor Play Mode.
            // The release guard is intentionally a Player-build concern.
            if (!BuildPipeline.isBuildingPlayer)
            {
                return;
            }

            foreach (GameObject root in scene.GetRootGameObjects())
            {
                TilemapCompositeColliderInitializer2D[] initializers =
                    root.GetComponentsInChildren<TilemapCompositeColliderInitializer2D>(true);
                foreach (TilemapCompositeColliderInitializer2D initializer in initializers)
                {
                    BakeOrFail(scene, initializer);
                }
            }
        }

        private static void BakeOrFail(
            Scene scene,
            TilemapCompositeColliderInitializer2D initializer)
        {
            Tilemap tilemap = initializer.GetComponent<Tilemap>();
            TilemapCollider2D tilemapCollider = initializer.GetComponent<TilemapCollider2D>();
            CompositeCollider2D composite = initializer.GetComponent<CompositeCollider2D>();

            if (tilemap == null ||
                tilemapCollider == null ||
                composite == null ||
                !initializer.enabled ||
                !tilemapCollider.enabled ||
                !composite.enabled ||
                tilemapCollider.compositeOperation != Collider2D.CompositeOperation.Merge)
            {
                throw new BuildFailedException(
                    $"Release collision contract is invalid in scene '{scene.path}' on " +
                    $"'{initializer.gameObject.name}'. See docs/RELEASE_COLLISION.md.");
            }

            // TilemapCollider2D normally processes authored changes in LateUpdate. A build export
            // must not depend on that Editor timing. Refresh the current Tile data, process pending
            // collider work immediately, then regenerate the Composite in the build-scene copy.
            tilemap.RefreshAllTiles();
            initializer.EnsureGeometry();

            if (composite.pathCount <= 0 || composite.pointCount <= 0)
            {
                throw new BuildFailedException(
                    $"Release collision bake produced empty Composite geometry in scene " +
                    $"'{scene.path}' on '{initializer.gameObject.name}'. The Player build was " +
                    "aborted instead of shipping a floor-through regression.");
            }

            Debug.Log(
                $"RUSTLINE_RELEASE_COLLISION_BAKED scene='{scene.path}' object='{initializer.gameObject.name}' " +
                $"tilemapShapes={tilemapCollider.shapeCount} compositePaths={composite.pathCount} " +
                $"compositePoints={composite.pointCount}");
        }
    }
}
