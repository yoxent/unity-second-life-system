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
        public IEnumerator StatusScreen_HasNoTabStripAfterSkillsMovedOut()
        {
            yield return LoadScene();

            var document = FindStatusScreen().GetComponent<UIDocument>();
            var root = document.rootVisualElement;

            // Skills became a navigation destination, so the character sheet has no
            // tabs left. Keeping a one-tab strip would be decoration that implies a
            // choice the player does not have. This guards against the tab being
            // reintroduced as part of a later screen.
            Assert.That(root.Q<VisualElement>("status-tabs"), Is.Null,
                "The status screen still has a tab strip. Skills moved to its own screen, " +
                "so the strip has nothing left to switch between.");
            Assert.That(root.Q<Button>("status-tab-skills"), Is.Null,
                "The status screen still has a Skills tab. It duplicates the bar's destination " +
                "and gives one section two owners.");

            // The content the tab used to hide must still be present and showing.
            var body = AssertElement(document, "status-body");
            Assert.That(body, Is.Not.Null);
            Assert.That(body.resolvedStyle.display, Is.Not.EqualTo(DisplayStyle.None),
                "The attributes content is not visible now that the tab strip is gone.");
        }

        [UnityTest]
        public IEnumerator SkillsScreen_RendersAndSurvivesDisableAndReenable()
        {
            yield return LoadScene();

            var router = Object.FindFirstObjectByType<SecondLife.UI.ScreenRouter>();
            Assert.That(router, Is.Not.Null);
            Assert.That(router.HasScreen("skills"), Is.True,
                "The router does not know the skills screen, so the bar's Skills button is dead.");

            var document = FindDocument("Skills Screen");
            Assert.That(document, Is.Not.Null, "'Skills Screen' must carry a UIDocument.");

            router.Show("skills");
            yield return null;
            yield return null;

            var before = AssertLabel(document, "skills-list-empty").text;
            Assert.That(before, Is.Not.Empty, "The skills screen rendered an empty state with no text.");

            // The cycle that has broken presenters in this project twice.
            document.gameObject.SetActive(false);
            yield return null;
            document.gameObject.SetActive(true);
            yield return null;

            // Re-query, because the tree was rebuilt.
            var after = AssertLabel(document, "skills-list-empty").text;
            Assert.That(after, Is.EqualTo(before),
                "The skills screen lost its content across a disable/enable cycle. " +
                "It is holding references into a discarded tree.");
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

            // Layout settles the frame after a style change, so measure after the style
            // pass and the layout pass, not in the frame the tree was built.
            yield return null;
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
        public IEnumerator StatusScreen_FrameFillsTheViewportAndKeepsItsHeight()
        {
            yield return LoadScene();

            var screen = FindStatusScreen();
            Assert.That(screen, Is.Not.Null, $"'{StatusObject}' must carry a CharacterStatusScreen in {SceneName}.");

            var document = screen.GetComponent<UIDocument>();
            var frame = AssertElement(document, "status-frame");

            // Two frames: one for the style pass, one for layout. The old single yield
            // was enough with a tab strip in the tree and is not reliable without one.
            yield return null;
            yield return null;

            var height = frame.resolvedStyle.height;
            Assert.That(height, Is.GreaterThan(0f),
                "The status frame resolved to zero height. Layout has not run yet.");

            // Removing the tab page wrapper changed the frame's flex chain, so this
            // guards the invariant that replaced the tab-height test: the frame still
            // fills the viewport instead of collapsing to its content.
            var viewportHeight = document.rootVisualElement.resolvedStyle.height;
            Assert.That(height, Is.GreaterThan(viewportHeight * 0.5f),
                $"The status frame is only {height:F1}px tall in a {viewportHeight:F1}px viewport. " +
                "It is sizing to its content rather than filling the screen.");

            // And the footer must still sit inside the frame, above the nav inset.
            var footer = AssertElement(document, "status-footer");
            Assert.That(footer.worldBound.yMax, Is.LessThanOrEqualTo(frame.worldBound.yMax + 1f),
                "The footer is outside the frame after the tab strip was removed.");
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
            // Always reload, even when this scene is already active. Tests share the
            // scene object and the router's current screen, so a test that switches to
            // skills or narrative would otherwise leave the next test starting on the
            // wrong screen. Reloading costs a frame or two and buys real isolation.
            var load = SceneManager.LoadSceneAsync(SceneName, LoadSceneMode.Single);
            if (load != null)
                yield return load;

            // One extra frame so every OnEnable and Start on the loaded objects
            // has run before the test queries the tree.
            yield return null;

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
            return FindDocument("Narrative Validation");
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
