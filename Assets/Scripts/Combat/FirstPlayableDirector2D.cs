using System;
using Rustline.Gameplay.Player;
using Rustline.Gameplay.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Rustline.Gameplay.Combat
{
    [DefaultExecutionOrder(-200)]
    [DisallowMultipleComponent]
    public sealed class FirstPlayableDirector2D : MonoBehaviour
    {
        [SerializeField] private PlayerMotor2D player;
        [SerializeField] private CombatHealth2D playerHealth;
        [SerializeField] private FirstPlayableEncounter2D[] encounters;
        [SerializeField] private Transform entrySpawn;
        [SerializeField] private FirstPlayableCheckpoint2D[] checkpoints;
        [SerializeField] private Rect extractionArea;
        [SerializeField] private float failureHeight = -10f;

        private Rigidbody2D _body;
        private PlayerInputReader _input;
        private PlayerWeaponController2D _weapon;
        private PlayerWeaponAmmo2D _ammo;
        private PlayerWeaponEquipment2D _equipment;
        private float _retryDelay;
        private bool _started;
        public FirstPlayableProgress Progress { get; private set; }
        public CombatHealth2D PlayerHealth => playerHealth;
        public PlayerMotor2D Player => player;
        public FirstPlayableEncounter2D[] Encounters => encounters;
        public bool IsRetrying { get; private set; }
        public float ElapsedSeconds { get; private set; }
        public FirstPlayableCheckpoint2D[] Checkpoints => checkpoints;
        public Transform ActiveSpawn => Progress.HasCheckpoint ? checkpoints[Progress.CheckpointIndex].transform : entrySpawn;
        public string Objective
        {
            get
            {
                int next = Progress.ClearedEncounters;
                if (next == encounters.Length)
                    return Progress.ExtractionTime > 0f ? "EXTRACTING / HOLD POSITION" : "REACH TERMINAL EXIT / HOLD POSITION";
                for (int i = Progress.CheckpointIndex + 1; i < checkpoints.Length; i++)
                    if (checkpoints[i].RequiredClears <= Progress.ClearedEncounters) return "REACH " + checkpoints[i].Label;
                return (next + 1) + " / " + encounters[next].Objective;
            }
        }
        public Rect ExtractionArea => extractionArea;
        public event Action Changed;
        public event Action CheckpointReached;

        private void Awake()
        {
            if (player == null || playerHealth == null || encounters == null ||
                encounters.Length == 0 || entrySpawn == null || checkpoints == null || checkpoints.Length == 0)
            {
                Debug.LogError("First playable scene is missing its authored run references.", this);
                enabled = false;
                return;
            }
            _body = player.GetComponent<Rigidbody2D>();
            _input = player.GetComponent<PlayerInputReader>();
            _weapon = player.GetComponent<PlayerWeaponController2D>();
            _ammo = player.GetComponent<PlayerWeaponAmmo2D>();
            _equipment = player.GetComponent<PlayerWeaponEquipment2D>();
            Progress = new FirstPlayableProgress(encounters.Length);
        }

        private void OnEnable()
        {
            if (playerHealth == null) return;
            playerHealth.Died += OnDeath;
            playerHealth.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            if (playerHealth == null) return;
            playerHealth.Died -= OnDeath;
            playerHealth.Damaged -= OnDamaged;
        }

        private void Start()
        {
            _started = true;
            foreach (FirstPlayableEncounter2D encounter in encounters) encounter.ResetEncounter();
            Changed?.Invoke();
        }

        private void Update()
        {
            if (!_started || (Rustline.Presentation.FrontendController.Current != null &&
                Rustline.Presentation.FrontendController.Current.Navigation.IsModal)) return;
            bool restart = Keyboard.current != null && Keyboard.current.f5Key.wasPressedThisFrame;
            if (Progress.IsComplete)
                restart |= (Keyboard.current != null && Keyboard.current.enterKey.wasPressedThisFrame) ||
                           (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);
            if (Rustline.Presentation.FrontendController.BlockGameplayInput) restart = false;
            if (restart) { RestartRun(); return; }
            if (Progress.IsComplete) return;
            ElapsedSeconds += Time.deltaTime;
            if (IsRetrying)
            {
                _retryDelay -= Time.deltaTime;
                if (_retryDelay <= 0f) RetryNow();
                return;
            }
            if (_body.position.y < failureHeight) { BeginRetry(); return; }
            // Proximity and early hits can wake future encounters. Only the defeated prefix
            // advances objectives; activation never resets health or requires an earlier trigger.
            foreach (FirstPlayableEncounter2D encounter in encounters)
            {
                if (!encounter.IsActive && encounter.ActivationArea.Contains(_body.position))
                {
                    encounter.Activate();
                    Changed?.Invoke();
                }
            }
            bool advanced = false;
            while (Progress.ClearedEncounters < encounters.Length && encounters[Progress.ClearedEncounters].IsDefeated)
            {
                Progress.ClearEncounter(Progress.ClearedEncounters);
                advanced = true;
            }
            if (advanced) Changed?.Invoke();
            for (int i = 0; i < checkpoints.Length; i++)
            {
                FirstPlayableCheckpoint2D checkpoint = checkpoints[i];
                if (Progress.TryCheckpoint(i, checkpoint.RequiredClears, checkpoint.Area.Contains(_body.position)))
                {
                    ClearTransientCombat();
                    playerHealth.ResetHealth();
                    _ammo.ResetAmmunition();
                    CheckpointReached?.Invoke();
                    Changed?.Invoke();
                }
            }
            if (Progress.TickExtraction(Time.deltaTime, extractionArea.Contains(_body.position), playerHealth.IsAlive))
            {
                ClearTransientCombat();
                SetPlayerActive(false);
                Changed?.Invoke();
            }
        }

        public void BeginRetry()
        {
            if (IsRetrying || Progress.IsComplete) return;
            IsRetrying = true;
            _retryDelay = 0.9f;
            ClearTransientCombat();
            SetPlayerActive(false);
            Changed?.Invoke();
        }

        public void RetryNow()
        {
            if (!_started || Progress.IsComplete) return;
            ClearTransientCombat();
            Progress.Retry();
            ResetPendingEncounters();
            RestorePlayer();
        }

        public void RestartRun()
        {
            if (!_started) return;
            Rustline.Presentation.FrontendController.Current?.PrepareForRunRestart();
            ClearTransientCombat();
            Progress.Restart();
            ElapsedSeconds = 0f;
            ResetPendingEncounters();
            RestorePlayer();
        }

        private void ResetPendingEncounters()
        {
            for (int i = Progress.ClearedEncounters; i < encounters.Length; i++) encounters[i].ResetEncounter();
        }

        private void RestorePlayer()
        {
            IsRetrying = false;
            // Re-enabling simulation can rebuild the body from its Transform. Set both
            // representations before collider reset/synchronization in this same frame.
            _body.simulated = true;
            Vector3 destination = ActiveSpawn.position;
            player.transform.position = destination;
            _body.position = destination;
            _body.rotation = 0f;
            _body.angularVelocity = 0f;
            _body.linearVelocity = Vector2.zero;
            player.ResetAfterRespawn();
            _weapon.ResetTransientState();
            playerHealth.ResetHealth();
            _ammo.ResetAmmunition();
            SetPlayerActive(true);
            Physics2D.SyncTransforms();
            Changed?.Invoke();
        }

        private void SetPlayerActive(bool active)
        {
            _body.linearVelocity = Vector2.zero;
            _body.simulated = active;
            player.enabled = active;
            _weapon.enabled = active;
            _equipment.enabled = active;
            _input.enabled = active;
        }

        private void ClearTransientCombat()
        {
            foreach (FirstPlayableEncounter2D encounter in encounters) encounter.ClearBombs();
            _weapon.ResetTransientState();
            // Only a retry/restart/completion path, never a per-frame discovery query.
            foreach (Latch9Projectile2D projectile in FindObjectsByType<Latch9Projectile2D>())
                projectile.CancelForLevelReset();
        }

        private void OnDeath(DamageResult2D result) => BeginRetry();
        private void OnDamaged(DamageResult2D result)
        {
            if (result.DidApply) { Progress.InterruptExtraction(); Changed?.Invoke(); }
        }
    }
}
