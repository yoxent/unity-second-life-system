using System;
using System.Collections.Generic;
using Ink.Runtime;
using UnityEngine;

namespace SecondLife.Narrative
{
    public readonly struct NarrativeChoice
    {
        public NarrativeChoice(int index, string text)
        {
            Index = index;
            Text = text ?? string.Empty;
        }

        public int Index { get; }

        public string Text { get; }
    }

    public sealed class NarrativeSession
    {
        readonly string _compiledStoryJson;
        NarrativeChoiceState _state;
        Story _story;

        public NarrativeSession(string compiledStoryJson, NarrativeChoiceState state = null)
        {
            if (string.IsNullOrWhiteSpace(compiledStoryJson))
                throw new ArgumentException("A compiled Ink story is required.", nameof(compiledStoryJson));

            _compiledStoryJson = compiledStoryJson;
            _state = state ?? new NarrativeChoiceState();
            _story = CreateStory(_state);
        }

        public NarrativeChoiceState State => _state;

        public bool CanContinue => _story.canContinue;

        public IReadOnlyList<NarrativeChoice> CurrentChoices
        {
            get
            {
                var choices = new List<NarrativeChoice>(_story.currentChoices.Count);
                for (var i = 0; i < _story.currentChoices.Count; i++)
                    choices.Add(new NarrativeChoice(i, _story.currentChoices[i].text));
                return choices;
            }
        }

        public object ReadVariable(string name)
        {
            return _story.variablesState[name];
        }

        public IReadOnlyList<string> Advance()
        {
            var lines = new List<string>();
            while (_story.canContinue)
            {
                var line = _story.Continue().Trim();
                if (line.Length > 0)
                    lines.Add(line);
            }

            return lines;
        }

        public void Choose(int index)
        {
            if (index < 0 || index >= _story.currentChoices.Count)
                throw new ArgumentOutOfRangeException(nameof(index));

            _story.ChooseChoiceIndex(index);
        }

        public string Capture()
        {
            var snapshot = new NarrativeSnapshot
            {
                version = NarrativeSnapshot.CurrentVersion,
                helpAvailable = _state.HelpAvailable,
                recordedChoice = _state.RecordedChoice ?? string.Empty,
                inkState = _story.state.ToJson()
            };
            return JsonUtility.ToJson(snapshot);
        }

        public bool TryRestore(string snapshotJson, out string error)
        {
            var snapshot = JsonUtility.FromJson<NarrativeSnapshot>(snapshotJson);
            if (snapshot == null || snapshot.version != NarrativeSnapshot.CurrentVersion)
            {
                error = "Unsupported narrative save version.";
                return false;
            }

            var restoredState = new NarrativeChoiceState();
            try
            {
                restoredState.Restore(snapshot.helpAvailable, snapshot.recordedChoice);
                var restoredStory = CreateStory(restoredState);
                restoredStory.state.LoadJson(snapshot.inkState);
                _state = restoredState;
                _story = restoredStory;
            }
            catch (Exception exception)
            {
                error = exception.Message;
                return false;
            }

            error = null;
            return true;
        }

        Story CreateStory(NarrativeChoiceState state)
        {
            var story = new Story(_compiledStoryJson);
            Bind(story, state);
            return story;
        }

        static void Bind(Story story, NarrativeChoiceState state)
        {
            story.BindExternalFunction("can_offer_help", (Func<object>)(() => state.CanOfferHelp()), true);
            story.BindExternalFunction("record_choice", (Func<string, object>)(choiceId => state.TryRecord(choiceId)), false);
        }
    }
}
