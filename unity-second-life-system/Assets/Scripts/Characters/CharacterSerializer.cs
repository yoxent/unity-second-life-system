using System;
using System.Collections.Generic;

namespace SecondLife.Characters
{
    /// <summary>
    /// Captures and restores a <see cref="CharacterState"/>.
    ///
    /// Kept separate from the state so the state stays free of a serialization
    /// format, and so a test can round-trip a character without a scene.
    ///
    /// A restore is all-or-nothing, matching the narrative session: a rejected
    /// snapshot leaves the live character untouched rather than half-applied.
    /// </summary>
    public static class CharacterSerializer
    {
        public static string Capture(CharacterState state)
        {
            if (state == null)
                throw new ArgumentNullException(nameof(state));

            var snapshot = new CharacterSnapshot
            {
                version = CharacterSnapshot.CurrentVersion,
                profileName = state.Profile.name,
                baseAttributes = ToArray(state.BaseAttributes),
                currentHealth = state.Health.Current,
                currentMana = state.Mana.Current,
                capabilityGrade = (int)state.CapabilityGrade,
                guildRank = state.GuildRank ?? string.Empty,
                titles = ToArray(state.Titles)
            };

            var modifiers = state.Modifiers;
            var count = modifiers.Count;
            snapshot.modifierAttributes = new int[count];
            snapshot.modifierValues = new int[count];
            snapshot.modifierSources = new int[count];
            snapshot.modifierStackKeys = new string[count];
            snapshot.modifierPolicies = new int[count];

            for (var i = 0; i < count; i++)
            {
                var modifier = modifiers[i];
                snapshot.modifierAttributes[i] = (int)modifier.Attribute;
                snapshot.modifierValues[i] = modifier.Value;
                snapshot.modifierSources[i] = (int)modifier.Source;
                snapshot.modifierStackKeys[i] = modifier.StackKey ?? string.Empty;
                snapshot.modifierPolicies[i] = (int)modifier.Policy;
            }

            return UnityEngine.JsonUtility.ToJson(snapshot);
        }

        /// <summary>
        /// Applies a snapshot to a state built from the same profile.
        ///
        /// The profile is not read from the snapshot: a save stores what the
        /// character became, not the authored template, and only the caller knows
        /// which asset that is.
        /// </summary>
        public static bool TryRestore(string snapshotJson, CharacterState state, out string error)
        {
            if (state == null)
            {
                error = "No character state to restore into.";
                return false;
            }

            CharacterSnapshot snapshot;
            try
            {
                snapshot = UnityEngine.JsonUtility.FromJson<CharacterSnapshot>(snapshotJson);
            }
            catch (Exception exception)
            {
                error = "Unreadable character save: " + exception.Message;
                return false;
            }

            if (snapshot == null)
            {
                error = "Unreadable character save.";
                return false;
            }

            if (snapshot.version != CharacterSnapshot.CurrentVersion)
            {
                error = "Unsupported character save version.";
                return false;
            }

            if (snapshot.baseAttributes == null || snapshot.baseAttributes.Length != AttributeKinds.All.Length)
            {
                error = "Character save does not contain all seven attributes.";
                return false;
            }

            if (!TryReadAttributes(snapshot.baseAttributes, out var baseAttributes, out error))
                return false;

            if (!TryReadModifiers(snapshot, out var modifiers, out error))
                return false;

            // Everything is validated; applying from here cannot fail part-way.
            state.RestoreFromSnapshot(baseAttributes, modifiers, snapshot);
            error = null;
            return true;
        }

        static bool TryReadAttributes(int[] values, out CharacterAttributes attributes, out string error)
        {
            foreach (var value in values)
            {
                if (value < 0)
                {
                    attributes = default;
                    error = "Character save contains a negative attribute.";
                    return false;
                }
            }

            attributes = new CharacterAttributes(
                values[(int)AttributeKind.Strength],
                values[(int)AttributeKind.Vitality],
                values[(int)AttributeKind.Agility],
                values[(int)AttributeKind.Dexterity],
                values[(int)AttributeKind.Intelligence],
                values[(int)AttributeKind.Wisdom],
                values[(int)AttributeKind.Spirit]);
            error = null;
            return true;
        }

        static bool TryReadModifiers(
            CharacterSnapshot snapshot,
            out List<AttributeModifier> modifiers,
            out string error)
        {
            modifiers = new List<AttributeModifier>();

            var counts = new[]
            {
                Length(snapshot.modifierAttributes),
                Length(snapshot.modifierValues),
                Length(snapshot.modifierSources),
                Length(snapshot.modifierStackKeys),
                Length(snapshot.modifierPolicies)
            };

            for (var i = 1; i < counts.Length; i++)
            {
                if (counts[i] != counts[0])
                {
                    error = "Character save has mismatched modifier arrays.";
                    modifiers = null;
                    return false;
                }
            }

            for (var i = 0; i < counts[0]; i++)
            {
                if (!Enum.IsDefined(typeof(AttributeKind), snapshot.modifierAttributes[i]) ||
                    !Enum.IsDefined(typeof(ModifierSource), snapshot.modifierSources[i]) ||
                    !Enum.IsDefined(typeof(StackingPolicy), snapshot.modifierPolicies[i]))
                {
                    error = "Character save contains an unknown modifier field.";
                    modifiers = null;
                    return false;
                }

                var stackKey = snapshot.modifierStackKeys[i] ?? string.Empty;
                var source = (ModifierSource)snapshot.modifierSources[i];

                if (source == ModifierSource.StatusEffect && string.IsNullOrWhiteSpace(stackKey))
                {
                    error = "Character save contains a status-effect modifier with no stack key.";
                    modifiers = null;
                    return false;
                }

                modifiers.Add(new AttributeModifier(
                    (AttributeKind)snapshot.modifierAttributes[i],
                    snapshot.modifierValues[i],
                    source,
                    stackKey,
                    (StackingPolicy)snapshot.modifierPolicies[i]));
            }

            error = null;
            return true;
        }

        static int Length(Array array)
        {
            return array?.Length ?? 0;
        }

        static int[] ToArray(CharacterAttributes attributes)
        {
            return new[]
            {
                attributes.Strength,
                attributes.Vitality,
                attributes.Agility,
                attributes.Dexterity,
                attributes.Intelligence,
                attributes.Wisdom,
                attributes.Spirit
            };
        }

        static string[] ToArray(IReadOnlyList<string> values)
        {
            if (values == null || values.Count == 0)
                return new string[0];

            var result = new string[values.Count];
            for (var i = 0; i < values.Count; i++)
                result[i] = values[i];
            return result;
        }
    }
}
