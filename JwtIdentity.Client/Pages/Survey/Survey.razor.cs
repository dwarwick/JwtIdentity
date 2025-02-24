using System.Text.Json;

namespace JwtIdentity.Client.Pages.Survey
{
    public class SurveyModel : BlazorBase
    {
        private static readonly JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new AnswerViewModelConverter() }
        };

        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();

        protected int SelectedOptionId { get; set; }

        protected override async Task OnInitializedAsync()
        {
            // get the survey based on the SurveyId
            await LoadData();
        }

        private async Task LoadData()
        {
            // get the survey based on the SurveyId
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Answer}/getanswersforsurvey/{SurveyId}");

            if (Survey != null && Survey.Id > 0)
            {
                foreach (var question in Survey.Questions)
                {
                    if (question.Answers.Count == 0)
                    {
                        if (question.QuestionType == QuestionType.MultipleChoice)
                        {
                            MultipleChoiceAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.MultipleChoice,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }

                        else if (question.QuestionType == QuestionType.Text)
                        {
                            TextAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.Text,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }

                        else if (question.QuestionType == QuestionType.TrueFalse)
                        {
                            TrueFalseAnswerViewModel answer = new()
                            {
                                AnswerType = AnswerType.TrueFalse,
                                QuestionId = question.Id
                            };

                            question.Answers.Add(answer);
                        }
                    }
                }
            }
        }

        protected async Task HandleAnswerQuestion(AnswerViewModel answer, object selectedAnswer)
        {
            if (answer is MultipleChoiceAnswerViewModel multipleChoiceAnswer)
            {
                multipleChoiceAnswer.SelectedOptionId = (int)selectedAnswer;
            }
            else if (answer is TextAnswerViewModel textAnswerViewModel)
            {
                textAnswerViewModel.Text = selectedAnswer.ToString();
            }
            else if (answer is TrueFalseAnswerViewModel trueFalseAnswerViewModel)
            {
                trueFalseAnswerViewModel.Value = (bool)selectedAnswer;
            }

            var response = await ApiService.PostAsync(ApiEndpoints.Answer, answer);

            if (response != null)
            {
                // copy answer to Survey.Questions.Answers
                foreach (var question in Survey.Questions)
                {
                    if (question.Id == answer.QuestionId)
                    {
                        for (int i = 0; i < question.Answers.Count; i++)
                        {
                            if (question.Answers[i].Id == answer.Id)
                            {
                                question.Answers[i] = response;
                            }
                        }
                    }
                }

            }
        }
    }
}
