using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Writes the identity block: portrait, name, species, gender, public
    /// discipline, and description.
    ///
    /// Bound to one root element so the screen controller never touches these
    /// labels itself. That keeps the controller a composition root as tabs are
    /// added, instead of growing a method per field.
    /// </summary>
    public sealed class StatusIdentityBinder
    {
        readonly Label _name;
        readonly Label _species;
        readonly Label _gender;
        readonly Label _discipline;
        readonly Label _description;
        readonly CharacterPortraitView _portrait;
        readonly VisualElement _portraitHost;

        StatusIdentityBinder(
            Label name,
            Label species,
            Label gender,
            Label discipline,
            Label description,
            CharacterPortraitView portrait,
            VisualElement portraitHost)
        {
            _name = name;
            _species = species;
            _gender = gender;
            _discipline = discipline;
            _description = description;
            _portrait = portrait;
            _portraitHost = portraitHost;
        }

        /// <summary>
        /// Queries the identity block and inserts the portrait view. Returns null
        /// and logs when an expected element is missing, so the caller disables
        /// rather than presenting a partly-empty identity.
        /// </summary>
        public static StatusIdentityBinder Create(
            VisualElement root,
            VisualTreeAsset portraitTemplate,
            Object context)
        {
            var name = root.Q<Label>("character-name");
            var species = root.Q<Label>("identity-species");
            var gender = root.Q<Label>("identity-gender");
            var discipline = root.Q<Label>("identity-public-discipline");
            var description = root.Q<Label>("identity-description");
            var portraitHost = root.Q<VisualElement>("identity-portrait-host");

            var missing = new List<string>();
            if (name == null) missing.Add("character-name");
            if (species == null) missing.Add("identity-species");
            if (gender == null) missing.Add("identity-gender");
            if (discipline == null) missing.Add("identity-public-discipline");
            if (description == null) missing.Add("identity-description");
            if (portraitHost == null) missing.Add("identity-portrait-host");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(StatusIdentityBinder)} could not find required elements. Missing: {string.Join(", ", missing)}.",
                    context);
                return null;
            }

            var portrait = CharacterPortraitView.Create(portraitTemplate, context);
            if (portrait == null)
                return null;

            portraitHost.Add(portrait.Root);

            return new StatusIdentityBinder(name, species, gender, discipline, description, portrait, portraitHost);
        }

        /// <summary>
        /// The identity block is authored data, so it reads from the profile. A
        /// name the player chose is authored once and does not change in play.
        /// </summary>
        public void Bind(CharacterIdentity identity)
        {
            _name.text = identity.DisplayName;
            _species.text = OrPlaceholder(identity.Species);
            _gender.text = OrPlaceholder(identity.Gender);
            _discipline.text = OrPlaceholder(identity.PublicDiscipline);
            _description.text = OrPlaceholder(identity.Description);
            _portrait.Bind(identity.Portrait);
        }

        static string OrPlaceholder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? CharacterDisplayText.NonePlaceholder : value;
        }
    }
}
