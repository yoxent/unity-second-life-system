namespace SecondLife.Characters
{
    /// <summary>
    /// Statistics derived from the effective attributes.
    ///
    /// A plain value holder. It does not clamp or validate, because the rule that
    /// decides the floor lives in <see cref="DerivedStatCalculator"/> and must not
    /// be duplicated in two places.
    /// </summary>
    public readonly struct DerivedStatistics
    {
        public DerivedStatistics(int maxHealth, int maxMana)
        {
            MaxHealth = maxHealth;
            MaxMana = maxMana;
        }

        public int MaxHealth { get; }

        public int MaxMana { get; }
    }
}
