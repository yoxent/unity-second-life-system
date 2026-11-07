using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Shows one character portrait inside an authored frame.
    ///
    /// Deliberately a plain class rather than a MonoBehaviour: dialogue, a party
    /// bar, and a save slot all want a portrait, and none of them should have to
    /// place a component to get one. The owner instantiates the authored template,
    /// then hands the resulting root to <see cref="Bind"/>.
    ///
    /// With no sprite the frame stays on screen showing the placeholder, so the
    /// layout never collapses and a missing portrait reads as unset rather than
    /// broken.
    /// </summary>
    public sealed class CharacterPortraitView
    {
        readonly VisualElement _image;
        readonly Label _placeholder;

        CharacterPortraitView(VisualElement root)
        {
            Root = root;
            _image = root.Q<VisualElement>("portrait-image");
            _placeholder = root.Q<Label>("portrait-placeholder");
        }

        public VisualElement Root { get; }

        /// <summary>
        /// Instantiates the authored template. Returns null and logs when the
        /// template or its expected elements are missing, so a caller cannot
        /// build half a portrait silently.
        /// </summary>
        public static CharacterPortraitView Create(VisualTreeAsset template, Object context)
        {
            if (template == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterPortraitView)} needs a portrait template. Assign CharacterPortrait.uxml in the inspector.",
                    context);
                return null;
            }

            var root = template.Instantiate();
            var frame = root.Q<VisualElement>("portrait-frame");
            var image = root.Q<VisualElement>("portrait-image");
            var placeholder = root.Q<Label>("portrait-placeholder");

            if (frame == null || image == null || placeholder == null)
            {
                Debug.LogError(
                    $"{nameof(CharacterPortraitView)} could not find portrait-frame, portrait-image, and portrait-placeholder " +
                    $"in '{template.name}'.",
                    context);
                return null;
            }

            return new CharacterPortraitView(root);
        }

        /// <summary>
        /// Shows the sprite, or the placeholder when there is none. Assigning null
        /// is a supported state, not an error: character art does not exist yet.
        /// </summary>
        public void Bind(Sprite portrait)
        {
            if (portrait == null)
            {
                _image.style.backgroundImage = new StyleBackground();
                _image.AddToClassList("portrait-image--empty");
                _placeholder.RemoveFromClassList("portrait-placeholder--hidden");
                return;
            }

            _image.style.backgroundImage = new StyleBackground(portrait);
            _image.RemoveFromClassList("portrait-image--empty");
            _placeholder.AddToClassList("portrait-placeholder--hidden");
        }
    }
}
