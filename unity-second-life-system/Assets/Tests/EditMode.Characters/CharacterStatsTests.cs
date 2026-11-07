using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace SecondLife.Characters.Tests
{
    public sealed class CharacterStatsTests
    {
        AttributeScaling _scaling;
        CharacterProfile _profile;

        [TearDown]
        public void TearDown()
        {
            if (_profile != null)
                Object.DestroyImmediate(_profile);

            if (_scaling != null)
                Object.DestroyImmediate(_scaling);

            _profile = null;
            _scaling = null;
        }

        // ---- Derived statistics ---------------------------------------------

        [Test]
        public void Derived_DerivesHealthFromVitalityAndManaFromSpirit()
        {
            _scaling = CreateScaling(baseHealth: 20, healthPerVitality: 5, baseMana: 10, manaPerSpirit: 4);

            var derived = DerivedStatCalculator.Evaluate(
                Attributes(strength: 1, vitality: 10, agility: 1, dexterity: 1, intelligence: 1, wisdom: 1, spirit: 7),
                _scaling);

            Assert.That(derived.MaxHealth, Is.EqualTo(70), "20 base + 10 Vitality x 5");
            Assert.That(derived.MaxMana, Is.EqualTo(38), "10 base + 7 Spirit x 4");
        }

        [Test]
        public void Derived_IgnoresAttributesThatDoNotInformTheStat()
        {
            _scaling = CreateScaling(20, 5, 10, 4);

            var low = DerivedStatCalculator.Evaluate(Attributes(1, 10, 1, 1, 1, 1, 7), _scaling);
            var high = DerivedStatCalculator.Evaluate(Attributes(99, 10, 99, 99, 99, 99, 7), _scaling);

            Assert.That(high.MaxHealth, Is.EqualTo(low.MaxHealth),
                "Systems 2.2 ties HP to Vitality, so Strength must not raise it.");
            Assert.That(high.MaxMana, Is.EqualTo(low.MaxMana),
                "Systems 2.1 ties mana capacity to Spirit, so Intelligence must not raise it.");
        }

        [Test]
        public void Derived_ClampsAPoolToAtLeastOne()
        {
            _scaling = CreateScaling(0, 0, 0, 0);

            var derived = DerivedStatCalculator.Evaluate(Attributes(0, 0, 0, 0, 0, 0, 0), _scaling);

            Assert.That(derived.MaxHealth, Is.EqualTo(DerivedStatCalculator.MinimumMaxHealth));
            Assert.That(derived.MaxMana, Is.EqualTo(DerivedStatCalculator.MinimumMaxMana));
        }

        // ---- Attributes ------------------------------------------------------

        [Test]
        public void Attributes_RejectNegativeValues()
        {
            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                _ = new CharacterAttributes(1, -1, 1, 1, 1, 1, 1));
        }

        [Test]
        public void Attributes_ReadsByKind()
        {
            var attributes = Attributes(1, 2, 3, 4, 5, 6, 7);

            Assert.That(attributes[AttributeKind.Strength], Is.EqualTo(1));
            Assert.That(attributes[AttributeKind.Vitality], Is.EqualTo(2));
            Assert.That(attributes[AttributeKind.Agility], Is.EqualTo(3));
            Assert.That(attributes[AttributeKind.Dexterity], Is.EqualTo(4));
            Assert.That(attributes[AttributeKind.Intelligence], Is.EqualTo(5));
            Assert.That(attributes[AttributeKind.Wisdom], Is.EqualTo(6));
            Assert.That(attributes[AttributeKind.Spirit], Is.EqualTo(7));
        }

        // ---- Modifiers -------------------------------------------------------

        [Test]
        public void Modifiers_AddTogetherAcrossSources()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.Equipment, "sword"),
                new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.Permanent, "training")
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(resolved.Strength, Is.EqualTo(15), "10 base + 3 equipment + 2 training");
        }

        [Test]
        public void Modifiers_DoNotChangeTheBaseValue()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 5, ModifierSource.Equipment, "sword")
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(baseAttributes.Strength, Is.EqualTo(10), "The value object is immutable.");
            Assert.That(resolved.Strength, Is.EqualTo(15));

            var without = AttributeResolver.Resolve(baseAttributes, new AttributeModifier[0]);
            Assert.That(without.Strength, Is.EqualTo(10), "Removing every modifier returns the base exactly.");
        }

        [Test]
        public void Modifiers_StatusEffectsWithTheSameKeyUseStrongestOnly()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 4, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                new AttributeModifier(AttributeKind.Strength, 7, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly)
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(resolved.Strength, Is.EqualTo(17),
                "CMB-005: stat modifiers use strongest-only, so 7 applies and 4 and 2 do not.");
        }

        [Test]
        public void Modifiers_StatusEffectsWithDifferentKeysBothApply()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 4, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.StatusEffect, "rage", StackingPolicy.StrongestOnly)
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(resolved.Strength, Is.EqualTo(17),
                "Different stack keys are distinct functions and coexist (CMB-005).");
        }

        [Test]
        public void Modifiers_AStrongestOnlyDebuffIsNotHiddenByABuffInTheSameGroup()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 6, ModifierSource.StatusEffect, "aura", StackingPolicy.StrongestOnly),
                new AttributeModifier(AttributeKind.Strength, -4, ModifierSource.StatusEffect, "aura", StackingPolicy.StrongestOnly)
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(resolved.Strength, Is.EqualTo(12), "10 + 6 buff - 4 debuff");
        }

        [Test]
        public void Modifiers_ClampAtZeroRatherThanGoingNegative()
        {
            var baseAttributes = Attributes(2, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, -50, ModifierSource.StatusEffect, "wither")
            };

            var resolved = AttributeResolver.Resolve(baseAttributes, modifiers);

            Assert.That(resolved.Strength, Is.EqualTo(0), "A negative attribute would break the status screen.");
        }

        [Test]
        public void Modifiers_StatusEffectRequiresAStackKey()
        {
            Assert.Throws<System.ArgumentException>(() =>
                _ = new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.StatusEffect, string.Empty));
        }

        [Test]
        public void Modifiers_ResolveOneMatchesTheFullResolve()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Wisdom, 4, ModifierSource.Equipment, "helm"),
                new AttributeModifier(AttributeKind.Wisdom, 2, ModifierSource.Permanent, "study")
            };

            var full = AttributeResolver.Resolve(baseAttributes, modifiers);
            var single = AttributeResolver.ResolveOne(baseAttributes, modifiers, AttributeKind.Wisdom);

            Assert.That(single, Is.EqualTo(full.Wisdom));
            Assert.That(single, Is.EqualTo(16));
        }

        // ---- Character state -------------------------------------------------

        [Test]
        public void State_StartsAtFullHealthAndMana()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            Assert.That(state.Health.Current, Is.EqualTo(state.Health.Max));
            Assert.That(state.Health.Max, Is.EqualTo(70));
            Assert.That(state.Mana.Max, Is.EqualTo(38));
            Assert.That(state.Mana.Current, Is.EqualTo(38));
        }

        [Test]
        public void State_AnEquipmentModifierRaisesTheEffectiveValueAndThePoolCeiling()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            state.AddModifier(new AttributeModifier(AttributeKind.Vitality, 2, ModifierSource.Equipment, "amulet"));

            Assert.That(state.BaseAttributes.Vitality, Is.EqualTo(10), "Equipment is a modifier, not growth.");
            Assert.That(state.EffectiveAttributes.Vitality, Is.EqualTo(12));
            Assert.That(state.Health.Max, Is.EqualTo(80), "20 base + 12 Vitality x 5");
            Assert.That(state.Health.Current, Is.EqualTo(70), "Equipping an item does not heal the character.");
        }

        [Test]
        public void State_RemovingAModifierRestoresTheOriginalValues()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));
            state.AddModifier(new AttributeModifier(AttributeKind.Vitality, 4, ModifierSource.Equipment, "amulet"));

            var removed = state.RemoveModifiersFrom(ModifierSource.Equipment);

            Assert.That(removed, Is.EqualTo(1));
            Assert.That(state.EffectiveAttributes.Vitality, Is.EqualTo(10));
            Assert.That(state.Health.Max, Is.EqualTo(70));
            Assert.That(state.Health.Current, Is.EqualTo(70), "Clamped from 70; the pool never exceeds its maximum.");
        }

        [Test]
        public void State_ALoweringModifierClampsCurrentHealth()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            state.AddModifier(new AttributeModifier(AttributeKind.Vitality, -8, ModifierSource.StatusEffect, "wasting"));

            Assert.That(state.Health.Max, Is.EqualTo(30), "20 base + 2 Vitality x 5");
            Assert.That(state.Health.Current, Is.EqualTo(30),
                "A character cannot hold more health than their maximum allows.");
        }

        [Test]
        public void State_GrowthRaisesTheBaseValueAndTheDerivedStat()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            state.SetBaseAttribute(AttributeKind.Vitality, 14);

            Assert.That(state.BaseAttributes.Vitality, Is.EqualTo(14));
            Assert.That(state.Health.Max, Is.EqualTo(90), "20 base + 14 Vitality x 5");
        }

        [Test]
        public void State_RejectsANegativeAttributeChange()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            Assert.Throws<System.ArgumentOutOfRangeException>(() =>
                state.SetBaseAttribute(AttributeKind.Vitality, -1));
            Assert.That(state.BaseAttributes.Vitality, Is.EqualTo(10), "A rejected change must not apply.");
        }

        [Test]
        public void State_SeedsAssessmentFromTheProfileThenChangesIndependently()
        {
            var first = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));
            var second = new CharacterState(_profile);

            first.SetCapabilityGrade(CapabilityGrade.C);
            first.SetGuildRank("Bronze");
            first.SetTitles(new[] { "Guild Initiate" });

            Assert.That(first.CapabilityGrade, Is.EqualTo(CapabilityGrade.C));
            Assert.That(second.CapabilityGrade, Is.EqualTo(CapabilityGrade.Unrated),
                "Two instances of one profile must not share mutable state.");
            Assert.That(_profile.DefaultCapabilityGrade, Is.EqualTo(CapabilityGrade.Unrated),
                "The authored asset itself is unchanged.");
            Assert.That(second.GuildRank, Is.Empty);
            Assert.That(second.Titles, Is.Empty);
        }

        [Test]
        public void State_RequiresAScalingAsset()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 1, 1, 1, 1, 1, 1));
            var withScaling = new CharacterState(_profile);
            Assert.That(withScaling, Is.Not.Null);

            var brokenProfile = CreateProfile(null, Attributes(1, 1, 1, 1, 1, 1, 1));
            try
            {
                Assert.That(brokenProfile.IsValid, Is.False);
                Assert.Throws<System.ArgumentException>(() => new CharacterState(brokenProfile));
            }
            finally
            {
                Object.DestroyImmediate(brokenProfile);
            }
        }

        // ---- Resource pools --------------------------------------------------

        [Test]
        public void Pool_StartsFullAndReportsTheFractionTheBarReads()
        {
            var pool = new ResourcePool(100);

            Assert.That(pool.Current, Is.EqualTo(100));
            Assert.That(pool.Max, Is.EqualTo(100));
            Assert.That(pool.Fraction, Is.EqualTo(1f));
        }

        [Test]
        public void Pool_SpendLowersTheFractionAndClampsAtZero()
        {
            var pool = new ResourcePool(100);

            pool.Spend(25);
            Assert.That(pool.Current, Is.EqualTo(75));
            Assert.That(pool.Fraction, Is.EqualTo(0.75f).Within(0.0001f));

            pool.Spend(500);
            Assert.That(pool.Current, Is.EqualTo(0), "Spending more than remains stops at zero, never negative.");
            Assert.That(pool.Fraction, Is.EqualTo(0f));
            Assert.That(pool.IsEmpty, Is.True);
        }

        [Test]
        public void Pool_RestoreDoesNotExceedTheMaximum()
        {
            var pool = new ResourcePool(100);
            pool.Spend(40);

            pool.Restore(20);
            Assert.That(pool.Current, Is.EqualTo(80));

            pool.Restore(999);
            Assert.That(pool.Current, Is.EqualTo(100), "Restoring more than the maximum stops at the maximum.");
            Assert.That(pool.Fraction, Is.EqualTo(1f));
        }

        [Test]
        public void Pool_AZeroMaximumDoesNotDivideByZero()
        {
            var pool = new ResourcePool(0);

            Assert.That(pool.Fraction, Is.EqualTo(0f), "A bar must not divide by a zero maximum.");
            Assert.That(pool.Current, Is.EqualTo(0));
        }

        [Test]
        public void Pool_RaisingTheMaximumKeepsTheCurrentValue()
        {
            var pool = new ResourcePool(100);
            pool.Spend(30);

            pool.SetMax(150);

            Assert.That(pool.Current, Is.EqualTo(70), "A higher ceiling does not heal the character.");
            Assert.That(pool.Max, Is.EqualTo(150));
        }

        [Test]
        public void Pool_LoweringTheMaximumClampsTheCurrentValue()
        {
            var pool = new ResourcePool(100);

            pool.SetMax(60);

            Assert.That(pool.Max, Is.EqualTo(60));
            Assert.That(pool.Current, Is.EqualTo(60), "A character cannot hold more than their ceiling.");
            Assert.That(pool.Fraction, Is.EqualTo(1f));
        }

        [Test]
        public void Pool_RejectsNegativeAmounts()
        {
            var pool = new ResourcePool(100);

            Assert.Throws<System.ArgumentOutOfRangeException>(() => pool.Spend(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => pool.Restore(-1));
            Assert.Throws<System.ArgumentOutOfRangeException>(() => pool.SetMax(-1));
        }

        [Test]
        public void Pool_AStateReachesTheDisplayedCurrentValue()
        {
            var state = CreateState(Attributes(1, 10, 1, 1, 1, 1, 7));

            state.Health.Spend(25);
            state.Mana.Spend(8);

            Assert.That(state.Health.Max, Is.EqualTo(70));
            Assert.That(state.Health.Current, Is.EqualTo(45));
            Assert.That(state.Health.Fraction, Is.EqualTo(45f / 70f).Within(0.0001f));

            Assert.That(state.Mana.Max, Is.EqualTo(38));
            Assert.That(state.Mana.Current, Is.EqualTo(30));
        }

        // ---- Serialization ---------------------------------------------------

        [Test]
        public void Snapshot_RoundTripsAttributesModifiersAndPools()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));
            var original = new CharacterState(_profile);
            original.SetBaseAttribute(AttributeKind.Vitality, 14);
            original.AddModifier(new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.Equipment, "sword"));
            original.AddModifier(new AttributeModifier(AttributeKind.Agility, 2, ModifierSource.StatusEffect, "haste", StackingPolicy.StrongestOnly));
            original.Health.Spend(25);
            original.Mana.Spend(8);
            original.SetCapabilityGrade(CapabilityGrade.D);
            original.SetGuildRank("Iron");
            original.SetTitles(new[] { "Beast Slayer" });

            var json = CharacterSerializer.Capture(original);

            var restored = new CharacterState(_profile);
            var ok = CharacterSerializer.TryRestore(json, restored, out var error);

            Assert.That(ok, Is.True, error);
            Assert.That(restored.BaseAttributes.Vitality, Is.EqualTo(14));
            Assert.That(restored.EffectiveAttributes.Strength, Is.EqualTo(4), "1 base + 3 equipment");
            Assert.That(restored.EffectiveAttributes.Agility, Is.EqualTo(3), "1 base + 2 status");
            Assert.That(restored.Health.Max, Is.EqualTo(90), "Recomputed, not restored from the save.");
            Assert.That(restored.Health.Current, Is.EqualTo(45),
                "70 at the time Vitality grew (raising a maximum does not heal), then 25 spent.");
            Assert.That(restored.Mana.Current, Is.EqualTo(30), "38 max - 8 spent");
            Assert.That(restored.CapabilityGrade, Is.EqualTo(CapabilityGrade.D));
            Assert.That(restored.GuildRank, Is.EqualTo("Iron"));
            Assert.That(restored.Titles, Is.EquivalentTo(new[] { "Beast Slayer" }));
        }

        [Test]
        public void Snapshot_DoesNotStoreDerivedValues()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));
            var state = new CharacterState(_profile);
            state.AddModifier(new AttributeModifier(AttributeKind.Vitality, 10, ModifierSource.Equipment, "plate"));

            var json = CharacterSerializer.Capture(state);

            Assert.That(json, Does.Not.Contain("maxHealth"),
                "A save must not carry a derived value that could drift from its own rules.");
            Assert.That(json, Does.Not.Contain("maxMana"));
        }

        [Test]
        public void Snapshot_RejectsAnUnsupportedVersionWithoutChangingTheCharacter()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));
            var state = new CharacterState(_profile);
            state.SetBaseAttribute(AttributeKind.Vitality, 12);

            var ok = CharacterSerializer.TryRestore("{\"version\":0}", state, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("version"));
            Assert.That(state.BaseAttributes.Vitality, Is.EqualTo(12), "A rejected save changes nothing.");
        }

        [Test]
        public void Snapshot_RejectsAnIncompleteAttributeSet()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));
            var state = new CharacterState(_profile);

            var ok = CharacterSerializer.TryRestore(
                "{\"version\":1,\"baseAttributes\":[1,2,3],\"currentHealth\":5,\"currentMana\":5}",
                state,
                out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("seven attributes"));
        }

        [Test]
        public void Snapshot_RejectsAStatusEffectModifierWithNoStackKey()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));
            var state = new CharacterState(_profile);

            const string json =
                "{\"version\":1,\"profileName\":\"Test Character\"," +
                "\"baseAttributes\":[1,10,1,1,1,1,7],\"currentHealth\":10,\"currentMana\":10," +
                "\"modifierAttributes\":[0],\"modifierValues\":[3],\"modifierSources\":[2]," +
                "\"modifierStackKeys\":[\"\"],\"modifierPolicies\":[1]," +
                "\"capabilityGrade\":0,\"guildRank\":\"\",\"titles\":[]}";

            var ok = CharacterSerializer.TryRestore(json, state, out var error);

            Assert.That(ok, Is.False);
            Assert.That(error, Does.Contain("stack key"));
        }

        // ---- Presentation contract -------------------------------------------

        [Test]
        public void GradeLabel_ShowsUnratedAsAWordRatherThanALetter()
        {
            Assert.That(CharacterDisplayText.GradeLabel(CapabilityGrade.Unrated), Is.EqualTo("Unrated"));
            Assert.That(CharacterDisplayText.GradeLabel(CapabilityGrade.F), Is.EqualTo("F"));
            Assert.That(CharacterDisplayText.GradeLabel(CapabilityGrade.S), Is.EqualTo("S"));
        }

        [Test]
        public void BindersElementNames_AllExistInTheAuthoredUxml()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "UI", "Status", "CharacterStatus.uxml");
            var uxml = System.IO.File.ReadAllText(path);

            var expected = new List<string>
            {
                // Identity binder
                "character-name",
                "identity-species",
                "identity-gender",
                "identity-public-discipline",
                "identity-description",
                "identity-portrait-host",
                // Attributes binder
                "derived-health-host",
                "derived-mana-host",
                // Header summary binder
                "summary-health-host",
                "summary-mana-host",
                "summary-grade",
                // Assessment binder
                "assessment-capability-grade",
                "assessment-guild-rank",
                "assessment-titles",
                // Full-bleed regions. The tab strip is gone: Skills became a
                // navigation destination, so status-tab-attributes,
                // status-tab-skills, status-page-attributes and status-page-skills
                // are deliberately absent and must not be re-added here.
                "status-body",
                "status-header",
                "status-content",
                "status-footer",
                "description-panel",
                "tabs-panel",
                // Breakdown popover. BreakdownPopover queries every one of these
                // and refuses to build without them, so a rename in the UXML must
                // fail here rather than at runtime in the scene.
                "breakdown-popover",
                "breakdown-title",
                "breakdown-lines",
                "breakdown-base-row",
                "breakdown-base-value",
                "breakdown-total-value"
            };

            foreach (var kind in AttributeKinds.All)
                expected.Add(CharacterDisplayText.ValueElementName(kind));

            foreach (var elementName in expected)
            {
                Assert.That(uxml, Does.Contain($"name=\"{elementName}\""),
                    $"A status binder or the controller queries '{elementName}', which the authored UXML must define.");
            }
        }

        [Test]
        public void TitleTemplate_DefinesTheLabelTheAssessmentBinderQueries()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "UI", "Status", "StatusTitle.uxml");
            var uxml = System.IO.File.ReadAllText(path);

            Assert.That(uxml, Does.Contain("name=\"status-title\""),
                "The assessment binder instantiates the title template and queries 'status-title'.");
        }

        [Test]
        public void PortraitTemplate_DefinesTheElementsThePortraitViewQueries()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "UI", "Status", "CharacterPortrait.uxml");
            var uxml = System.IO.File.ReadAllText(path);

            foreach (var elementName in new[] { "portrait-frame", "portrait-image", "portrait-placeholder" })
            {
                Assert.That(uxml, Does.Contain($"name=\"{elementName}\""),
                    $"CharacterPortraitView queries '{elementName}', which the authored template must define.");
            }
        }

        [Test]
        public void BreakdownLineTemplate_DefinesTheElementsThePopoverQueries()
        {
            var path = System.IO.Path.Combine(Application.dataPath, "UI", "Status", "BreakdownLine.uxml");
            var uxml = System.IO.File.ReadAllText(path);

            foreach (var elementName in new[] { "breakdown-line-label", "breakdown-line-value" })
            {
                Assert.That(uxml, Does.Contain($"name=\"{elementName}\""),
                    $"BreakdownPopover queries '{elementName}' on each instantiated line, " +
                    "which the authored template must define.");
            }
        }

        [Test]
        public void Identity_CarriesTheAuthoredFieldsRatherThanTheProfileFlatteningThem()
        {
            _scaling = CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, Attributes(1, 10, 1, 1, 1, 1, 7));

            Assert.That(_profile.Identity, Is.Not.Null, "A profile must always expose an identity block.");
            Assert.That(_profile.DisplayName, Is.EqualTo("Test Character"));
            Assert.That(_profile.Identity.Portrait, Is.Null, "Character art does not exist yet; null is a valid state.");
        }

        // ---- Breakdown -------------------------------------------------------

        /// <summary>
        /// The contract that makes the popover trustworthy: whatever
        /// <see cref="AttributeResolver.Explain"/> reports as the total must be the
        /// number the game actually uses. If someone changes the resolver's
        /// stacking and forgets Explain, this fails instead of the screen quietly
        /// lying about arithmetic.
        /// </summary>
        [Test]
        public void Breakdown_TotalAlwaysMatchesTheResolvedValue()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);

            var cases = new List<AttributeModifier[]>
            {
                new AttributeModifier[0],
                new[] { new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.Equipment, "sword") },
                new[]
                {
                    new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.Equipment, "sword"),
                    new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.Permanent, "training")
                },
                new[]
                {
                    new AttributeModifier(AttributeKind.Strength, 4, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                    new AttributeModifier(AttributeKind.Strength, 7, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                    new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly)
                },
                new[]
                {
                    new AttributeModifier(AttributeKind.Strength, 6, ModifierSource.StatusEffect, "aura", StackingPolicy.StrongestOnly),
                    new AttributeModifier(AttributeKind.Strength, -4, ModifierSource.StatusEffect, "aura", StackingPolicy.StrongestOnly)
                },
                new[]
                {
                    new AttributeModifier(AttributeKind.Strength, -50, ModifierSource.StatusEffect, "wither")
                }
            };

            foreach (var modifiers in cases)
            {
                var resolved = AttributeResolver.Resolve(baseAttributes, modifiers).Strength;
                var breakdown = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Strength);

                Assert.That(breakdown.Total, Is.EqualTo(resolved),
                    $"Explain reported {breakdown.Total} where the resolver produced {resolved}. " +
                    "The popover would contradict the number on the screen.");
            }
        }

        [Test]
        public void Breakdown_ReportsOneLinePerSourceAndStackKey()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 3, ModifierSource.Equipment, "sword"),
                new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.Equipment, "ring"),
                new AttributeModifier(AttributeKind.Strength, 1, ModifierSource.Permanent, "training")
            };

            var breakdown = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Strength);

            Assert.That(breakdown.BaseValue, Is.EqualTo(10));
            Assert.That(breakdown.Entries.Count, Is.EqualTo(3),
                "Each source and stack key is one line, because that is the unit the player can act on.");
            Assert.That(breakdown.Total, Is.EqualTo(16));
        }

        [Test]
        public void Breakdown_StrongestOnlyReportsTheWinnerNotTheSum()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Strength, 4, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly),
                new AttributeModifier(AttributeKind.Strength, 7, ModifierSource.StatusEffect, "blessing", StackingPolicy.StrongestOnly)
            };

            var breakdown = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Strength);

            // Three overlapping blessings are one line, saying 7. Reporting 4 and 7
            // as separate lines would imply both applied.
            Assert.That(breakdown.Entries.Count, Is.EqualTo(1));
            Assert.That(breakdown.Entries[0].Value, Is.EqualTo(7));
            Assert.That(breakdown.Total, Is.EqualTo(17));
        }

        [Test]
        public void Breakdown_IgnoresModifiersForOtherAttributes()
        {
            var baseAttributes = Attributes(10, 10, 10, 10, 10, 10, 10);
            var modifiers = new[]
            {
                new AttributeModifier(AttributeKind.Wisdom, 5, ModifierSource.Equipment, "helm"),
                new AttributeModifier(AttributeKind.Strength, 2, ModifierSource.Equipment, "sword")
            };

            var strength = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Strength);
            var wisdom = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Wisdom);
            var agility = AttributeResolver.Explain(baseAttributes, modifiers, AttributeKind.Agility);

            Assert.That(strength.Total, Is.EqualTo(12));
            Assert.That(wisdom.Total, Is.EqualTo(15));
            Assert.That(agility.Total, Is.EqualTo(10));
            Assert.That(agility.HasContributions, Is.False, "An unmodified attribute reports no contributions.");
        }

        [Test]
        public void Breakdown_ZeroModifiersReportsBaseEqualsTotal()
        {
            var baseAttributes = Attributes(12, 14, 13, 15, 17, 16, 18);

            var breakdown = AttributeResolver.Explain(baseAttributes, null, AttributeKind.Strength);

            Assert.That(breakdown.BaseValue, Is.EqualTo(12));
            Assert.That(breakdown.Total, Is.EqualTo(12));
            Assert.That(breakdown.HasContributions, Is.False);
            Assert.That(breakdown.Title, Is.EqualTo("Strength"), "The popover title is the System's own word.");
        }

        [Test]
        public void Breakdown_MaxHealthExplainsTheFlatBaseAndTheVitalityContribution()
        {
            _scaling = CreateScaling(baseHealth: 20, healthPerVitality: 5, baseMana: 10, manaPerSpirit: 4);
            var attributes = Attributes(1, 14, 1, 1, 1, 1, 7);

            var health = DerivedStatCalculator.ExplainMaxHealth(attributes, _scaling);

            Assert.That(health.Title, Is.EqualTo("MAX HP"));
            Assert.That(health.BaseValue, Is.EqualTo(20));
            Assert.That(health.Total, Is.EqualTo(90), "20 base + 14 Vitality x 5");
            Assert.That(health.Entries.Count, Is.EqualTo(1));
            Assert.That(health.Entries[0].Label, Does.Contain("Vitality"));
            Assert.That(health.Entries[0].Value, Is.EqualTo(70));

            // The lines must add up to the total, or the panel is decoration.
            Assert.That(health.BaseValue + health.Entries[0].Value, Is.EqualTo(health.Total));
        }

        [Test]
        public void Breakdown_MaxManaExplainsTheFlatBaseAndTheSpiritContribution()
        {
            _scaling = CreateScaling(baseHealth: 20, healthPerVitality: 5, baseMana: 10, manaPerSpirit: 4);
            var attributes = Attributes(1, 10, 1, 1, 1, 1, 7);

            var mana = DerivedStatCalculator.ExplainMaxMana(attributes, _scaling);

            Assert.That(mana.Title, Is.EqualTo("MANA"));
            Assert.That(mana.BaseValue, Is.EqualTo(10));
            Assert.That(mana.Total, Is.EqualTo(38), "10 base + 7 Spirit x 4");
            Assert.That(mana.Entries.Count, Is.EqualTo(1));
            Assert.That(mana.Entries[0].Value, Is.EqualTo(28));
            Assert.That(mana.BaseValue + mana.Entries[0].Value, Is.EqualTo(mana.Total));
        }

        /// <summary>
        /// The floor raises a value that would otherwise be zero. The contribution
        /// line must report the difference that was actually realised, so base
        /// plus lines still equals the total instead of claiming points the total
        /// does not include.
        /// </summary>
        [Test]
        public void Breakdown_FlooredDerivedValueStillAddsUp()
        {
            _scaling = CreateScaling(baseHealth: 0, healthPerVitality: 0, baseMana: 0, manaPerSpirit: 0);
            var attributes = Attributes(1, 5, 1, 1, 1, 1, 5);

            var health = DerivedStatCalculator.ExplainMaxHealth(attributes, _scaling);

            Assert.That(health.Total, Is.EqualTo(DerivedStatCalculator.MinimumMaxHealth),
                "A living character always has at least one point in a pool.");
            Assert.That(health.BaseValue + SumEntries(health), Is.EqualTo(health.Total),
                "The breakdown must reconcile with the floored total.");
        }

        [Test]
        public void Breakdown_StateExposesThePopoversProviders()
        {
            var state = CreateState(Attributes(12, 14, 13, 15, 17, 16, 18));

            state.AddModifier(new AttributeModifier(AttributeKind.Strength, 4, ModifierSource.Equipment, "sword"));

            // The attribute breakdown is model-only for now: the screen wires the two
            // vitals, and this path is what the attribute rows use once modifiers
            // exist. It is covered here so it cannot rot in the meantime.
            var strength = state.ExplainAttribute(AttributeKind.Strength);
            Assert.That(strength.Total, Is.EqualTo(16));
            Assert.That(strength.Entries.Count, Is.EqualTo(1));

            // Vitality is unchanged, so Max HP reflects only the base Vitality.
            var health = state.ExplainMaxHealth();
            Assert.That(health.Total, Is.EqualTo(state.Derived.MaxHealth),
                "The popover and the bar must read the same derivation.");

            var mana = state.ExplainMaxMana();
            Assert.That(mana.Total, Is.EqualTo(state.Derived.MaxMana));
        }

        static int SumEntries(ValueBreakdown breakdown)
        {
            var sum = 0;
            foreach (var entry in breakdown.Entries)
                sum += entry.Value;
            return sum;
        }

        // ---- Helpers ---------------------------------------------------------

        CharacterState CreateState(CharacterAttributes attributes)
        {
            _scaling ??= CreateScaling(20, 5, 10, 4);
            _profile = CreateProfile(_scaling, attributes);
            return new CharacterState(_profile);
        }

        static CharacterAttributes Attributes(
            int strength,
            int vitality,
            int agility,
            int dexterity,
            int intelligence,
            int wisdom,
            int spirit)
        {
            return new CharacterAttributes(strength, vitality, agility, dexterity, intelligence, wisdom, spirit);
        }

        /// <summary>
        /// Assigns the private serialized coefficients the way the inspector
        /// would. Test-only: project scripts never write serialized fields this way.
        /// </summary>
        static AttributeScaling CreateScaling(int baseHealth, int healthPerVitality, int baseMana, int manaPerSpirit)
        {
            var scaling = ScriptableObject.CreateInstance<AttributeScaling>();
            var serialized = new SerializedObject(scaling);
            serialized.FindProperty("baseHealth").intValue = baseHealth;
            serialized.FindProperty("healthPerVitality").intValue = healthPerVitality;
            serialized.FindProperty("baseMana").intValue = baseMana;
            serialized.FindProperty("manaPerSpirit").intValue = manaPerSpirit;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return scaling;
        }

        static CharacterProfile CreateProfile(AttributeScaling scaling, CharacterAttributes attributes)
        {
            var profile = ScriptableObject.CreateInstance<CharacterProfile>();
            var serialized = new SerializedObject(profile);
            serialized.FindProperty("identity").FindPropertyRelative("displayName").stringValue = "Test Character";
            serialized.FindProperty("scaling").objectReferenceValue = scaling;

            var attributeProperty = serialized.FindProperty("attributes");
            attributeProperty.FindPropertyRelative("strength").intValue = attributes.Strength;
            attributeProperty.FindPropertyRelative("vitality").intValue = attributes.Vitality;
            attributeProperty.FindPropertyRelative("agility").intValue = attributes.Agility;
            attributeProperty.FindPropertyRelative("dexterity").intValue = attributes.Dexterity;
            attributeProperty.FindPropertyRelative("intelligence").intValue = attributes.Intelligence;
            attributeProperty.FindPropertyRelative("wisdom").intValue = attributes.Wisdom;
            attributeProperty.FindPropertyRelative("spirit").intValue = attributes.Spirit;

            serialized.ApplyModifiedPropertiesWithoutUndo();
            return profile;
        }
    }
}
