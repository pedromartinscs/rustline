using System.Collections;
using Rustline.Gameplay.Combat;
using Rustline.Gameplay.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace Rustline.Presentation
{
    [DefaultExecutionOrder(-1000), DisallowMultipleComponent]
    public sealed class FrontendController : MonoBehaviour
    {
        [SerializeField] private Shader pixelShader;
        public Shader PixelShader => pixelShader != null ? pixelShader : Shader.Find("Rustline/Frontend Pixel Mask");
        public const string TitleScene = "Assets/Scenes/UI/MainMenu.unity";
        public const string PlayScene = "Assets/Scenes/Demo/SalvageIntake.unity";
        private static FrontendController _current;
        public static FrontendController Current => _current;
        public static bool BlockGameplayInput => _current != null &&
            (_current.Navigation.IsModal || _current._releaseBarrier || Time.frameCount <= _current._blockedThroughFrame);
        public FrontendStateMachine Navigation { get; private set; }
        public FrontendView View { get; private set; }
        private bool _ownsPause, _releaseBarrier;
        private float _priorTimeScale;
        private bool _priorListenerPause;
        private int _blockedThroughFrame = -1;
        private PlayerInputReader _input;
        private int _selected;
        private int _pressed = -1;
        private Vector2 _lastPointer;
        private float _nextNavigationAt;
        private Vector2 _lastNavigation;
        public int Selected => _selected;
        public int Pressed => _pressed;
        public bool DeveloperHudEnabled { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _current = null;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.path != TitleScene && scene.path != PlayScene) return;
            foreach (GameObject root in scene.GetRootGameObjects())
                if (root.GetComponentInChildren<FrontendController>(true) != null) return;
            var rootObject = new GameObject("Rustline Frontend");
            SceneManager.MoveGameObjectToScene(rootObject, scene);
            rootObject.AddComponent<FrontendController>();
        }

        private void Awake()
        {
            if (_current != null && _current != this) { Destroy(gameObject); return; }
            _current = this;
            Navigation = new FrontendStateMachine(gameObject.scene.path == TitleScene);
            _releaseBarrier = !Navigation.IsModal;
            Time.timeScale = 1f;
            AudioListener.pause = false;
            _input = FindAnyObjectByType<FirstPlayableDirector2D>()?.Player.GetComponent<PlayerInputReader>();
            // The pre-audio playable rig has no listener. Make the future bus sources usable
            // on direct Editor gameplay loads as well as title-to-play transitions.
            if (_input != null && FindAnyObjectByType<AudioListener>() == null)
                _input.gameObject.AddComponent<AudioListener>();
            View = gameObject.AddComponent<FrontendView>();
            View.Configure(this);
            _lastPointer = Mouse.current != null ? Mouse.current.position.ReadValue() : Vector2.zero;
        }

        private void Update()
        {
            if (_current != this) return;
            Keyboard keyboard = Keyboard.current;
            Gamepad pad = Gamepad.current;
            if (_releaseBarrier)
            {
                _input?.FlushForMenu();
                if (!AnyControlHeld()) { _releaseBarrier = false; _blockedThroughFrame = Time.frameCount; }
            }
            if ((keyboard != null && keyboard.escapeKey.wasPressedThisFrame) ||
                (pad != null && (pad.startButton.wasPressedThisFrame || pad.buttonEast.wasPressedThisFrame)))
            {
                Back();
                return; // One edge can perform at most one navigation transition.
            }
            if (!Navigation.IsModal)
            {
                if (!BlockGameplayInput && keyboard != null && keyboard.f3Key.wasPressedThisFrame)
                    DeveloperHudEnabled = !DeveloperHudEnabled;
                return;
            }
            if (Navigation.State == FrontendState.Transition) return;
            Vector2 navigation = Vector2.zero;
            if (keyboard != null)
            {
                if (keyboard.upArrowKey.isPressed || keyboard.wKey.isPressed) navigation.y = 1;
                if (keyboard.downArrowKey.isPressed || keyboard.sKey.isPressed) navigation.y = -1;
                if (keyboard.leftArrowKey.isPressed || keyboard.aKey.isPressed) navigation.x = -1;
                if (keyboard.rightArrowKey.isPressed || keyboard.dKey.isPressed) navigation.x = 1;
            }
            if (pad != null)
            {
                Vector2 axis = pad.dpad.ReadValue() + pad.leftStick.ReadValue();
                if (Mathf.Abs(axis.y) > 0.5f) navigation.y = Mathf.Sign(axis.y);
                if (Mathf.Abs(axis.x) > 0.5f) navigation.x = Mathf.Sign(axis.x);
            }
            if (navigation != Vector2.zero && (navigation != _lastNavigation || Time.unscaledTime >= _nextNavigationAt))
            {
                if (navigation.y != 0) Select((_selected + (navigation.y > 0 ? -1 : 1) + View.ControlCount) % View.ControlCount);
                if (navigation.x != 0 && Navigation.IsOptions && _selected < 2)
                    AdjustVolume(_selected, (int)navigation.x * 5);
                _nextNavigationAt = Time.unscaledTime + (navigation != _lastNavigation ? 0.32f : 0.09f);
            }
            _lastNavigation = navigation;
            bool submit = (keyboard != null && (keyboard.enterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)) ||
                (pad != null && pad.buttonSouth.wasPressedThisFrame);
            if (submit) { Activate(_selected); return; }
            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            Vector2 pointer = mouse.position.ReadValue();
            int hit = View.HitTest(pointer);
            if (pointer != _lastPointer) { if (hit >= 0) Select(hit); _lastPointer = pointer; }
            if (mouse.leftButton.wasPressedThisFrame)
            {
                _pressed = hit;
                if (hit >= 0) Select(hit);
                View.Invalidate();
            }
            if (mouse.leftButton.isPressed && _pressed >= 0 && _pressed < 2 && Navigation.IsOptions)
                SetVolumeFromPointer(_pressed, pointer);
            if (mouse.leftButton.wasReleasedThisFrame)
            {
                int pressed = _pressed;
                _pressed = -1;
                View.Invalidate();
                if (pressed >= 0 && pressed == hit && !(Navigation.IsOptions && pressed < 2)) Activate(pressed);
            }
        }

        private static bool AnyControlHeld()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            Gamepad pad = Gamepad.current;
            return (keyboard != null && keyboard.anyKey.isPressed) ||
                (mouse != null && (mouse.leftButton.isPressed || mouse.rightButton.isPressed)) ||
                (pad != null && (pad.buttonSouth.isPressed || pad.buttonEast.isPressed || pad.startButton.isPressed ||
                    pad.dpad.ReadValue().sqrMagnitude > 0.1f || pad.leftStick.ReadValue().sqrMagnitude > 0.1f));
        }

        public void Select(int index) { _selected = Mathf.Clamp(index, 0, Mathf.Max(0, View.ControlCount - 1)); View.Invalidate(); }
        public void Back()
        {
            if (!Navigation.Back()) return;
            ApplyState();
        }
        public void OpenOptions() { if (Navigation.OpenOptions()) ApplyState(); }
        public void OpenCredits() { if (Navigation.OpenCredits()) ApplyState(); }
        public void Activate(int index)
        {
            switch (Navigation.State)
            {
                case FrontendState.Title:
                    if (index == 0) StartGame();
                    else if (index == 1) OpenOptions();
                    else if (index == 2) OpenCredits();
                    else if (index == 3) Quit();
                    break;
                case FrontendState.Paused:
                    if (index == 0) Back();
                    else if (index == 1) OpenOptions();
                    else if (index == 2) QuitToTitle();
                    break;
                case FrontendState.PauseOptions:
                case FrontendState.TitleOptions: if (index == 2) Back(); break;
                case FrontendState.Credits: Back(); break;
            }
        }

        public void StartGame() { if (Navigation.State == FrontendState.Title) Load(PlayScene); }
        public void QuitToTitle() { if (Navigation.State == FrontendState.Paused) Load(TitleScene); }
        private void Load(string path)
        {
            if (!Navigation.BeginTransition()) return;
            RestorePause();
            Time.timeScale = 1f;
            AudioListener.pause = false;
            RustlineAudioSettings.Save();
            _input?.FlushForMenu();
            View.Invalidate();
            StartCoroutine(LoadScene(path));
        }
        private IEnumerator LoadScene(string path)
        {
            yield return null; // Present transition feedback before loading.
            AsyncOperation load = SceneManager.LoadSceneAsync(path, LoadSceneMode.Single);
            while (!load.isDone) yield return null;
        }

        public void PrepareForRunRestart()
        {
            DeveloperHudEnabled = false;
            if (Navigation.IsOptions && Navigation.IsPaused) Navigation.Back();
            if (Navigation.IsPaused) { Navigation.Back(); ApplyState(); }
        }

        private void ApplyState()
        {
            if (Navigation.IsPaused && !_ownsPause)
            {
                _priorTimeScale = Time.timeScale;
                _priorListenerPause = AudioListener.pause;
                _ownsPause = true;
                Time.timeScale = 0f;
                AudioListener.pause = true;
                _input?.FlushForMenu();
            }
            else if (!Navigation.IsPaused && _ownsPause)
            {
                RestorePause();
                _releaseBarrier = true;
                _blockedThroughFrame = Time.frameCount;
                _input?.FlushForMenu();
            }
            _selected = 0; _pressed = -1; _lastNavigation = Vector2.zero;
            RustlineAudioSettings.Save();
            View.Invalidate();
        }

        private void RestorePause()
        {
            if (!_ownsPause) return;
            Time.timeScale = _priorTimeScale;
            AudioListener.pause = _priorListenerPause;
            _ownsPause = false;
        }
        public void AdjustVolume(int row, int delta) => RustlineAudioSettings.Set(row == 0 ? RustlineAudioBus.Music : RustlineAudioBus.Sfx,
            (row == 0 ? RustlineAudioSettings.Music : RustlineAudioSettings.Sfx) + delta);
        private void SetVolumeFromPointer(int row, Vector2 pointer) => RustlineAudioSettings.Set(
            row == 0 ? RustlineAudioBus.Music : RustlineAudioBus.Sfx, View.VolumeFromPointer(row, pointer));

        private static void Quit()
        {
            RustlineAudioSettings.Save();
#if UNITY_EDITOR
            Debug.Log("RUSTLINE_FRONTEND_QUIT: standalone exits; Editor Play Mode remains open.");
#else
            Application.Quit();
#endif
        }
        private void OnApplicationPause(bool paused) { if (paused) RustlineAudioSettings.Save(); }
        private void OnApplicationQuit() => RustlineAudioSettings.Save();
        private void OnDestroy()
        {
            if (_current != this) return;
            RestorePause();
            _input?.FlushForMenu();
            RustlineAudioSettings.Save();
            _current = null;
        }
    }
}
