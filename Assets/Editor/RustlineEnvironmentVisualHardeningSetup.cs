using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>Visual-only M7 authoring. Terrain and runtime breaching remain authoritative.</summary>
    public static class RustlineEnvironmentVisualHardeningSetup
    {
        public const string Art = "Assets/Art/Environment/Hardening/";
        private static readonly RectInt[] ReinforcedAreas =
        {
            new RectInt(32,15,2,2), // existing first-shaft exit above its permanent skin
            new RectInt(120,21,2,10), new RectInt(126,17,2,12), // mandatory second shaft
            new RectInt(142,21,2,6), new RectInt(148,21,2,6), // rail recovery well
            new RectInt(195,29,1,6), // terminal end wall
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
            Debug.Log("RUSTLINE_M7_VISUAL_HARDENING_OK: reinforced skins, intact modular traversal plates, anchored service machinery; collision untouched.");
        }

        public static void ApplyVisuals(Scene scene)
        {
            Tilemap surface = Find<Tilemap>(scene,"Industrial Surface - Visual");
            Tilemap collision = Find<Tilemap>(scene,"Ground Collision - Hidden");
            Tile left = Plate("Left"), right = Plate("Right");
            foreach (RectInt area in ReinforcedAreas)
                foreach (Vector2Int point in area.allPositionsWithin)
                {
                    var cell = new Vector3Int(point.x,point.y,0);
                    if (collision == null || surface == null || !collision.HasTile(cell) || !surface.HasTile(cell))
                        throw new InvalidOperationException("M7 may only replace existing visible/colliding plates: " + cell);
                    // Presence of BOTH maps is the existing Longwatch eligibility rule.
                    // Replacing the visual tile preserves it, unlike clearing for a sprite skin.
                    surface.SetTile(cell, point.x == area.xMin ? left : right);
                }
            surface.RefreshAllTiles();
            EditorUtility.SetDirty(surface);
        }

        private static Tile Plate(string side)
        {
            string path = Art + "ReinforcedPlate" + side + ".asset";
            Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile,path);
            }
            tile.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(Art+"reinforced_plate_"+side.ToLowerInvariant()+".png");
            if (tile.sprite == null) throw new InvalidOperationException("Missing reinforced plate sprite.");
            tile.color = Color.white; tile.transform = Matrix4x4.identity;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        public static void ValidateScene(Scene scene)
        {
            Tilemap surface = Find<Tilemap>(scene,"Industrial Surface - Visual");
            Tilemap collision = Find<Tilemap>(scene,"Ground Collision - Hidden");
            foreach (RectInt area in ReinforcedAreas)
                foreach (Vector2Int point in area.allPositionsWithin)
                {
                    var cell = new Vector3Int(point.x,point.y,0);
                    Tile tile = surface != null ? surface.GetTile<Tile>(cell) : null;
                    if (tile == null || !AssetDatabase.GetAssetPath(tile).StartsWith(Art) ||
                        collision == null || !collision.HasTile(cell))
                        throw new InvalidOperationException("Missing M7 reinforced traversal plate at " + cell);
                }
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

        private static T Find<T>(Scene scene,string name) where T : Component => scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<T>(true)).FirstOrDefault(c=>c.name==name);
    }
}
