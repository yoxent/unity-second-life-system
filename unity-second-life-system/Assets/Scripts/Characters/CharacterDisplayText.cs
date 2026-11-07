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
        /// The System's word for where a modifier came from, used by the
        /// breakdown popover. Kept here rather than in the resolver so the
        /// resolver stays free of display concerns.
        /// </summary>
        public static string ModifierSourceLabel(ModifierSource source)
        {
            switch (source)
            {
                case ModifierSource.Equipment: return "Equipment";
                case ModifierSource.Skill: return "Skill";
                case ModifierSource.StatusEffect: return "Status effect";
                case ModifierSource.Permanent: return "Permanent";
                case ModifierSource.Temporary: return "Temporary";
                default: return "Unknown";
            }
        }

        /// <summary>
        /// One contribution's caption: the source, plus the stack key when the
        /// key names something the player would recognise. A keyless modifier
        /// reads as its source alone rather than trailing a separator.
        /// </summary>
        public static string ContributionLabel(ModifierSource source, string stackKey)
        {
            var origin = ModifierSourceLabel(source);
            return string.IsNullOrWhiteSpace(stackKey) ? origin : origin + " · " + stackKey;
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
