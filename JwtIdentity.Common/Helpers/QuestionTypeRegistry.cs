using System.Collections.Concurrent;
using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Common.Helpers
{
    public static class QuestionTypeRegistry
    {
        private static readonly ConcurrentDictionary<QuestionType, QuestionTypeDefinition> _definitions = new();
        private static readonly ConcurrentDictionary<AnswerType, QuestionTypeDefinition> _answerLookup = new();
        private static readonly IReadOnlyList<QuestionTypeDefinition> _orderedDefinitions;

        static QuestionTypeRegistry()
        {
            Register(new QuestionTypeDefinition<TextQuestionViewModel, TextAnswerViewModel>(
                QuestionType.Text,
                AnswerType.Text,
                "Text"));

            Register(new QuestionTypeDefinition<TrueFalseQuestionViewModel, TrueFalseAnswerViewModel>(
                QuestionType.TrueFalse,
                AnswerType.TrueFalse,
                "True / False"));

            Register(new QuestionTypeDefinition<Rating1To10QuestionViewModel, Rating1To10AnswerViewModel>(
                QuestionType.Rating1To10,
                AnswerType.Rating1To10,
                "Rating 1 to 10"));

            Register(new QuestionTypeDefinition<MultipleChoiceQuestionViewModel, MultipleChoiceAnswerViewModel>(
                QuestionType.MultipleChoice,
                AnswerType.MultipleChoice,
                "Multiple Choice",
                requiresOptions: true,
                allowsMultipleSelections: false,
                (question, answer) =>
                {
                    answer.Options = question.Options ?? new List<ChoiceOptionViewModel>();
                }));

            Register(new QuestionTypeDefinition<SelectAllThatApplyQuestionViewModel, SelectAllThatApplyAnswerViewModel>(
                QuestionType.SelectAllThatApply,
                AnswerType.SelectAllThatApply,
                "Select All That Apply",
                requiresOptions: true,
                allowsMultipleSelections: true,
                (question, answer) =>
                {
                    answer.Options = question.Options ?? new List<ChoiceOptionViewModel>();

                    answer.SelectedOptions ??= new List<bool>();

                    while (answer.SelectedOptions.Count < answer.Options.Count)
                    {
                        answer.SelectedOptions.Add(false);
                    }

                    if (!string.IsNullOrWhiteSpace(answer.SelectedOptionIds))
                    {
                        var selected = answer.SelectedOptionIds
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .Select(id => int.Parse(id))
                            .ToHashSet();

                        for (int i = 0; i < answer.Options.Count; i++)
                        {
                            answer.SelectedOptions[i] = selected.Contains(answer.Options[i].Id);
                        }
                    }
                }));

            _orderedDefinitions = _definitions
                .OrderBy(kvp => kvp.Key)
                .Select(kvp => kvp.Value)
                .ToArray();
        }

        public static IReadOnlyList<QuestionTypeDefinition> Definitions => _orderedDefinitions;

        public static QuestionTypeDefinition GetDefinition(QuestionType questionType)
        {
            if (_definitions.TryGetValue(questionType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Question type '{questionType}' is not registered.");
        }

        public static QuestionTypeDefinition GetDefinitionForAnswer(AnswerType answerType)
        {
            if (_answerLookup.TryGetValue(answerType, out var definition))
            {
                return definition;
            }

            throw new KeyNotFoundException($"Answer type '{answerType}' is not registered.");
        }

        public static void AttachDomainTypes<TQuestion, TAnswer>(QuestionType questionType)
            where TQuestion : class
            where TAnswer : class
        {
            AttachDomainTypes(questionType, typeof(TQuestion), typeof(TAnswer));
        }

        public static void AttachDomainTypes(QuestionType questionType, Type questionEntityType, Type answerEntityType)
        {
            var definition = GetDefinition(questionType);
            definition.AttachDomainTypes(questionEntityType, answerEntityType);
        }

        private static void Register(QuestionTypeDefinition definition)
        {
            if (!_definitions.TryAdd(definition.QuestionType, definition))
            {
                throw new InvalidOperationException($"Question type '{definition.QuestionType}' is already registered.");
            }

            if (!_answerLookup.TryAdd(definition.AnswerType, definition))
            {
                throw new InvalidOperationException($"Answer type '{definition.AnswerType}' is already registered.");
            }
        }
    }
}
