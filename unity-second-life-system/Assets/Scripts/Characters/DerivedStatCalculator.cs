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
    }
}
