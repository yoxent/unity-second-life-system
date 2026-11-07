using System;
using System.Collections.Generic;

namespace SecondLife.Characters
{
    /// <summary>
    /// Applies modifiers to base attributes and produces the effective values the
    /// rest of the game reads.
    ///
    /// Two rules make this more than a sum:
    ///
    /// 1. Modifiers are grouped by source plus stack key, and each group resolves
    ///    by its own policy. CMB-005 says status-effect stat modifiers are
    ///    strongest-only, so two overlapping buffs must not both apply. Equipment
    ///    and permanent growth add normally.
    /// 2. Modifiers never change the stored base. Removing every modifier returns
    ///    exactly the base value, which is what makes the base authoritative and
    ///    the derived values reproducible.
    ///
    /// Deliberately has no percent modifiers yet. Nothing in the reference states
    /// a percentage stage, and adding one would fix an order of operations the
    /// design has not chosen.
    /// </summary>
    public static class AttributeResolver
    {
        /// <summary>
        /// The effective value of every attribute: base plus its resolved
        /// modifiers, never below zero.
        /// </summary>
        public static CharacterAttributes Resolve(
            CharacterAttributes baseAttributes,
            IReadOnlyList<AttributeModifier> modifiers)
        {
            if (modifiers == null || modifiers.Count == 0)
                return baseAttributes;

            var totals = new int[AttributeKinds.All.Length];
            ApplyModifiers(baseAttributes, modifiers, totals);

            return Build(baseAttributes, totals);
        }

        /// <summary>
        /// Resolves one attribute. Equivalent to <see cref="Resolve"/> for a single
        /// value, without allocating the whole set.
        /// </summary>
        public static int ResolveOne(
            CharacterAttributes baseAttributes,
            IReadOnlyList<AttributeModifier> modifiers,
            AttributeKind attribute)
        {
            var baseValue = baseAttributes.Get(attribute);
            if (modifiers == null || modifiers.Count == 0)
                return baseValue;

            var totals = new int[AttributeKinds.All.Length];
            ApplyModifiers(baseAttributes, modifiers, totals);
            return totals[(int)attribute];
        }

        static void ApplyModifiers(
            CharacterAttributes baseAttributes,
            IReadOnlyList<AttributeModifier> modifiers,
            int[] totals)
        {
            var additive = new int[AttributeKinds.All.Length];
            var groups = new Dictionary<string, int>();

            foreach (var modifier in modifiers)
            {
                var index = (int)modifier.Attribute;
                if (index < 0 || index >= totals.Length)
                    throw new ArgumentOutOfRangeException(
                        nameof(modifiers), modifier.Attribute, "Modifier names an attribute outside the known set.");

                if (modifier.Policy == StackingPolicy.Additive)
                {
                    additive[index] += modifier.Value;
                    continue;
                }

                // Strongest-only competes inside one source-and-key group. Keeping
                // positive and negative magnitudes apart means a debuff cannot
                // hide behind a large buff in the same group.
                var positiveKey = $"{index}|{modifier.Source}|{modifier.StackKey}|+";
                var negativeKey = $"{index}|{modifier.Source}|{modifier.StackKey}|-";

                if (modifier.Value >= 0)
                {
                    groups.TryGetValue(positiveKey, out var existing);
                    if (modifier.Value > existing)
                        groups[positiveKey] = modifier.Value;
                }
                else
                {
                    groups.TryGetValue(negativeKey, out var existing);
                    if (modifier.Value < existing)
                        groups[negativeKey] = modifier.Value;
                }
            }

            foreach (var pair in groups)
            {
                // The group key starts with the attribute index.
                var separator = pair.Key.IndexOf('|');
                var attributeIndex = int.Parse(pair.Key.Substring(0, separator));
                additive[attributeIndex] += pair.Value;
            }

            for (var i = 0; i < totals.Length; i++)
            {
                var effective = baseAttributes.Get((AttributeKind)i) + additive[i];
                totals[i] = effective < 0 ? 0 : effective;
            }
        }

        /// <summary>
        /// Resolves one attribute and reports how it got there.
        ///
        /// The <see cref="ValueBreakdown.Total"/> is produced by
        /// <see cref="ResolveOne"/> rather than recomputed here, so the popover
        /// cannot show a total that differs from the number on the screen. Only
        /// the per-contribution figures are derived, and they follow the same
        /// grouping rules <see cref="ApplyModifiers"/> uses.
        ///
        /// Contributions are reported per source and stack key, not per modifier,
        /// because that is the unit the player can act on: three overlapping
        /// blessings are one line reading "whichever was strongest", not three
        /// lines implying they all applied.
        /// </summary>
        public static ValueBreakdown Explain(
            CharacterAttributes baseAttributes,
            IReadOnlyList<AttributeModifier> modifiers,
            AttributeKind attribute)
        {
            var baseValue = baseAttributes.Get(attribute);
            var total = ResolveOne(baseAttributes, modifiers, attribute);

            var title = CharacterDisplayText.FullLabel(attribute);

            if (modifiers == null || modifiers.Count == 0)
                return new ValueBreakdown(title, baseValue, null, total);

            return new ValueBreakdown(title, baseValue, CollectContributions(modifiers, attribute), total);
        }

        /// <summary>
        /// Groups modifiers the way <see cref="ApplyModifiers"/> does, then reports
        /// each group as one contribution.
        ///
        /// Additive modifiers keep insertion order so a stable state reads the same
        /// way twice. Strongest-only groups collapse to the winning magnitude,
        /// which is why the reported figure can be smaller than the sum of what was
        /// applied — that is the rule doing its job, not a display error.
        /// </summary>
        static List<BreakdownEntry> CollectContributions(
            IReadOnlyList<AttributeModifier> modifiers,
            AttributeKind attribute)
        {
            var entries = new List<BreakdownEntry>();

            // Additive totals per (source, stackKey), in first-seen order.
            var additiveOrder = new List<string>();
            var additiveLabels = new Dictionary<string, string>();
            var additiveTotals = new Dictionary<string, int>();

            // Strongest-only winners per (source, stackKey, sign).
            var strongestOrder = new List<string>();
            var strongestLabels = new Dictionary<string, string>();
            var strongestValues = new Dictionary<string, int>();

            foreach (var modifier in modifiers)
            {
                if (modifier.Attribute != attribute)
                    continue;

                var groupKey = (int)modifier.Source + "|" + modifier.StackKey;
                var label = CharacterDisplayText.ContributionLabel(modifier.Source, modifier.StackKey);

                if (modifier.Policy == StackingPolicy.Additive)
                {
                    if (!additiveTotals.ContainsKey(groupKey))
                    {
                        additiveOrder.Add(groupKey);
                        additiveLabels[groupKey] = label;
                        additiveTotals[groupKey] = 0;
                    }

                    additiveTotals[groupKey] += modifier.Value;
                    continue;
                }

                // The sign is part of the group, matching ApplyModifiers: a debuff
                // competes only with other debuffs, so a buff cannot hide it.
                var signedKey = groupKey + "|" + (modifier.Value >= 0 ? "+" : "-");
                if (!strongestValues.TryGetValue(signedKey, out var existing))
                {
                    strongestOrder.Add(signedKey);
                    strongestLabels[signedKey] = label;
                    strongestValues[signedKey] = modifier.Value;
                    continue;
                }

                var better = modifier.Value >= 0
                    ? modifier.Value > existing
                    : modifier.Value < existing;

                if (better)
                    strongestValues[signedKey] = modifier.Value;
            }

            foreach (var key in additiveOrder)
            {
                if (additiveTotals[key] != 0)
                    entries.Add(new BreakdownEntry(additiveLabels[key], additiveTotals[key]));
            }

            foreach (var key in strongestOrder)
            {
                if (strongestValues[key] != 0)
                    entries.Add(new BreakdownEntry(strongestLabels[key], strongestValues[key]));
            }

            return entries;
        }

        static CharacterAttributes Build(CharacterAttributes source, int[] totals)
        {
            return new CharacterAttributes(
                totals[(int)AttributeKind.Strength],
                totals[(int)AttributeKind.Vitality],
                totals[(int)AttributeKind.Agility],
                totals[(int)AttributeKind.Dexterity],
                totals[(int)AttributeKind.Intelligence],
                totals[(int)AttributeKind.Wisdom],
                totals[(int)AttributeKind.Spirit]);
        }
    }
}
