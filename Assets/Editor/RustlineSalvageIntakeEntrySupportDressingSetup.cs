using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>
    /// Replaces the temporary tile-based supports beneath the west starting deck with tall native-scale
    /// architectural pillars built from the accepted Wall art family. The pillars are presentation-only,
    /// extend far below the current camera frame, and intentionally have no visible bottom cap.
    /// </summary>
    public static class RustlineSalvageIntakeEntrySupportDressingSetup
    {
        private const string ScenePath = "Assets/Scenes/Demo/SalvageIntake.unity";
        private const string RootName = "RUSTLINE DEMO - SALVAGE INTAKE";
        private const string ArtDressingPreserveName = "Art Dressing - Preserve";
        private const string ManagedRootName = "Entry Support Pillars v0 - Managed";
        private const string BackgroundTilemapName = "Background Structure - Visual";
        private const string SpriteUnlitMaterialPath =
            "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        private const string WallTopPath =
            "Assets/Art/Environment/Architecture/Wall/wall_edge_top.png";
        private const string WallMidPath =
            "Assets/Art/Environment/Architecture/Wall/wall_edge_mid.png";

        private const int SourceWidth = 50;
        private const int SourceHeight = 150;
        private const int TopSpriteRectHeight = 145;
        private const int MidSpriteRectHeight = 150;
        private const int PixelsPerUnit = 16;
        private const int MidSegmentsPerPillar = 4;
        private const int SortingOrder = -1;

        // The temporary RuleTile columns occupied these cells. This setup explicitly clears them so
        // rerunning the route-refinement pass before this dressing remains deterministic.
        private static readonly CellRect[] LegacyTileColumns =
        {
            new CellRect(-23, -12, 2, 8),
            new CellRect(-19, -12, 2, 8),
        };

        // Keep the accepted left support beneath the west entry. Move the second support to the far
        // right end of the 62u safety-floor mass so the two pillars visually carry the whole deck
        // instead of reading as a tight local pair. The right sprite edge lands exactly on x=33.
        private static readonly float[] PillarCenterX = { -22f, 31.4375f };

        // The visible safety-floor mass ends at y=-4. The odd 145px top sprite therefore uses a
        // half-source-pixel center so its upper edge lands exactly on y=-4 without scaling.
        private const float FloorUndersideY = -4f;
        private const float TopCenterY = -8.53125f;
        private const float FirstMidCenterY = -17.75f;
        private const float MidStepWorld = 9.375f;

        private readonly struct CellRect
        {
            internal CellRect(int left, int bottom, int width, int height)
            {
                Left = left;
                Bottom = bottom;
                Width = width;
                Height = height;
            }

            internal int Left { get; }
            internal int Bottom { get; }
            internal int Width { get; }
            internal int Height { get; }
        }

        [MenuItem("Tools/Rustline/Apply Salvage Intake Entry Support Dressing")]
        public static void ApplyFromMenu()
        {
            ApplyAndValidate();
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Architectural entry-support pillars applied beneath the west deck.",
                "OK");
        }

        [MenuItem("Tools/Rustline/Validate Salvage Intake Entry Support Dressing")]
        public static void ValidateFromMenu()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateScene(scene);
            EditorUtility.DisplayDialog(
                "Rustline Salvage Intake",
                "Entry-support pillar validation passed.",
                "OK");
        }

        private static void ApplyAndValidate()
        {
            Require(AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null,
                "SalvageIntake does not exist.");

            Material unlitMaterial = AssetDatabase.LoadAssetAtPath<Material>(SpriteUnlitMaterialPath);
            Require(unlitMaterial != null, "Sprite-Unlit-Default material is unavailable.");

            Sprite topSprite = RequireProductionSprite(WallTopPath, TopSpriteRectHeight);
            Sprite midSprite = RequireProductionSprite(WallMidPath, MidSpriteRectHeight);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameObject root = FindGameObject(scene, RootName);
            Require(root != null, "SalvageIntake root is missing.");
            Transform artRoot = root.transform.Find(ArtDressingPreserveName);
            Require(artRoot != null, "Art Dressing - Preserve is missing.");

            Tilemap background = FindTilemap(scene, BackgroundTilemapName);
            Require(background != null, "Background Structure - Visual Tilemap is missing.");

            foreach (CellRect legacyColumn in LegacyTileColumns)
            {
                ClearRect(background, legacyColumn);
            }
            background.RefreshAllTiles();
            EditorUtility.SetDirty(background);

            Transform previous = artRoot.Find(ManagedRootName);
            if (previous != null)
            {
                UnityEngine.Object.DestroyImmediate(previous.gameObject);
            }

            GameObject managedRoot = new GameObject(ManagedRootName);
            managedRoot.transform.SetParent(artRoot, false);
            managedRoot.transform.position = Vector3.zero;
            managedRoot.transform.localScale = Vector3.one;

            for (int pillarIndex = 0; pillarIndex < PillarCenterX.Length; pillarIndex++)
            {
                bool flipX = pillarIndex == 1;
                CreatePiece(
                    managedRoot.transform,
                    "Entry Support " + (pillarIndex + 1) + " - Top",
                    topSprite,
                    unlitMaterial,
                    new Vector2(PillarCenterX[pillarIndex], TopCenterY),
                    flipX);

                for (int segment = 0; segment < MidSegmentsPerPillar; segment++)
                {
                    CreatePiece(
                        managedRoot.transform,
                        "Entry Support " + (pillarIndex + 1) + " - Mid " + (segment + 1),
                        midSprite,
                        unlitMaterial,
                        new Vector2(
                            PillarCenterX[pillarIndex],
                            FirstMidCenterY - segment * MidStepWorld),
                        flipX);
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            Require(EditorSceneManager.SaveScene(scene, ScenePath),
                "Could not save SalvageIntake after applying entry-support dressing.");
            AssetDatabase.SaveAssets();

            ValidateScene(scene);
        }

        private static void CreatePiece(
            Transform parent,
            string name,
            Sprite sprite,
            Material material,
            Vector2 position,
            bool flipX)
        {
            GameObject piece = new GameObject(name);
            piece.transform.SetParent(parent, false);
            piece.transform.position = new Vector3(position.x, position.y, 0f);
            piece.transform.localScale = Vector3.one;

            SpriteRenderer renderer = piece.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = SortingOrder;
            renderer.flipX = flipX;
        }

        private static Sprite RequireProductionSprite(string assetPath, int expectedRectHeight)
        {
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            Require(sprite != null, "Missing production sprite: " + assetPath);
            Require(Mathf.Approximately(sprite.pixelsPerUnit, PixelsPerUnit),
                assetPath + " must import at 16 PPU.");
            Require(Mathf.RoundToInt(sprite.rect.width) == SourceWidth &&
                Mathf.RoundToInt(sprite.rect.height) == expectedRectHeight,
                assetPath + " imported Sprite rect changed unexpectedly.");

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Require(importer != null, "Missing TextureImporter for production sprite: " + assetPath);
            Require(importer.filterMode == FilterMode.Point,
                assetPath + " must use Point filtering.");
            Require(!importer.mipmapEnabled,
                assetPath + " must keep mipmaps disabled.");

            importer.GetSourceTextureWidthAndHeight(out int actualWidth, out int actualHeight);
            Require(actualWidth == SourceWidth && actualHeight == SourceHeight,
                assetPath + " source dimensions changed unexpectedly.");
            return sprite;
        }

        private static void ValidateScene(Scene scene)
        {
            Require(scene.IsValid() && scene.isLoaded && scene.path == ScenePath,
                "Entry-support validation requires the saved SalvageIntake scene.");

            GameObject root = FindGameObject(scene, RootName);
            Transform artRoot = root?.transform.Find(ArtDressingPreserveName);
            Transform managedRoot = artRoot?.Find(ManagedRootName);
            Require(managedRoot != null, "Entry Support Pillars v0 managed root is missing.");
            Require(managedRoot.localScale == Vector3.one,
                "Entry-support managed root must remain at native scale 1.0.");
            Require(managedRoot.GetComponentsInChildren<Collider2D>(true).Length == 0,
                "Entry-support pillars must remain presentation-only and contain no Collider2D.");

            Tilemap background = FindTilemap(scene, BackgroundTilemapName);
            Require(background != null, "Background Structure - Visual Tilemap is missing.");
            foreach (CellRect legacyColumn in LegacyTileColumns)
            {
                for (int x = legacyColumn.Left; x < legacyColumn.Left + legacyColumn.Width; x++)
                {
                    for (int y = legacyColumn.Bottom; y < legacyColumn.Bottom + legacyColumn.Height; y++)
                    {
                        Require(!background.HasTile(new Vector3Int(x, y, 0)),
                            "Legacy tile-based entry support remains at (" + x + ", " + y + ").");
                    }
                }
            }

            Sprite expectedTop = RequireProductionSprite(WallTopPath, TopSpriteRectHeight);
            Sprite expectedMid = RequireProductionSprite(WallMidPath, MidSpriteRectHeight);
            List<SpriteRenderer> renderers = new List<SpriteRenderer>(
                managedRoot.GetComponentsInChildren<SpriteRenderer>(true));
            Require(renderers.Count == PillarCenterX.Length * (1 + MidSegmentsPerPillar),
                "Entry-support dressing must contain exactly two complete architectural pillars.");

            for (int pillarIndex = 0; pillarIndex < PillarCenterX.Length; pillarIndex++)
            {
                bool expectedFlip = pillarIndex == 1;
                Transform top = managedRoot.Find("Entry Support " + (pillarIndex + 1) + " - Top");
                Require(top != null, "Entry-support top piece is missing.");
                ValidatePiece(top, expectedTop,
                    new Vector2(PillarCenterX[pillarIndex], TopCenterY), expectedFlip);

                for (int segment = 0; segment < MidSegmentsPerPillar; segment++)
                {
                    Transform mid = managedRoot.Find(
                        "Entry Support " + (pillarIndex + 1) + " - Mid " + (segment + 1));
                    Require(mid != null, "Entry-support mid piece is missing.");
                    ValidatePiece(mid, expectedMid,
                        new Vector2(
                            PillarCenterX[pillarIndex],
                            FirstMidCenterY - segment * MidStepWorld),
                        expectedFlip);
                }
            }

            float topHalfHeight = TopSpriteRectHeight / (2f * PixelsPerUnit);
            Require(Mathf.Approximately(TopCenterY + topHalfHeight, FloorUndersideY),
                "Entry-support top pieces must meet the floor underside exactly at y=-4.");
        }

        private static void ValidatePiece(
            Transform piece,
            Sprite expectedSprite,
            Vector2 expectedPosition,
            bool expectedFlip)
        {
            Require(piece.localScale == Vector3.one,
                piece.name + " must remain at native scale 1.0.");
            Require(Mathf.Approximately(piece.position.x, expectedPosition.x) &&
                Mathf.Approximately(piece.position.y, expectedPosition.y),
                piece.name + " moved away from the accepted pillar composition.");

            SpriteRenderer renderer = piece.GetComponent<SpriteRenderer>();
            Require(renderer != null && renderer.sprite == expectedSprite &&
                renderer.sortingOrder == SortingOrder && renderer.flipX == expectedFlip,
                piece.name + " renderer contract is invalid.");
        }

        private static void ClearRect(Tilemap tilemap, CellRect rect)
        {
            for (int x = rect.Left; x < rect.Left + rect.Width; x++)
            {
                for (int y = rect.Bottom; y < rect.Bottom + rect.Height; y++)
                {
                    tilemap.SetTile(new Vector3Int(x, y, 0), null);
                }
            }
        }

        private static GameObject FindGameObject(Scene scene, string name)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Transform transform in sceneRoot.GetComponentsInChildren<Transform>(true))
                {
                    if (transform.name == name)
                    {
                        return transform.gameObject;
                    }
                }
            }
            return null;
        }

        private static Tilemap FindTilemap(Scene scene, string name)
        {
            foreach (GameObject sceneRoot in scene.GetRootGameObjects())
            {
                foreach (Tilemap tilemap in sceneRoot.GetComponentsInChildren<Tilemap>(true))
                {
                    if (tilemap.name == name)
                    {
                        return tilemap;
                    }
                }
            }
            return null;
        }

        private static void Require(bool condition, string message)
        {
            if (!condition)
            {
                throw new InvalidOperationException(message);
            }
        }
    }
}
