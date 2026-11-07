namespace SecondLife.Characters
{
    /// <summary>
    /// Fixed wording the System uses for attribute and assessment labels.
    ///
    /// Systems 14.2 makes the System's labels the protagonist's private
    /// representation, so the names live in one place the screen reads rather
    /// than being duplicated in the UXML and the controller.
    /// </summary>
    public static class CharacterDisplayText
    {
        public static string FullLabel(AttributeKind kind)
        {
            switch (kind)
            {
                case AttributeKind.Strength: return "Strength";
                case AttributeKind.Vitality: return "Vitality";
                case AttributeKind.Agility: return "Agility";
                case AttributeKind.Dexterity: return "Dexterity";
                case AttributeKind.Intelligence: return "Intelligence";
                case AttributeKind.Wisdom: return "Wisdom";
                case AttributeKind.Spirit: return "Spirit";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// The UXML element name for one attribute's value, e.g. "attribute-vitality".
        /// Declared once so the authoring and the lookup cannot disagree.
        /// </summary>
        public static string ValueElementName(AttributeKind kind)
        {
            return "attribute-" + FullLabel(kind).ToLowerInvariant();
        }

        /// <summary>
        /// Unrated reads as a word, not a letter, so it is not mistaken for a grade.
        /// </summary>
        public static string GradeLabel(CapabilityGrade grade)
        {
            return grade == CapabilityGrade.Unrated ? "Unrated" : grade.ToString();
        }

        /// <summary>
        /// The System shows an em dash for a value it has nothing to display for,
        /// rather than an empty gap that reads as a rendering fault.
        /// </summary>
        public const string NonePlaceholder = "—";
    }
}
