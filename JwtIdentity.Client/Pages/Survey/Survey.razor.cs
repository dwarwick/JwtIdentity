using JwtIdentity.Client.Pages.Common;
using System.Net.Http.Json;
using System.Security.Claims;

namespace JwtIdentity.Client.Pages.Survey
{
    public class SurveyModel : BlazorBase, IAsyncDisposable
    {
        [Parameter]
        public string SurveyId { get; set; }

        protected SurveyViewModel Survey { get; set; }

        protected List<AnswerViewModel> Answers { get; set; } = new List<AnswerViewModel>();

        protected int SelectedOptionId { get; set; }

        protected string Url => $"{NavigationManager.BaseUri}survey/{Survey?.Guid ?? ""}";

        protected bool isCaptchaVerified { get; set; } = false;

        protected bool Preview { get; set; }

        private readonly DialogOptions _topCenter = new() { Position = DialogPosition.TopCenter, CloseButton = false, CloseOnEscapeKey = false };

        private DotNetObjectReference<SurveyModel> objRef;

        private bool disposed = false;

        protected bool Loading { get; set; } = true;

        protected override async Task OnInitializedAsync()
        {
            var uri = Navigation.ToAbsoluteUri(Navigation.Uri);
            var queryParams = QueryHelpers.ParseQuery(uri.Query);

            if (queryParams.TryGetValue("Preview", out var preview))
            {
                Preview = bool.Parse(preview);
            }

            await HandleLoggingInUser();

            // get the survey based on the SurveyId
            await LoadData();

            Loading = false;
        }

        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            if (firstRender)
            {
                await JSRuntime.InvokeVoidAsync("registerCaptchaCallback", objRef);
                // Call JavaScript to manually render the widget in the container with your site key.
                await JSRuntime.InvokeVoidAsync("renderReCaptcha", "captcha-container", Configuration["ReCaptcha:SiteKey"]);
            }
        }

        private async Task HandleLoggingInUser()
        {
            objRef = DotNetObjectReference.Create(this);

            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            ClaimsPrincipal user = authState.User;

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
                    if (!loginResponse.Success)
                    {
                        _ = Snackbar.Add("Problem logging you in anonymously. You will not be able to complete the survey. You may be able to take the survey if you create an account and login.", Severity.Error);

                        Navigation.NavigateTo("/");
                    }
                }
            }
        }

        private async Task LoadData()
        {
            // get the survey based on the SurveyId
            Survey = await ApiService.GetAsync<SurveyViewModel>($"{ApiEndpoints.Answer}/getanswersforsurveyforloggedinuser/{SurveyId}?Preview={Preview}");

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
                                QuestionId = question.Id,
                                Value = null
                            };

                            question.Answers.Add(answer);
                        }
                    }
                    else
                    { // this user or ip address has answered this before

                    }
                }
            }
            else
            {
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

        [JSInvokable("ReceiveCaptchaToken")]
        // This method is called by JavaScript once the captcha is solved.
        // [JSInvokable("ReceiveCaptchaToken")]
        public async Task ReceiveCaptchaToken(string token)
        {
            // Call the server-side API endpoint to verify the token using the client-side HttpClient.
            var response = await Client.PostAsJsonAsync("api/recaptcha/validate", new { Token = token });
            var result = await response.Content.ReadFromJsonAsync<RecaptchaValidationResult>();
            if (result != null && result.Success)
            {
                isCaptchaVerified = true;
                StateHasChanged();
            }
            else
            {
                // Optionally, alert the user or reset the captcha
                _ = Snackbar.Add("Captcha verification failed", Severity.Error);
            }
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeAsync(true);
            GC.SuppressFinalize(this);
        }

        protected virtual async ValueTask DisposeAsync(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Dispose managed resources
                    objRef?.Dispose();

                    var authState = await AuthStateProvider.GetAuthenticationStateAsync();
                    ClaimsPrincipal user = authState.User;

                    // get the users username and email
                    if (user.Identity.IsAuthenticated)
                    {
                        var username = user.FindFirst(ClaimTypes.Name)?.Value;

                        if (username == "anonymous")
                        {
                            // log the user out
                            await ((CustomAuthStateProvider)AuthStateProvider).LoggedOut();

                        }
                    }
                }

                // Dispose unmanaged resources

                disposed = true;
            }
        }

        ~SurveyModel()
        {
            DisposeAsync(false).AsTask().GetAwaiter().GetResult();
        }

        protected async Task SubmitSurvey()
        {
            if (AllQuestionsAnswered())
            {
                Survey.Complete = true;

                SurveyViewModel submittedSurvey = await ApiService.UpdateAsync(ApiEndpoints.Survey, Survey);

                if (submittedSurvey?.Complete ?? false)
                {
                    _ = Snackbar.Add("Survey submitted", Severity.Success);

                    Navigation.NavigateTo("/");
                }
                else
                {
                    _ = Snackbar.Add("There was a problem submitting the survey", Severity.Error);
                }
            }
        }

        private bool AllQuestionsAnswered()
        {
            // check if all questions have been answered
            foreach (var question in Survey.Questions)
            {
                switch (question.QuestionType)
                {
                    case QuestionType.Text:
                        if (string.IsNullOrEmpty(((TextAnswerViewModel)question.Answers[0]).Text))
                        {
                            return false;
                        }
                        break;
                    case QuestionType.TrueFalse:
                        if (((TrueFalseAnswerViewModel)question.Answers[0]).Value == null)
                        {
                            return false;
                        }
                        break;
                    case QuestionType.MultipleChoice:
                        if (((MultipleChoiceAnswerViewModel)question.Answers[0]).SelectedOptionId == 0)
                        {
                            return false;
                        }
                        break;
                    default:
                        break;
                }
            }
            return true;
        }
    }
}
