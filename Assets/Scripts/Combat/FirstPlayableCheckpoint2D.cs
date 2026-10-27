using UnityEngine;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Authored station data; its Transform is the supported retry spawn.</summary>
    public sealed class FirstPlayableCheckpoint2D : MonoBehaviour
    {
        [SerializeField] private string label;
        [SerializeField, Min(1)] private int requiredClears;
        [SerializeField] private Rect area;
        [SerializeField] private LineRenderer beacon;
        public string Label => label;
        public int RequiredClears => requiredClears;
        public Rect Area => area;
        public LineRenderer Beacon => beacon;
        private LineRenderer _lamp;
        private bool _saved, _eligible, _debug, _presentationInitialized;

        private void Awake()
        {
            if (beacon == null) return;
            // Trigger/spawn data stays authored. The old beacon is now exact developer bounds.
            beacon.enabled = false;
            beacon.useWorldSpace = true;
            beacon.positionCount = 4;
            beacon.SetPositions(new[] { new Vector3(area.xMin,area.yMin), new Vector3(area.xMax,area.yMin),
                new Vector3(area.xMax,area.yMax), new Vector3(area.xMin,area.yMax) });
            Vector3 position = transform.position + new Vector3(1.125f,2.5f,0);
            position.x = Mathf.Round(position.x*16)/16; position.y = Mathf.Round(position.y*16)/16;
            LineRenderer housing = Lamp("Station Lamp Housing",position,7f/16f,0.5f);
            housing.startColor = housing.endColor = Rustline.Presentation.RustlinePalette.GetColor(2);
            _lamp = Lamp("Station Status Lamp",position,3f/16f,0.25f);
            _lamp.sortingOrder = 14;
            SetPresentation(false,false,false);
        }

        private LineRenderer Lamp(string name, Vector3 position, float width, float height)
        {
            var item = new GameObject(name,typeof(LineRenderer));
            item.transform.SetParent(transform,false); item.transform.position = position;
            var line = item.GetComponent<LineRenderer>();
            line.sharedMaterial = beacon.sharedMaterial;
            line.useWorldSpace = false; line.widthMultiplier = width; line.sortingOrder = 13;
            line.positionCount = 2;
            line.SetPosition(0,new Vector3(0,-height/2,0)); line.SetPosition(1,new Vector3(0,height/2,0));
            return line;
        }

        public void SetPresentation(bool developer, bool eligible, bool saved)
        {
            if (_presentationInitialized && developer==_debug && eligible==_eligible && saved==_saved) return;
            _presentationInitialized = true;
            _debug = developer; _eligible = eligible; _saved = saved;
            if (beacon != null)
            {
                beacon.enabled = developer;
                beacon.startColor = beacon.endColor = Rustline.Presentation.RustlinePalette.GetColor(saved ? 20 : eligible ? 19 : 5);
            }
            if (_lamp != null)
                _lamp.startColor = _lamp.endColor = Rustline.Presentation.RustlinePalette.GetColor(saved ? 20 : eligible ? 19 : 3);
        }

        public bool DebugBoundsVisible => _debug && beacon != null && beacon.enabled;
        public string PresentationState => _saved ? "SAVED" : _eligible ? "READY" : "LOCKED";
    }
}
