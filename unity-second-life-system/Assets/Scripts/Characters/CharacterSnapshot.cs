using System;

namespace SecondLife.Characters
{
    /// <summary>
    /// The serialized form of a <see cref="CharacterState"/>.
    ///
    /// Follows the narrative snapshot: a schema version, then flat fields. Arrays
    /// rather than dictionaries, because JsonUtility cannot serialize a map, and
    /// no polymorphism, because it cannot round-trip a derived type.
    ///
    /// Deliberately does not store derived values. Max HP and mana capacity are
    /// recomputed from the saved base attributes and modifiers, so a save can never
    /// disagree with the rules that produced it. Only the spent current values are
    /// saved, because those are genuinely played state.
    /// </summary>
    [Serializable]
    public sealed class CharacterSnapshot
    {
        public const int CurrentVersion = 1;

        public int version = CurrentVersion;

        public string profileName;

        // Base attributes. Arrays keep the snapshot independent of field order.
        public int[] baseAttributes = new int[0];

        public int currentHealth;
        public int currentMana;

        // Modifiers, flattened. One entry per modifier so stacking stays reproducible.
        public int[] modifierAttributes = new int[0];
        public int[] modifierValues = new int[0];
        public int[] modifierSources = new int[0];
        public string[] modifierStackKeys = new string[0];
        public int[] modifierPolicies = new int[0];

        // Assessment state.
        public int capabilityGrade;
        public string guildRank = string.Empty;
        public string[] titles = new string[0];
    }
}
