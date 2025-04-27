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

        protected override async Task OnInitializedAsync()
        {
            if (((CustomAuthStateProvider)AuthStateProvider).CurrentUser == null || !((CustomAuthStateProvider)AuthStateProvider).CurrentUser.Permissions.Contains(Permissions.LeaveFeedback))
            {
                // Redirect to home page if user does not have permission
                Snackbar.Add("You do not have permission to leave feedback", Severity.Warning);
                NavigationManager.NavigateTo("/");
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
                    await ApiService.PostAsync<FeedbackViewModel>(ApiEndpoints.Feedback, feedback);
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