using System.IO;
using System.Linq;
using Ink;
using NUnit.Framework;
using UnityEngine;

namespace SecondLife.Narrative.Tests
{
    public sealed class NarrativeSessionTests
    {
        [Test]
        public void OfferHelp_RecordsChoiceAndRestoresInkState()
        {
            var session = new NarrativeSession(CompileFixture().ToJson());

            var opening = session.Advance();
            Assert.That(opening, Does.Contain("A classmate asks for help before the next lesson."));
            Assert.That(session.CurrentChoices.Select(choice => choice.Text), Is.EquivalentTo(new[] { "Offer help", "Keep the evening free" }));

            Choose(session, "Offer help");
            var result = session.Advance();

            Assert.That(result, Does.Contain("You agree to help."));
            Assert.That(session.State.RecordedChoice, Is.EqualTo(NarrativeChoiceState.OfferHelp));
            Assert.That(session.ReadVariable("validation_marker"), Is.EqualTo(1));

            var snapshot = session.Capture();
            var restored = new NarrativeSession(CompileFixture().ToJson());
            Assert.That(restored.TryRestore(snapshot, out var error), Is.True, error);
            Assert.That(restored.State.RecordedChoice, Is.EqualTo(NarrativeChoiceState.OfferHelp));
            Assert.That(restored.ReadVariable("validation_marker"), Is.EqualTo(1));
            Assert.That(restored.CanContinue, Is.False);
            Assert.That(restored.Advance(), Is.Empty);
            Assert.That(restored.State.RecordedChoice, Is.EqualTo(NarrativeChoiceState.OfferHelp));
        }

        [Test]
        public void UnavailableHelp_HidesOfferWithoutChangingState()
        {
            var state = new NarrativeChoiceState { HelpAvailable = false };
            var session = new NarrativeSession(CompileFixture().ToJson(), state);

            session.Advance();

            Assert.That(session.CurrentChoices.Select(choice => choice.Text), Is.EqualTo(new[] { "Keep the evening free" }));
            Assert.That(state.RecordedChoice, Is.Null);
            Assert.That(state.HelpAvailable, Is.False);
            Assert.That(state.LastError, Is.Null);
        }

        [Test]
        public void UnknownChoice_IsRejected()
        {
            const string source = @"
EXTERNAL record_choice(choice_id)
~ temp accepted = record_choice(""grant_unlimited_experience"")
{ accepted:
    The invalid choice was recorded.
}
{ not accepted:
    The invalid choice was rejected.
}
-> END
";
            var session = new NarrativeSession(Compile(source).ToJson());

            var lines = session.Advance();

            Assert.That(lines, Does.Contain("The invalid choice was rejected."));
            Assert.That(session.State.RecordedChoice, Is.Null);
            Assert.That(session.State.LastError, Does.Contain("grant_unlimited_experience"));
        }

        [Test]
        public void SecondChoice_DoesNotReplaceTheFirst()
        {
            const string source = @"
EXTERNAL record_choice(choice_id)
~ temp first = record_choice(""offer_help"")
~ temp second = record_choice(""decline_help"")
{ first && not second:
    The first choice stayed recorded.
}
-> END
";
            var session = new NarrativeSession(Compile(source).ToJson());

            var lines = session.Advance();

            Assert.That(lines, Does.Contain("The first choice stayed recorded."));
            Assert.That(session.State.RecordedChoice, Is.EqualTo(NarrativeChoiceState.OfferHelp));
        }

        [Test]
        public void UnsupportedSnapshot_DoesNotChangeTheSession()
        {
            var session = new NarrativeSession(CompileFixture().ToJson());
            session.Advance();
            Choose(session, "Offer help");
            session.Advance();

            var restored = session.TryRestore("{\"version\":0}", out var error);

            Assert.That(restored, Is.False);
            Assert.That(error, Does.Contain("Unsupported"));
            Assert.That(session.State.RecordedChoice, Is.EqualTo(NarrativeChoiceState.OfferHelp));
            Assert.That(session.ReadVariable("validation_marker"), Is.EqualTo(1));
        }

        [Test]
        public void ScreenUxml_DefinesEveryElementThePresenterQueries()
        {
            var path = Path.Combine(Application.dataPath, "UI", "Narrative", "ValidationScreen.uxml");
            var uxml = File.ReadAllText(path);

            var expected = new[]
            {
                "narrative-body",
                "narrative-status",
                "narrative-choices",
                "narrative-save",
                "narrative-load"
            };

            foreach (var elementName in expected)
            {
                Assert.That(uxml, Does.Contain($"name=\"{elementName}\""),
                    $"NarrativeValidationScreen queries '{elementName}', which the authored UXML must define.");
            }
        }

        [Test]
        public void ChoiceTemplate_DefinesTheButtonThePresenterQueries()
        {
            var path = Path.Combine(Application.dataPath, "UI", "Narrative", "ValidationChoice.uxml");
            var uxml = File.ReadAllText(path);

            Assert.That(uxml, Does.Contain("name=\"narrative-choice\""),
                "NarrativeValidationScreen instantiates the choice template and queries 'narrative-choice'.");
        }

        static void Choose(NarrativeSession session, string text)
        {
            var choice = session.CurrentChoices.Single(candidate => candidate.Text == text);
            session.Choose(choice.Index);
        }

        static Ink.Runtime.Story CompileFixture()
        {
            var path = Path.Combine(Application.dataPath, "Narrative", "ValidationConversation.ink");
            return Compile(File.ReadAllText(path));
        }

        static Ink.Runtime.Story Compile(string source)
        {
            var errors = new System.Collections.Generic.List<string>();
            var compiler = new Compiler(source, new Compiler.Options
            {
                errorHandler = (message, type) => errors.Add(type + ": " + message)
            });
            var story = compiler.Compile();
            if (story == null)
                Assert.Fail(string.Join("\n", errors));
            return story;
        }
    }
}
