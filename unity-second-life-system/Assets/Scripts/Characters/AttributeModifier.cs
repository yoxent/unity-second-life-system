using System;

namespace SecondLife.Characters
{
    /// <summary>
    /// Where an attribute modifier came from. The source decides how it stacks,
    /// because CMB-005 and the equipment rules do not stack the same way.
    /// </summary>
    public enum ModifierSource
    {
        /// <summary>Worn gear. Equipment bonuses add together.</summary>
        Equipment = 0,

        /// <summary>A passive skill bonus, such as general swordsmanship.</summary>
        Skill = 1,

        /// <summary>An active status effect. Stat modifiers here are strongest-only per stack key (CMB-005).</summary>
        StatusEffect = 2,

        /// <summary>A lasting change from play, such as training or a story event.</summary>
        Permanent = 3,

        /// <summary>An effect that expires on its own, such as a temporary blessing.</summary>
        Temporary = 4
    }

    /// <summary>
    /// The stacking policy for a group of modifiers. Modifiers sharing a stack
    /// key and a policy are combined by that policy.
    /// </summary>
    public enum StackingPolicy
    {
        /// <summary>Every modifier in the group is summed.</summary>
        Additive = 0,

        /// <summary>
        /// Only the largest positive magnitude (and the largest negative one)
        /// applies within the group. This is how CMB-005 treats status-effect
        /// stat modifiers: "Stat modifiers use strongest-only."
        /// </summary>
        StrongestOnly = 1
    }

    /// <summary>
    /// One contribution to an attribute from outside the character's base value.
    ///
    /// A modifier is value plus provenance. Without the provenance the resolver
    /// cannot apply per-source stacking, and a second buff would silently stack
    /// with the first.
    /// </summary>
    public readonly struct AttributeModifier : IEquatable<AttributeModifier>
    {
        public AttributeModifier(
            AttributeKind attribute,
            int value,
            ModifierSource source,
            string stackKey,
            StackingPolicy policy = StackingPolicy.Additive)
        {
            if (source == ModifierSource.StatusEffect && string.IsNullOrWhiteSpace(stackKey))
            {
                throw new ArgumentException(
                    "A status-effect modifier needs a stack key, because CMB-005 resolves them strongest-only per key.",
                    nameof(stackKey));
            }

            Attribute = attribute;
            Value = value;
            Source = source;
            StackKey = string.IsNullOrWhiteSpace(stackKey) ? string.Empty : stackKey;
            Policy = policy;
        }

        public AttributeKind Attribute { get; }

        public int Value { get; }

        public ModifierSource Source { get; }

        /// <summary>
        /// Groups modifiers that compete rather than accumulate. An empty key
        /// groups with every other modifier of the same source that is also
        /// keyless, which is correct for equipment, where each item is separate.
        /// </summary>
        public string StackKey { get; }

        public StackingPolicy Policy { get; }

        public bool Equals(AttributeModifier other)
        {
            return Attribute == other.Attribute &&
                   Value == other.Value &&
                   Source == other.Source &&
                   StackKey == other.StackKey &&
                   Policy == other.Policy;
        }

        public override bool Equals(object obj)
        {
            return obj is AttributeModifier other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hash = (int)Attribute;
                hash = (hash * 397) ^ Value;
                hash = (hash * 397) ^ (int)Source;
                hash = (hash * 397) ^ StackKey.GetHashCode();
                hash = (hash * 397) ^ (int)Policy;
                return hash;
            }
        }

        public override string ToString()
        {
            var sign = Value >= 0 ? "+" : string.Empty;
            var key = StackKey.Length == 0 ? string.Empty : $" [{StackKey}]";
            return $"{Attribute} {sign}{Value} from {Source}{key} ({Policy})";
        }
    }
}
