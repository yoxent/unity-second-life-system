using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SecondLife.Characters.PlayModeTests
{
    /// <summary>
    /// Guards the presenter lifecycle that EditMode tests cannot reach.
    ///
    /// <see cref="UIDocument"/> clears its visual tree on disable and rebuilds it
    /// on enable. A presenter that queries elements only in <c>Start</c> keeps
    /// references into the discarded tree, so a re-enabled screen renders empty
    /// labels and ignores clicks. That defect has been introduced twice: once when
    /// the screen was first written, and again when it was split into binders.
    ///
    /// An EditMode test cannot see this, because it never builds a visual tree.
    /// These tests load the real scene and assert the rendered text.
    /// </summary>
    public sealed class StatusScreenLifecycleTests
    {
        const string SceneName = "SampleScene";
        const string StatusObject = "Character Status";

        // Generous enough for a domain reload on a slow machine, short enough that
        // a genuine hang fails instead of stalling the run.
        const int StartupFrameBudget = 600;
        const int RestoreFrameBudget = 120;

        [UnityTest]
        public IEnumerator StatusScreen_RendersNameAndDerivedValues()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            Assert.That(screen, Is.Not.Null, $"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            var document = screen.GetComponent<UIDocument>();
            Assert.That(document, Is.Not.Null, $"'{StatusObject}' must carry a UIDocument.");

            var name = AssertLabel(document, "character-name");
            var healthValue = AssertLabel(document, "resource-value");

            Assert.That(name.text, Is.Not.Empty,
                "The status screen rendered an empty name. The presenter did not bind to the live tree.");
            Assert.That(healthValue.text, Is.Not.Empty,
                "The status screen rendered an empty resource readout. The presenter did not bind to the live tree.");
            Assert.That(healthValue.text, Does.Contain("/"),
                "The resource readout must show current and maximum, not a bare number.");
        }

        [UnityTest]
        public IEnumerator StatusScreen_SurvivesDisableAndReenable()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            Assert.That(screen, Is.Not.Null, $"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            var document = screen.GetComponent<UIDocument>();
            var before = AssertLabel(document, "character-name").text;
            var beforeReadout = FirstResourceReadout(document);

            Assert.That(before, Is.Not.Empty, "Precondition: the screen rendered a name before the cycle.");

            // The cycle that used to leave the screen permanently blank.
            document.gameObject.SetActive(false);
            yield return null;

            document.gameObject.SetActive(true);

            // The rebuilt tree is ready on the frame after OnEnable; the presenter
            // rebinds from OnEnable too, so one frame covers both.
            yield return null;

            var after = AssertLabel(document, "character-name").text;
            var afterReadout = FirstResourceReadout(document);

            Assert.That(after, Is.EqualTo(before),
                "The name was lost across a disable/enable cycle. The presenter is holding references into a discarded tree.");
            Assert.That(after, Is.Not.Empty,
                "The name was empty after a disable/enable cycle. Bind in Start and rebind in OnEnable.");
            Assert.That(afterReadout, Is.EqualTo(beforeReadout),
                "The resource readout was lost across a disable/enable cycle.");
        }

        [UnityTest]
        public IEnumerator StatusScreen_TabsStillRespondAfterReenable()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            var document = screen.GetComponent<UIDocument>();

            // Preconditions are read before the cycle, but only to prove the screen
            // was in a sane state to begin with.
            var attributesPage = AssertElement(document, "status-page-attributes");
            var skillsPage = AssertElement(document, "status-page-skills");

            Assert.That(attributesPage.ClassListContains("status-page--hidden"), Is.False,
                "Precondition: the attributes page is visible on open.");
            Assert.That(skillsPage.ClassListContains("status-page--hidden"), Is.True,
                "Precondition: the skills page is hidden on open.");

            document.gameObject.SetActive(false);
            yield return null;
            document.gameObject.SetActive(true);
            yield return null;

            // Re-query: UIDocument rebuilds the tree, so the element references held
            // from before the cycle point at discarded instances. Asserting on those
            // would report a controller bug that is really a stale test reference.
            var skillsTab = AssertButton(document, "status-tab-skills");
            InvokeHandlers(skillsTab);

            var skillsPageAfter = AssertElement(document, "status-page-skills");
            var attributesPageAfter = AssertElement(document, "status-page-attributes");

            Assert.That(skillsPageAfter.ClassListContains("status-page--hidden"), Is.False,
                "The skills page did not open after a re-enable. The tab handler was not re-registered.");
            Assert.That(attributesPageAfter.ClassListContains("status-page--hidden"), Is.True,
                "The attributes page stayed visible after switching to skills on a re-enabled screen.");
        }

        [UnityTest]
        public IEnumerator StatusScreen_ResourceBarsShowCurrentAndMaxWithAProportionalFill()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            Assert.That(screen, Is.Not.Null, $"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            var document = screen.GetComponent<UIDocument>();
            var root = document.rootVisualElement;

            var readouts = root.Query<Label>(name: "resource-value").ToList();
            Assert.That(readouts.Count, Is.GreaterThanOrEqualTo(2),
                "Expected at least an HP bar and a mana bar, each with a current / max readout.");

            foreach (var readout in readouts)
            {
                Assert.That(readout.text, Does.Contain("/"),
                    $"Resource readout '{readout.text}' must show current and maximum.");
            }

            // Layout settles the frame after a style change, so measure after a yield.
            yield return null;

            var bars = root.Query<VisualElement>(name: "resource-bar").ToList();
            var fills = root.Query<VisualElement>(name: "resource-bar-fill").ToList();
            Assert.That(fills.Count, Is.EqualTo(readouts.Count),
                "Every resource bar needs a fill element.");

            for (var i = 0; i < fills.Count; i++)
            {
                Assert.That(bars[i].resolvedStyle.height, Is.GreaterThan(0f),
                    "A resource bar rendered with no height, so the fill cannot be seen.");
                Assert.That(fills[i].resolvedStyle.width, Is.GreaterThan(0f),
                    "A non-empty pool must render a visible fill.");

                // The fill must be a proportion of its track, not an arbitrary width.
                var fraction = fills[i].resolvedStyle.width / bars[i].resolvedStyle.width;
                var expected = ParseFraction(readouts[i].text);
                Assert.That(fraction, Is.EqualTo(expected).Within(0.02f),
                    $"Bar {i} shows '{readouts[i].text}' but its fill is {fraction:P0} of the track. " +
                    "The bar does not match the readout beside it.");
            }
        }

        /// <summary>
        /// Reads "current / max" and returns the fraction. Guards the bar against a
        /// readout that changes while its fill does not.
        /// </summary>
        static float ParseFraction(string readout)
        {
            var parts = readout.Split('/');
            Assert.That(parts.Length, Is.EqualTo(2), $"Resource readout '{readout}' must be 'current / max'.");

            var current = float.Parse(parts[0].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            var max = float.Parse(parts[1].Trim(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(max, Is.GreaterThan(0f), $"Resource readout '{readout}' must have a positive maximum.");
            return current / max;
        }

        [UnityTest]
        public IEnumerator StatusScreen_PanelKeepsItsHeightAcrossTabs()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            Assert.That(screen, Is.Not.Null, $"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            var document = screen.GetComponent<UIDocument>();
            var frame = AssertElement(document, "status-frame");

            // Layout settles on the frame after a style change, so each reading is
            // taken once after a yield rather than in the same frame as the click.
            yield return null;
            var attributesHeight = frame.resolvedStyle.height;
            Assert.That(attributesHeight, Is.GreaterThan(0f),
                "The status frame resolved to zero height. Layout has not run yet.");

            var skillsTab = AssertButton(document, "status-tab-skills");
            InvokeHandlers(skillsTab);
            yield return null;

            var skillsHeight = frame.resolvedStyle.height;

            Assert.That(skillsHeight, Is.EqualTo(attributesHeight).Within(0.5f),
                "The panel resized when switching tabs. A short tab must not shrink the frame, " +
                "or the tab strip moves under the pointer between clicks.");

            var attributesTab = AssertButton(document, "status-tab-attributes");
            InvokeHandlers(attributesTab);
            yield return null;

            Assert.That(frame.resolvedStyle.height, Is.EqualTo(attributesHeight).Within(0.5f),
                "The panel did not return to its original height when switching back.");
        }

        [UnityTest]
        public IEnumerator ScreenRouter_ShowsOneScreenAndHidesTheOther()
        {
            yield return LoadScene();

            var router = Object.FindFirstObjectByType<SecondLife.UI.ScreenRouter>();
            Assert.That(router, Is.Not.Null, $"'UI Router' must carry a ScreenRouter in {SceneName}.");

            var status = FindStatusScreen().GetComponent<UIDocument>();
            var narrative = FindNarrativeDocument();
            Assert.That(narrative, Is.Not.Null, "'Narrative Validation' must carry a UIDocument.");

            // The router starts on the status screen.
            Assert.That(DisplayOf(status), Is.EqualTo(DisplayStyle.Flex),
                "The router did not show the initial status screen.");
            Assert.That(DisplayOf(narrative), Is.EqualTo(DisplayStyle.None),
                "The router did not hide the non-initial screen.");

            // Hiding with display: none must keep the tree intact, which is the
            // whole reason this approach replaced deactivating the GameObject.
            router.Show("narrative");
            yield return null;

            Assert.That(DisplayOf(status), Is.EqualTo(DisplayStyle.None), "Show(\"narrative\") did not hide status.");
            Assert.That(DisplayOf(narrative), Is.EqualTo(DisplayStyle.Flex), "Show(\"narrative\") did not show narrative.");

            router.Show("status");
            yield return null;

            Assert.That(DisplayOf(status), Is.EqualTo(DisplayStyle.Flex), "Show(\"status\") did not restore status.");
            Assert.That(AssertLabel(status, "character-name").text, Is.Not.Empty,
                "The status screen lost its contents across a router swap.");
        }

        // ---- Helpers ---------------------------------------------------------

        static IEnumerator LoadScene()
        {
            if (SceneManager.GetActiveScene().name != SceneName)
            {
                var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
                if (load != null)
                    yield return load;

                // One extra frame so every OnEnable and Start on the loaded objects
                // has run before the test queries the tree.
                yield return null;
            }

            // A presentation component may still be waiting on its document. Frame
            // budget rather than a fixed delay, so a fast machine is not slowed and
            // a slow one is not failed early.
            for (var frame = 0; frame < StartupFrameBudget; frame++)
            {
                var screen = FindStatusScreen();
                if (screen != null)
                {
                    var document = screen.GetComponent<UIDocument>();
                    if (document != null)
                    {
                        var root = document.rootVisualElement;
                        if (root != null && root.childCount > 0)
                            break;
                    }
                }

                yield return null;
            }
        }

        static CharacterStatusScreen FindStatusScreen()
        {
            foreach (var candidate in Object.FindObjectsByType<CharacterStatusScreen>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == StatusObject)
                    return candidate;
            }

            return null;
        }

        static UIDocument FindNarrativeDocument()
        {
            foreach (var candidate in Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
            {
                if (candidate.gameObject.name == "Narrative Validation")
                    return candidate;
            }

            return null;
        }

        static DisplayStyle DisplayOf(UIDocument document)
        {
            Assert.That(document, Is.Not.Null);
            return document.rootVisualElement.resolvedStyle.display;
        }

        /// <summary>
        /// The first "current / max" readout on screen, which the resource bars
        /// render. Used to prove the pools survive a lifecycle cycle without
        /// pinning the test to one bar's position in the tree.
        /// </summary>
        static string FirstResourceReadout(UIDocument document)
        {
            var labels = document.rootVisualElement.Query<Label>(name: "resource-value").ToList();
            Assert.That(labels.Count, Is.GreaterThan(0),
                "No resource bars rendered. The status screen must show current and maximum for HP and mana.");
            return labels[0].text;
        }

        static Label AssertLabel(UIDocument document, string elementName)
        {
            var element = AssertElement(document, elementName);
            var label = element as Label;
            Assert.That(label, Is.Not.Null, $"'{elementName}' must be a Label in the authored UXML.");
            return label;
        }

        static Button AssertButton(UIDocument document, string elementName)
        {
            var element = AssertElement(document, elementName);
            var button = element as Button;
            Assert.That(button, Is.Not.Null, $"'{elementName}' must be a Button in the authored UXML.");
            return button;
        }

        static VisualElement AssertElement(UIDocument document, string elementName)
        {
            Assert.That(document, Is.Not.Null, "UIDocument is missing.");
            Assert.That(document.rootVisualElement, Is.Not.Null, "The document has no visual tree.");

            var element = document.rootVisualElement.Q<VisualElement>(elementName);
            Assert.That(element, Is.Not.Null,
                $"Could not find '{elementName}' in the live tree. The presenter and the UXML disagree, " +
                "or the presenter never bound.");
            return element;
        }

        /// <summary>
        /// Runs the callbacks registered on a button's <c>clicked</c> event.
        ///
        /// <c>Clickable.SimulateSingleClick</c> is internal, and sending a pooled
        /// <c>ClickEvent</c> does not reach <c>clicked</c> because that event is
        /// raised by the manipulator, not by the dispatch. Reading the delegate is
        /// the only reliable way to prove a handler exists and works.
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
                $"'{button.name}' has no click handler. The presenter did not register it, " +
                "which is what a Start-only bind produces after a re-enable.");

            handlers.DynamicInvoke();
        }
    }
}
