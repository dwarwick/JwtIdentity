namespace JwtIdentity.Client.Helpers
{
    public static class QuestionComponentRegistry
    {
        private static readonly IReadOnlyDictionary<QuestionType, Type> RendererComponents = new Dictionary<QuestionType, Type>
        {
            { QuestionType.Text, typeof(Components.Survey.Questions.TextQuestionAnswer) },
            { QuestionType.TrueFalse, typeof(Components.Survey.Questions.TrueFalseQuestionAnswer) },
            { QuestionType.Rating1To10, typeof(Components.Survey.Questions.RatingQuestionAnswer) },
            { QuestionType.MultipleChoice, typeof(Components.Survey.Questions.MultipleChoiceQuestionAnswer) },
            { QuestionType.SelectAllThatApply, typeof(Components.Survey.Questions.SelectAllThatApplyQuestionAnswer) }
        };

        public static Type GetRendererComponent(QuestionType questionType)
        {
            if (RendererComponents.TryGetValue(questionType, out var componentType))
            {
                return componentType;
            }

            throw new KeyNotFoundException($"No renderer component registered for question type '{questionType}'.");
        }
    }
}
