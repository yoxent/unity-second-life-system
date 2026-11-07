using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.UI
{
    /// <summary>
    /// The always-visible bottom navigation bar.
    ///
    /// One bar for the whole game, not one per screen: it is its own
    /// <see cref="UIDocument"/> layered above whichever screen is showing. That
    /// keeps a single owner for navigation and means a new screen never has to
    /// author a nav bar of its own.
    ///
    /// The bar does not know which screen is built. It asks
    /// <see cref="ScreenRouter"/> whether each destination's id exists and
    /// disables the ones that do not, so adding a screen to the router lights its
    /// button without editing the bar, and a typo shows as a disabled button
    /// rather than a dead click.
    ///
    /// Layout and styling are authored in NavigationBar.uxml and
    /// NavigationBar.uss. The group and item templates exist because the number
    /// of groups and the number of destinations per group are data.
    /// </summary>
    public sealed class NavigationBar : MonoBehaviour
    {
        /// <summary>The affordance marking a group that opens a flyout above it.</summary>
        const string Caret = "  ^";

        [Serializable]
        public struct Destination
        {
            [Tooltip("Screen id the router knows. Leave empty until the screen exists; the button then renders disabled.")]
            [SerializeField] string screenId;

            [Tooltip("Label on the flyout button.")]
            [SerializeField] string label;

            public Destination(string screenId, string label)
            {
                this.screenId = screenId;
                this.label = label;
            }

            public string ScreenId => screenId;

            public string Label => label;
        }

        [Serializable]
        public struct Group
        {
            [Tooltip("Label on the group button. The caret is appended by the controller.")]
            [SerializeField] string label;

            [Tooltip("The buttons this group opens. At least one is required.")]
            [SerializeField] Destination[] destinations;

            public Group(string label, Destination[] destinations)
            {
                this.label = label;
                this.destinations = destinations;
            }

            public string Label => label;

            public Destination[] Destinations => destinations ?? Array.Empty<Destination>();
        }

        [Header("References")]
        [Tooltip("Required. The UIDocument whose Visual Tree Asset is NavigationBar.uxml.")]
        [SerializeField] UIDocument document;

        [Tooltip("Required. The owner of screen visibility. The bar asks it which destinations exist and calls Show().")]
        [SerializeField] ScreenRouter router;

        [Tooltip("Required. Template for one group: its button and its flyout.")]
        [SerializeField] VisualTreeAsset groupTemplate;

        [Tooltip("Required. Template for one flyout button, because a group's destination count is data.")]
        [SerializeField] VisualTreeAsset itemTemplate;

        [Header("Groups")]
        [Tooltip("Left to right. Labels and per-group destination labels must be unique.")]
        [SerializeField] Group[] groups = new Group[0];

        readonly List<GroupBinding> _groups = new List<GroupBinding>();
        readonly List<ItemBinding> _items = new List<ItemBinding>();

        VisualElement _groupsHost;
        bool _started;
        bool _bound;

        // Which flyout is showing, and which one a click has latched. One index
        // each rather than per-group flags, so two flyouts can never be open.
        int _openIndex = -1;
        int _pinnedIndex = -1;

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

        /// <summary>
        /// Checks the authored manifest. Returns <c>null</c> when it is usable, or
        /// a phrase naming the first problem. Kept free of UnityEngine state so it
        /// can be asserted without a scene.
        /// </summary>
        public static string ValidateManifest(Group[] groups)
        {
            if (groups == null || groups.Length == 0)
                return "no groups are configured";

            var seenGroups = new HashSet<string>(StringComparer.Ordinal);
            foreach (var group in groups)
            {
                if (string.IsNullOrWhiteSpace(group.Label))
                    return "a group has no label";

                if (!seenGroups.Add(group.Label))
                    return $"the group label '{group.Label}' is used more than once";

                var destinations = group.Destinations;
                if (destinations.Length == 0)
                    return $"the group '{group.Label}' has no destinations";

                var seenLabels = new HashSet<string>(StringComparer.Ordinal);
                foreach (var destination in destinations)
                {
                    if (string.IsNullOrWhiteSpace(destination.Label))
                        return $"a destination in the group '{group.Label}' has no label";

                    if (!seenLabels.Add(destination.Label))
                        return $"the group '{group.Label}' lists '{destination.Label}' more than once";
                }
            }

            return null;
        }

        bool ValidateReferences()
        {
            if (document == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has no UIDocument assigned. Assign one in the inspector.",
                    this);
                return false;
            }

            if (document.visualTreeAsset == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has a UIDocument with no Visual Tree Asset assigned.",
                    this);
                return false;
            }

            if (router == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has no Screen Router assigned. " +
                    "The bar cannot route or tell which destinations exist without it.",
                    this);
                return false;
            }

            if (groupTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has no Group Template assigned. " +
                    "Assign NavigationGroup.uxml in the inspector.",
                    this);
                return false;
            }

            if (itemTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has no Item Template assigned. " +
                    "Assign NavigationItem.uxml in the inspector.",
                    this);
                return false;
            }

            var problem = ValidateManifest(groups);
            if (problem != null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' has an unusable group list: {problem}.",
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
                    $"{nameof(NavigationBar)} on '{name}' found no visual tree on its UIDocument.",
                    this);
                enabled = false;
                return;
            }

            // The document root spans the viewport, so it would swallow every pick
            // meant for the screen underneath. Only the bar and its flyouts, which
            // opt back in with PickingMode.Position, may take input.
            root.pickingMode = PickingMode.Ignore;

            _groupsHost = root.Q<VisualElement>("nav-groups");
            if (_groupsHost == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' could not find 'nav-groups' in NavigationBar.uxml.",
                    this);
                enabled = false;
                return;
            }

            for (var i = 0; i < groups.Length; i++)
            {
                if (!BuildGroup(i, groups[i]))
                {
                    enabled = false;
                    return;
                }
            }

            _openIndex = -1;
            _pinnedIndex = -1;
            _bound = true;
            SetOpen(-1);

            // The bar marks where the player is, not just where they can go. Without
            // this every group reads as equally inactive and the player has to remember
            // which screen they opened.
            router.Changed += HandleScreenChanged;
            MarkCurrent(router.CurrentScreen);
        }

        void HandleScreenChanged(string screenId)
        {
            MarkCurrent(screenId);
        }

        void MarkCurrent(string screenId)
        {
            foreach (var item in _items)
                item.Button.EnableInClassList("nav-item--current", item.ScreenId == screenId);
        }

        bool BuildGroup(int index, Group group)
        {
            var container = groupTemplate.Instantiate();

            var groupRoot = container.Q<VisualElement>("nav-group");
            var button = container.Q<Button>("nav-group-button");
            var flyout = container.Q<VisualElement>("nav-group-flyout");
            var itemsHost = container.Q<VisualElement>("nav-group-items");

            if (groupRoot == null || button == null || flyout == null || itemsHost == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' cannot use Group Template '{groupTemplate.name}'. " +
                    "It must contain nav-group, nav-group-button, nav-group-flyout, and nav-group-items.",
                    this);
                return false;
            }

            button.text = (group.Label ?? string.Empty) + Caret;

            var binding = new GroupBinding
            {
                Index = index,
                Container = groupRoot,
                Button = button
            };

            binding.EnterHandler = _ => HandleEnter(binding);
            binding.LeaveHandler = _ => HandleLeave(binding);
            binding.ClickHandler = () => HandleGroupClick(binding);

            // Enter and leave are registered on the group container, not the button,
            // because the flyout is its child. Moving from the button into the panel
            // therefore never leaves the container, which is what keeps the flyout
            // from flickering shut as the pointer crosses the gap.
            groupRoot.RegisterCallback(binding.EnterHandler);
            groupRoot.RegisterCallback(binding.LeaveHandler);
            button.clicked += binding.ClickHandler;

            foreach (var destination in group.Destinations)
            {
                if (!BuildItem(itemsHost, destination))
                    return false;
            }

            _groupsHost.Add(container);
            _groups.Add(binding);
            return true;
        }

        bool BuildItem(VisualElement host, Destination destination)
        {
            var container = itemTemplate.Instantiate();
            var button = container.Q<Button>("nav-item");
            if (button == null)
            {
                Debug.LogError(
                    $"{nameof(NavigationBar)} on '{name}' cannot use Item Template '{itemTemplate.name}'. " +
                    "It must contain a Button named nav-item.",
                    this);
                return false;
            }

            button.text = destination.Label ?? string.Empty;

            var binding = new ItemBinding { Button = button, ScreenId = destination.ScreenId };

            // A destination with no id, or an id the router does not have, has no
            // screen behind it. Disabling it is the honest state: the bar still
            // shows the shape of the game, and a misconfigured id cannot look like
            // a working button that does nothing.
            var available = !string.IsNullOrWhiteSpace(destination.ScreenId) && router.HasScreen(destination.ScreenId);
            button.SetEnabled(available);

            if (available)
            {
                var screenId = destination.ScreenId;
                binding.ClickHandler = () => Navigate(screenId);
                button.clicked += binding.ClickHandler;
            }

            _items.Add(binding);
            host.Add(container);
            return true;
        }

        void Navigate(string screenId)
        {
            _pinnedIndex = -1;
            SetOpen(-1);
            router.Show(screenId);
        }

        void HandleEnter(GroupBinding binding)
        {
            SetOpen(binding.Index);
        }

        void HandleLeave(GroupBinding binding)
        {
            // A pinned group outlives the pointer. That is the whole point of the
            // click latch: the player can move away and the flyout stays.
            if (_pinnedIndex == binding.Index)
                return;

            if (_openIndex != binding.Index)
                return;

            // Falling back to the pinned group rather than closing outright keeps a
            // latched flyout on screen while the pointer passes over its neighbours.
            SetOpen(_pinnedIndex >= 0 ? _pinnedIndex : -1);
        }

        void HandleGroupClick(GroupBinding binding)
        {
            if (_pinnedIndex == binding.Index)
            {
                _pinnedIndex = -1;
                SetOpen(-1);
                return;
            }

            // Hover previews; a click latches. The latch is what keeps a flyout
            // reachable without hovering, and it gives the group button a second,
            // deliberate action besides opening on approach.
            _pinnedIndex = binding.Index;
            SetOpen(binding.Index);
        }

        void SetOpen(int index)
        {
            _openIndex = index;

            foreach (var binding in _groups)
                binding.Container.EnableInClassList("nav-group--open", binding.Index == index);
        }

        void Unbind()
        {
            if (router != null)
                router.Changed -= HandleScreenChanged;

            foreach (var binding in _groups)
            {
                if (binding.Container != null)
                {
                    binding.Container.UnregisterCallback(binding.EnterHandler);
                    binding.Container.UnregisterCallback(binding.LeaveHandler);
                }

                if (binding.Button != null)
                    binding.Button.clicked -= binding.ClickHandler;
            }

            foreach (var item in _items)
            {
                if (item.ClickHandler != null && item.Button != null)
                    item.Button.clicked -= item.ClickHandler;
            }

            _groups.Clear();
            _items.Clear();

            // The document is about to clear its contents, so nothing here may
            // outlive this call. Clearing first also keeps a rebind on a surviving
            // tree from stacking a second copy of every group.
            _groupsHost?.Clear();
            _groupsHost = null;

            _openIndex = -1;
            _pinnedIndex = -1;
            _bound = false;
        }

        sealed class GroupBinding
        {
            public int Index;
            public VisualElement Container;
            public Button Button;
            public EventCallback<PointerEnterEvent> EnterHandler;
            public EventCallback<PointerLeaveEvent> LeaveHandler;
            public Action ClickHandler;
        }

        sealed class ItemBinding
        {
            public string ScreenId;
            public Button Button;
            public Action ClickHandler;
        }
    }
}
