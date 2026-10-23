using System;
using System.Globalization;
using UnityEngine;

namespace Rustline.Presentation
{
    public enum RustlineAudioBus { Music, Sfx }

    /// <summary>Only preferences survive scene changes. String storage rejects malformed/legacy types safely.</summary>
    public static class RustlineAudioSettings
    {
        public const string MusicKey = "Rustline.Audio.Music";
        public const string SfxKey = "Rustline.Audio.Sfx";
        private static bool _loaded;
        private static int _music = 100, _sfx = 100;
        public static event Action Changed;
        public static int Music { get { EnsureLoaded(); return _music; } }
        public static int Sfx { get { EnsureLoaded(); return _sfx; } }
        public static float Gain(RustlineAudioBus bus) => (bus == RustlineAudioBus.Music ? Music : Sfx) / 100f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() { _loaded = false; Changed = null; }

        public static int Parse(string value) => int.TryParse(value, NumberStyles.Integer,
            CultureInfo.InvariantCulture, out int parsed) && parsed >= 0 && parsed <= 100 ? parsed : 100;

        public static void Reload()
        {
            _music = Parse(PlayerPrefs.GetString(MusicKey, "100"));
            _sfx = Parse(PlayerPrefs.GetString(SfxKey, "100"));
            _loaded = true;
            Changed?.Invoke();
        }

        public static void Set(RustlineAudioBus bus, int percent)
        {
            EnsureLoaded();
            percent = Mathf.Clamp(percent, 0, 100);
            if (bus == RustlineAudioBus.Music) _music = percent; else _sfx = percent;
            PlayerPrefs.SetString(bus == RustlineAudioBus.Music ? MusicKey : SfxKey,
                percent.ToString(CultureInfo.InvariantCulture));
            Changed?.Invoke();
        }

        public static void Save() => PlayerPrefs.Save();
        private static void EnsureLoaded() { if (!_loaded) Reload(); }
    }
}
