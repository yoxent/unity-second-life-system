using System;

namespace SecondLife.Narrative
{
    public sealed class NarrativeChoiceState
    {
        public const string OfferHelp = "offer_help";
        public const string DeclineHelp = "decline_help";

        public bool HelpAvailable { get; set; } = true;

        public string RecordedChoice { get; private set; }

        public string LastError { get; private set; }

        public bool CanOfferHelp()
        {
            return HelpAvailable && string.IsNullOrEmpty(RecordedChoice);
        }

        public bool TryRecord(string choiceId)
        {
            if (!IsAllowed(choiceId))
            {
                LastError = $"Rejected narrative choice '{choiceId ?? string.Empty}'.";
                return false;
            }

            if (!string.IsNullOrEmpty(RecordedChoice))
            {
                LastError = "A narrative choice is already recorded.";
                return false;
            }

            RecordedChoice = choiceId;
            LastError = null;
            return true;
        }

        public void Restore(bool helpAvailable, string recordedChoice)
        {
            if (!string.IsNullOrEmpty(recordedChoice) && !IsAllowed(recordedChoice))
                throw new InvalidOperationException($"Rejected narrative choice '{recordedChoice}'.");

            HelpAvailable = helpAvailable;
            RecordedChoice = string.IsNullOrEmpty(recordedChoice) ? null : recordedChoice;
            LastError = null;
        }

        static bool IsAllowed(string choiceId)
        {
            return choiceId == OfferHelp || choiceId == DeclineHelp;
        }
    }
}
