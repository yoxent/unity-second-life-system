using NUnit.Framework;

namespace SecondLife.UI.EditModeTests
{
    /// <summary>
    /// Guards the navigation bar's authored manifest.
    ///
    /// The group list is inspector data, so a mistake in it is a build-time defect
    /// that no compile error catches: a blank label, or the same label twice, which
    /// leaves the player looking at two identical buttons. <see cref="NavigationBar"/>
    /// refuses to bind when the manifest is unusable, and this suite pins down what
    /// "unusable" means so the rule cannot drift.
    ///
    /// The bar's hover, routing, and lifecycle are PlayMode concerns; see
    /// <c>NavigationBarTests</c> in the PlayMode suite.
    /// </summary>
    public sealed class NavigationBarManifestTests
    {
        static NavigationBar.Destination Destination(string screenId, string label)
        {
            return new NavigationBar.Destination(screenId, label);
        }

        static NavigationBar.Group Group(string label, params NavigationBar.Destination[] destinations)
        {
            return new NavigationBar.Group(label, destinations);
        }

        [Test]
        public void ValidateManifest_AcceptsDistinctGroupsAndDestinations()
        {
            var groups = new[]
            {
                Group("World", Destination("world-map", "World Map"), Destination("dungeon", "Dungeon")),
                Group("Character", Destination("status", "Status"))
            };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Null,
                "A well-formed manifest must be accepted.");
        }

        [Test]
        public void ValidateManifest_RejectsNoGroups()
        {
            Assert.That(NavigationBar.ValidateManifest(null), Is.Not.Null);
            Assert.That(NavigationBar.ValidateManifest(new NavigationBar.Group[0]), Is.Not.Null,
                "A bar with no groups would render as an empty strip.");
        }

        [Test]
        public void ValidateManifest_RejectsBlankGroupLabel()
        {
            var groups = new[] { Group("  ", Destination("status", "Status")) };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Not.Null,
                "A group with no label renders as a nameless button.");
        }

        [Test]
        public void ValidateManifest_RejectsDuplicateGroupLabel()
        {
            var groups = new[]
            {
                Group("World", Destination("world-map", "World Map")),
                Group("World", Destination("dungeon", "Dungeon"))
            };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Not.Null,
                "Two groups with one label are indistinguishable to the player.");
        }

        [Test]
        public void ValidateManifest_RejectsGroupWithNoDestinations()
        {
            var groups = new[] { Group("World") };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Not.Null,
                "A group with no destinations would open an empty flyout.");
        }

        [Test]
        public void ValidateManifest_RejectsBlankDestinationLabel()
        {
            var groups = new[] { Group("World", Destination("world-map", "")) };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Not.Null,
                "A destination with no label renders as an unreadable button.");
        }

        [Test]
        public void ValidateManifest_RejectsDuplicateDestinationLabelWithinAGroup()
        {
            var groups = new[]
            {
                Group("World", Destination("dungeon", "Dungeon"), Destination("raid", "Dungeon"))
            };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Not.Null,
                "Two destinations with one label in the same flyout are indistinguishable.");
        }

        [Test]
        public void ValidateManifest_AllowsTheSameDestinationLabelInDifferentGroups()
        {
            // "Shop" as a group and "Shop" as a destination elsewhere is readable,
            // because each is in its own flyout. Only the group labels must be
            // unique across the whole bar.
            var groups = new[]
            {
                Group("World", Destination("world-map", "Shop")),
                Group("Shop", Destination("shop", "Market"))
            };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Null);
        }

        [Test]
        public void ValidateManifest_AllowsAnEmptyScreenId()
        {
            // An id is deliberately optional: an unbuilt destination keeps its
            // label and renders disabled. Only the label is required.
            var groups = new[] { Group("World", Destination("", "World Map")) };

            Assert.That(NavigationBar.ValidateManifest(groups), Is.Null,
                "A destination awaiting its screen is a valid, disabled entry.");
        }
    }
}
