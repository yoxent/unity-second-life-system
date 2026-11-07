using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Writes the seven attribute rows and the vital bars.
    ///
    /// Reads <see cref="CharacterState.EffectiveAttributes"/>, so an equipment or
    /// status modifier is reflected without this binder knowing modifiers exist.
    /// The bars read the live pools, so a spent point shows here immediately.
    /// </summary>
    public sealed class StatusAttributesBinder
    {
        readonly Dictionary<AttributeKind, Label> _values = new Dictionary<AttributeKind, Label>();
        readonly ResourceBarView _health;
        readonly ResourceBarView _mana;

        // The last state bound. The breakdown providers read it when the popover
        // opens, so they report the current character rather than the one that
        // existed when the trigger was registered.
        CharacterState _state;

        StatusAttributesBinder(ResourceBarView health, ResourceBarView mana)
        {
            _health = health;
            _mana = mana;
        }

        public static StatusAttributesBinder Create(
            VisualElement root,
            VisualTreeAsset barTemplate,
            Object context)
        {
            var healthHost = root.Q<VisualElement>("derived-health-host");
            var manaHost = root.Q<VisualElement>("derived-mana-host");

            var missing = new List<string>();
            if (healthHost == null) missing.Add("derived-health-host");
            if (manaHost == null) missing.Add("derived-mana-host");

            var health = ResourceBarView.Create(barTemplate, "HP", context);
            var mana = ResourceBarView.Create(barTemplate, "MANA", context);
            if (health == null || mana == null)
                return null;

            health.SetTone(ResourceTone.Health);
            mana.SetTone(ResourceTone.Mana);

            healthHost.Add(health.Root);
            manaHost.Add(mana.Root);

            var binder = new StatusAttributesBinder(health, mana);

            foreach (var kind in AttributeKinds.All)
            {
                var elementName = CharacterDisplayText.ValueElementName(kind);
                var value = root.Q<Label>(elementName);
                if (value == null)
                {
                    missing.Add(elementName);
                    continue;
                }

                binder._values[kind] = value;
            }

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(StatusAttributesBinder)} could not find required elements. Missing: {string.Join(", ", missing)}.",
                    context);
                return null;
            }

            return binder;
        }

        public void Bind(CharacterState state)
        {
            var attributes = state.EffectiveAttributes;

            foreach (var kind in AttributeKinds.All)
                _values[kind].text = attributes.Get(kind).ToString();

            _health.Bind(state.Health);
            _mana.Bind(state.Mana);

            // Held so the popover's providers can read current data each time it
            // opens, rather than a snapshot taken at registration.
            _state = state;
        }

        /// <summary>
        /// Makes the two vital bars explainable.
        ///
        /// Only the vitals are registered. Max HP and mana capacity are the values
        /// this game actually derives today, so the popover has real arithmetic to
        /// show. The attribute rows are deliberately left inert: with no equipment
        /// or growth path yet their modifier list is empty, and a hover that opened
        /// a panel reading "Base 12 / Final 12" would be an affordance advertising
        /// nothing. Register them when modifiers exist.
        /// </summary>
        public void RegisterBreakdowns(BreakdownPopover popover)
        {
            if (popover == null)
                return;

            popover.RegisterTrigger(_health.Root, () => _state?.ExplainMaxHealth());
            popover.RegisterTrigger(_mana.Root, () => _state?.ExplainMaxMana());
        }
    }
}
