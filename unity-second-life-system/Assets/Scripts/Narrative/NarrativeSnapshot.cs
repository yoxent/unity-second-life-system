using System;

namespace SecondLife.Narrative
{
    [Serializable]
    public sealed class NarrativeSnapshot
    {
        public const int CurrentVersion = 1;

        public int version;
        public bool helpAvailable;
        public string recordedChoice;
        public string inkState;
    }
}
