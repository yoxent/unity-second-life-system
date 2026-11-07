namespace SecondLife.Characters
{
    /// <summary>
    /// Derives the computed statistics the game reads.
    ///
    /// Only two values exist, because only two attribute-to-stat links are stated
    /// in the reference: Max HP from Vitality (Systems 2.2) and mana capacity from
    /// Spirit (Systems 2.1, 2.2). Everything else in the 2.2 candidate list either
    /// contradicts a settled combat rule or has no formula. See STS-001.
    ///
    /// The method takes the *effective* attributes, so equipment and status
    /// modifiers automatically change the derived values without this class
    /// knowing they exist. That is why there is no event wiring here: resolve
    /// attributes, then derive.
    /// </summary>
    public static class DerivedStatCalculator
    {
        /// <summary>
        /// A living character always has at least one point in each pool, so a
        /// zero coefficient cannot present a character who is already defeated.
        /// </summary>
        public const int MinimumMaxHealth = 1;

        public const int MinimumMaxMana = 1;

        public static DerivedStatistics Evaluate(CharacterAttributes effective, AttributeScaling scaling)
        {
            if (scaling == null)
                throw new System.ArgumentNullException(nameof(scaling));

            return new DerivedStatistics(
                ApplyFloor(scaling.MaxHealthFor(effective), MinimumMaxHealth),
                ApplyFloor(scaling.MaxManaFor(effective), MinimumMaxMana));
        }

        static int ApplyFloor(int value, int floor)
        {
            return value < floor ? floor : value;
        }

        /// <summary>
        /// Explains Max HP: the flat base from the scaling asset, then a
        /// per-point contribution from Vitality.
        ///
        /// Total comes from <see cref="Evaluate"/>, so the popover and the bar
        /// cannot disagree even if the floor above ever changes the result. The
        /// contribution line is reported as the realised difference rather than
        /// the raw product, so a floored value still adds up on screen.
        /// </summary>
        public static ValueBreakdown ExplainMaxHealth(CharacterAttributes effective, AttributeScaling scaling)
        {
            return ExplainDerived(
                "MAX HP",
                scaling.BaseHealth,
                "Vitality",
                effective.Vitality,
                scaling.HealthPerVitality,
                Evaluate(effective, scaling).MaxHealth);
        }

        /// <summary>
        /// Explains mana capacity the same way, from Spirit.
        /// </summary>
        public static ValueBreakdown ExplainMaxMana(CharacterAttributes effective, AttributeScaling scaling)
        {
            return ExplainDerived(
                "MANA",
                scaling.BaseMana,
                "Spirit",
                effective.Spirit,
                scaling.ManaPerSpirit,
                Evaluate(effective, scaling).MaxMana);
        }

        /// <summary>
        /// Shared shape for a "flat base plus per-point attribute" statistic.
        ///
        /// The attribute line is the difference between the resolved total and the
        /// base, not the raw coefficient product. When the floor raises a value,
        /// the line shrinks with it instead of claiming a contribution the total
        /// does not include.
        ///
        /// The label names the attribute and the coefficient, because "Vitality
        /// ×5" tells the player which number to raise; the amount beside it is the
        /// realised gain, which is what they are checking.
        /// </summary>
        static ValueBreakdown ExplainDerived(
            string title,
            int baseValue,
            string attributeLabel,
            int attributeValue,
            int coefficient,
            int total)
        {
            var contribution = total - baseValue;

            var entries = contribution == 0
                ? null
                : new[]
                {
                    new BreakdownEntry(
                        attributeLabel + " ×" + coefficient + " (" + attributeValue + ")",
                        contribution)
                };

            return new ValueBreakdown(title, baseValue, entries, total);
        }
    }
}
