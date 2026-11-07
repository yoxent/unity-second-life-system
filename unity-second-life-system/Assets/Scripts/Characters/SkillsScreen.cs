using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// The skills screen, reached from the navigation bar rather than from a tab on
    /// the character sheet.
    ///
    /// Deliberately almost empty. The skill model does not exist yet. SKL-002 is
    /// closed and confirmed: a skill holds one 0-100 learning-completion value,
    /// and 100 means the full intended effect is learned and immediately usable
    /// with no separate mastery tier. That removes the conflict but does not build
    /// the model, so this controller binds the regions it will fill and presents an
    /// honest empty state rather than inventing a data shape that would then be
    /// read as a rule.
    ///
    /// What it does own is the lifecycle: query in Start, rebind on re-enable, drop
    /// everything on disable. <see cref="UIDocument"/> rebuilds its tree after a
    /// toggle, so references from before the toggle point at discarded elements.
    /// </summary>
    public sealed class SkillsScreen : MonoBehaviour
    {
        [Header("UI")]
        [Tooltip("Required. The UIDocument whose Visual Tree Asset is SkillsScreen.uxml.")]
        [SerializeField] UIDocument document;

        Label _listEmpty;
        Label _treeEmpty;
        bool _started;
        bool _bound;

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
                    $"{nameof(SkillsScreen)} on '{name}' has no UIDocument assigned. Assign one in the inspector.",
                    this);
                return false;
            }

            if (document.visualTreeAsset == null)
            {
                Debug.LogError(
                    $"{nameof(SkillsScreen)} on '{name}' has a UIDocument with no Visual Tree Asset assigned.",
                    this);
                return false;
            }

            return true;
        }

        void Bind()
        {
            if (_bound)
                return;

            var root = document.rootVisualElement;
            if (root == null)
            {
                Debug.LogError(
                    $"{nameof(SkillsScreen)} on '{name}' found no visual tree on its UIDocument.",
                    this);
                enabled = false;
                return;
            }

            _listEmpty = root.Q<Label>("skills-list-empty");
            _treeEmpty = root.Q<Label>("skills-tree-empty");

            if (_listEmpty == null || _treeEmpty == null)
            {
                Debug.LogError(
                    $"{nameof(SkillsScreen)} on '{name}' could not find the empty-state labels. " +
                    "Expected skills-list-empty and skills-tree-empty.",
                    this);
                enabled = false;
                return;
            }

            _bound = true;
            Refresh();
        }

        void Unbind()
        {
            _listEmpty = null;
            _treeEmpty = null;
            _bound = false;
        }

        /// <summary>
        /// There is nothing to count yet, so this only re-states that. It exists as
        /// the single place to add rows once the skill model lands, so the screen has
        /// one refresh path from the start rather than gaining one later.
        ///
        /// SKL-002 is closed, so the row shape is decided: one 0-100 learning value
        /// per skill. What is missing is the model, not the rule.
        /// </summary>
        void Refresh()
        {
            _listEmpty.text =
                "No skills are recorded yet. A skill is learned from a teacher, a book, a trial, " +
                "practice, experience, or a resource. Learning is not mastery.";
            _treeEmpty.text =
                "No tree to draw yet. The tree is built from recorded skills, and none are recorded.";
        }
    }
}
