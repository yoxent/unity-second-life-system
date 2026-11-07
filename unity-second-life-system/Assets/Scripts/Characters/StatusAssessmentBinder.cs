using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Writes the three assessment axes.
    ///
    /// Reads the instance, not the profile: Capability Grade, Guild Rank, and
    /// titles change through play, and the authored asset holds only the starting
    /// values (Systems 8.1).
    /// </summary>
    public sealed class StatusAssessmentBinder
    {
        readonly Label _capabilityGrade;
        readonly Label _guildRank;
        readonly VisualElement _titles;
        readonly VisualTreeAsset _titleTemplate;

        StatusAssessmentBinder(
            Label capabilityGrade,
            Label guildRank,
            VisualElement titles,
            VisualTreeAsset titleTemplate)
        {
            _capabilityGrade = capabilityGrade;
            _guildRank = guildRank;
            _titles = titles;
            _titleTemplate = titleTemplate;
        }

        public static StatusAssessmentBinder Create(
            VisualElement root,
            VisualTreeAsset titleTemplate,
            Object context)
        {
            if (titleTemplate == null)
            {
                Debug.LogError(
                    $"{nameof(StatusAssessmentBinder)} needs a title template. Assign StatusTitle.uxml in the inspector.",
                    context);
                return null;
            }

            var capabilityGrade = root.Q<Label>("assessment-capability-grade");
            var guildRank = root.Q<Label>("assessment-guild-rank");
            var titles = root.Q<VisualElement>("assessment-titles");

            var missing = new List<string>();
            if (capabilityGrade == null) missing.Add("assessment-capability-grade");
            if (guildRank == null) missing.Add("assessment-guild-rank");
            if (titles == null) missing.Add("assessment-titles");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(StatusAssessmentBinder)} could not find required elements. Missing: {string.Join(", ", missing)}.",
                    context);
                return null;
            }

            return new StatusAssessmentBinder(capabilityGrade, guildRank, titles, titleTemplate);
        }

        /// <summary>
        /// Returns false when the title template is malformed, so the caller can
        /// disable instead of presenting a broken list.
        /// </summary>
        public bool Bind(CharacterState state)
        {
            _capabilityGrade.text = CharacterDisplayText.GradeLabel(state.CapabilityGrade);
            _guildRank.text = OrPlaceholder(state.GuildRank);

            return BindTitles(state.Titles);
        }

        bool BindTitles(IReadOnlyList<string> titles)
        {
            _titles.Clear();

            var hasAny = false;
            foreach (var title in titles)
            {
                if (string.IsNullOrWhiteSpace(title))
                    continue;

                var label = _titleTemplate.Instantiate().Q<Label>("status-title");
                if (label == null)
                    return false;

                label.text = title;
                _titles.Add(label);
                hasAny = true;
            }

            if (hasAny)
                return true;

            var none = _titleTemplate.Instantiate().Q<Label>("status-title");
            if (none == null)
                return false;

            none.text = CharacterDisplayText.NonePlaceholder;
            none.AddToClassList("status-title--none");
            _titles.Add(none);
            return true;
        }

        static string OrPlaceholder(string value)
        {
            return string.IsNullOrWhiteSpace(value) ? CharacterDisplayText.NonePlaceholder : value;
        }
    }
}
