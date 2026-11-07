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
