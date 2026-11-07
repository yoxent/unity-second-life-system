using System.Collections.Generic;

namespace SecondLife.Characters
{
    /// <summary>
    /// A value the screen shows, together with how it was arrived at.
    ///
    /// This is the shape behind the "why is this number this number" popover.
    /// It is deliberately one type for both call sites — an attribute total and a
    /// derived pool maximum — because the player is asking the same question in
    /// both cases and a second near-identical type would drift from the first.
    ///
    /// <see cref="Total"/> is computed by the same rule that produces the value
    /// the rest of the game reads, so a breakdown can never disagree with the
    /// number beside it. EditMode tests assert that agreement rather than trusting
    /// it.
    /// </summary>
    public sealed class ValueBreakdown
    {
        static readonly BreakdownEntry[] NoEntries = new BreakdownEntry[0];

        public ValueBreakdown(
            string title,
            int baseValue,
            IReadOnlyList<BreakdownEntry> entries,
            int total)
        {
            Title = title ?? string.Empty;
            BaseValue = baseValue;
            Entries = entries ?? NoEntries;
            Total = total;
        }

        /// <summary>What is being explained, e.g. "Strength" or "MAX HP".</summary>
        public string Title { get; }

        /// <summary>The value before any contribution. Never negative.</summary>
        public int BaseValue { get; }

        /// <summary>
        /// Ordered contributions. Order follows the order the modifiers were
        /// added, so a list built from a stable state reads the same way twice.
        /// </summary>
        public IReadOnlyList<BreakdownEntry> Entries { get; }

        /// <summary>The value the game reads. Equals what the resolver returns.</summary>
        public int Total { get; }

        /// <summary>
        /// False when nothing outside the base has applied. The popover still has
        /// something to say in that case — the base and the total — so this is a
        /// presentation hint, not a reason to refuse to open.
        /// </summary>
        public bool HasContributions => Entries.Count > 0;
    }
}
