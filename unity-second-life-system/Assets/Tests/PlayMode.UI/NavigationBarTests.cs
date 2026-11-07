using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SecondLife.UI.PlayModeTests
{
    /// <summary>
    /// Guards the navigation bar against the defects that only appear once a real
    /// visual tree exists.
    ///
    /// Three things cannot be checked from EditMode. The flyout is shown and hidden
    /// by a USS class, so whether a group is "open" is a live-tree question. The
    /// router call is a click-time side effect. And the whole bar is rebuilt after
    /// a disable, which is the defect that has already broken two presenters in
    /// this project.
    /// </summary>
    public sealed class NavigationBarTests
    {
        const string SceneName = "SampleScene";
        const string BarObject = "Navigation Bar";

        const int StartupFrameBudget = 600;

        [UnityTest]
        public IEnumerator NavigationBar_RendersEveryGroupWithACaret()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            Assert.That(document, Is.Not.Null, $"'{BarObject}' must carry a UIDocument in {SceneName}.");

            var buttons = document.rootVisualElement.Query<Button>(name: "nav-group-button").ToList();
            Assert.That(buttons.Count, Is.EqualTo(5),
                "The bar must render one button per configured group.");

            foreach (var button in buttons)
            {
                Assert.That(button.text, Does.EndWith("^"),
                    $"Group '{button.text}' has no caret. The caret is the only cue that the button opens a flyout.");
                Assert.That(button.text.Trim(), Is.Not.Empty,
                    "A group button rendered without a label.");
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_EveryFlyoutStartsClosed()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var groups = document.rootVisualElement.Query<VisualElement>(name: "nav-group").ToList();
            Assert.That(groups.Count, Is.EqualTo(5));

            foreach (var group in groups)
            {
                Assert.That(group.ClassListContains("nav-group--open"), Is.False,
                    "A flyout was open before the player asked for it.");
            }

            // A closed flyout must also be out of layout, or it would still paint
            // and still take clicks from the screen underneath.
            yield return null;
            foreach (var flyout in document.rootVisualElement.Query<VisualElement>(name: "nav-group-flyout").ToList())
            {
                Assert.That(flyout.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                    "A closed flyout is still being drawn.");
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_HoverOpensTheGroupAndLeavingClosesIt()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var groups = document.rootVisualElement.Query<VisualElement>(name: "nav-group").ToList();
            var first = groups[0];

            Enter(first);
            yield return null;

            Assert.That(first.ClassListContains("nav-group--open"), Is.True,
                "Hovering a group did not open its flyout.");
            Assert.That(DisplayOf(FirstFlyout(first)), Is.EqualTo(DisplayStyle.Flex),
                "The open flyout is still display: none.");

            Leave(first);
            yield return null;

            Assert.That(first.ClassListContains("nav-group--open"), Is.False,
                "Leaving the group did not close its flyout.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_MovingFromButtonIntoTheFlyoutKeepsItOpen()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var group = document.rootVisualElement.Query<VisualElement>(name: "nav-group").First();

            // The pointer entering the flyout is still inside the group container,
            // because the flyout is its child. A leave on the group would therefore
            // be a bug: it would shut the panel the player is reaching for.
            Enter(group);
            yield return null;

            var flyout = FirstFlyout(group);
            Enter(flyout);
            yield return null;

            Assert.That(group.ClassListContains("nav-group--open"), Is.True,
                "Entering the flyout closed its own group. The flyout must be a child of the group, " +
                "not a sibling, so crossing the gap never leaves the hover region.");

            // Only leaving the group itself may close it.
            Leave(group);
            yield return null;

            Assert.That(group.ClassListContains("nav-group--open"), Is.False,
                "Leaving the group with the pointer did not close the flyout.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_OpeningOneGroupClosesTheOther()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var groups = document.rootVisualElement.Query<VisualElement>(name: "nav-group").ToList();

            Enter(groups[0]);
            yield return null;
            Enter(groups[2]);
            yield return null;

            Assert.That(groups[0].ClassListContains("nav-group--open"), Is.False,
                "Two flyouts were open at once. Only one may show, or they overlap.");
            Assert.That(groups[2].ClassListContains("nav-group--open"), Is.True,
                "Hovering a second group did not open it.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_ClickingAGroupKeepsItOpenAfterThePointerLeaves()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var group = document.rootVisualElement.Query<VisualElement>(name: "nav-group").First();
            var button = group.Q<Button>("nav-group-button");

            Enter(group);
            InvokeHandlers(button);
            yield return null;

            Leave(group);
            yield return null;

            Assert.That(group.ClassListContains("nav-group--open"), Is.True,
                "A clicked group closed when the pointer left. Clicking latches the flyout open " +
                "so it stays reachable without hovering.");

            // A second click unlatches it.
            InvokeHandlers(button);
            yield return null;

            Assert.That(group.ClassListContains("nav-group--open"), Is.False,
                "Clicking an open group did not close it.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_UnbuiltDestinationsAreDisabled()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var items = document.rootVisualElement.Query<Button>(name: "nav-item").ToList();
            Assert.That(items.Count, Is.GreaterThan(0), "No destinations rendered.");

            // Only the character sheet and the skills screen exist today. Every other
            // destination must present as unavailable rather than as a button that does
            // nothing. This list is deliberately explicit: when a screen is added to the
            // router, this assertion should fail so the bar's availability is re-checked.
            foreach (var item in items)
            {
                var expected = item.text == "Status" || item.text == "Skills";
                Assert.That(item.enabledSelf, Is.EqualTo(expected),
                    $"Destination '{item.text}' is {(item.enabledSelf ? "enabled" : "disabled")}. " +
                    "Availability comes from the router's screen list, which currently holds " +
                    "status, narrative, and skills.");
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_StatusDestinationShowsTheCharacterSheet()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var router = Object.FindFirstObjectByType<ScreenRouter>();
            Assert.That(router, Is.Not.Null, $"'UI Router' must carry a ScreenRouter in {SceneName}.");

            // Start from another screen so the click has something to change.
            router.Show("narrative");
            yield return null;

            var statusItem = document.rootVisualElement
                .Query<Button>(name: "nav-item")
                .ToList()
                .Find(item => item.text == "Status");
            Assert.That(statusItem, Is.Not.Null, "The bar has no Status destination.");
            Assert.That(statusItem.enabledSelf, Is.True, "Status must be reachable; its screen exists.");

            InvokeHandlers(statusItem);
            yield return null;

            var status = FindStatusDocument();
            Assert.That(status, Is.Not.Null);
            Assert.That(DisplayOf(status), Is.EqualTo(DisplayStyle.Flex),
                "Clicking Status did not show the character sheet.");

            // The click also closes the flyout it came from.
            foreach (var group in document.rootVisualElement.Query<VisualElement>(name: "nav-group").ToList())
            {
                Assert.That(group.ClassListContains("nav-group--open"), Is.False,
                    "The flyout stayed open after navigating.");
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_InertsWhenNoDestinationIsAvailable()
        {
            yield return LoadScene();

            var router = Object.FindFirstObjectByType<ScreenRouter>();
            Assert.That(router, Is.Not.Null);

            // Availability is read from the router, so an id the router does not
            // know must read as unavailable. This is what makes a typo in the
            // manifest a disabled button instead of a dead click.
            Assert.That(router.HasScreen("status"), Is.True);
            Assert.That(router.HasScreen("dungeon"), Is.False,
                "A screen id was added to the router. Update the expectation for this bar test.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_SurvivesDisableAndReenable()
        {
            yield return LoadScene();

            var document = FindBarDocument();
            var before = document.rootVisualElement.Query<Button>(name: "nav-group-button").ToList();
            Assert.That(before.Count, Is.EqualTo(5), "Precondition: the bar rendered its groups.");

            // The cycle that has broken two presenters in this project.
            document.gameObject.SetActive(false);
            yield return null;

            document.gameObject.SetActive(true);
            yield return null;

            // Re-query: UIDocument rebuilds the tree, so the old references point
            // at discarded elements and would report a false failure.
            var buttons = document.rootVisualElement.Query<Button>(name: "nav-group-button").ToList();
            Assert.That(buttons.Count, Is.EqualTo(5),
                "The bar did not rebuild its groups after a re-enable. The controller holds references " +
                "into a discarded tree, or it never rebuilt on enable.");

            // Handlers must be live again, not merely present as elements. A
            // rebuilt tree with no registered callbacks looks identical until a click.
            var group = document.rootVisualElement.Query<VisualElement>(name: "nav-group").First();
            Enter(group);
            yield return null;

            Assert.That(group.ClassListContains("nav-group--open"), Is.True,
                "The flyout did not open after a re-enable. The hover handlers were not re-registered.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_SitsAboveTheScreensAndDoesNotCoverTheFooter()
        {
            yield return LoadScene();
            yield return null;

            var bar = FindBarDocument();
            var status = FindStatusDocument();

            var barHeight = bar.rootVisualElement.Q<VisualElement>("nav-bar").resolvedStyle.height;
            Assert.That(barHeight, Is.GreaterThan(0f), "The bar resolved to zero height.");

            // The bar is absolute, so it must start at the viewport's bottom edge.
            var barTop = bar.rootVisualElement.Q<VisualElement>("nav-bar").worldBound.y;
            var viewportHeight = bar.rootVisualElement.resolvedStyle.height;
            Assert.That(barTop + barHeight, Is.EqualTo(viewportHeight).Within(1f),
                "The bar is not pinned to the bottom of the viewport.");

            // The character sheet's footer must end above the bar, which is what the
            // reserved --nav-inset on its root is for.
            var footer = status.rootVisualElement.Q<VisualElement>("status-footer");
            Assert.That(footer, Is.Not.Null);

            var footerBottom = footer.worldBound.yMax;
            Assert.That(footerBottom, Is.LessThanOrEqualTo(barTop + 1f),
                "The status footer is covered by the navigation bar. The screen must reserve the bar's height.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_GroupButtonsFillTheBarHeight()
        {
            yield return LoadScene();
            yield return null;

            var document = FindBarDocument();
            var barHeight = document.rootVisualElement.Q<VisualElement>("nav-bar").resolvedStyle.height;
            Assert.That(barHeight, Is.GreaterThan(0f));

            // A group is the hover region, so it must span the bar. When align-items:
            // stretch alone was relied on, the group collapsed to its content and
            // left a dead strip above every button. This pins the result, not the
            // CSS technique, so either layout approach is allowed to satisfy it.
            foreach (var group in document.rootVisualElement.Query<VisualElement>(name: "nav-group").ToList())
            {
                var groupHeight = group.worldBound.height;
                Assert.That(groupHeight, Is.GreaterThanOrEqualTo(barHeight - 1f),
                    $"A group is {groupHeight:F1}px tall inside a {barHeight:F1}px bar. The strip above it " +
                    "is not part of the hover region, so the flyout closes without warning there.");
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_OpenFlyoutMeetsItsGroupWithNoDeadGap()
        {
            yield return LoadScene();
            yield return null;

            var document = FindBarDocument();
            var group = document.rootVisualElement.Query<VisualElement>(name: "nav-group").First();

            Enter(group);
            yield return null;

            var flyout = FirstFlyout(group);
            var groupTop = group.worldBound.y;
            var flyoutBottom = flyout.worldBound.yMax;

            // The flyout's outer box must reach down to the group's top edge. The
            // visible gap is padding inside that box, so the pointer crosses it while
            // still inside the group. A margin would leave untouchable space here and
            // the flyout would shut as the pointer crossed it.
            Assert.That(flyoutBottom, Is.EqualTo(groupTop).Within(1f),
                $"The flyout ends at y={flyoutBottom:F1} but its group starts at y={groupTop:F1}. " +
                "The space between them is a dead gap: the pointer leaves the group while crossing it.");

            var panel = group.Q<VisualElement>("nav-group-panel");
            Assert.That(panel, Is.Not.Null);
            Assert.That(panel.worldBound.yMax, Is.LessThan(flyoutBottom),
                "The panel must sit above the flyout's bottom edge, leaving part of the element " +
                "as the crossed gap rather than putting the painted panel flush against the bar.");
        }

        [UnityTest]
        public IEnumerator NavigationBar_FlyoutsStayInsideTheViewport()
        {
            yield return LoadScene();
            yield return null;

            var document = FindBarDocument();
            var root = document.rootVisualElement;
            var viewportWidth = root.resolvedStyle.width;

            foreach (var group in root.Query<VisualElement>(name: "nav-group").ToList())
            {
                Enter(group);
                yield return null;

                var flyout = FirstFlyout(group);
                var left = flyout.worldBound.xMin;
                var right = flyout.worldBound.xMax;

                Assert.That(left, Is.GreaterThanOrEqualTo(-1f),
                    "A flyout extends past the left edge of the viewport, so part of it cannot be clicked.");
                Assert.That(right, Is.LessThanOrEqualTo(viewportWidth + 1f),
                    "A flyout extends past the right edge of the viewport, so part of it cannot be clicked.");

                Leave(group);
                yield return null;
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_MarksTheCurrentDestination()
        {
            yield return LoadScene();
            yield return null;

            var document = FindBarDocument();
            var router = Object.FindFirstObjectByType<ScreenRouter>();
            Assert.That(router, Is.Not.Null);

            // The screen the router opened on must already be marked. The bar used to
            // show only where you could go, so every group looked equally inactive and
            // the player had to remember which screen they were on.
            AssertMarked(document, "Status",
                "The initial screen is not marked current in the bar.");

            router.Show("skills");
            yield return null;

            AssertMarked(document, "Skills",
                "The marker did not follow the screen change.");
            AssertNotMarked(document, "Status",
                "Two destinations are marked current at once.");

            router.Show("status");
            yield return null;

            AssertMarked(document, "Status", "The marker did not return with the screen.");
        }

        [UnityTest]
        public IEnumerator ScreenRouter_RaisesChangedOnlyOnAnActualChange()
        {
            yield return LoadScene();
            yield return null;

            var router = Object.FindFirstObjectByType<ScreenRouter>();
            Assert.That(router, Is.Not.Null);

            var raises = 0;
            var last = string.Empty;
            System.Action<string> handler = id =>
            {
                raises++;
                last = id;
            };

            router.Changed += handler;
            try
            {
                router.Show("narrative");
                Assert.That(raises, Is.EqualTo(1), "Changing screens must notify once.");
                Assert.That(last, Is.EqualTo("narrative"));

                // Showing the screen that is already visible is a no-op for the player,
                // so it must not wake every listener that redraws.
                router.Show("narrative");
                Assert.That(raises, Is.EqualTo(1),
                    "Show() with the current id raised Changed again. Listeners would repaint for nothing.");

                // An unknown id logs an error and shows nothing. Declared here because
                // the error is the designed behaviour, not a failure of this test.
                LogAssert.Expect(LogType.Error,
                    new System.Text.RegularExpressions.Regex("has no screen with id 'not-a-screen'"));
                router.Show("not-a-screen");
                Assert.That(raises, Is.EqualTo(1),
                    "An unknown id must not raise Changed, because no screen was shown.");
            }
            finally
            {
                router.Changed -= handler;
            }
        }

        [UnityTest]
        public IEnumerator NavigationBar_DoesNotCoverTheNarrativeActionButtons()
        {
            yield return LoadScene();

            var router = Object.FindFirstObjectByType<ScreenRouter>();
            router.Show("narrative");
            yield return null;
            yield return null;

            var bar = FindBarDocument();
            var narrative = FindDocument("Narrative Validation");
            Assert.That(narrative, Is.Not.Null);

            var barTop = bar.rootVisualElement.Q<VisualElement>("nav-bar").worldBound.y;
            Assert.That(barTop, Is.GreaterThan(0f), "Layout has not run yet.");

            // Only the status footer was guarded before. The narrative screen reserves
            // a different inset because it has a different edge gap, so it needed its
            // own check; a change to the bar height would strand this one silently.
            foreach (var button in narrative.rootVisualElement.Query<Button>().ToList())
            {
                Assert.That(button.worldBound.yMax, Is.LessThanOrEqualTo(barTop + 1f),
                    $"Narrative button '{button.text}' is covered by the navigation bar. " +
                    "Its screen must reserve the bar's height too.");
            }
        }

        // ---- Helpers ---------------------------------------------------------

        static IEnumerator LoadScene()
        {
            // Always reload, even when this scene is already active, so a test that
            // switches screens cannot leave the next test starting on the wrong one.
            var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            if (load != null)
                yield return load;

            yield return null;

            for (var frame = 0; frame < StartupFrameBudget; frame++)
            {
                var document = FindBarDocument();
                if (document != null)
                {
                    var root = document.rootVisualElement;
                    if (root != null && root.Q<VisualElement>("nav-groups")?.childCount > 0)
                        break;
                }

                yield return null;
            }
        }

        static UIDocument FindBarDocument()
        {
            foreach (var candidate in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == BarObject)
                    return candidate;
            }

            return null;
        }

        static UIDocument FindStatusDocument()
        {
            return FindDocument("Character Status");
        }

        static UIDocument FindDocument(string objectName)
        {
            foreach (var candidate in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == objectName)
                    return candidate;
            }

            return null;
        }

        static void AssertMarked(UIDocument bar, string label, string because)
        {
            var item = FindItem(bar, label);
            Assert.That(item.ClassListContains("nav-item--current"), Is.True, because);
        }

        static void AssertNotMarked(UIDocument bar, string label, string because)
        {
            var item = FindItem(bar, label);
            Assert.That(item.ClassListContains("nav-item--current"), Is.False, because);
        }

        static Button FindItem(UIDocument bar, string label)
        {
            foreach (var item in bar.rootVisualElement.Query<Button>(name: "nav-item").ToList())
            {
                if (item.text == label)
                    return item;
            }

            Assert.Fail($"The bar has no destination labelled '{label}'.");
            return null;
        }

        static VisualElement FirstFlyout(VisualElement group)
        {
            var flyout = group.Q<VisualElement>("nav-group-flyout");
            Assert.That(flyout, Is.Not.Null, "A group has no nav-group-flyout.");
            return flyout;
        }

        static DisplayStyle DisplayOf(VisualElement element)
        {
            return element.resolvedStyle.display;
        }

        static DisplayStyle DisplayOf(UIDocument document)
        {
            return DisplayOf(document.rootVisualElement);
        }

        static void Enter(VisualElement element)
        {
            using (var pointer = PointerEnterEvent.GetPooled())
            {
                pointer.target = element;
                element.SendEvent(pointer);
            }
        }

        static void Leave(VisualElement element)
        {
            using (var pointer = PointerLeaveEvent.GetPooled())
            {
                pointer.target = element;
                element.SendEvent(pointer);
            }
        }

        /// <summary>
        /// Runs the callbacks registered on a button's <c>clicked</c> event.
        /// <c>Clickable.SimulateSingleClick</c> is internal, and a pooled
        /// <c>ClickEvent</c> does not reach <c>clicked</c> because the manipulator
        /// raises it rather than the dispatch.
        /// </summary>
        static void InvokeHandlers(Button button)
        {
            var field = typeof(Clickable).GetField(
                "clicked",
                System.Reflection.BindingFlags.NonPublic |
                System.Reflection.BindingFlags.Instance |
                System.Reflection.BindingFlags.Public);

            Assert.That(field, Is.Not.Null, "Clickable no longer exposes its handler delegate. Update this test.");

            var handlers = field.GetValue(button.clickable) as System.Delegate;
            Assert.That(handlers, Is.Not.Null,
                $"'{button.name}' has no click handler. The controller did not register it.");

            handlers.DynamicInvoke();
        }
    }
}
