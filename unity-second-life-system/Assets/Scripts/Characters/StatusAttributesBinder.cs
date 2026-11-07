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
        }
    }
}
