#if UNITY_EDITOR || DEVELOPMENT_BUILD || RUSTLINE_BENCHMARK
using System;
using UnityEngine;

namespace Rustline.Diagnostics
{
    /// <summary>
    /// Register IMGUI only while a diagnostic is visible. An early return inside
    /// OnGUI still incurs Unity's GUI event setup; the input/sampling owner stays separate.
    /// </summary>
    [AddComponentMenu("")]
    public sealed class DiagnosticGuiView : MonoBehaviour
    {
        private Action _draw;

        private void Awake() => useGUILayout = false;

        public void Initialize(Action draw) => _draw = draw;

        private void OnGUI() => _draw?.Invoke();
    }
}
#endif
