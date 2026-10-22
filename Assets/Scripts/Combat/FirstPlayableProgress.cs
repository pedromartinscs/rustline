using System;

namespace Rustline.Gameplay.Combat
{
    /// <summary>Run state only. Checkpoints commit cleared encounters, never visual/scene state.</summary>
    public sealed class FirstPlayableProgress
    {
        public const int EncounterCount = 3;
        public const float ExtractionDuration = 2f;
        public int ClearedEncounters { get; private set; }
        public bool HasCheckpoint { get; private set; }
        public bool IsComplete { get; private set; }
        public int Retries { get; private set; }
        public float ExtractionTime { get; private set; }

        public bool ClearEncounter(int index)
        {
            if (IsComplete || index != ClearedEncounters || index >= EncounterCount) return false;
            ClearedEncounters++;
            return true;
        }

        public bool TryCheckpoint(bool insideStation)
        {
            if (!insideStation || HasCheckpoint || IsComplete || ClearedEncounters < 2) return false;
            HasCheckpoint = true;
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
            ClearedEncounters = HasCheckpoint ? 2 : 0;
            ExtractionTime = 0f;
            Retries++;
        }

        public void Restart()
        {
            ClearedEncounters = Retries = 0;
            ExtractionTime = 0f;
            HasCheckpoint = IsComplete = false;
        }
    }
}
