using System.Collections.Generic;
using System.Globalization;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Weapons;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Native-pixel player status and an opt-in read-only developer view.</summary>
    [DefaultExecutionOrder(1050)]
    public sealed class FirstPlayableHud2D : MonoBehaviour
    {
        [SerializeField] private FirstPlayableDirector2D director;
        [SerializeField] private Shader uiShader;
        private Texture2D _atlas;
        private Material _material;
        private CanvasRenderer _renderer;
        private RectTransform _root;
        private Mesh _mesh, _scratch;
        private Canvas _canvas;
        private PlayerWeaponEquipment2D _equipment;
        private readonly List<Vector3> _vertices = new List<Vector3>(4096);
        private readonly List<Vector2> _uvs = new List<Vector2>(4096);
        private readonly List<Color32> _colors = new List<Color32>(4096);
        private readonly List<int> _triangles = new List<int>(6144);
        private readonly List<Vector3> _glyphVertices = new List<Vector3>(512);
        private readonly List<Vector2> _glyphUvs = new List<Vector2>(512);
        private NativePixelViewport _viewport;
        private float _refreshAt, _feedbackUntil, _damageUntil;
        private bool _dirty = true, _wasRetrying, _lastDeveloper;
        private string _feedback = "", _feedbackDetail = "";
        [SerializeField] private LineRenderer exitMarker;
        public Canvas Canvas => _canvas;
        public Mesh PixelMesh => _mesh;
        public Texture2D MaskAtlas => _atlas;
        public bool DeveloperVisible => FrontendController.Current != null &&
            FrontendController.Current.DeveloperHudEnabled && !FrontendController.Current.Navigation.IsModal;
        public string FeedbackText => Time.time < _feedbackUntil ? _feedback : "";
        public string ObjectiveText => director.Progress.IsComplete ? "EXTRACTION COMPLETE" : director.Objective;

        private void Start()
        {
            _equipment = director.Player.GetComponent<PlayerWeaponEquipment2D>();
            _atlas = WeaponHudBitmapFont.CreateAtlas();
            Color32[] pixels = _atlas.GetPixels32();
            for (int i=0;i<pixels.Length;i++) if (pixels[i].a!=0) pixels[i]=new Color32(255,255,255,255);
            _atlas.SetPixels32(pixels); _atlas.Apply(false,false);
            _material = new Material(FrontendController.Current != null ? FrontendController.Current.PixelShader : uiShader)
                { hideFlags=HideFlags.HideAndDontSave, mainTexture=_atlas };
            _mesh = new Mesh { name="Player HUD Pixels", hideFlags=HideFlags.HideAndDontSave };
            _scratch = new Mesh { hideFlags=HideFlags.HideAndDontSave };
            var canvasObject = new GameObject("First Playable Pixel HUD",typeof(RectTransform),typeof(Canvas));
            canvasObject.transform.SetParent(transform,false);
            _canvas=canvasObject.GetComponent<Canvas>();
            _canvas.renderMode=RenderMode.ScreenSpaceOverlay; _canvas.sortingOrder=1000;
            _canvas.pixelPerfect=true; _canvas.vertexColorAlwaysGammaSpace=true;
            var surface = new GameObject("Player Status Pixels",typeof(RectTransform),typeof(CanvasRenderer));
            surface.transform.SetParent(canvasObject.transform,false);
            _root=surface.GetComponent<RectTransform>();
            _root.anchorMin=_root.anchorMax=_root.pivot=new Vector2(0,1);
            _renderer=surface.GetComponent<CanvasRenderer>();
            _renderer.SetMaterial(_material,_atlas);
            director.Changed += OnChanged;
            director.CheckpointReached += OnCheckpoint;
            director.PlayerHealth.Damaged += OnDamage;
        }

        private void LateUpdate()
        {
            if (_renderer == null) return;
            bool developer=DeveloperVisible;
            for (int i = 0; i < director.Checkpoints.Length; i++)
            {
                director.Checkpoints[i].SetPresentation(developer,
                    director.Progress.ClearedEncounters>=director.Checkpoints[i].RequiredClears,
                    i<=director.Progress.CheckpointIndex);
            }
            if (exitMarker != null)
                exitMarker.startColor = exitMarker.endColor =
                    RustlinePalette.GetColor(director.Progress.ClearedEncounters == director.Progress.EncounterCount ? 20 : 10);
            NativePixelViewport next=NativePixelViewportMath.Calculate(Mathf.Max(1,Screen.width),Mathf.Max(1,Screen.height));
            if(next.PhysicalWidth!=_viewport.PhysicalWidth || next.PhysicalHeight!=_viewport.PhysicalHeight)
            {
                _viewport=next;
                _root.anchoredPosition=new Vector2(next.OutputOffsetX,-(next.PhysicalHeight-next.OutputOffsetY-next.OutputHeight));
                _root.localScale=Vector3.one*next.IntegerScale;
                _dirty=true;
            }
            if(developer!=_lastDeveloper) { _lastDeveloper=developer; _dirty=true; }
            if(!_dirty && Time.unscaledTime<_refreshAt) return;
            _refreshAt=Time.unscaledTime+0.2f; _dirty=false;
            Rebuild(next.LogicalWidth,next.LogicalHeight);
        }

        private void OnChanged()
        {
            bool freshRun=director.ElapsedSeconds==0f && director.Progress.Retries==0 && !director.Progress.HasCheckpoint;
            if(freshRun) { _feedbackUntil=0; _damageUntil=0; }
            else if(_wasRetrying && !director.IsRetrying) ShowFeedback("BACK IN ACTION","HEALTH AND AMMO RESTORED",2f);
            _wasRetrying=director.IsRetrying; _dirty=true;
        }
        private void ShowFeedback(string message,string detail,float duration)
        { _feedback=message; _feedbackDetail=detail; _feedbackUntil=Time.time+duration; _dirty=true; }

        public void Rebuild(int width,int height)
        {
            _vertices.Clear(); _uvs.Clear(); _colors.Clear(); _triangles.Clear();
            int scale=width>=420 ? 2 : 1;
            int hp=director.PlayerHealth.CurrentHealth;
            int healthColor=hp<=25 || Time.time<_damageUntil ? 14 : 19;
            Rect(12,12,Mathf.Min(156,width-24),32,2); Rect(13,13,Mathf.Min(154,width-26),30,0);
            Text("HEALTH",20,18,1,5);
            Text(hp.ToString(CultureInfo.InvariantCulture),Mathf.Min(114,width-46),17,2,healthColor);
            int filled=Mathf.CeilToInt(hp*10f/director.PlayerHealth.MaximumHealth);
            for(int i=0;i<10;i++) Rect(20+i*8,31,7,5,i<filled ? healthColor : 2);
            Text("OBJECTIVE",12,53,1,5);
            Text(Fit(ObjectiveText,width-24,scale),12,65,scale,6);
            string message="",detail="";
            if(director.Progress.IsComplete) { message="EXTRACTION COMPLETE"; detail="ENTER TO RESTART"; }
            else if(director.IsRetrying) { message="DOWN / RETURNING TO CHECKPOINT"; detail=director.Progress.HasCheckpoint ? director.Checkpoints[director.Progress.CheckpointIndex].Label : "WEST BULKHEAD"; }
            else if(Time.time<_feedbackUntil) { message=_feedback; detail=_feedbackDetail; }
            if(message.Length>0)
            {
                int panelWidth=Mathf.Min(width-24,Mathf.Max(message.Length,detail.Length)*6+16);
                Rect(12,92,panelWidth,detail.Length>0 ? 32 : 20,1);
                Rect(12,92,2,detail.Length>0 ? 32 : 20,19);
                Text(Fit(message,panelWidth-14,1),20,98,1,20);
                if(detail.Length>0) Text(Fit(detail,panelWidth-14,1),20,110,1,6);
            }
            else if(director.ElapsedSeconds<18f)
            {
                string hint=director.ElapsedSeconds<6f ? "A/D MOVE / SPACE JUMP / S CROUCH" :
                    director.ElapsedSeconds<12f ? "MOUSE AIM / CLICK FIRE / R RELOAD" : "0/1/2 WEAPONS / WHEEL CYCLE / RIGHT MODE";
                Text(Fit(hint,width-24,1),12,98,1,5);
            }
            if(DeveloperVisible) DrawDeveloper(width);
            _mesh.Clear(); _mesh.SetVertices(_vertices); _mesh.SetUVs(0,_uvs); _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles,0); _mesh.RecalculateBounds();
            _renderer.SetMaterial(_material,_atlas); _renderer.SetMesh(_mesh);
        }

        private void DrawDeveloper(int width)
        {
            int w=Mathf.Min(width>=700 ? 404 : 320,width-24);
            int x=width-w-12, y=width>=700 ? 12 : 110;
            Rect(x,y,w,112,1); Rect(x,y,w,2,18);
            var p=director.Progress;
            string[] rows={"DEVELOPER / F3 TO HIDE", "HP "+director.PlayerHealth.CurrentHealth+"/"+director.PlayerHealth.MaximumHealth+" / SLOT "+_equipment.SelectedSlot,
                "ENCOUNTERS "+p.ClearedEncounters+"/"+p.EncounterCount+" / NEXT "+Mathf.Min(p.ClearedEncounters+1,p.EncounterCount),
                "TIME "+Mathf.FloorToInt(director.ElapsedSeconds)+"S / RETRIES "+p.Retries,
                "CHECKPOINT "+(p.HasCheckpoint ? director.Checkpoints[p.CheckpointIndex].Label : "NONE")};
            int line=0;
            foreach(string row in rows) Text(Fit(row,w-16,1),x+8,y+8+line++*12,1,6);
            for(int i=0;i<director.Checkpoints.Length;i++)
            {
                var checkpoint=director.Checkpoints[i]; UnityEngine.Rect r=checkpoint.Area;
                string row="CP"+(i+1)+" "+checkpoint.PresentationState+" X"+F(r.xMin)+"-"+F(r.xMax)+" Y"+F(r.yMin)+"-"+F(r.yMax);
                Text(Fit(row,w-16,1),x+8,y+8+line++*12,1,5);
            }
        }
        private static string F(float value) => value.ToString("0.00",CultureInfo.InvariantCulture);
        private static string Fit(string value,int width,int scale)
        {
            int count=Mathf.Max(1,width/(6*scale));
            return value.Length<=count ? value : value.Substring(0,Mathf.Max(0,count-3))+"...";
        }
        private void Text(string value,int x,int y,int scale,int color)
        {
            WeaponHudBitmapFont.BuildMesh(_scratch,value,scale);
            _scratch.GetVertices(_glyphVertices); _scratch.GetUVs(0,_glyphUvs);
            int first=_vertices.Count;
            for(int i=0;i<_glyphVertices.Count;i++)
            { _vertices.Add(_glyphVertices[i]*16f+new Vector3(x,-y,0)); _uvs.Add(_glyphUvs[i]); _colors.Add(RustlinePalette.GetColor(color)); }
            for(int i=0;i<_glyphVertices.Count;i+=4) Quad(first+i);
        }
        private void Rect(int x,int y,int width,int height,int color)
        {
            if(width<=0 || height<=0) return;
            int first=_vertices.Count;
            _vertices.Add(new Vector3(x,-y,0)); _vertices.Add(new Vector3(x+width,-y,0));
            _vertices.Add(new Vector3(x,-y-height,0)); _vertices.Add(new Vector3(x+width,-y-height,0));
            Vector2 uv=new Vector2(2.5f/_atlas.width,(_atlas.height-0.5f)/_atlas.height);
            for(int i=0;i<4;i++) { _uvs.Add(uv); _colors.Add(RustlinePalette.GetColor(color)); }
            Quad(first);
        }
        private void Quad(int first)
        { _triangles.Add(first); _triangles.Add(first+2); _triangles.Add(first+1); _triangles.Add(first+1); _triangles.Add(first+2); _triangles.Add(first+3); }

        private void OnCheckpoint() => ShowFeedback("CHECKPOINT SAVED","HEALTH AND AMMO RESTORED",3f);
        private void OnDamage(DamageResult2D damage)
        {
            if (!damage.DidApply) return;
            _damageUntil=Time.time+0.4f;
            if(director.PlayerHealth.IsAlive) ShowFeedback("TAKING DAMAGE","",0.8f);
        }

        private void OnDestroy()
        {
            if (director != null)
            {
                director.Changed -= OnChanged;
                director.CheckpointReached -= OnCheckpoint;
                if (director.PlayerHealth != null) director.PlayerHealth.Damaged -= OnDamage;
            }
            if (_mesh != null) Destroy(_mesh);
            if (_scratch != null) Destroy(_scratch);
            if (_material != null) Destroy(_material);
            if (_atlas != null) Destroy(_atlas);
            if (_canvas != null) Destroy(_canvas.gameObject);
        }
    }
}
