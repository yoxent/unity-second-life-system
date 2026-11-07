using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Which pool a bar represents. Only used to pick a CSS class; it carries no
    /// rule about what either resource does.
    /// </summary>
    public enum ResourceTone
    {
        Health = 0,
        Mana = 1
    }

    /// <summary>
    /// Shows one resource pool as a label, a current/max readout, and a fill bar.
    ///
    /// Deliberately a plain class rather than a MonoBehaviour, for the same reason
    /// as <see cref="CharacterPortraitView"/>: a combat HUD, a party list, and a
    /// status screen all show a bar, and none of them should have to place a
    /// component to get one.
    ///
    /// The fill is a data-driven width, which the UI standards permit: it is a
    /// value that depends on runtime state, not styling. Everything else about
    /// the row is authored in ResourceBar.uxml and CharacterStatus.uss.
    /// </summary>
    public sealed class ResourceBarView
    {
        readonly Label _label;
        readonly Label _value;
        readonly VisualElement _bar;
        readonly VisualElement _fill;

        ResourceBarView(VisualElement root, Label label, Label value, VisualElement bar, VisualElement fill)
        {
            Root = root;
            _label = label;
            _value = value;
            _bar = bar;
            _fill = fill;
        }

        public VisualElement Root { get; }

        /// <summary>
        /// Instantiates the authored template. Returns null and logs when the
        /// template or its expected elements are missing, so a caller cannot
        /// build half a bar silently.
        /// </summary>
        public static ResourceBarView Create(VisualTreeAsset template, string caption, Object context)
        {
            if (template == null)
            {
                Debug.LogError(
                    $"{nameof(ResourceBarView)} needs a bar template. Assign ResourceBar.uxml in the inspector.",
                    context);
                return null;
            }

            var root = template.Instantiate();
            var label = root.Q<Label>("resource-label");
            var value = root.Q<Label>("resource-value");
            var bar = root.Q<VisualElement>("resource-bar");
            var fill = root.Q<VisualElement>("resource-bar-fill");

            if (label == null || value == null || bar == null || fill == null)
            {
                Debug.LogError(
                    $"{nameof(ResourceBarView)} could not find resource-label, resource-value, resource-bar, " +
                    $"and resource-bar-fill in '{template.name}'.",
                    context);
                return null;
            }

            var view = new ResourceBarView(root, label, value, bar, fill);
            if (!string.IsNullOrWhiteSpace(caption))
                label.text = caption;
            return view;
        }

        /// <summary>
        /// Writes the current and maximum values and sizes the fill.
        ///
        /// A zero maximum would divide by zero, and an empty pool still shows a
        /// track so the row does not collapse to nothing.
        /// </summary>
        public void Bind(ResourcePool pool)
        {
            if (pool == null)
            {
                _value.text = "—";
                SetFillFraction(0f);
                return;
            }

            _value.text = $"{pool.Current} / {pool.Max}";
            SetFillFraction(pool.Fraction);
        }

        /// <summary>
        /// Applies a tone so HP and mana are told apart by more than their label.
        /// The tone is a CSS class, not an inline colour.
        /// </summary>
        public void SetTone(ResourceTone tone)
        {
            var health = tone == ResourceTone.Health;

            _bar.EnableInClassList("resource-bar--health", health);
            _bar.EnableInClassList("resource-bar--mana", !health);

            _fill.EnableInClassList("resource-bar-fill--health", health);
            _fill.EnableInClassList("resource-bar-fill--mana", !health);
        }

        void SetFillFraction(float fraction)
        {
            var clamped = fraction < 0f ? 0f : (fraction > 1f ? 1f : fraction);
            _fill.style.width = Length.Percent(clamped * 100f);
        }
    }
}
