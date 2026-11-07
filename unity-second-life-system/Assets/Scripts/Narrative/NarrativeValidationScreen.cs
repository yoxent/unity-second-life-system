using System;
using System.Collections.Generic;
using Ink.UnityIntegration;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Narrative
{
    /// <summary>
    /// Drives the narrative validation screen. Layout and styling are authored in
    /// ValidationScreen.uxml and ValidationScreen.uss; this class only queries
    /// elements, handles interaction, and updates displayed data.
    ///
    /// <see cref="UIDocument"/> clears its tree on disable and rebuilds it on
    /// enable, so the query and the button handlers are paired with those
    /// callbacks. Handlers registered only in Start would never come back after a
    /// toggle.
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
        string _bodyText = string.Empty;
        Label _body;
        Label _status;
        VisualElement _choices;
        Button _save;
        Button _load;

        // One handler per instantiated choice button, so each can be detached when
        // the row is thrown away. A lambda that is never removed keeps the whole
        // controller alive through the button.
        readonly List<Action> _choiceHandlers = new List<Action>();
        readonly List<Button> _choiceButtons = new List<Button>();
        bool _started;
        bool _bound;
        bool _hasAdvanced;

        void Awake()
        {
            if (!ValidateReferences())
                enabled = false;
        }

        void Start()
        {
            // Start, not OnEnable: UIDocument builds its tree in its own OnEnable,
            // and component order on one GameObject is not guaranteed.
            _started = true;
            Bind();
        }

        void OnEnable()
        {
            // Fires before Start on the first enable, so only a later re-enable
            // rebinds here.
            if (_started)
                Bind();
        }

        void OnDisable()
        {
            Unbind();
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

            if (choiceTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' has no Choice Template assigned. Assign ValidationChoice.uxml in the inspector.",
                    this);
                return false;
            }

            return true;
        }

        void Bind()
        {
            if (_bound)
                return;

            if (!CacheElements())
            {
                enabled = false;
                return;
            }

            _save.clicked += Save;
            _load.clicked += Load;
            _bound = true;

            if (_session == null)
                StartSession();

            // The story is a consuming cursor, so a rebind restores what was
            // already read instead of advancing it a second time.
            var firstBind = !_hasAdvanced;
            Refresh(replaceBody: firstBind);
        }

        void Unbind()
        {
            if (_save != null)
                _save.clicked -= Save;

            if (_load != null)
                _load.clicked -= Load;

            ClearChoiceRows();
            _bound = false;

            _body = null;
            _status = null;
            _choices = null;
            _save = null;
            _load = null;
        }

        bool CacheElements()
        {
            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(
                    $"{nameof(NarrativeValidationScreen)} on '{name}' found no visual tree on its UIDocument.",
                    this);
                return false;
            }

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
                return false;
            }

            return true;
        }

        void StartSession()
        {
            if (!inkFile.isCompiled)
                return;

            var state = new NarrativeChoiceState { HelpAvailable = helpAvailable };
            _session = new NarrativeSession(inkFile.storyJson, state);
        }

        void Refresh(bool replaceBody)
        {
            if (_session == null)
            {
                _bodyText = inkFile.isCompiled
                    ? string.Empty
                    : "Validation story is not compiled. Import the ink file in the Unity Editor.";
                _body.text = _bodyText;
                _status.text = "No choice recorded.";
                ClearChoiceRows();
                return;
            }

            if (replaceBody)
            {
                var lines = _session.Advance();
                if (lines.Count > 0)
                    _bodyText = string.Join("\n", lines);

                _hasAdvanced = true;
            }

            _body.text = _bodyText;
            RebuildChoices();
            _status.text = Status();
        }

        void RebuildChoices()
        {
            ClearChoiceRows();

            if (_session == null)
                return;

            foreach (var choice in _session.CurrentChoices)
            {
                var button = choiceTemplate.Instantiate().Q<Button>("narrative-choice");
                if (button == null)
                {
                    Debug.LogError(
                        $"{nameof(NarrativeValidationScreen)} on '{name}' cannot use Choice Template " +
                        $"'{choiceTemplate.name}': it has no Button named narrative-choice.",
                        this);
                    enabled = false;
                    return;
                }

                button.text = choice.Text;

                // Capture the index, not the button. A VisualElement is recycled
                // presentation, never an identity.
                var index = choice.Index;
                Action handler = () =>
                {
                    _session.Choose(index);
                    Refresh(replaceBody: true);
                };

                button.clicked += handler;
                _choiceButtons.Add(button);
                _choiceHandlers.Add(handler);
                _choices.Add(button);
            }
        }

        void ClearChoiceRows()
        {
            for (var i = 0; i < _choiceButtons.Count; i++)
            {
                if (_choiceButtons[i] != null)
                    _choiceButtons[i].clicked -= _choiceHandlers[i];
            }

            _choiceButtons.Clear();
            _choiceHandlers.Clear();
            _choices?.Clear();
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

            _bodyText = "Restored the saved validation state.";
            Refresh(replaceBody: false);
        }
    }
}
