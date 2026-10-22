using System;
using System.Linq;
using Rustline.Physics;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>Owns only extension cells east of the accepted intro and its named dressing root.</summary>
    public static class RustlineSalvageIntakeExpansionSetup
    {
        private const string ManagedName = "Expansion Environment - Managed";
        private const string Art = "Assets/Art/Environment/";
        // Walkable surface is one unit above the top row. Existing cells x < 46 are untouched.
        private static readonly RectInt[] Terrain =
        {
            new RectInt(46, 15, 82, 2),                 // continuous gallery / maintenance deck, surface 17
            new RectInt(65, 17, 5, 1),                 // low processing cover, surface 18
            new RectInt(75, 17, 3, 1),                 // approach step to raised firing position
            new RectInt(78, 17, 9, 3),                 // raised gallery machinery deck, surface 20
            new RectInt(120, 21, 2, 10),               // second shaft left wall; 4 u entrance below
            new RectInt(126, 17, 2, 12),               // second shaft right wall; exit surface 29
            new RectInt(128, 27, 16, 2),               // service refuge / bridge approach, surface 29
            new RectInt(148, 27, 48, 2),               // rail / final terminal deck, surface 29
            new RectInt(142, 19, 8, 2),                // bridge-gap catch floor, surface 21
            new RectInt(142, 21, 2, 6),                // four-unit wall-kick recovery well
            new RectInt(148, 21, 2, 6),
            new RectInt(158, 29, 3, 1),                // rail platform approach, surface 30
            new RectInt(161, 29, 8, 3),                // raised rail battery, surface 32
            new RectInt(180, 29, 2, 1),                // terminal low cover
            new RectInt(195, 29, 1, 6),                // terminal end stop
        };

        [UnityEditor.MenuItem("Tools/Rustline/First Playable/Apply Expanded Environment")]
        public static void ApplyEnvironment()
        {
            Scene scene = EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath, OpenSceneMode.Single);
            Tilemap collision = Find<Tilemap>(scene, "Ground Collision - Hidden");
            Tilemap surface = Find<Tilemap>(scene, "Industrial Surface - Visual");
            Tilemap background = Find<Tilemap>(scene, "Background Structure - Visual");
            Transform environment = Find<Transform>(scene, "Environment - Managed Graybox");
            if (collision == null || surface == null || background == null || environment == null)
                throw new InvalidOperationException("Accepted production Tilemaps are missing.");
            TileBase collisionTile = AssetDatabase.LoadAssetAtPath<TileBase>(Art + "Tiles/Generated/MovementCollisionTile.asset");
            TileBase visualTile = AssetDatabase.LoadAssetAtPath<TileBase>(Art + "Tiles/Generated/IndustrialSurfaceRuleTile.asset");
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat");
            if (collisionTile == null || visualTile == null || material == null) throw new InvalidOperationException("Expansion assets missing.");
            Transform previous = environment.Find(ManagedName);
            if (previous != null) UnityEngine.Object.DestroyImmediate(previous.gameObject);
            Transform root = new GameObject(ManagedName).transform;
            root.SetParent(environment, false);
            for (int x = 46; x < 196; x++)
                for (int y = 16; y <= 40; y++)
                {
                    collision.SetTile(new Vector3Int(x, y, 0), null);
                    surface.SetTile(new Vector3Int(x, y, 0), null);
                    background.SetTile(new Vector3Int(x, y, 0), null);
                }
            // Include the two underside rows in this pass's owned range.
            for (int x = 46; x < 196; x++)
                for (int y = 15; y < 16; y++)
                { collision.SetTile(new Vector3Int(x, y, 0), null); surface.SetTile(new Vector3Int(x, y, 0), null); }
            foreach (RectInt rect in Terrain)
            {
                Fill(collision, rect, collisionTile);
                Fill(surface, rect, visualTile);
            }

            // Deliberate 42 px underpass: 19.625 underside minus 17 floor = 2.625 u.
            // Its own offset Tilemaps keep the floor on the original Composite, without Box substitutes.
            Transform grid = new GameObject("Maintenance Precision Grid", typeof(Grid)).transform;
            grid.SetParent(root, false);
            grid.localPosition = Vector3.up * 0.625f;
            Tilemap ceiling = CreateMap(grid, "Maintenance Crouch Collision", true, material);
            Tilemap ceilingVisual = CreateMap(grid, "Maintenance Crouch Visual", false, material);
            Fill(ceiling, new RectInt(102, 19, 6, 1), collisionTile);
            Fill(ceilingVisual, new RectInt(102, 19, 6, 1), visualTile);

            // High background beams / foundations; no decorative collision in the route.
            Fill(background, new RectInt(46, 31, 49, 1), visualTile);
            Fill(background, new RectInt(96, 34, 32, 1), visualTile);
            Fill(background, new RectInt(148, 39, 48, 1), visualTile);
            foreach (int x in new[] { 48, 72, 94 }) Fill(background, new RectInt(x, 5, 2, 10), visualTile);
            foreach (int x in new[] { 152, 174, 192 }) Fill(background, new RectInt(x, 17, 2, 10), visualTile);
            Dress(root, material);
            foreach (Tilemap map in new[] { collision, surface, background, ceiling, ceilingVisual }) map.RefreshAllTiles();
            Bake(collision);
            Bake(ceiling);
            PixelSnappedVerticalDepthBackdrop2D depth = Find<PixelSnappedVerticalDepthBackdrop2D>(scene);
            if (depth != null) depth.Configure(0f, 35f, 4320, 4320, 16);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            ValidateScene(scene);
            Debug.Log($"RUSTLINE_EXPANSION_ENVIRONMENT_OK introEnd=43 newEnd=193 horizontalSpan=213 " +
                $"collisionBounds={collision.cellBounds} originalCompositePaths={collision.GetComponent<CompositeCollider2D>().pathCount}");
        }

        public static void ValidateScene(Scene scene)
        {
            Tilemap collision = Find<Tilemap>(scene, "Ground Collision - Hidden");
            Tilemap ceiling = Find<Tilemap>(scene, "Maintenance Crouch Collision");
            if (collision == null || ceiling == null) throw new InvalidOperationException("Expanded collision maps missing.");
            Bake(collision); Bake(ceiling);
            foreach (RectInt rect in Terrain)
                for (int x = rect.xMin; x < rect.xMax; x++)
                    for (int y = rect.yMin; y < rect.yMax; y++)
                        if (!collision.HasTile(new Vector3Int(x, y, 0))) throw new InvalidOperationException("Missing expanded terrain cell.");
            if (!collision.HasTile(new Vector3Int(45,16,0)) || !collision.HasTile(new Vector3Int(46,16,0)))
                throw new InvalidOperationException("Intro / expansion seam is disconnected.");
            for (int x = 122; x <= 125; x++)
                for (int y = 17; y < 31; y++)
                    if (collision.HasTile(new Vector3Int(x,y,0))) throw new InvalidOperationException("Second shaft opening obstructed.");
            Transform root = Find<Transform>(scene, ManagedName);
            if (root == null || root.GetComponentsInChildren<SpriteRenderer>().Length < 40)
                throw new InvalidOperationException("Expansion dressing missing.");
            foreach (SpriteRenderer sprite in root.GetComponentsInChildren<SpriteRenderer>())
                if (sprite.sprite == null || sprite.transform.localScale != Vector3.one || sprite.GetComponent<Collider2D>() != null)
                    throw new InvalidOperationException("Expansion art must remain native scale, valid and noncolliding.");
        }

        private static Tilemap CreateMap(Transform parent, string name, bool colliding, Material material)
        {
            var go = new GameObject(name, typeof(Tilemap), typeof(TilemapRenderer));
            go.transform.SetParent(parent, false);
            TilemapRenderer renderer = go.GetComponent<TilemapRenderer>();
            renderer.sharedMaterial = material;
            renderer.enabled = !colliding;
            renderer.sortingOrder = 1;
            if (colliding)
            {
                go.layer = 6;
                go.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Static;
                go.AddComponent<TilemapCollider2D>().compositeOperation = Collider2D.CompositeOperation.Merge;
                go.AddComponent<CompositeCollider2D>().geometryType = CompositeCollider2D.GeometryType.Polygons;
                go.AddComponent<TilemapCompositeColliderInitializer2D>();
            }
            return go.GetComponent<Tilemap>();
        }
        private static void Bake(Tilemap map)
        {
            var body = map.GetComponent<Rigidbody2D>();
            var tiles = map.GetComponent<TilemapCollider2D>();
            var composite = map.GetComponent<CompositeCollider2D>();
            if (map.gameObject.layer != 6 || body == null || body.bodyType != RigidbodyType2D.Static ||
                tiles == null || !tiles.enabled || tiles.compositeOperation != Collider2D.CompositeOperation.Merge ||
                composite == null || !composite.enabled || composite.geometryType != CompositeCollider2D.GeometryType.Polygons)
                throw new InvalidOperationException("Release collision contract invalid on " + map.name);
            map.RefreshAllTiles();
            map.GetComponent<TilemapCompositeColliderInitializer2D>().EnsureGeometry();
            if (composite.pathCount == 0 || composite.pointCount == 0) throw new InvalidOperationException("Empty expansion Composite.");
        }
        private static void Fill(Tilemap map, RectInt rect, TileBase tile)
        {
            for (int x = rect.xMin; x < rect.xMax; x++)
                for (int y = rect.yMin; y < rect.yMax; y++) map.SetTile(new Vector3Int(x,y,0), tile);
        }
        private static T Find<T>(Scene scene, string name = null) where T : Component =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<T>(true)).FirstOrDefault(c => name == null || c.name == name);

        private static void Dress(Transform root, Material material)
        {
            void Sprite(string name, string path, float x, float y, int order = -12, bool flip = false)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(Art + path + ".png");
                if (sprite == null) throw new InvalidOperationException("Missing expansion art: " + path);
                var go = new GameObject(name);
                go.transform.SetParent(root, false); go.transform.position = new Vector3(x,y,0f);
                var renderer = go.AddComponent<SpriteRenderer>();
                renderer.sprite = sprite; renderer.sharedMaterial = material; renderer.sortingOrder = order; renderer.flipX = flip;
            }
            for (int x = 46; x < 127; x += 3)
                Sprite("Gallery / maintenance deck " + x, "Architecture/Floor/floor_edge_mid_" + (x % 5 == 0 ? "b" : "a"), x + 1.5f, 16f, 2);
            for (int x = 128; x < 143; x += 3) Sprite("Upper service deck " + x, "Architecture/Floor/floor_edge_mid_a", x + 1.5f, 28f, 2);
            for (int x = 148; x < 193; x += 3) Sprite("Terminal deck " + x, "Architecture/Floor/floor_edge_mid_" + (x % 5 == 0 ? "b" : "a"), x + 1.5f, 28f, 2);
            foreach (int x in new[] { 52, 72, 93 }) Sprite("Gallery gantry " + x, "Machinery/gantry_upright_a", x, 23.625f);
            Sprite("Processing handler", "Machinery/salvage_handler", 62f, 30f);
            Sprite("Processing housing", "Machinery/machine_housing", 82f, 19.3125f, -16);
            Sprite("Maintenance housing", "Machinery/machine_housing", 108f, 19.3125f, -16, true);
            Sprite("Maintenance suspended handler", "Machinery/salvage_handler", 115f, 32f);
            Sprite("Maintenance west gantry", "Machinery/gantry_upright_a", 117f, 26f);
            Sprite("Maintenance east gantry", "Machinery/gantry_upright_b", 131f, 26f);
            foreach (int x in new[] { 153, 174, 190 }) Sprite("Terminal gantry " + x, "Machinery/gantry_upright_b", x, 35.625f);
            Sprite("Rail terminal overhead", "Machinery/gantry_overhead_rail", 175f, 39f);
            Sprite("Terminal machine housing", "Machinery/machine_housing", 164f, 31.3125f, -16);
            Sprite("Extraction bulkhead", "Architecture/bulkhead_door_frame_small", 192f, 32.125f, -10);
        }
    }
}
