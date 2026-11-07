using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace SecondLife.UI.EditModeTests
{
    /// <summary>
    /// Guards the stylesheets against properties USS does not support.
    ///
    /// This exists because three sheets carried <c>line-height</c> for months. Unity
    /// does not implement it, so the importer logged a warning and dropped it: the
    /// text was never styled the way the author reading the file would assume. A
    /// warning is easy to miss in a console full of unrelated noise, and nothing
    /// fails, so the defect is invisible until someone measures the pixels.
    ///
    /// <para><b>This is a partial list, not a validator.</b> It covers the properties
    /// this project has actually reached for, plus the CSS ones a developer trained on
    /// the web tends to expect. USS implements roughly a hundred properties; anything
    /// outside this list and outside USS passes both ways. Passing this suite means
    /// "no known-bad property names", not "the stylesheet is correct".</para>
    ///
    /// <para>It also cannot see version gating. A property Unity lists but drops on this
    /// version, such as effects above 6000.3, would pass here and still do nothing. Only
    /// Unity's own importer knows that, which is why reading the console after a
    /// stylesheet edit is required by <c>unity-ui-toolkit.mdc</c> section 8 rather than
    /// delegating it to a test.</para>
    ///
    /// <para><b>It cannot catch a misspelling.</b> Verified 2026-11-07 by planting
    /// <c>padding-botton</c> alongside five unsupported properties: the five were
    /// reported and the typo was not, because a misspelling is not on this list. Closing
    /// that gap needs the inverse check, a whitelist of the properties USS does
    /// implement. That was considered and declined as not worth the hour; the console
    /// check in the rule covers the outcome, since Unity warns on the typo too.</para>
    /// </summary>
    public sealed class StylesheetPropertyTests
    {
        /// <summary>
        /// CSS properties that do not exist in USS. Unity accepts the file but warns
        /// and ignores the declaration, so the rule silently does nothing.
        ///
        /// Adding to this list is the intended maintenance: when a style does nothing,
        /// name the property here so it cannot happen twice. Taken as a snapshot of
        /// Unity 6000.3 behaviour rather than from the property reference, which is
        /// exactly why it is incomplete.
        /// </summary>
        static readonly string[] Unsupported = new[]
        {
            // Already caught this project before it caught them.
            "line-height",
            "font-weight",

            // The CSS spelling of something USS does have under another name.
            // These are the highest-value entries: the intent is right, the name is
            // wrong, and the result is a rule that does nothing.
            "text-align",     // -unity-text-align
            "font-family",    // -unity-font-definition
            "background",     // background-color / background-image, no shorthand

            // No USS equivalent at all.
            "text-transform",
            "text-decoration",
            "vertical-align",
            "border-style",
            "box-shadow",
            "outline",
            "z-index",
            "float",
            "inset",
            "gap",
            "row-gap",
            "column-gap",
            "justify-self",
            "justify-items",
            "place-items",
            "grid-template-columns",
            "grid-template-rows",
            "overflow-x",
            "overflow-y",
            "word-wrap"
        };

        static IEnumerable<string> AllStylesheets()
        {
            var root = Path.Combine(Application.dataPath, "UI");
            if (!Directory.Exists(root))
                yield break;

            foreach (var path in Directory.GetFiles(root, "*.uss", SearchOption.AllDirectories))
                yield return path;

            foreach (var path in Directory.GetFiles(root, "*.tss", SearchOption.AllDirectories))
                yield return path;
        }

        [Test]
        public void Stylesheets_DoNotUsePropertiesUssDoesNotSupport()
        {
            var offenders = new List<string>();

            foreach (var path in AllStylesheets())
            {
                var text = File.ReadAllText(path);
                var relative = path.Replace(Application.dataPath, "Assets").Replace('\\', '/');

                foreach (var property in Unsupported)
                {
                    // Only a declaration at the start of a line counts, so a comment
                    // that mentions the property (as these notes do) is not a failure.
                    if (System.Text.RegularExpressions.Regex.IsMatch(
                            text,
                            @"^\s*" + System.Text.RegularExpressions.Regex.Escape(property) + @"\s*:",
                            System.Text.RegularExpressions.RegexOptions.Multiline))
                    {
                        offenders.Add($"{relative}: {property}");
                    }
                }
            }

            Assert.That(offenders, Is.Empty,
                "These declarations use properties USS does not implement, so Unity ignores them:\n  " +
                string.Join("\n  ", offenders) +
                "\nReplace them with a supported property, or remove them.");
        }

        [Test]
        public void Stylesheets_DoNotUseCalc()
        {
            // Verified against the importer on 2026-11-07: "Unknown function 'calc'".
            // USS has no calc(), so a sum has to be a literal. The theme is where those
            // literals live, which is why this matters: a calc() that looked correct
            // would silently resolve to nothing and the spacing would collapse.
            var offenders = new List<string>();

            foreach (var path in AllStylesheets())
            {
                var text = File.ReadAllText(path);

                // Declaration only, so a comment explaining that calc() is unavailable
                // is not itself flagged. The whole reason this test exists is that the
                // notes in these files name the unsupported things.
                if (System.Text.RegularExpressions.Regex.IsMatch(
                        text,
                        @"^\s*[-\w]+\s*:[^;{}]*calc\(",
                        System.Text.RegularExpressions.RegexOptions.Multiline))
                {
                    offenders.Add(path.Replace(Application.dataPath, "Assets").Replace('\\', '/'));
                }
            }

            Assert.That(offenders, Is.Empty,
                "USS does not implement calc(). These sheets use it and the declaration is dropped:\n  " +
                string.Join("\n  ", offenders));
        }

        [Test]
        public void EveryStylesheetDeclaresItsVariablesFromTheThemeOrLocally()
        {
            // A var() with no declaration anywhere is invisible: the property falls back
            // to its default and the element renders unstyled rather than erroring.
            // This catches the common case, a token used in a screen sheet that was only
            // ever declared in a different screen's sheet.
            var theme = Path.Combine(Application.dataPath, "UI", "ValidationTheme.tss");
            Assert.That(File.Exists(theme), Is.True, "ValidationTheme.tss must exist; it owns the shared tokens.");

            var defined = CollectDeclarations(File.ReadAllText(theme));
            foreach (var path in AllStylesheets())
                foreach (var name in CollectDeclarations(File.ReadAllText(path)))
                    defined.Add(name);

            var missing = new List<string>();
            foreach (var path in AllStylesheets())
            {
                var text = File.ReadAllText(path);
                foreach (System.Text.RegularExpressions.Match match in
                         System.Text.RegularExpressions.Regex.Matches(text, @"var\(\s*(--[\w-]+)\s*\)"))
                {
                    var name = match.Groups[1].Value;
                    if (!defined.Contains(name))
                    {
                        missing.Add(
                            path.Replace(Application.dataPath, "Assets").Replace('\\', '/') + ": " + name);
                    }
                }
            }

            Assert.That(missing, Is.Empty,
                "These variables are used but never declared, so the property silently falls back:\n  " +
                string.Join("\n  ", missing));
        }

        static HashSet<string> CollectDeclarations(string text)
        {
            var names = new HashSet<string>();
            foreach (System.Text.RegularExpressions.Match match in
                     System.Text.RegularExpressions.Regex.Matches(text, @"^\s*(--[\w-]+)\s*:", 
                         System.Text.RegularExpressions.RegexOptions.Multiline))
            {
                names.Add(match.Groups[1].Value);
            }

            return names;
        }
    }
}
