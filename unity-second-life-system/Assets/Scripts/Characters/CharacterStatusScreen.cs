using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// The status screen's composition root.
    ///
    /// Owns no field-level presentation. It validates references, hands each page
    /// to the binder that writes it, and switches tabs. Adding a tab adds a page
    /// and a binder, not another block of label assignments here.
    ///
    /// Layout and styling are authored in CharacterStatus.uxml and
    /// CharacterStatus.uss.
    ///
    /// <see cref="UIDocument"/> clears its tree on disable and rebuilds it on
    /// enable, so the binders, the tab handlers, and the state subscription are
    /// all rebuilt by <see cref="Bind"/> and dropped by <see cref="Unbind"/>.
    /// A binder created only in Start holds references into a tree that no longer
    /// exists after the first toggle.
    /// </summary>
    public sealed class CharacterStatusScreen : MonoBehaviour
    {
        [Header("Data")]
        [Tooltip("Required. The character whose status is presented.")]
        [SerializeField] CharacterProfile profile;

        [Header("UI")]
        [Tooltip("Required. The UIDocument whose Visual Tree Asset is CharacterStatus.uxml.")]
        [SerializeField] UIDocument document;

        [Tooltip("Required. Template for one title row, because the number of titles is data.")]
        [SerializeField] VisualTreeAsset titleTemplate;

        [Tooltip("Required. Template for the portrait frame, reused by other screens that show a face.")]
        [SerializeField] VisualTreeAsset portraitTemplate;

        CharacterState _state;
        StatusIdentityBinder _identity;
        StatusAttributesBinder _attributes;
        StatusAssessmentBinder _assessment;

        Button _attributesTab;
        Button _skillsTab;
        VisualElement _attributesPage;
        VisualElement _skillsPage;
        bool _started;
        bool _bound;

        void Awake()
        {
            // References only. The visual tree does not exist yet.
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            _state = new CharacterState(profile);
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
            if (profile == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has no Character Profile assigned. Assign one in the inspector.",
                    this);
                return false;
            }

            if (!profile.IsValid)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has an invalid Character Profile '{profile.name}'. " +
                    "Assign an Attribute Scaling asset and use non-negative values.",
                    this);
                return false;
            }

            if (document == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has no UIDocument assigned. Assign one in the inspector.",
                    this);
                return false;
            }

            if (document.visualTreeAsset == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has a UIDocument with no Visual Tree Asset assigned.",
                    this);
                return false;
            }

            if (titleTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has no Title Template assigned. Assign StatusTitle.uxml in the inspector.",
                    this);
                return false;
            }

            if (portraitTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' has no Portrait Template assigned. Assign the portrait UXML in the inspector.",
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
                    $"{nameof(CharacterStatusScreen)} on '{name}' found no visual tree on its UIDocument.",
                    this);
                enabled = false;
                return;
            }

            _identity = StatusIdentityBinder.Create(root, portraitTemplate, this);
            _attributes = StatusAttributesBinder.Create(root, this);
            _assessment = StatusAssessmentBinder.Create(root, titleTemplate, this);
            if (_identity == null || _attributes == null || _assessment == null)
            {
                enabled = false;
                return;
            }

            if (!CacheTabs(root))
            {
                enabled = false;
                return;
            }

            _state.Changed += Refresh;
            _bound = true;

            Refresh();
            ShowPage(Page.Attributes);
        }

        void Unbind()
        {
            if (_attributesTab != null)
                _attributesTab.clicked -= ShowAttributes;

            if (_skillsTab != null)
                _skillsTab.clicked -= ShowSkills;

            if (_bound && _state != null)
                _state.Changed -= Refresh;

            _bound = false;

            // The document is about to clear its contents, so nothing here may
            // outlive this call.
            _identity = null;
            _attributes = null;
            _assessment = null;
            _attributesTab = null;
            _skillsTab = null;
            _attributesPage = null;
            _skillsPage = null;
        }

        bool CacheTabs(VisualElement root)
        {
            _attributesTab = root.Q<Button>("status-tab-attributes");
            _skillsTab = root.Q<Button>("status-tab-skills");
            _attributesPage = root.Q<VisualElement>("status-page-attributes");
            _skillsPage = root.Q<VisualElement>("status-page-skills");

            if (_attributesTab == null || _skillsTab == null || _attributesPage == null || _skillsPage == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' could not find the tab strip. Expected " +
                    "status-tab-attributes, status-tab-skills, status-page-attributes, status-page-skills.",
                    this);
                return false;
            }

            _attributesTab.clicked += ShowAttributes;
            _skillsTab.clicked += ShowSkills;
            return true;
        }

        void ShowAttributes()
        {
            ShowPage(Page.Attributes);
        }

        void ShowSkills()
        {
            ShowPage(Page.Skills);
        }

        void ShowPage(Page page)
        {
            var showAttributes = page == Page.Attributes;

            SetVisible(_attributesPage, showAttributes);
            SetVisible(_skillsPage, !showAttributes);

            SetActive(_attributesTab, showAttributes);
            SetActive(_skillsTab, !showAttributes);
        }

        static void SetVisible(VisualElement element, bool visible)
        {
            element.EnableInClassList("status-page--hidden", !visible);
        }

        static void SetActive(Button tab, bool active)
        {
            tab.EnableInClassList("status-tab--active", active);
        }

        void Refresh()
        {
            _identity.Bind(_state.Profile.Identity);
            _attributes.Bind(_state);

            if (!_assessment.Bind(_state))
            {
                Debug.LogError(
                    $"{nameof(CharacterStatusScreen)} on '{name}' cannot use Title Template '{titleTemplate.name}': " +
                    "it has no Label named status-title.",
                    this);
                enabled = false;
            }
        }

        enum Page
        {
            Attributes,
            Skills
        }
    }
}
