namespace SecondLife.Characters
{
    /// <summary>
    /// The seven core attributes confirmed in Systems 2.1. The world does not
    /// measure people with these; they are the protagonist's System presenting
    /// his own underlying capability (Systems 14.2).
    /// </summary>
    public enum AttributeKind
    {
        Strength = 0,
        Vitality = 1,
        Agility = 2,
        Dexterity = 3,
        Intelligence = 4,
        Wisdom = 5,
        Spirit = 6
    }

    public static class AttributeKinds
    {
        /// <summary>Canonical display order, matching Systems 2.1.</summary>
        public static readonly AttributeKind[] All =
        {
            AttributeKind.Strength,
            AttributeKind.Vitality,
            AttributeKind.Agility,
            AttributeKind.Dexterity,
            AttributeKind.Intelligence,
            AttributeKind.Wisdom,
            AttributeKind.Spirit
        };
    }
}
