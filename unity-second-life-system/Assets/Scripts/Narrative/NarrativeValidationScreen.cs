using Ink.UnityIntegration;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Narrative
{
    /// <summary>
    /// Drives the narrative validation screen. Layout and styling are authored in
    /// ValidationScreen.uxml and ValidationScreen.uss; this class only queries
    /// elements, handles interaction, and updates displayed data.
    /// </summary>
    public sealed class NarrativeValidationScreen : MonoBehaviour
    {
        [Header("Content")]
        [SerializeField] InkFile inkFile;

        [Header("UI")]
        [SerializeField] UIDocument document;
        [SerializeField] VisualTreeAsset choiceTemplate;

        [Header("State")]
        [SerializeField] bool helpAvailable = true;

        NarrativeSession _session;
        string _snapshot;
        Label _body;
        Label _status;
        VisualElement _choices;
        Button _save;
        Button _load;

        void Start()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            CacheElements();

            if (!inkFile.isCompiled)
            {
                Show("Validation story is not compiled. Import the ink file in the Unity Editor.", "No choice recorded.");
                return;
            }

            var state = new NarrativeChoiceState { HelpAvailable = helpAvailable };
            _session = new NarrativeSession(inkFile.storyJson, state);
            Refresh(replaceBody: true);
        }

        bool ValidateReferences()
        {
            if (document == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' has no UIDocument assigned. Assign one in the inspector.",
                    this);
                return false;
            }

            if (document.visualTreeAsset == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' has a UIDocument with no Visual Tree Asset assigned.",
                    this);
                return false;
            }

            if (inkFile == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' has no InkFile assigned. Drag the compiled .ink asset in the inspector.",
                    this);
                return false;
            }

            return true;
        }

        void CacheElements()
        {
            var root = document.rootVisualElement;
            _body = root.Q<Label>("narrative-body");
            _status = root.Q<Label>("narrative-status");
            _choices = root.Q<VisualElement>("narrative-choices");
            _save = root.Q<Button>("narrative-save");
            _load = root.Q<Button>("narrative-load");

            if (_body == null || _status == null || _choices == null || _save == null || _load == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' could not find all required elements in the UXML. " +
                    "Expected narrative-body, narrative-status, narrative-choices, narrative-save, narrative-load.",
                    this);
                enabled = false;
                return;
            }

            _save.clicked += Save;
            _load.clicked += Load;
        }

        void OnDisable()
        {
            if (_save != null)
                _save.clicked -= Save;

            if (_load != null)
                _load.clicked -= Load;
        }

        void Refresh(bool replaceBody)
        {
            if (replaceBody)
            {
                var lines = _session.Advance();
                if (lines.Count > 0)
                    _body.text = string.Join("\n", lines);
            }

            RebuildChoices();
            _status.text = Status();
        }

        void RebuildChoices()
        {
            _choices.Clear();
            if (_session == null)
                return;

            foreach (var choice in _session.CurrentChoices)
            {
                var index = choice.Index;
                var button = CreateChoiceButton(choice.Text);
                button.clicked += () =>
                {
                    _session.Choose(index);
                    Refresh(replaceBody: true);
                };
                _choices.Add(button);
            }
        }

        Button CreateChoiceButton(string label)
        {
            var button = choiceTemplate.Instantiate().Q<Button>("narrative-choice");
            button.text = label;
            return button;
        }

        string Status()
        {
            if (_session == null)
                return "No choice recorded.";

            if (!string.IsNullOrEmpty(_session.State.LastError))
                return _session.State.LastError;

            return string.IsNullOrEmpty(_session.State.RecordedChoice)
                ? "No choice recorded."
                : "Recorded choice: " + _session.State.RecordedChoice;
        }

        void Save()
        {
            if (_session == null)
                return;

            _snapshot = _session.Capture();
            _status.text = "Saved. " + Status();
        }

        void Load()
        {
            if (_session == null || string.IsNullOrEmpty(_snapshot))
            {
                _status.text = "Nothing is saved.";
                return;
            }

            if (!_session.TryRestore(_snapshot, out var error))
            {
                _status.text = error;
                return;
            }

            _body.text = "Restored the saved validation state.";
            Refresh(replaceBody: false);
        }

        void Show(string body, string status)
        {
            _body.text = body;
            _status.text = status;
        }
    }
}
