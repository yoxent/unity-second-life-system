using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Shows the base, the contributions, and the final value behind one number.
    ///
    /// Behaviour is copied from the navigation bar rather than invented, because
    /// the screen has the same two problems there: a pointer is not guaranteed,
    /// and a popover must not vanish because the pointer crossed a gap. So hover
    /// previews, a click latches, and a latched popover stays until it is
    /// dismissed deliberately.
    ///
    /// Lives in SecondLife.Characters beside the binders it serves. It depends on
    /// no other screen: it is handed a title, a breakdown, and an anchor, which
    /// means a later combat HUD can reuse it unchanged.
    /// </summary>
    public sealed class BreakdownPopover
    {
        readonly VisualElement _root;
        readonly Label _title;
        readonly Label _baseValue;
        readonly Label _totalValue;
        readonly VisualElement _linesHost;
        readonly VisualElement _baseRow;
        readonly VisualTreeAsset _lineTemplate;
        readonly UnityEngine.Object _context;

        readonly VisualElement _anchor;

        // The trigger currently shown, and the one a click has latched. One index
        // each, so two popovers can never be open and a latch has one owner.
        VisualElement _hovered;
        VisualElement _pinned;

        // Placement state. The popover has no size until layout runs after it
        // becomes visible, so positioning is retried on the geometry event.
        VisualElement _pendingAnchor;
        bool _measuring;

        // Created once and reused for every trigger, so a hover does not allocate
        // a callback per row.
        readonly EventCallback<PointerEnterEvent> _enterHandler;
        readonly EventCallback<PointerLeaveEvent> _leaveHandler;
        readonly EventCallback<PointerDownEvent> _downHandler;

        BreakdownPopover(
            VisualElement root,
            VisualElement anchor,
            Label title,
            Label baseValue,
            Label totalValue,
            VisualElement linesHost,
            VisualElement baseRow,
            VisualTreeAsset lineTemplate,
            UnityEngine.Object context)
        {
            _root = root;
            _anchor = anchor;
            _title = title;
            _baseValue = baseValue;
            _totalValue = totalValue;
            _linesHost = linesHost;
            _baseRow = baseRow;
            _lineTemplate = lineTemplate;
            _context = context;

            _enterHandler = OnPointerEnter;
            _leaveHandler = OnPointerLeave;
            _downHandler = OnPointerDown;
        }

        /// <summary>
        /// Builds the popover from the authored tree and returns null when an
        /// expected element or the line template is missing, so a caller cannot
        /// end up with a half-wired control that silently does nothing.
        /// </summary>
        public static BreakdownPopover Create(
            VisualElement screenRoot,
            VisualTreeAsset lineTemplate,
            UnityEngine.Object context)
        {
            if (screenRoot == null)
            {
                Debug.LogError(
                    $"{nameof(BreakdownPopover)} needs the screen root. The document has no visual tree.",
                    context);
                return null;
            }

            if (lineTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(BreakdownPopover)} needs a line template. Assign BreakdownLine.uxml in the inspector.",
                    context);
                return null;
            }

            var root = screenRoot.Q<VisualElement>("breakdown-popover");
            var title = screenRoot.Q<Label>("breakdown-title");
            var linesHost = screenRoot.Q<VisualElement>("breakdown-lines");
            var baseRow = screenRoot.Q<VisualElement>("breakdown-base-row");
            var baseValue = screenRoot.Q<Label>("breakdown-base-value");
            var totalValue = screenRoot.Q<Label>("breakdown-total-value");

            var missing = new System.Collections.Generic.List<string>();
            if (root == null) missing.Add("breakdown-popover");
            if (title == null) missing.Add("breakdown-title");
            if (linesHost == null) missing.Add("breakdown-lines");
            if (baseRow == null) missing.Add("breakdown-base-row");
            if (baseValue == null) missing.Add("breakdown-base-value");
            if (totalValue == null) missing.Add("breakdown-total-value");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(BreakdownPopover)} could not find required elements. Missing: {string.Join(", ", missing)}.",
                    context);
                return null;
            }

            // The popover panel itself must not eat pointer events, or crossing it
            // would count as leaving the trigger and close the panel underneath the
            // pointer.
            root.pickingMode = PickingMode.Ignore;

            return new BreakdownPopover(
                root, screenRoot, title, baseValue, totalValue, linesHost, baseRow, lineTemplate, context);
        }

        /// <summary>
        /// Makes a value inspectable. The provider is called when the popover
        /// opens, not here, so a trigger shows current data after the character
        /// changes without the caller re-registering anything.
        /// </summary>
        public void RegisterTrigger(VisualElement trigger, Func<ValueBreakdown> provider)
        {
            if (trigger == null || provider == null)
                return;

            // Registration is where the provider is stored, so a hover arriving
            // before any click already has one to call.
            _providers[trigger] = provider;

            // A Label is not focusable or pickable by default, so it would never
            // receive the events this control depends on.
            trigger.focusable = true;
            trigger.pickingMode = PickingMode.Position;
            trigger.AddToClassList("breakdown-trigger");

            trigger.RegisterCallback(_enterHandler);
            trigger.RegisterCallback(_leaveHandler);
            trigger.RegisterCallback(_downHandler);

            // Keyboard and gamepad open it too, since a pointer is not guaranteed.
            trigger.RegisterCallback<NavigationSubmitEvent>(_ => Latch(trigger));
            trigger.RegisterCallback<FocusInEvent>(_ => Hover(trigger));
            trigger.RegisterCallback<FocusOutEvent>(_ => Unhover(trigger));
        }

        /// <summary>
        /// Drops every trigger. Called from the same place the tree is released,
        /// so pooled or rebuilt trees do not keep this control alive.
        /// </summary>
        public void Clear()
        {
            Hide();
            _pinned = null;
            _hovered = null;
            _providers.Clear();
        }

        void OnPointerEnter(PointerEnterEvent evt)
        {
            Hover(evt.currentTarget as VisualElement);
        }

        void OnPointerLeave(PointerLeaveEvent evt)
        {
            Unhover(evt.currentTarget as VisualElement);
        }

        void OnPointerDown(PointerDownEvent evt)
        {
            var trigger = evt.currentTarget as VisualElement;
            if (trigger == null)
                return;

            if (_pinned == trigger)
            {
                _pinned = null;
                Hide();
                return;
            }

            _pinned = trigger;
            Show(trigger);
        }

        void Hover(VisualElement trigger)
        {
            if (trigger == null)
                return;

            _hovered = trigger;

            // A latched popover stays on its own trigger. Hovering elsewhere must
            // not steal it, or the latch would be meaningless.
            if (_pinned != null && _pinned != trigger)
                return;

            Show(trigger);
        }

        void Unhover(VisualElement trigger)
        {
            if (trigger == null || _hovered != trigger)
                return;

            _hovered = null;

            if (_pinned != null)
                return;

            Hide();
        }

        void Latch(VisualElement trigger)
        {
            if (trigger == null)
                return;

            if (_pinned == trigger)
            {
                _pinned = null;
                Hide();
                return;
            }

            _pinned = trigger;
            Show(trigger);
        }

        void Show(VisualElement trigger)
        {
            if (!_providers.TryGetValue(trigger, out var provider))
                return;

            var breakdown = provider();
            if (breakdown == null)
                return;

            _title.text = breakdown.Title;
            _baseValue.text = breakdown.BaseValue.ToString();
            _totalValue.text = breakdown.Total.ToString();

            _linesHost.Clear();
            foreach (var entry in breakdown.Entries)
            {
                var line = _lineTemplate.Instantiate();

                var label = line.Q<Label>("breakdown-line-label");
                var value = line.Q<Label>("breakdown-line-value");

                if (label == null || value == null)
                {
                    Debug.LogError(
                        $"{nameof(BreakdownPopover)} found a line template without breakdown-line-label " +
                        $"and breakdown-line-value in '{_lineTemplate.name}'.",
                        _context);
                    return;
                }

                label.text = entry.Label;
                value.text = FormatSigned(entry.Value);

                _linesHost.Add(line);
            }

            // With no contributions the base and the final are the same number, so
            // the base row would be a duplicate line. Hiding it keeps the popover
            // honest rather than padded.
            _baseRow.style.display = breakdown.HasContributions
                ? DisplayStyle.Flex
                : DisplayStyle.None;

            _root.style.display = DisplayStyle.Flex;
            Place(trigger);
        }

        /// <summary>
        /// Positions the popover under its trigger, flipping above it when there is
        /// no room below, and clamping horizontally to the screen.
        ///
        /// <see cref="VisualElement.WorldToLocal"/> converts the trigger's world
        /// position into the anchor's local space. Doing that subtraction by hand
        /// gets the padding box wrong, which put the panel past the right edge.
        ///
        /// The panel has no size on the frame its display changes, so measuring
        /// immediately returns zero width and the clamp silently does nothing.
        /// Placement therefore runs twice: once now to make it visible in the right
        /// place, and once on the geometry event that arrives when layout resolves.
        /// </summary>
        void Place(VisualElement trigger)
        {
            if (_measuring)
                return;

            _pendingAnchor = trigger;
            _measuring = true;

            _root.RegisterCallback<GeometryChangedEvent>(OnGeometryChanged, TrickleDown.NoTrickleDown);

            _root.schedule.Execute(() => ApplyPosition()).ExecuteLater(0);
        }

        void OnGeometryChanged(GeometryChangedEvent evt)
        {
            // Layout has resolved, so the size below is real. Re-place once and
            // stop listening: further geometry events come from the position write.
            _root.UnregisterCallback<GeometryChangedEvent>(OnGeometryChanged, TrickleDown.NoTrickleDown);
            _measuring = false;
            ApplyPosition();
        }

        void ApplyPosition()
        {
            var trigger = _pendingAnchor;
            if (trigger == null || _root.resolvedStyle.display == DisplayStyle.None)
            {
                _measuring = false;
                return;
            }

            var size = _root.layout.size;

            // Fall back to the authored width when layout has not run yet. Without
            // this the horizontal clamp compares against zero and lets the panel
            // hang off the edge.
            var width = size.x > 0f ? size.x : _root.resolvedStyle.width;
            var height = size.y > 0f ? size.y : _root.resolvedStyle.height;

            if (float.IsNaN(width) || width <= 0f)
                return;

            var triggerInAnchor = _anchor.WorldToLocal(trigger.worldBound);
            var anchorWidth = _anchor.layout.size.x;
            var anchorHeight = _anchor.layout.size.y;

            var left = triggerInAnchor.x;
            var top = triggerInAnchor.yMax + 6f;

            // Flip above the trigger when there is no room below and room above.
            if (!float.IsNaN(height) && top + height > anchorHeight && triggerInAnchor.y - height - 6f > 0f)
                top = triggerInAnchor.y - height - 6f;

            var maxLeft = anchorWidth - width;
            if (left > maxLeft)
                left = maxLeft;

            if (left < 0f)
                left = 0f;

            if (top < 0f)
                top = 0f;

            _root.style.left = left;
            _root.style.top = top;
        }

        void Hide()
        {
            _root.style.display = DisplayStyle.None;
        }

        static string FormatSigned(int value)
        {
            return value >= 0 ? "+" + value : value.ToString();
        }

        // A trigger's provider, keyed by the element so a rebuilt tree cannot keep
        // a stale entry: Clear drops the lot.
        readonly System.Collections.Generic.Dictionary<VisualElement, Func<ValueBreakdown>> _providers =
            new System.Collections.Generic.Dictionary<VisualElement, Func<ValueBreakdown>>();
    }
}
