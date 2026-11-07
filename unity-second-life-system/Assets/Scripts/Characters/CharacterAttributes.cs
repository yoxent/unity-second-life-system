using System;
using UnityEngine;

namespace SecondLife.Characters
{
    /// <summary>
    /// Whole-number values for the seven core attributes. The System only ever
    /// shows non-negative values, so a negative attribute is a rule violation
    /// rather than a number to display.
    /// </summary>
    [Serializable]
    public struct CharacterAttributes : IEquatable<CharacterAttributes>
    {
        [SerializeField] int strength;
        [SerializeField] int vitality;
        [SerializeField] int agility;
        [SerializeField] int dexterity;
        [SerializeField] int intelligence;
        [SerializeField] int wisdom;
        [SerializeField] int spirit;

        public CharacterAttributes(
            int strength,
            int vitality,
            int agility,
            int dexterity,
            int intelligence,
            int wisdom,
            int spirit)
        {
            RequireNonNegative(strength, nameof(strength));
            RequireNonNegative(vitality, nameof(vitality));
            RequireNonNegative(agility, nameof(agility));
            RequireNonNegative(dexterity, nameof(dexterity));
            RequireNonNegative(intelligence, nameof(intelligence));
            RequireNonNegative(wisdom, nameof(wisdom));
            RequireNonNegative(spirit, nameof(spirit));

            this.strength = strength;
            this.vitality = vitality;
            this.agility = agility;
            this.dexterity = dexterity;
            this.intelligence = intelligence;
            this.wisdom = wisdom;
            this.spirit = spirit;
        }

        public int Strength => strength;

        public int Vitality => vitality;

        public int Agility => agility;

        public int Dexterity => dexterity;

        public int Intelligence => intelligence;

        public int Wisdom => wisdom;

        public int Spirit => spirit;

        /// <summary>
        /// Guards values that reached the struct through serialization, which
        /// bypasses the constructor.
        /// </summary>
        public bool HasValidValues =>
            strength >= 0 &&
            vitality >= 0 &&
            agility >= 0 &&
            dexterity >= 0 &&
            intelligence >= 0 &&
            wisdom >= 0 &&
            spirit >= 0;

        public int this[AttributeKind kind] => Get(kind);

        public int Get(AttributeKind kind)
        {
            switch (kind)
            {
                case AttributeKind.Strength: return strength;
                case AttributeKind.Vitality: return vitality;
                case AttributeKind.Agility: return agility;
                case AttributeKind.Dexterity: return dexterity;
                case AttributeKind.Intelligence: return intelligence;
                case AttributeKind.Wisdom: return wisdom;
                case AttributeKind.Spirit: return spirit;
                default:
                    throw new ArgumentOutOfRangeException(nameof(kind), kind, "Unknown attribute kind.");
            }
        }

        public bool Equals(CharacterAttributes other)
        {
            return strength == other.strength &&
                   vitality == other.vitality &&
                   agility == other.agility &&
                   dexterity == other.dexterity &&
                   intelligence == other.intelligence &&
                   wisdom == other.wisdom &&
                   spirit == other.spirit;
        }

        public override bool Equals(object obj)
        {
            return obj is CharacterAttributes other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = strength;
                hash = (hash * 397) ^ vitality;
                hash = (hash * 397) ^ agility;
                hash = (hash * 397) ^ dexterity;
                hash = (hash * 397) ^ intelligence;
                hash = (hash * 397) ^ wisdom;
                hash = (hash * 397) ^ spirit;
                return hash;
            }
        }

        public override string ToString()
        {
            return $"STR {strength}, VIT {vitality}, AGI {agility}, DEX {dexterity}, " +
                   $"INT {intelligence}, WIS {wisdom}, SPI {spirit}";
        }

        static void RequireNonNegative(int value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName, value, "Attribute values cannot be negative.");
        }
    }
}
