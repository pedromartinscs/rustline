using System;
using System.Collections.Generic;
using System.Linq;
using Rustline.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

namespace Rustline.Editor
{
    /// <summary>Authors only background presentation; route/collision remain the source of truth.</summary>
    public static class RustlineIndustrialBackgroundSetup
    {
        public const string RootName = "Industrial Background System - Phase 3";
        public const string Art = "Assets/Art/Environment/IndustrialBackground/";
        public const string ArchitectureName = "Facility Architecture - World Anchored";
        private const string MaterialPath = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";
        public static readonly string[] ModuleNames = { "panel_a", "panel_b", "bulkhead", "vent", "pipes", "conduits",
            "shaft_left", "shaft_right", "window", "truss", "service_recess", "relay", "rail_duct", "column", "terminal", "quiet_panel" };

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Apply Full-Level Industrial Background")]
        public static void Apply()
        {
            Scene scene = EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath,OpenSceneMode.Single);
            ApplyToScene(scene);
            ValidateScene(scene);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("RUSTLINE_BACKGROUND_PHASE3_APPLIED: two-axis planes, modular sections, original service wall and gameplay preserved.");
        }

        public static void ApplyToScene(Scene scene)
        {
            ConfigureImports();
            Transform art = Find<Transform>(scene,"Art Dressing - Preserve");
            if (art == null) throw new InvalidOperationException("Missing preserved environment art root.");
            // Keep legacy component data byte-identical for preservation checks, but retire its
            // renderers/references. No old camouflage texture is loaded by the final Player scene.
            foreach (string name in new[] {"Far Parallax v0 - Managed","Vertical Depth Backdrop v0 - Managed"})
            {
                Transform legacy = art.Find(name);
                if (legacy == null) throw new InvalidOperationException("Missing expected legacy background: " + name);
                foreach (SpriteRenderer renderer in legacy.GetComponentsInChildren<SpriteRenderer>(true))
                { renderer.enabled = false; renderer.sprite = null; }
                legacy.gameObject.SetActive(false);
            }
            Tilemap legacyMap = Find<Tilemap>(scene,"Background Structure - Visual");
            legacyMap.GetComponent<TilemapRenderer>().enabled = false;
            Transform root = art.Find(RootName);
            if (root == null) { root = new GameObject(RootName).transform; root.SetParent(art,false); }
            Plane(root,"Far Infrastructure - Parallax","far_infrastructure",-40,new Vector2(0.94f,0.96f));
            Plane(root,"Mid Service Network - Parallax","mid_service_network",-30,new Vector2(0.80f,0.86f));
            Transform grid = root.Find(ArchitectureName);
            if (grid == null) { grid = new GameObject(ArchitectureName,typeof(Grid)).transform; grid.SetParent(root,false); }
            grid.GetComponent<Grid>().cellSize = new Vector3(8,8,1);
            Transform mapObject = grid.Find("Modules");
            if (mapObject == null)
            { mapObject = new GameObject("Modules",typeof(Tilemap),typeof(TilemapRenderer)).transform; mapObject.SetParent(grid,false); }
            Tilemap map = mapObject.GetComponent<Tilemap>();
            TilemapRenderer mapRenderer = mapObject.GetComponent<TilemapRenderer>();
            mapRenderer.sharedMaterial = Material;
            mapRenderer.sortingOrder = -25; // Behind approved local wall (-24), before middle distance (-30).
            Tile[] modules = LoadTiles();
            map.ClearAllTiles();
            for (int x=-8;x<=29;x++)
                for (int y=-5;y<=8;y++)
                {
                    int kind = ModuleForCell(x,y);
                    if (kind >= 0) map.SetTile(new Vector3Int(x,y,0),modules[kind]);
                }
            map.CompressBounds();
            EditorUtility.SetDirty(map);
        }

        // Section authoring is a small deterministic layout, rather than randomized dressing or
        // room-sized duplicate paintings. Empty cells are intentional views into the two depths.
        public static int ModuleForCell(int x,int y)
        {
            int wx=x*8+4, wy=y*8+4;
            if (wx < 26) return ((x+y)%4==0) ? 5 : 15; // Service-wall continuation beyond the intro painting.
            if (wx < 42) return x%2==0 ? 6 : 7;       // Open vertical service shaft.
            if (wx < 96)                             // Processing: alternating remote window bays and plant backing.
            {
                if (wy < 16) return (x+y)%3==0 ? 13 : 8;
                if (wy < 24) return x%3==0 ? 3 : (x%3==1 ? 10 : 4);
                if (wy < 32) return x%2==0 ? 8 : -1;
                return wy < 40 ? 9 : -1;
            }
            if (wx < 120) return wy<40 ? new[] { 0,5,11,1 }[(x+y+24)%4] : -1;
            if (wx < 136) return x%2==0 ? 6 : 7;     // Maintenance rise, depth remains open.
            if (wx < 176)                            // Transfer rail: broad voids, high reinforcement, ducts below.
            {
                if (wy < 24) return y%2==0 ? 12 : 8;
                if (wy < 40) return x%3==0 ? 13 : -1;
                return wy < 48 ? 9 : -1;
            }
            if (wy < 24) return (x+y)%2==0 ? 1 : 15;
            if (wy < 40) return new[] { 14,2,5,10 }[(x+28)%4];
            return wy < 48 ? 9 : 8;
        }

        private static Material Material => AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        private static void Plane(Transform root,string name,string spriteName,int order,Vector2 follow)
        {
            Transform child = root.Find(name);
            if (child == null) { child=new GameObject(name,typeof(SpriteRenderer),typeof(PixelSnappedBackgroundPlane2D)).transform; child.SetParent(root,false); }
            child.localPosition=Vector3.zero; child.localScale=Vector3.one; child.localRotation=Quaternion.identity;
            SpriteRenderer renderer=child.GetComponent<SpriteRenderer>();
            renderer.sprite=AssetDatabase.LoadAssetAtPath<Sprite>(Art+spriteName+".png");
            renderer.sharedMaterial=Material; renderer.sortingOrder=order;
            renderer.drawMode=SpriteDrawMode.Tiled; renderer.tileMode=SpriteTileMode.Continuous;
            // 4x4 source repeats cover 128u. Even at maximum viewport + half-period drift,
            // only 99u is needed. The extra margin avoids wrap-edge holes on both axes.
            renderer.size=new Vector2(128,128);
            child.GetComponent<PixelSnappedBackgroundPlane2D>().Configure(follow,new Vector2(32,32));
        }

        private static void ConfigureImports()
        {
            foreach (string name in new[] {"far_infrastructure","mid_service_network","architecture_modules"})
            {
                string path=Art+name+".png";
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                RustlinePixelArtPostprocessor.ApplyBaseline(importer);
                importer.spriteImportMode=name=="architecture_modules" ? SpriteImportMode.Multiple : SpriteImportMode.Single;
                importer.maxTextureSize=512;
                importer.SaveAndReimport();
            }
            var atlas=(TextureImporter)AssetImporter.GetAtPath(Art+"architecture_modules.png");
            var factories=new SpriteDataProviderFactories(); factories.Init();
            ISpriteEditorDataProvider provider=factories.GetSpriteEditorDataProviderFromObject(atlas); provider.InitSpriteEditorDataProvider();
            Dictionary<string,GUID> ids=provider.GetSpriteRects().ToDictionary(r=>r.name,r=>r.spriteID);
            var rects=new SpriteRect[ModuleNames.Length];
            for (int i=0;i<rects.Length;i++) rects[i]=new SpriteRect { name=ModuleNames[i],
                rect=new Rect(i%4*128,(3-i/4)*128,128,128),alignment=SpriteAlignment.Center,pivot=new Vector2(0.5f,0.5f),
                spriteID=ids.TryGetValue(ModuleNames[i],out GUID id) ? id : GUID.Generate() };
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply(); atlas.SaveAndReimport();
        }

        private static Tile[] LoadTiles()
        {
            if (!AssetDatabase.IsValidFolder(Art+"Tiles")) AssetDatabase.CreateFolder(Art.TrimEnd('/'),"Tiles");
            var sprites=AssetDatabase.LoadAllAssetsAtPath(Art+"architecture_modules.png").OfType<Sprite>().ToDictionary(s=>s.name);
            return ModuleNames.Select(name=> {
                string path=Art+"Tiles/"+name+".asset";
                Tile tile=AssetDatabase.LoadAssetAtPath<Tile>(path);
                if (tile==null) { tile=ScriptableObject.CreateInstance<Tile>(); AssetDatabase.CreateAsset(tile,path); }
                tile.sprite=sprites[name]; tile.colliderType=Tile.ColliderType.None; tile.color=Color.white;
                EditorUtility.SetDirty(tile); return tile;
            }).ToArray();
        }

        [UnityEditor.MenuItem("Tools/Rustline/Visual Direction/Validate Full-Level Industrial Background")]
        public static void Validate()
        {
            Scene scene=EditorSceneManager.OpenScene(RustlineFirstPlayableSetup.ScenePath,OpenSceneMode.Single);
            ValidateScene(scene); RustlineVisualRefinementSetup.ValidateScene(scene);
            Debug.Log("RUSTLINE_BACKGROUND_PHASE3_VALIDATED: layered coverage, native modules, retired legacy art, Phase 2 retained.");
        }

        public static void ValidateScene(Scene scene)
        {
            Transform art=Find<Transform>(scene,"Art Dressing - Preserve"), root=art.Find(RootName);
            if (root==null || root.GetComponentsInChildren<Collider2D>(true).Length!=0)
                throw new InvalidOperationException("Missing/nondecorative industrial background root.");
            foreach (string name in new[] {"Far Parallax v0 - Managed","Vertical Depth Backdrop v0 - Managed"})
            {
                Transform legacy=art.Find(name);
                if (legacy==null || legacy.gameObject.activeSelf || legacy.GetComponentsInChildren<SpriteRenderer>(true).Any(r=>r.enabled||r.sprite!=null))
                    throw new InvalidOperationException("Legacy backdrop still active or referenced: "+name);
            }
            if (Find<Tilemap>(scene,"Background Structure - Visual").GetComponent<TilemapRenderer>().enabled)
                throw new InvalidOperationException("Old noisy background structure is still rendered.");
            var planes=root.GetComponentsInChildren<PixelSnappedBackgroundPlane2D>();
            if (planes.Length!=2) throw new InvalidOperationException("Expected two background depths.");
            foreach (var plane in planes)
            {
                SpriteRenderer renderer=plane.GetComponent<SpriteRenderer>();
                bool far=plane.name=="Far Infrastructure - Parallax";
                Vector2 follow=far ? new Vector2(.94f,.96f) : new Vector2(.80f,.86f);
                string path=Art+(far ? "far_infrastructure" : "mid_service_network")+".png";
                if (plane.RepeatWorldSize!=new Vector2(32,32)||renderer.size!=new Vector2(128,128)||
                    plane.CameraFollow!=follow||renderer.sortingOrder!=(far ? -40 : -30)||
                    renderer.sprite!=AssetDatabase.LoadAssetAtPath<Sprite>(path)||
                    renderer.drawMode!=SpriteDrawMode.Tiled||renderer.sharedMaterial!=Material||plane.transform.lossyScale!=Vector3.one)
                    throw new InvalidOperationException("Background coverage/native rendering contract changed.");
            }
            Tilemap map=root.Find(ArchitectureName+"/Modules").GetComponent<Tilemap>();
            if (map.GetComponent<TilemapRenderer>().sortingOrder!=-25 || map.GetUsedTilesCount()!=ModuleNames.Length)
                throw new InvalidOperationException("Invalid modular architecture layout/sorting.");
            for (int x=-8;x<=29;x++) for(int y=-5;y<=8;y++)
            {
                int kind=ModuleForCell(x,y);
                Tile tile=map.GetTile<Tile>(new Vector3Int(x,y,0));
                if (kind<0 ? tile!=null : tile==null||tile.sprite.name!=ModuleNames[kind]||tile.colliderType!=Tile.ColliderType.None)
                    throw new InvalidOperationException("Background section mismatch at "+x+","+y);
            }
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>())
                if (renderer.sprite==null || renderer.color!=Color.white || renderer.transform.lossyScale!=Vector3.one)
                    throw new InvalidOperationException("Non-native background renderer: "+renderer.name);
        }

        private static T Find<T>(Scene scene,string name) where T:Component => scene.GetRootGameObjects()
            .SelectMany(r=>r.GetComponentsInChildren<T>(true)).SingleOrDefault(c=>c.name==name);
    }
}
