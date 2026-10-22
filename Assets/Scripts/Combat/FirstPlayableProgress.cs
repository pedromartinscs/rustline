using System;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Run state only. Checkpoints commit cleared encounters, never visual/scene state.</summary>
    public sealed class FirstPlayableProgress
    {
        public const float ExtractionDuration = 2f;
        public int EncounterCount { get; }
        public int ClearedEncounters { get; private set; }
        public int CheckpointIndex { get; private set; } = -1;
        public int CommittedEncounters { get; private set; }
        public bool HasCheckpoint => CheckpointIndex >= 0;
        public bool IsComplete { get; private set; }
        public int Retries { get; private set; }
        public float ExtractionTime { get; private set; }

        public FirstPlayableProgress(int encounterCount)
        {
            if (encounterCount <= 0) throw new ArgumentOutOfRangeException(nameof(encounterCount));
            EncounterCount = encounterCount;
        }

        public bool ClearEncounter(int index)
        {
            if (IsComplete || index != ClearedEncounters || index >= EncounterCount) return false;
            ClearedEncounters++;
            return true;
        }

        public bool TryCheckpoint(int index, int requiredClears, bool insideStation)
        {
            if (!insideStation || IsComplete || index <= CheckpointIndex || requiredClears <= CommittedEncounters ||
                requiredClears > ClearedEncounters || requiredClears > EncounterCount) return false;
            CheckpointIndex = index;
            CommittedEncounters = requiredClears;
            return true;
        }

        public bool TickExtraction(float deltaTime, bool insideZone, bool playerAlive)
        {
            if (deltaTime < 0f || !float.IsFinite(deltaTime))
                throw new ArgumentOutOfRangeException(nameof(deltaTime));
            if (IsComplete) return false;
            if (!insideZone || !playerAlive || ClearedEncounters != EncounterCount)
            {
                ExtractionTime = 0f;
                return false;
            }
            ExtractionTime = Math.Min(ExtractionDuration, ExtractionTime + deltaTime);
            IsComplete = ExtractionTime >= ExtractionDuration;
            return IsComplete;
        }

        public void InterruptExtraction() => ExtractionTime = 0f;
        public void Retry()
        {
            if (IsComplete) return;
            ClearedEncounters = CommittedEncounters;
            ExtractionTime = 0f;
            Retries++;
        }

        public void Restart()
        {
            ClearedEncounters = Retries = 0;
            ExtractionTime = 0f;
            CheckpointIndex = -1;
            CommittedEncounters = 0;
            IsComplete = false;
        }
    }
}
