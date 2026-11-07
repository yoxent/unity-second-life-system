using System;
using System.Collections.Generic;

namespace SecondLife.Characters
{
    /// <summary>
    /// One character as they stand right now.
    ///
    /// Built from a <see cref="CharacterProfile"/> and then independent of it: the
    /// profile is a reusable template, and two instances of the same profile can
    /// diverge without either touching the asset.
    ///
    /// Owns:
    /// - the base attributes, which modifiers never alter;
    /// - the modifier list, from equipment, skills, status effects, and growth;
    /// - the current HP and mana pools;
    /// - the mutable assessment values, seeded from the profile's defaults.
    ///
    /// Does not own any rule. Effective attributes come from
    /// <see cref="AttributeResolver"/> and derived values from
    /// <see cref="DerivedStatCalculator"/>; this type only holds state and asks
    /// them to resolve.
    /// </summary>
    public sealed class CharacterState
    {
        readonly CharacterProfile _profile;
        readonly List<AttributeModifier> _modifiers = new List<AttributeModifier>();
        readonly List<string> _titles = new List<string>();

        CharacterAttributes _baseAttributes;
        DerivedStatistics _derived;
        bool _derivedStale = true;

        public CharacterState(CharacterProfile profile)
        {
            if (profile == null)
                throw new ArgumentNullException(nameof(profile));

            if (profile.Scaling == null)
                throw new ArgumentException(
                    $"Character profile '{profile.name}' has no Attribute Scaling assigned.", nameof(profile));

            if (!profile.Attributes.HasValidValues)
                throw new ArgumentException(
                    $"Character profile '{profile.name}' has negative attribute values.", nameof(profile));

            if (!profile.Scaling.HasValidCoefficients)
                throw new ArgumentException(
                    $"Attribute Scaling '{profile.Scaling.name}' has negative coefficients.", nameof(profile));

            _profile = profile;
            _baseAttributes = profile.Attributes;

            // A character starts at full health and mana. Nothing in the design
            // starts one wounded, and a fresh instance should not appear damaged.
            var derived = CalculateDerived();
            Health = new ResourcePool(derived.MaxHealth);
            Mana = new ResourcePool(derived.MaxMana);

            CapabilityGrade = profile.DefaultCapabilityGrade;
            GuildRank = profile.DefaultGuildRank;

            foreach (var title in profile.DefaultTitles)
            {
                if (!string.IsNullOrWhiteSpace(title))
                    _titles.Add(title);
            }
        }

        /// <summary>
        /// Raised after any value a screen can show has changed. Presentation
        /// subscribes and rereads; this type never knows who is listening and
        /// never pushes a formatted value at anyone.
        /// </summary>
        public event Action Changed;

        public CharacterProfile Profile => _profile;

        public string DisplayName => _profile.DisplayName;

        public AttributeScaling Scaling => _profile.Scaling;

        /// <summary>The stored values. Modifiers never change these.</summary>
        public CharacterAttributes BaseAttributes => _baseAttributes;

        /// <summary>The values the game reads: base plus every applied modifier.</summary>
        public CharacterAttributes EffectiveAttributes =>
            AttributeResolver.Resolve(_baseAttributes, _modifiers);

        public IReadOnlyList<AttributeModifier> Modifiers => _modifiers;

        /// <summary>
        /// How one attribute reached its current value, for the breakdown
        /// popover. Presentation asks this rather than reaching into the
        /// resolver, so the screen keeps depending on state, not on rules.
        /// </summary>
        public ValueBreakdown ExplainAttribute(AttributeKind kind)
        {
            return AttributeResolver.Explain(_baseAttributes, _modifiers, kind);
        }

        /// <summary>
        /// How a derived pool maximum was reached. Reads the effective
        /// attributes, so an equipped Vitality item shows as a raised ceiling
        /// with the contribution named.
        /// </summary>
        public ValueBreakdown ExplainMaxHealth()
        {
            return DerivedStatCalculator.ExplainMaxHealth(EffectiveAttributes, _profile.Scaling);
        }

        public ValueBreakdown ExplainMaxMana()
        {
            return DerivedStatCalculator.ExplainMaxMana(EffectiveAttributes, _profile.Scaling);
        }

        public DerivedStatistics Derived
        {
            get
            {
                if (_derivedStale)
                {
                    _derived = CalculateDerived();
                    _derivedStale = false;
                }

                return _derived;
            }
        }

        public ResourcePool Health { get; }

        public ResourcePool Mana { get; }

        // Mutable assessment state. Seeded from the profile, changed at runtime.
        public CapabilityGrade CapabilityGrade { get; private set; }

        public string GuildRank { get; private set; }

        public IReadOnlyList<string> Titles => _titles;

        /// <summary>
        /// Changes the stored base value. This is growth, not a modifier: training
        /// and authored awards raise the base, while gear and effects are modifiers.
        /// </summary>
        public void SetBaseAttribute(AttributeKind kind, int value)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Attribute values cannot be negative.");

            _baseAttributes = With(_baseAttributes, kind, value);
            InvalidateDerived();
            Changed?.Invoke();
        }

        public void AddModifier(AttributeModifier modifier)
        {
            _modifiers.Add(modifier);
            InvalidateDerived();
            Changed?.Invoke();
        }

        /// <summary>
        /// Removes every modifier from one source, such as unequipping all gear or
        /// clearing expired effects. The source may be narrowed by stack key.
        /// </summary>
        public int RemoveModifiersFrom(ModifierSource source, string stackKey = null)
        {
            var removed = _modifiers.RemoveAll(modifier =>
                modifier.Source == source &&
                (stackKey == null || modifier.StackKey == stackKey));

            if (removed > 0)
            {
                InvalidateDerived();
                Changed?.Invoke();
            }

            return removed;
        }

        public void ClearModifiers()
        {
            if (_modifiers.Count == 0)
                return;

            _modifiers.Clear();
            InvalidateDerived();
            Changed?.Invoke();
        }

        public void SetCapabilityGrade(CapabilityGrade grade)
        {
            CapabilityGrade = grade;
            Changed?.Invoke();
        }

        public void SetGuildRank(string rank)
        {
            GuildRank = rank ?? string.Empty;
            Changed?.Invoke();
        }

        public void SetTitles(IReadOnlyList<string> titles)
        {
            _titles.Clear();
            if (titles != null)
            {
                foreach (var title in titles)
                {
                    if (!string.IsNullOrWhiteSpace(title))
                        _titles.Add(title);
                }
            }

            Changed?.Invoke();
        }

        void InvalidateDerived()
        {
            _derivedStale = true;

            // A maximum change must reach the pools, or an equipped Vitality item
            // would raise the displayed maximum without raising the ceiling.
            var derived = CalculateDerived();
            Health.SetMax(derived.MaxHealth);
            Mana.SetMax(derived.MaxMana);
        }

        /// <summary>
        /// Applies validated snapshot data. Called by <see cref="CharacterSerializer"/>
        /// after it has checked the whole snapshot, so this never fails part-way.
        /// </summary>
        internal void RestoreFromSnapshot(
            CharacterAttributes baseAttributes,
            IReadOnlyList<AttributeModifier> modifiers,
            CharacterSnapshot snapshot)
        {
            _baseAttributes = baseAttributes;

            _modifiers.Clear();
            foreach (var modifier in modifiers)
                _modifiers.Add(modifier);

            _derivedStale = true;
            var derived = CalculateDerived();
            Health.SetMax(derived.MaxHealth);
            Mana.SetMax(derived.MaxMana);

            Health.RestoreSaved(snapshot.currentHealth);
            Mana.RestoreSaved(snapshot.currentMana);

            CapabilityGrade = Enum.IsDefined(typeof(CapabilityGrade), snapshot.capabilityGrade)
                ? (CapabilityGrade)snapshot.capabilityGrade
                : CapabilityGrade.Unrated;
            GuildRank = snapshot.guildRank ?? string.Empty;

            _titles.Clear();
            if (snapshot.titles != null)
            {
                foreach (var title in snapshot.titles)
                {
                    if (!string.IsNullOrWhiteSpace(title))
                        _titles.Add(title);
                }
            }

            Changed?.Invoke();
        }

        DerivedStatistics CalculateDerived()
        {
            return DerivedStatCalculator.Evaluate(EffectiveAttributes, _profile.Scaling);
        }

        static CharacterAttributes With(CharacterAttributes source, AttributeKind kind, int value)
        {
            return new CharacterAttributes(
                kind == AttributeKind.Strength ? value : source.Strength,
                kind == AttributeKind.Vitality ? value : source.Vitality,
                kind == AttributeKind.Agility ? value : source.Agility,
                kind == AttributeKind.Dexterity ? value : source.Dexterity,
                kind == AttributeKind.Intelligence ? value : source.Intelligence,
                kind == AttributeKind.Wisdom ? value : source.Wisdom,
                kind == AttributeKind.Spirit ? value : source.Spirit);
        }
    }
}
