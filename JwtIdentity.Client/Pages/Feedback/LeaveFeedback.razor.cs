using JwtIdentity.Common.ViewModels;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using Microsoft.AspNetCore.Components.Authorization;

namespace JwtIdentity.Client.Pages.Feedback
{
    public partial class LeaveFeedbackModel : BlazorBase
    {
        protected FeedbackViewModel feedback { get; set; } = new() { Type = FeedbackType.GeneralFeedback };
        protected bool success { get; set; }
        protected MudForm form { get; set; }
        private const string ApiEndpoint = "api/feedback";

        protected override async Task OnInitializedAsync()
        {
            // Check if user is authenticated
            var authState = await AuthStateProvider.GetAuthenticationStateAsync();
            var user = authState.User;

            if (!user.Identity.IsAuthenticated)
            {
                // Redirect to login page if not authenticated
                Snackbar.Add("You must be logged in to leave feedback", Severity.Warning);
                NavigationManager.NavigateTo("/login?returnUrl=/feedback");
                return;
            }

            await base.OnInitializedAsync();
        }

        protected void Cancel()
        {
            NavigationManager.NavigateTo("/");
        }

        protected async Task Submit()
        {
            await form.Validate();
            
            if (success)
            {
                try
                {
                    await ApiService.PostAsync<FeedbackViewModel>(ApiEndpoint, feedback);
                    Snackbar.Add("Thank you for your feedback!", Severity.Success);
                    NavigationManager.NavigateTo("/");
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error submitting feedback: {ex.Message}", Severity.Error);
                }
            }
        }
    }
}