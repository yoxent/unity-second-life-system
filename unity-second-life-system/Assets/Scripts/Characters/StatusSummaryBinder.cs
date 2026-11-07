using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SecondLife.Characters
{
    /// <summary>
    /// Writes the always-visible readout in the screen header: the values a
    /// player checks most often.
    ///
    /// HP and mana appear as current/max with a fill bar, because a pool is only
    /// meaningful against its ceiling and a bar is read faster than two numbers.
    /// These also appear inside the Attributes tab. That duplication is
    /// deliberate: the header is readable from any tab, so the player does not
    /// have to navigate to check their own state.
    /// </summary>
    public sealed class StatusSummaryBinder
    {
        readonly ResourceBarView _health;
        readonly ResourceBarView _mana;
        readonly Label _grade;

        StatusSummaryBinder(ResourceBarView health, ResourceBarView mana, Label grade)
        {
            _health = health;
            _mana = mana;
            _grade = grade;
        }

        public static StatusSummaryBinder Create(
            VisualElement root,
            VisualTreeAsset barTemplate,
            Object context)
        {
            var healthHost = root.Q<VisualElement>("summary-health-host");
            var manaHost = root.Q<VisualElement>("summary-mana-host");
            var grade = root.Q<Label>("summary-grade");

            var missing = new List<string>();
            if (healthHost == null) missing.Add("summary-health-host");
            if (manaHost == null) missing.Add("summary-mana-host");
            if (grade == null) missing.Add("summary-grade");

            if (missing.Count > 0)
            {
                Debug.LogError(
                    $"{nameof(StatusSummaryBinder)} could not find required elements. Missing: {string.Join(", ", missing)}.",
                    context);
                return null;
            }

            var health = ResourceBarView.Create(barTemplate, "HP", context);
            var mana = ResourceBarView.Create(barTemplate, "MANA", context);
            if (health == null || mana == null)
                return null;

            health.SetTone(ResourceTone.Health);
            mana.SetTone(ResourceTone.Mana);

            healthHost.Add(health.Root);
            manaHost.Add(mana.Root);

            return new StatusSummaryBinder(health, mana, grade);
        }

        public void Bind(CharacterState state)
        {
            _health.Bind(state.Health);
            _mana.Bind(state.Mana);
            _grade.text = CharacterDisplayText.GradeLabel(state.CapabilityGrade);
        }
    }
}
