using System.Collections.Generic;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>One binary-mask Canvas mesh above existing HUD. No font engine, scaler or UI input module.</summary>
    [DefaultExecutionOrder(1100)]
    public sealed class FrontendView : MonoBehaviour
    {
        private FrontendController _owner;
        private CanvasRenderer _renderer;
        private RectTransform _root;
        private Canvas _canvas;
        private Mesh _mesh, _scratch;
        private Material _material;
        private Texture2D _atlas;
        private readonly List<Vector3> _vertices = new List<Vector3>(8192);
        private readonly List<Vector2> _uvs = new List<Vector2>(8192);
        private readonly List<Color32> _colors = new List<Color32>(8192);
        private readonly List<int> _triangles = new List<int>(12288);
        private readonly List<Vector3> _glyphVertices = new List<Vector3>(256);
        private readonly List<Vector2> _glyphUvs = new List<Vector2>(256);
        private NativePixelViewport _viewport;
        private bool _dirty = true;
        private int _blink;
        public FrontendLayout Layout { get; private set; }
        public Canvas Canvas => _canvas;
        public Mesh PixelMesh => _mesh;
        public Texture2D MaskAtlas => _atlas;
        public int ControlCount => Layout?.Controls.Length ??
            (_owner.Navigation.State == FrontendState.Title ? 4 : _owner.Navigation.State == FrontendState.Credits ? 1 : 3);
        public static readonly string[] TitleLabels = { "START GAME", "OPTIONS", "CREDITS", "QUIT" };
        public static readonly string[] PauseLabels = { "RESUME GAME", "OPTIONS", "QUIT TO MAIN MENU" };
        public static readonly string[] OptionLabels = { "MUSIC VOLUME", "SFX VOLUME", "BACK" };

        // Verified repository identity; deliberately no invented production roles.
        public static readonly string[] Credits =
        {
            "A PROJECT BY", "PEDROMARTINSCS", "ORIGINAL RUSTLINE PIXEL ART",
            "BUILT WITH UNITY 6", "UNIVERSAL RENDER PIPELINE", "UNITY INPUT SYSTEM"
        };

        public void Configure(FrontendController owner)
        {
            _owner = owner;
            _atlas = WeaponHudBitmapFont.CreateAtlas();
            // Atlas RGB is a mask, never a displayed color. Avoid multiplying the font's
            // canonical near-white into the desired canonical text color.
            Color32[] pixels = _atlas.GetPixels32();
            for (int i = 0; i < pixels.Length; i++) if (pixels[i].a != 0) pixels[i] = new Color32(255,255,255,255);
            _atlas.SetPixels32(pixels); _atlas.Apply(false, false);
            _material = new Material(owner.PixelShader)
                { name = "Frontend Canonical Pixel Mask", hideFlags = HideFlags.HideAndDontSave };
            _material.mainTexture = _atlas;
            _mesh = new Mesh { name = "Rustline Frontend Pixels", hideFlags = HideFlags.HideAndDontSave };
            _scratch = new Mesh { hideFlags = HideFlags.HideAndDontSave };
            var canvasObject = new GameObject("Frontend Pixel Canvas", typeof(RectTransform), typeof(Canvas));
            canvasObject.transform.SetParent(transform, false);
            _canvas = canvasObject.GetComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 2000;
            _canvas.pixelPerfect = true;
            _canvas.vertexColorAlwaysGammaSpace = true;
            var rootObject = new GameObject("Native Pixel Menu Surface", typeof(RectTransform), typeof(CanvasRenderer));
            rootObject.transform.SetParent(canvasObject.transform, false);
            _root = rootObject.GetComponent<RectTransform>();
            _root.anchorMin = _root.anchorMax = _root.pivot = new Vector2(0, 1);
            _renderer = rootObject.GetComponent<CanvasRenderer>();
            _renderer.SetMaterial(_material, _atlas);
            RustlineAudioSettings.Changed += Invalidate;
        }

        public void Invalidate() => _dirty = true;

        private void LateUpdate()
        {
            if (_owner == null) return;
            NativePixelViewport next = NativePixelViewportMath.Calculate(Mathf.Max(1, Screen.width), Mathf.Max(1, Screen.height));
            if (next.PhysicalWidth != _viewport.PhysicalWidth || next.PhysicalHeight != _viewport.PhysicalHeight)
            {
                RefreshViewport(next.PhysicalWidth, next.PhysicalHeight);
            }
            int blink = Mathf.FloorToInt(Time.unscaledTime * 2f) % 2;
            if (_blink != blink) { _blink = blink; _dirty = true; }
            if (!_dirty) return;
            Rebuild(next.LogicalWidth, next.LogicalHeight);
            _dirty = false;
        }

        public void RefreshViewport(int physicalWidth, int physicalHeight)
        {
            _viewport = NativePixelViewportMath.Calculate(physicalWidth, physicalHeight);
            _root.anchoredPosition = new Vector2(_viewport.OutputOffsetX,
                -(_viewport.PhysicalHeight - _viewport.OutputOffsetY - _viewport.OutputHeight));
            _root.localScale = Vector3.one * _viewport.IntegerScale;
            _root.sizeDelta = new Vector2(_viewport.LogicalWidth, _viewport.LogicalHeight);
            Rebuild(_viewport.LogicalWidth, _viewport.LogicalHeight);
            _dirty = false;
        }

        public void Rebuild(int width, int height)
        {
            FrontendState state = _owner.Navigation.State;
            Layout = new FrontendLayout(width, height, state);
            _vertices.Clear(); _uvs.Clear(); _colors.Clear(); _triangles.Clear();
            if (state == FrontendState.Playing)
            {
                // Clear() also discards CanvasRenderer's material/texture bindings.
                // Hide only the geometry so the next pause can render normally.
                _mesh.Clear();
                _renderer.SetMesh(_mesh);
                return;
            }
            if (state == FrontendState.Transition)
            {
                Rect(0, 0, width, height, 0);
                CenterText("DEPLOYING", width / 2, height / 2 - 7, width >= 300 ? 2 : 1, 19);
            }
            else DrawPanel(state);
            _mesh.Clear();
            _mesh.SetVertices(_vertices); _mesh.SetUVs(0, _uvs); _mesh.SetColors(_colors);
            _mesh.SetTriangles(_triangles, 0); _mesh.RecalculateBounds();
            // Reassert both bindings whenever presenting a visible menu.
            _renderer.SetMaterial(_material, _atlas);
            _renderer.SetMesh(_mesh);
        }

        private void DrawPanel(FrontendState state)
        {
            RectInt p = Layout.Panel;
            bool title = state == FrontendState.Title;
            bool options = _owner.Navigation.IsOptions;
            bool compact = Layout.Compact;
            int scale = Layout.TextScale;
            // Opaque surfaces retain exact Canonical 28 over the live world/HUD.
            Rect(p.x + 3, p.y + 3, p.width, p.height, 0);
            Rect(p.x, p.y, p.width, p.height, 4);
            Rect(p.x + 1, p.y + 1, p.width - 2, p.height - 2, 1);
            Rect(p.x + 1, p.y + 1, p.width - 2, 3, 5);
            Rect(p.x + 1, p.y + 4, 4, p.height - 5, 9);
            Rect(p.x + 5, p.y + 4, 2, p.height - 5, 10);
            foreach (int x in new[] {p.x + 11, p.xMax - 13})
                foreach (int y in new[] {p.y + 9, p.yMax - 11})
                { Rect(x, y, 3, 3, 5); Rect(x + 1, y + 1, 1, 1, 0); }
            int center = p.x + p.width / 2;
            string heading = title ? "RUSTLINE" : options ? "OPTIONS" : state == FrontendState.Credits ? "RUSTLINE" : "PAUSED";
            int titleWidth = TextWidth(heading, Layout.TitleScale);
            Text(heading, center - titleWidth / 2 + Layout.TitleScale, Layout.HeaderY + Layout.TitleScale, Layout.TitleScale, 8);
            CenterText(heading, center, Layout.HeaderY, Layout.TitleScale, title ? 6 : 19);
            int subtitleY = Layout.HeaderY + Layout.TitleScale * 7 + (compact ? 7 : 17);
            string subtitle = title ? (compact ? "SALVAGE INTAKE" : "ORBITAL SALVAGE / INTAKE 07") :
                options ? (compact ? "AUDIO" : "AUDIO CONTROL / LIVE SETTINGS") :
                state == FrontendState.Credits ? "PROJECT CREDITS" : (compact ? "RUN ON HOLD" : "SALVAGE INTAKE / RUN ON HOLD");
            CenterText(subtitle, center, subtitleY, scale, 5);
            int ruleY = subtitleY + 7 * scale + (compact ? 3 : 12);
            Rect(p.x + 20, ruleY, p.width - 40, 1, 3);
            if (state == FrontendState.Credits)
            {
                int startY = ruleY + (compact ? 8 : 22);
                string[] credits = p.width < 220 || p.height < 180 ?
                    new[] { "PEDROMARTINSCS", "ORIGINAL ART", "UNITY 6 / URP", "INPUT SYSTEM" } : Credits;
                int available = Layout.Controls[0].y - startY - 3;
                int step = Mathf.Max(9, Mathf.Min(compact ? 14 : 30, available / credits.Length));
                for (int i = 0; i < credits.Length; i++)
                    CenterText(credits[i], center, startY + i * step, scale,
                        credits[i] == "PEDROMARTINSCS" ? 12 : 6);
            }
            string[] labels = title ? TitleLabels : options ? OptionLabels :
                state == FrontendState.Credits ? new[] { "BACK" } : PauseLabels;
            for (int i = 0; i < Layout.Controls.Length; i++)
            {
                RectInt r = Layout.Controls[i];
                bool selected = _owner.Selected == i;
                bool pressed = _owner.Pressed == i;
                Rect(r.x, r.y, r.width, r.height, selected ? 18 : 3);
                Rect(r.x + 1, r.y + 1, r.width - 2, r.height - 2, pressed ? 3 : selected ? 2 : 1);
                if (selected)
                {
                    Rect(r.x, r.y, 3, r.height, pressed ? 12 : 19);
                    if (r.width >= 180)
                    {
                        int cy = r.y + r.height / 2;
                        Rect(r.x + 9, cy - 2, 2, 5, 19); Rect(r.x + 11, cy - 1, 2, 3, 19);
                    }
                }
                if (options && i < 2)
                {
                    int value = i == 0 ? RustlineAudioSettings.Music : RustlineAudioSettings.Sfx;
                    string label = r.width < 180 ? (i == 0 ? "MUSIC" : "SFX") : labels[i];
                    Text(label, r.x + 9, r.y + (compact ? 3 : 10), scale, selected ? 20 : 6);
                    string number = value.ToString();
                    Text(number, r.xMax - 9 - TextWidth(number, scale), r.y + (compact ? 3 : 10), scale, 12);
                    RectInt bar = Layout.Sliders[i];
                    Rect(bar.x, bar.y, bar.width, bar.height, 0);
                    int filled = Mathf.RoundToInt((bar.width - 1) * value / 100f);
                    Rect(bar.x, bar.y, filled, bar.height, selected ? 19 : 18);
                    Rect(bar.x + filled - 1, bar.y - (compact ? 0 : 2), 3,
                        bar.height + (compact ? 0 : 4), selected ? 12 : 5);
                }
                else CenterText(r.width < 180 && labels[i] == "QUIT TO MAIN MENU" ? "QUIT TO TITLE" : labels[i], center + (pressed ? 1 : 0),
                    r.y + (r.height - scale * 7) / 2 + (pressed ? 1 : 0), scale, selected ? 20 : 6);
            }
            string hint = compact ? (options ? "LEFT/RIGHT / ESC BACK" : "ENTER / ESC BACK") :
                options ? "LEFT / RIGHT TO ADJUST    ESC BACK" : title ? "SELECT / ENTER    MOUSE / CLICK" : "SELECT / ENTER    ESC BACK";
            if (TextWidth(hint, 1) < p.width - 24) CenterText(hint, center, Layout.FooterY, 1, 5);
            if (!compact)
            {
                if (title) CenterText("DEPLOY / TRAVERSE / FIGHT / EXTRACT", center, p.yMax - 54, 1, 18);
                Rect(p.x + 20, p.yMax - 8, p.width - 40, 2, 8);
                for (int x = p.x + 20; x < p.x + 76; x += 8) Rect(x, p.yMax - 8, 4, 2, 12);
                Rect(p.xMax - 26, p.y + 12, 4, 4, _blink == 0 ? 18 : 19);
            }
        }

        public int HitTest(Vector2 physicalPointer)
        {
            if (Layout == null) return -1;
            Vector2 local = PointerToLocal(physicalPointer);
            for (int i = 0; i < Layout.Controls.Length; i++)
                if (Layout.Controls[i].Contains(new Vector2Int(Mathf.FloorToInt(local.x), Mathf.FloorToInt(local.y)))) return i;
            return -1;
        }
        private Vector2 PointerToLocal(Vector2 pointer) => new Vector2(
            (pointer.x - _viewport.OutputOffsetX) / _viewport.IntegerScale,
            _viewport.LogicalHeight - (pointer.y - _viewport.OutputOffsetY) / _viewport.IntegerScale);
        public int VolumeFromPointer(int row, Vector2 pointer)
        {
            RectInt bar = Layout.Sliders[row];
            return Mathf.Clamp(Mathf.RoundToInt((PointerToLocal(pointer).x - bar.x) * 100f / Mathf.Max(1, bar.width - 1)), 0, 100);
        }

        private static int TextWidth(string value, int scale) => (value.Length * WeaponHudBitmapFont.CellWidth - 1) * scale;
        private void CenterText(string value, int center, int top, int scale, int color) =>
            Text(value, center - TextWidth(value, scale) / 2, top, scale, color);
        private void Text(string value, int x, int y, int scale, int color)
        {
            WeaponHudBitmapFont.BuildMesh(_scratch, value, scale);
            _scratch.GetVertices(_glyphVertices); _scratch.GetUVs(0, _glyphUvs);
            int first = _vertices.Count;
            for (int i = 0; i < _glyphVertices.Count; i++)
            {
                Vector3 point = _glyphVertices[i] * NativePixelPresentation.PixelsPerUnit;
                _vertices.Add(point + new Vector3(x, -y, 0));
                _uvs.Add(_glyphUvs[i]); _colors.Add(RustlinePalette.GetColor(color));
            }
            for (int i = 0; i < _glyphVertices.Count; i += 4) AddTriangles(first + i);
        }
        private void Rect(int x, int y, int width, int height, int color)
        {
            if (width <= 0 || height <= 0) return;
            int first = _vertices.Count;
            _vertices.Add(new Vector3(x, -y, 0)); _vertices.Add(new Vector3(x + width, -y, 0));
            _vertices.Add(new Vector3(x, -y - height, 0)); _vertices.Add(new Vector3(x + width, -y - height, 0));
            Vector2 uv = new Vector2(2.5f / _atlas.width, (_atlas.height - 0.5f) / _atlas.height);
            for (int i = 0; i < 4; i++) { _uvs.Add(uv); _colors.Add(RustlinePalette.GetColor(color)); }
            AddTriangles(first);
        }
        private void AddTriangles(int first)
        {
            _triangles.Add(first); _triangles.Add(first + 2); _triangles.Add(first + 1);
            _triangles.Add(first + 1); _triangles.Add(first + 2); _triangles.Add(first + 3);
        }
        private void OnDestroy()
        {
            RustlineAudioSettings.Changed -= Invalidate;
            if (_canvas != null) Destroy(_canvas.gameObject);
            if (_mesh != null) Destroy(_mesh);
            if (_scratch != null) Destroy(_scratch);
            if (_material != null) Destroy(_material);
            if (_atlas != null) Destroy(_atlas);
        }
    }
}
