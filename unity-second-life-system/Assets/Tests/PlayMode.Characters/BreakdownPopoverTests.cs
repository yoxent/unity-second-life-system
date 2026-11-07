using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SecondLife.Characters.PlayModeTests
{
    /// <summary>
    /// Guards the breakdown popover's behaviour, which EditMode cannot see.
    ///
    /// Three things are only provable with a live panel: that a trigger actually
    /// opens the popover, that a latched popover survives the pointer leaving, and
    /// that the panel lands inside the screen. <c>worldBound</c> and
    /// <c>resolvedStyle</c> are meaningless before layout runs, so every
    /// measurement here waits for a layout pass.
    /// </summary>
    public sealed class BreakdownPopoverTests
    {
        const string SceneName = "SampleScene";
        const string StatusObject = "Character Status";
        const int StartupFrameBudget = 600;

        [UnityTest]
        public IEnumerator Breakdown_StartsHidden()
        {
            yield return LoadScene();

            var popover = AssertElement(FindStatusDocument(), "breakdown-popover");

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                "The breakdown popover must be hidden until a value is hovered or latched.");
        }

        [UnityTest]
        public IEnumerator Breakdown_ShowsBaseContributionAndTotalForMaxHealth()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var trigger = FindVitalTrigger(document);

            // Hovering the vitals is the call site with real derivation data:
            // Max HP is a flat base plus Vitality x the scaling coefficient.
            SendPointerEnter(trigger);
            yield return null;
            yield return null;

            var popover = AssertElement(document, "breakdown-popover");
            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "Hovering a vital did not open the breakdown popover.");

            var title = AssertLabel(document, "breakdown-title").text;
            Assert.That(title, Is.EqualTo("MAX HP"), $"The popover opened with title '{title}'.");

            var baseValue = AssertLabel(document, "breakdown-base-value").text;
            var totalValue = AssertLabel(document, "breakdown-total-value").text;
            Assert.That(baseValue, Is.Not.Empty, "The popover rendered no base value.");
            Assert.That(totalValue, Is.Not.Empty, "The popover rendered no final value.");

            // The final must equal the maximum the resource bar beside it shows.
            // Otherwise the panel explains a different number than the one clicked.
            var readout = AssertLabel(document, "resource-value").text;
            var max = readout.Split('/')[1].Trim();
            Assert.That(totalValue, Is.EqualTo(max),
                $"The popover says the maximum is {totalValue} but the bar reads {readout}.");
            Assert.That(totalValue, Is.Not.EqualTo(baseValue),
                "Max HP has a Vitality contribution in this profile, so base and final must differ. " +
                "If they match, the popover is not reading the derivation.");
        }

        /// <summary>
        /// Only the two vital bars are inspectable. The attribute rows are left
        /// inert deliberately: with no modifier data they would open a panel saying
        /// base equals final, which advertises a feature with nothing behind it.
        /// This pins that scope decision so it cannot drift back silently.
        /// </summary>
        [UnityTest]
        public IEnumerator Breakdown_RegistersTheTwoVitalsAndNotTheAttributeRows()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var triggers = document.rootVisualElement
                .Query<VisualElement>(className: "breakdown-trigger")
                .ToList();

            Assert.That(triggers.Count, Is.EqualTo(4),
                "Expected two vitals panels plus the two header readouts, all explainable. " +
                "Found " + triggers.Count + " triggers.");

            foreach (var trigger in triggers)
            {
                Assert.That(trigger.ClassListContains("attribute-value"), Is.False,
                    $"'{trigger.name}' is an attribute row. Attributes are not wired until modifiers exist.");
            }
        }

        [UnityTest]
        public IEnumerator Breakdown_ClosingOnSecondInteractionHidesItAgain()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var trigger = FindVitalTrigger(document);
            var popover = AssertElement(document, "breakdown-popover");

            SendPointerEnter(trigger);
            yield return null;

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "Precondition: hovering did not open the popover.");

            SendPointerLeave(trigger);
            yield return null;

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                "An unlatched popover must close when the pointer leaves. " +
                "If this fails, hovering is being treated as a latch.");
        }

        /// <summary>
        /// The behaviour the click latch exists for: a player can open the panel
        /// and move the pointer away to read it without it vanishing.
        /// </summary>
        [UnityTest]
        public IEnumerator Breakdown_LatchedPopoverSurvivesPointerLeaving()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var trigger = FindVitalTrigger(document);
            var popover = AssertElement(document, "breakdown-popover");

            SendPointerDown(trigger);
            yield return null;

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "Clicking a vital did not latch the popover.");

            SendPointerLeave(trigger);
            yield return null;
            yield return null;

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.Flex),
                "A latched popover closed when the pointer left. That defeats the latch: " +
                "the player cannot move the pointer away to read it.");

            // A second click dismisses it.
            SendPointerDown(trigger);
            yield return null;

            Assert.That(popover.resolvedStyle.display, Is.EqualTo(DisplayStyle.None),
                "A second click must dismiss a latched popover.");
        }

        [UnityTest]
        public IEnumerator Breakdown_PlacesItselfInsideTheScreen()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var trigger = FindVitalTrigger(document);

            SendPointerDown(trigger);

            // Two frames: one for the style pass that shows it, one for the
            // deferred placement to run against real layout.
            yield return null;
            yield return null;
            yield return null;

            var popover = AssertElement(document, "breakdown-popover");
            var screenRoot = AssertElement(document, "status-root");

            Assert.That(popover.layout.width, Is.GreaterThan(0f),
                "The popover resolved to zero width, so it is not laid out.");
            Assert.That(popover.layout.height, Is.GreaterThan(0f),
                "The popover resolved to zero height, so it is not laid out.");

            // Absolute positioning is easy to get wrong, and an off-screen panel is
            // invisible without failing to open. worldBound is in panel space for
            // both elements, so comparing them is valid.
            var bounds = popover.worldBound;
            var screen = screenRoot.worldBound;

            Assert.That(bounds.xMin, Is.GreaterThanOrEqualTo(screen.xMin - 1f),
                $"The popover starts at x={bounds.xMin}, left of the screen at {screen.xMin}.");
            Assert.That(bounds.yMin, Is.GreaterThanOrEqualTo(screen.yMin - 1f),
                $"The popover starts at y={bounds.yMin}, above the screen at {screen.yMin}.");
            Assert.That(bounds.xMax, Is.LessThanOrEqualTo(screen.xMax + 1f),
                $"The popover ends at x={bounds.xMax}, past the screen right edge at {screen.xMax}.");
            Assert.That(bounds.yMax, Is.LessThanOrEqualTo(screen.yMax + 1f),
                $"The popover ends at y={bounds.yMax}, past the screen bottom at {screen.yMax}.");
        }

        /// <summary>
        /// The popover must sit outside the frame, as a direct child of the screen
        /// root and as its last child.
        ///
        /// This is the structural half of "it is not clipped": the panel is
        /// absolutely positioned and drawn above the frame, so it must not be
        /// nested inside a sub-region whose layout could cut it off. Unity exposes
        /// no resolved overflow property to assert the clipping directly, so the
        /// placement test above and this structural one are what is checkable.
        /// </summary>
        [UnityTest]
        public IEnumerator Breakdown_SitsOutsideTheFrameAsTheLastRootChild()
        {
            yield return LoadScene();

            var document = FindStatusDocument();
            var popover = AssertElement(document, "breakdown-popover");
            var screenRoot = AssertElement(document, "status-root");

            // status-root is the authored element under the document root, so the
            // popover's parent is the screen, not the UIDocument's implicit root.
            Assert.That(popover.hierarchy.parent, Is.Not.Null);
            Assert.That(popover.hierarchy.parent.name, Is.EqualTo("status-root"),
                $"The popover's parent is '{popover.hierarchy.parent.name}'. It must be a child of status-root " +
                "so no sub-region's layout can clip it.");

            var frame = AssertElement(document, "status-frame");

            // Walk up by hierarchy.parent: VisualElement has no IsChildOf.
            var insideFrame = false;
            for (var ancestor = popover.hierarchy.parent; ancestor != null; ancestor = ancestor.hierarchy.parent)
            {
                if (ancestor == frame)
                {
                    insideFrame = true;
                    break;
                }
            }

            Assert.That(insideFrame, Is.False,
                "The popover is nested inside status-frame. It must sit outside the frame.");

            var lastChild = screenRoot[screenRoot.hierarchy.childCount - 1];
            Assert.That(lastChild, Is.EqualTo(popover),
                "The popover must be the last child of status-root: USS has no z-index, so draw order " +
                "among siblings decides what is on top.");
        }

        // ---- Helpers ---------------------------------------------------------

        static IEnumerator LoadScene()
        {
            var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            if (load != null)
                yield return load;

            yield return null;

            for (var frame = 0; frame < StartupFrameBudget; frame++)
            {
                var document = FindStatusDocument(quiet: true);
                if (document != null && document.rootVisualElement != null &&
                    document.rootVisualElement.childCount > 0)
                {
                    break;
                }

                yield return null;
            }
        }

        static UIDocument FindStatusDocument(bool quiet = false)
        {
            foreach (var candidate in Object.FindObjectsByType<CharacterStatusScreen>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == StatusObject)
                    return candidate.GetComponent<UIDocument>();
            }

            if (!quiet)
                Assert.Fail($"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            return null;
        }

        /// <summary>
        /// The first registered breakdown trigger. The vitals bars are registered
        /// with the "breakdown-trigger" class by BreakdownPopover, so this finds a
        /// real trigger rather than guessing at element names.
        /// </summary>
        static VisualElement FindVitalTrigger(UIDocument document)
        {
            var triggers = document.rootVisualElement
                .Query<VisualElement>(className: "breakdown-trigger")
                .ToList();

            Assert.That(triggers.Count, Is.GreaterThan(0),
                "No breakdown triggers were registered. The screen never called RegisterBreakdowns.");

            return triggers[0];
        }

        static void SendPointerEnter(VisualElement target)
        {
            using (var evt = PointerEnterEvent.GetPooled())
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        static void SendPointerLeave(VisualElement target)
        {
            using (var evt = PointerLeaveEvent.GetPooled())
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        static void SendPointerDown(VisualElement target)
        {
            using (var evt = PointerDownEvent.GetPooled())
            {
                evt.target = target;
                target.SendEvent(evt);
            }
        }

        static VisualElement AssertElement(UIDocument document, string elementName)
        {
            Assert.That(document, Is.Not.Null, "UIDocument is missing.");
            Assert.That(document.rootVisualElement, Is.Not.Null, "The document has no visual tree.");

            var element = document.rootVisualElement.Q<VisualElement>(elementName);
            Assert.That(element, Is.Not.Null,
                $"Could not find '{elementName}' in the live tree. The presenter and the UXML disagree.");
            return element;
        }

        static Label AssertLabel(UIDocument document, string elementName)
        {
            var element = AssertElement(document, elementName);
            var label = element as Label;
            Assert.That(label, Is.Not.Null, $"'{elementName}' must be a Label in the authored UXML.");
            return label;
        }
    }
}
