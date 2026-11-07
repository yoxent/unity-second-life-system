using System;

namespace SecondLife.Characters
{
    /// <summary>
    /// One labelled contribution to a value the screen shows.
    ///
    /// A contribution is display data, not a rule. It says "this much came from
    /// here" so the player can check the arithmetic; the policy that decided the
    /// amount stays in <see cref="AttributeResolver"/> or
    /// <see cref="DerivedStatCalculator"/>. Nothing here may acquire a stacking
    /// opinion, or there would be two places that decide how a modifier resolves.
    /// </summary>
    public readonly struct BreakdownEntry : IEquatable<BreakdownEntry>
    {
        public BreakdownEntry(string label, int value)
        {
            Label = string.IsNullOrEmpty(label) ? string.Empty : label;
            Value = value;
        }

        /// <summary>What this contribution is, in the System's vocabulary.</summary>
        public string Label { get; }

        /// <summary>
        /// The amount contributed. Signed, so a penalty reads the same way as a
        /// bonus instead of needing a second convention for negatives.
        /// </summary>
        public int Value { get; }

        public bool Equals(BreakdownEntry other)
        {
            return Label == other.Label && Value == other.Value;
        }

        public override bool Equals(object obj)
        {
            return obj is BreakdownEntry other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (Label.GetHashCode() * 397) ^ Value;
            }
        }

        public override string ToString()
        {
            return Value >= 0 ? $"{Label} +{Value}" : $"{Label} {Value}";
        }
    }
}
