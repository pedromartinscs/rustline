using UnityEngine;

namespace Rustline.Presentation
{
    /// <summary>Explicit playback route: authored gain multiplied by the selected persistent bus gain.</summary>
    [DisallowMultipleComponent, RequireComponent(typeof(AudioSource))]
    public sealed class AudioBusSource : MonoBehaviour
    {
        [SerializeField] private RustlineAudioBus bus = RustlineAudioBus.Sfx;
        [SerializeField, Range(0f, 1f)] private float authoredGain = 1f;
        private AudioSource _source;
        public AudioSource Source => _source != null ? _source : (_source = GetComponent<AudioSource>());
        public RustlineAudioBus Bus => bus;
        private void OnEnable() { RustlineAudioSettings.Changed += Apply; Apply(); }
        private void OnDisable() => RustlineAudioSettings.Changed -= Apply;
        public void Configure(RustlineAudioBus channel, float gain = 1f)
        {
            bus = channel;
            authoredGain = Mathf.Clamp01(gain);
            Apply();
        }
        private void Apply() => Source.volume = authoredGain * RustlineAudioSettings.Gain(bus);
    }
}
