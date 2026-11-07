using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.UI
{
    /// <summary>
    /// Shows one screen at a time on a shared panel by toggling
    /// <see cref="VisualElement.style.display"/> on each document root.
    ///
    /// Hiding this way keeps every screen's visual tree, bindings, and cached
    /// element references alive, so re-showing a screen costs no UXML parse and
    /// no re-query. Deactivating the GameObject would run OnDisable, clear the
    /// document, and force a rebuild on the next show.
    ///
    /// The router knows nothing about what a screen contains. It does not query
    /// inner buttons and does not call into a screen's controller.
    /// </summary>
    public sealed class ScreenRouter : MonoBehaviour
    {
        [Serializable]
        public struct Screen
        {
            [Tooltip("Stable id used by Show(). Not the GameObject name.")]
            [SerializeField] string id;

            [Tooltip("The document whose root is shown or hidden. Its GameObject must stay active.")]
            [SerializeField] UIDocument document;

            public string Id => id;

            public UIDocument Document => document;
        }

        [Header("Screens")]
        [Tooltip("Every screen the router knows. Ids must be unique and non-empty.")]
        [SerializeField] Screen[] screens = new Screen[0];

        [Header("Startup")]
        [Tooltip("Id of the screen shown on load. Must match one entry above.")]
        [SerializeField] string initialScreen = string.Empty;

        void Start()
        {
            // Start, not Awake or OnEnable: each UIDocument builds its tree in its
            // own OnEnable, and component order on one GameObject is not guaranteed.
            if (!ValidateScreens())
            {
                enabled = false;
                return;
            }

            Show(initialScreen);
        }

        /// <summary>
        /// Shows the screen with this id and hides every other one. An unknown id
        /// logs an error and changes nothing, so a typo cannot blank the screen.
        /// </summary>
        public void Show(string id)
        {
            if (!ValidateScreens())
                return;

            if (!HasScreen(id))
            {
                Debug.LogError(
                    $"{nameof(ScreenRouter)} on '{name}' has no screen with id '{id}'. " +
                    "Known ids: " + KnownIds() + ".",
                    this);
                return;
            }

            foreach (var screen in screens)
                SetVisible(screen.Document, screen.Id == id);
        }

        /// <summary>Hides every screen. Used when no screen should be shown.</summary>
        public void HideAll()
        {
            if (!ValidateScreens())
                return;

            foreach (var screen in screens)
                SetVisible(screen.Document, false);
        }

        static void SetVisible(UIDocument document, bool visible)
        {
            if (document == null)
                return;

            var root = document.rootVisualElement;
            if (root == null)
                return;

            root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }

        bool ValidateScreens()
        {
            if (screens == null || screens.Length == 0)
            {
                Debug.LogError(
                    $"{nameof(ScreenRouter)} on '{name}' has no screens assigned. " +
                    "Add at least one entry with an id and a UIDocument.",
                    this);
                return false;
            }

            foreach (var screen in screens)
            {
                if (screen.Document == null)
                {
                    Debug.LogError(
                        $"{nameof(ScreenRouter)} on '{name}' has a screen entry " +
                        $"('{screen.Id}') with no UIDocument assigned.",
                        this);
                    return false;
                }

                if (string.IsNullOrWhiteSpace(screen.Id))
                {
                    Debug.LogError(
                        $"{nameof(ScreenRouter)} on '{name}' has a screen entry " +
                        $"('{screen.Document.name}') with no id. Ids are how Show() selects a screen.",
                        this);
                    return false;
                }

                if (CountOf(screen.Id) > 1)
                {
                    Debug.LogError(
                        $"{nameof(ScreenRouter)} on '{name}' has the id '{screen.Id}' more than once. " +
                        "Ids must be unique, or Show() would reveal two screens.",
                        this);
                    return false;
                }
            }

            return true;
        }

        int CountOf(string id)
        {
            var count = 0;
            foreach (var screen in screens)
            {
                if (screen.Id == id)
                    count++;
            }

            return count;
        }

        bool HasScreen(string id)
        {
            foreach (var screen in screens)
            {
                if (screen.Id == id)
                    return true;
            }

            return false;
        }

        string KnownIds()
        {
            var ids = new string[screens.Length];
            for (var i = 0; i < screens.Length; i++)
                ids[i] = screens[i].Id;

            return string.Join(", ", ids);
        }
    }
}
