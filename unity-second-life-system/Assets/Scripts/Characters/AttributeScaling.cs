using UnityEngine;

namespace SecondLife.Characters
{
    /// <summary>
    /// The coefficients that turn attributes into derived statistics.
    ///
    /// Systems 2.2 and 14.2 leave the exact formulas open and say not to create
    /// detailed ones until gameplay implementation requires them. They are
    /// therefore held as balance data in this asset, tunable in the inspector,
    /// rather than baked into C#.
    ///
    /// Only the two links the reference states as likely are modelled:
    /// HP from VIT (2.2) and mana capacity from SPI (2.1, 2.2).
    /// </summary>
    [CreateAssetMenu(
        menuName = "Second Life/Characters/Attribute Scaling",
        fileName = "AttributeScaling")]
    public sealed class AttributeScaling : ScriptableObject
    {
        [Header("Health")]
        [Tooltip("Max HP before any Vitality contribution.")]
        [SerializeField] int baseHealth = 20;

        [Tooltip("Max HP added per point of Vitality.")]
        [SerializeField] int healthPerVitality = 5;

        [Header("Mana")]
        [Tooltip("Mana capacity before any Spirit contribution.")]
        [SerializeField] int baseMana = 10;

        [Tooltip("Mana capacity added per point of Spirit.")]
        [SerializeField] int manaPerSpirit = 4;

        public int BaseHealth => baseHealth;

        public int HealthPerVitality => healthPerVitality;

        public int BaseMana => baseMana;

        public int ManaPerSpirit => manaPerSpirit;

        /// <summary>
        /// A coefficient that removes or inverts growth would produce a status
        /// screen that contradicts the attributes shown beside it.
        /// </summary>
        public bool HasValidCoefficients =>
            baseHealth >= 0 &&
            healthPerVitality >= 0 &&
            baseMana >= 0 &&
            manaPerSpirit >= 0;

        public int MaxHealthFor(CharacterAttributes attributes)
        {
            return baseHealth + attributes.Vitality * healthPerVitality;
        }

        public int MaxManaFor(CharacterAttributes attributes)
        {
            return baseMana + attributes.Spirit * manaPerSpirit;
        }
    }
}
