using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Writes the seven attribute rows and the derived values.
    ///
    /// Reads <see cref="CharacterState.EffectiveAttributes"/>, so an equipment or
    /// status modifier is reflected without this binder knowing modifiers exist.
    /// </summary>
    public sealed class StatusAttributesBinder
    {
        readonly Dictionary<AttributeKind, Label> _values = new Dictionary<AttributeKind, Label>();
        readonly Label _maxHealth;
        readonly Label _maxMana;

        StatusAttributesBinder(Label maxHealth, Label maxMana)
        {
            _maxHealth = maxHealth;
            _maxMana = maxMana;
        }

        public static StatusAttributesBinder Create(VisualElement root, Object context)
        {
            var maxHealth = root.Q<Label>("derived-max-health");
            var maxMana = root.Q<Label>("derived-max-mana");

            var missing = new List<string>();
            if (maxHealth == null) missing.Add("derived-max-health");
            if (maxMana == null) missing.Add("derived-max-mana");

            var binder = new StatusAttributesBinder(maxHealth, maxMana);

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

            var derived = state.Derived;
            _maxHealth.text = derived.MaxHealth.ToString();
            _maxMana.text = derived.MaxMana.ToString();
        }
    }
}
