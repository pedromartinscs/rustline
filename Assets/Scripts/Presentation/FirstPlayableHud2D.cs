using Rustline.Gameplay.Combat;
using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Event-driven pixel text over the existing presentation; no extra camera or RT.</summary>
    public sealed class FirstPlayableHud2D : MonoBehaviour
    {
        [SerializeField] private FirstPlayableDirector2D director;
        [SerializeField] private Shader uiShader;
        private Texture2D _atlas;
        private Material _material;
        private CanvasRenderer[] _lines;
        private Mesh[] _meshes;
        private string[] _values;
        private GameObject _canvasObject;
        private float _refreshAt;
        private float _flashUntil;
        private float _damageUntil;
        private LineRenderer _exitMarker;
        [SerializeField] private LineRenderer exitMarker;

        private void Start()
        {
            _exitMarker = exitMarker;
            _atlas = WeaponHudBitmapFont.CreateAtlas();
            _material = new Material(uiShader) { hideFlags = HideFlags.HideAndDontSave };
            _material.mainTexture = _atlas;
            _canvasObject = new GameObject("First Playable Pixel HUD", typeof(RectTransform), typeof(Canvas));
            _canvasObject.transform.SetParent(transform, false);
            Canvas canvas = _canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 1000;
            _lines = new CanvasRenderer[7];
            _meshes = new Mesh[7];
            _values = new string[7];
            for (int i = 0; i < _lines.Length; i++)
            {
                GameObject line = new GameObject("Status " + i, typeof(RectTransform), typeof(CanvasRenderer));
                line.transform.SetParent(_canvasObject.transform, false);
                RectTransform rect = line.GetComponent<RectTransform>();
                rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(20f, -20f - i * 22f);
                rect.localScale = Vector3.one * 16f;
                _meshes[i] = new Mesh { name = "First Playable Status", hideFlags = HideFlags.HideAndDontSave };
                _lines[i] = line.GetComponent<CanvasRenderer>();
                _lines[i].SetMaterial(_material, _atlas);
                _lines[i].SetColor(RustlinePalette.GetColor(i < 3 ? 20 : 24));
            }
            director.Changed += Refresh;
            director.CheckpointReached += OnCheckpoint;
            director.PlayerHealth.Damaged += OnDamage;
            Refresh();
        }

        private void Update()
        {
            if (_lines == null) return;
            _lines[1].SetColor(RustlinePalette.GetColor(Time.unscaledTime < _damageUntil ? 13 : 20));
            if (Time.unscaledTime >= _refreshAt)
            {
                _refreshAt = Time.unscaledTime + 0.2f;
                Refresh();
            }
            for (int i = 0; i < director.Checkpoints.Length; i++)
            {
                LineRenderer marker = director.Checkpoints[i].Beacon;
                if (marker != null) marker.startColor = marker.endColor =
                    RustlinePalette.GetColor(i <= director.Progress.CheckpointIndex ? 20 : 13);
            }
            if (_exitMarker != null)
                _exitMarker.startColor = _exitMarker.endColor =
                    RustlinePalette.GetColor(director.Progress.ClearedEncounters == director.Progress.EncounterCount ? 20 : 10);
        }

        private void Refresh()
        {
            if (_lines == null) return;
            FirstPlayableProgress progress = director.Progress;
            SetLine(0, "RUSTLINE / SALVAGE INTAKE");
            SetLine(1, "HEALTH " + director.PlayerHealth.CurrentHealth + " / 100   RETRIES " + progress.Retries);
            string objective;
            if (progress.IsComplete) objective = "EXTRACTION COMPLETE / ENTER TO RESTART";
            else if (director.IsRetrying) objective = progress.HasCheckpoint ? "DOWN / RETURNING TO " + director.Checkpoints[progress.CheckpointIndex].Label : "DOWN / RETRYING FROM WEST BULKHEAD";
            else if (progress.HasCheckpoint && Time.unscaledTime < _flashUntil) objective = "CHECKPOINT SAVED / HEALTH AND AMMO RESTORED";
            else objective = director.Objective;
            SetLine(2, objective);
            SetLine(3, "A/D MOVE / SPACE JUMP / S CROUCH");
            SetLine(4, "MOUSE AIM / LEFT FIRE / RIGHT MODE");
            SetLine(5, "1 LATCH-9 / 2 LONGWATCH / R RELOAD / F5 RESTART / ESC PAUSE");
            int seconds = Mathf.FloorToInt(director.ElapsedSeconds);
            SetLine(6, "TIME " + seconds / 60 + "M " + seconds % 60 + "S / ENCOUNTERS " + progress.ClearedEncounters + " / " + progress.EncounterCount);
        }

        private void SetLine(int index, string value)
        {
            if (_values[index] == value) return;
            _values[index] = value;
            WeaponHudBitmapFont.BuildMesh(_meshes[index], value, 2);
            _lines[index].SetMesh(_meshes[index]);
        }

        private void OnCheckpoint() { _flashUntil = Time.unscaledTime + 3f; }
        private void OnDamage(DamageResult2D damage)
        {
            if (damage.DidApply) _damageUntil = Time.unscaledTime + 0.4f;
        }

        private void OnDestroy()
        {
            if (director != null)
            {
                director.Changed -= Refresh;
                director.CheckpointReached -= OnCheckpoint;
                if (director.PlayerHealth != null) director.PlayerHealth.Damaged -= OnDamage;
            }
            if (_meshes != null) foreach (Mesh mesh in _meshes) if (mesh != null) Destroy(mesh);
            if (_material != null) Destroy(_material);
            if (_atlas != null) Destroy(_atlas);
            if (_canvasObject != null) Destroy(_canvasObject);
        }
    }
}
