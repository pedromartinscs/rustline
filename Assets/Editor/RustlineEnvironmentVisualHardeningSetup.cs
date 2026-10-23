using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>Final environment presentation and solid/destructible material authoring; never alters collision.</summary>
    public static class RustlineEnvironmentVisualHardeningSetup
    {
        public const string Art = "Assets/Art/Environment/Hardening/";
        private const string ManagedName = "Solid Traversal Structure - Managed";
        private const string MaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        // Existing collision rectangles, never new terrain. Left-hand skins align their
        // right edge to the shaft; right-hand skins align their left edge to the shaft.
        private static readonly (string name, RectInt cells, string sprite, bool rightEdge)[] Walls =
        {
            ("Service Shaft Wall - Left A", new RectInt(26,4,2,15), "reinforced_shaft_a", true),
            ("Service Shaft Wall - Right B", new RectInt(32,0,2,17), "reinforced_shaft_b", false),
            ("Maintenance Shaft - Left", new RectInt(120,21,2,10), "reinforced_shaft_a", true),
            ("Maintenance Shaft - Right", new RectInt(126,17,2,12), "reinforced_shaft_b", false),
            ("Rail Recovery - Left", new RectInt(142,21,2,8), "reinforced_shaft_a", true),
            ("Rail Recovery - Right", new RectInt(148,21,2,8), "reinforced_shaft_b", false),
            ("Terminal Solid End Stop", new RectInt(195,29,1,6), "reinforced_shaft_b", false),
        };

        [UnityEditor.MenuItem("Tools/Rustline/Apply Environment Visual Hardening")]
        public static void Apply()
        {
            // Existing specialized passes preserve unrelated authored content and own their
            // exact legacy slice/pivot geometry. None of these passes touches collision.
            RustlineSalvageIntakeFloorDressingSetup.ApplyAndValidate();
            RustlineSalvageIntakeSafetyFloorArmorSetup.ApplyAndValidate();
            RustlineSalvageIntakeWallDressingSetup.ApplyAndValidate();
            RustlineSalvageIntakeServiceShaftDressingSetup.ApplyAndValidate();
            Scene scene = EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath, OpenSceneMode.Single);
            RustlineSalvageIntakeExpansionSetup.RefreshDressing(scene);
            ApplyVisuals(scene);
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RUSTLINE_M71_COHERENCE_OK: continuous solid traversal skins; industrial-only destructible tiles; collision untouched.");
        }

        public static void ApplyVisuals(Scene scene)
        {
            Tilemap surface = Find<Tilemap>(scene,"Industrial Surface - Visual");
            Tilemap collision = Find<Tilemap>(scene,"Ground Collision - Hidden");
            Transform artRoot = Find<Transform>(scene,"Art Dressing - Preserve");
            if (surface == null || collision == null || artRoot == null)
                throw new InvalidOperationException("Missing environment authoring roots/maps.");
            Transform oldRoot = artRoot.Find(ManagedName);
            if (oldRoot != null) UnityEngine.Object.DestroyImmediate(oldRoot.gameObject);
            var root = new GameObject(ManagedName).transform;
            root.SetParent(artRoot,false);
            foreach (var wall in Walls)
            {
                ClearStructuralCells(surface,collision,wall.cells);
                Sprite sprite = RequireSprite(wall.sprite);
                float width = wall.cells.width == 1 ? 1f : sprite.rect.width / 16f;
                float x = wall.rightEdge ? wall.cells.xMax-width : wall.cells.xMin;
                // First shaft objects are owned by their specialized preserved-art pass.
                SpriteRenderer renderer = Find<SpriteRenderer>(scene,wall.name);
                if (renderer == null) renderer = CreateRenderer(root,wall.name);
                Configure(renderer,sprite,new Rect(x,wall.cells.yMin,width,wall.cells.height));
            }
            Tilemap ceiling = Find<Tilemap>(scene,"Maintenance Crouch Visual");
            Tilemap ceilingCollision = Find<Tilemap>(scene,"Maintenance Crouch Collision");
            if (ceiling == null || ceilingCollision == null)
                throw new InvalidOperationException("Missing precision ceiling maps.");
            ClearStructuralCells(ceiling,ceilingCollision,new RectInt(102,19,6,1));
            Configure(CreateRenderer(root,"Maintenance Solid Ceiling"),RequireSprite("reinforced_floor_mid_a"),
                new Rect(102,19.625f,6,1));
            foreach (var deck in new[] { (name:"Catwalk Skin - Left", x:4f), (name:"Catwalk Skin - Right", x:10f) })
            {
                SpriteRenderer renderer = Find<SpriteRenderer>(scene,deck.name);
                if (renderer == null) throw new InvalidOperationException("Missing transfer catwalk skin.");
                Configure(renderer,RequireSprite("reinforced_floor_mid_a"),new Rect(deck.x,4,6,1));
            }
            surface.RefreshAllTiles(); ceiling.RefreshAllTiles();
            EditorUtility.SetDirty(surface); EditorUtility.SetDirty(ceiling);
        }

        private static void ClearStructuralCells(Tilemap visual,Tilemap collision,RectInt area)
        {
            foreach (Vector2Int point in area.allPositionsWithin)
            {
                var cell = new Vector3Int(point.x,point.y,0);
                if (!collision.HasTile(cell)) throw new InvalidOperationException("Missing existing solid collision at " + cell);
                // BreachableTilemap2D already treats collision without a visual tile as
                // permanent architecture. Never change the actual collider or weapon code.
                visual.SetTile(cell,null);
            }
        }

        private static Sprite RequireSprite(string name) => AssetDatabase.LoadAssetAtPath<Sprite>(Art+name+".png")
            ?? throw new InvalidOperationException("Missing structural sprite: " + name);

        private static SpriteRenderer CreateRenderer(Transform parent,string name)
        {
            var go = new GameObject(name); go.transform.SetParent(parent,false);
            return go.AddComponent<SpriteRenderer>();
        }

        private static void Configure(SpriteRenderer renderer,Sprite sprite,Rect bounds)
        {
            renderer.sprite = sprite;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            renderer.drawMode = SpriteDrawMode.Tiled;
            renderer.tileMode = SpriteTileMode.Continuous;
            renderer.size = bounds.size;
            renderer.color = Color.white; renderer.flipX = renderer.flipY = false;
            renderer.sortingOrder = 2;
            renderer.transform.localScale = Vector3.one;
            renderer.transform.position = new Vector3(bounds.center.x,bounds.center.y,0);
        }

        public static void ValidateScene(Scene scene)
        {
            Tilemap surface = Find<Tilemap>(scene,"Industrial Surface - Visual");
            Tilemap collision = Find<Tilemap>(scene,"Ground Collision - Hidden");
            if (surface == null || collision == null) throw new InvalidOperationException("Missing structural maps.");
            foreach (var wall in Walls)
            {
                foreach (Vector2Int point in wall.cells.allPositionsWithin)
                {
                    var cell = new Vector3Int(point.x,point.y,0);
                    if (surface.HasTile(cell) || !collision.HasTile(cell))
                        throw new InvalidOperationException("Solid structure must retain collision without breachable tiles: " + cell);
                }
                float width = wall.cells.width == 1 ? 1f : RequireSprite(wall.sprite).rect.width/16f;
                ValidateSkin(scene,wall.name,wall.sprite,new Rect(wall.rightEdge ? wall.cells.xMax-width : wall.cells.xMin,
                    wall.cells.yMin,width,wall.cells.height));
            }
            foreach (Vector3Int cell in surface.cellBounds.allPositionsWithin)
                if (surface.HasTile(cell) && (!collision.HasTile(cell) || surface.GetSprite(cell)?.texture.name != "industrial_surface"))
                    throw new InvalidOperationException("Only industrial_surface may represent destructible gameplay structure: " + cell);
            Tilemap ceiling = Find<Tilemap>(scene,"Maintenance Crouch Visual");
            if (ceiling == null || ceiling.GetUsedTilesCount() != 0)
                throw new InvalidOperationException("Permanent precision ceiling must not show destructible tiles.");
            ValidateSkin(scene,"Maintenance Solid Ceiling","reinforced_floor_mid_a",new Rect(102,19.625f,6,1));
            ValidateSkin(scene,"Catwalk Skin - Left","reinforced_floor_mid_a",new Rect(4,4,6,1));
            ValidateSkin(scene,"Catwalk Skin - Right","reinforced_floor_mid_a",new Rect(10,4,6,1));
            SpriteRenderer[] renderers = scene.GetRootGameObjects().SelectMany(r=>r.GetComponentsInChildren<SpriteRenderer>(true)).ToArray();
            var handlers = renderers.Where(r=>r.sprite != null && r.sprite.texture.name == "salvage_handler").ToArray();
            if (handlers.Length != 1 || handlers[0].transform.position.x != 0f)
                throw new InvalidOperationException("Keep only the supported intro salvage handler.");
            foreach (string name in new[] {"pressure_reclaimer","exhaust_station","conduit_rack","service_column"})
                if (!renderers.Any(r=>r.sprite != null && r.sprite.texture.name == name))
                    throw new InvalidOperationException("Missing M7 environment family: " + name);
            foreach (SpriteRenderer renderer in renderers.Where(r=>r.sprite != null && AssetDatabase.GetAssetPath(r.sprite).StartsWith(Art)))
            {
                if (renderer.transform.lossyScale != Vector3.one || renderer.color != Color.white ||
                    renderer.GetComponent<Collider2D>() != null || renderer.sortingOrder > 2 || renderer.sprite.pixelsPerUnit != 16f)
                    throw new InvalidOperationException("M7 art must remain native, untinted, behind actors and noncolliding: " + renderer.name);
            }
        }

        private static void ValidateSkin(Scene scene,string name,string sprite,Rect expected)
        {
            SpriteRenderer renderer = Find<SpriteRenderer>(scene,name);
            if (renderer == null || renderer.sprite != RequireSprite(sprite) || renderer.drawMode != SpriteDrawMode.Tiled ||
                renderer.tileMode != SpriteTileMode.Continuous ||
                Vector2.Distance(renderer.bounds.min,expected.min)>0.001f ||
                Vector2.Distance(renderer.bounds.max,expected.max)>0.001f)
                throw new InvalidOperationException("Continuous native structure has invalid source/bounds: " + name);
        }

        private static T Find<T>(Scene scene,string name) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<T>(true)).FirstOrDefault(c=>c.name==name);
    }
}
