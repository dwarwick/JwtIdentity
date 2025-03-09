using JwtIdentity.Client.Pages.Common;

namespace JwtIdentity.Client.Pages.Survey
{
    public class SurveyModel : BlazorBase
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();

        protected int SelectedOptionId { get; set; }

        protected string Url => $"{NavigationManager.BaseUri}survey/{Survey?.Guid ?? ""}";

        private readonly DialogOptions _topCenter = new() { Position = DialogPosition.TopCenter, CloseButton = false, CloseOnEscapeKey = false };

        protected override async Task OnInitializedAsync()
        {
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
            {
                DialogParameters keyValuePairs = new()
                {
                    { "Message", "You can take the survey annonymously, or you can create an account. If you create an account first, you will be able to access your survey results in the future. Would you like to create an account first?" },
                    { "OkText" , "Yes" },
                    { "CancelText" , "No" }
                };

                IDialogReference response = await MudDialog.ShowAsync<ConfirmDialog>("Create Account?", keyValuePairs, _topCenter);
                var result = await response.Result;
                if (!result.Canceled)
                {
                    // User pressed OK
                    NavigationManager.NavigateTo($"/register/{SurveyId}");
                }
                else
                {
                    _ = Snackbar.Add("You are now being logged in as an anonymous user", Severity.Success);
                    Response<ApplicationUserViewModel> loginResponse = await AuthService.Login(new ApplicationUserViewModel() { UserName = "logmein", Password = "123" });
                    if (loginResponse.Success)
                    {
                        _ = Snackbar.Add("You may now complete the survey", Severity.Success);
                    }
                    else
                    {
                        _ = Snackbar.Add("Problem logging you in anonymously. You will not be able to complete the survey. You may be able to take the survey if you create an account and login.", Severity.Error);

                        Navigation.NavigateTo("/");
                    }
                }
            }

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
            else
            {
                _ = Snackbar.Add("Survey not found", Severity.Error);
                NavigationManager.NavigateTo("/");
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
