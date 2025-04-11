using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq;
using JwtIdentity.Client.Helpers;
using Syncfusion.Blazor.Grids;

namespace JwtIdentity.Client.Pages.Feedback
{
    public class MyFeedbackModel : BlazorBase
    {
        [Inject] private IDialogService DialogService { get; set; }

        protected List<FeedbackViewModel> MyFeedbackItems { get; set; } = new();
        protected bool _loading = true;
        
        // Syncfusion Grid properties
        protected int FrozenColumns { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadMyFeedback();
        }
        
        private async Task LoadMyFeedback()
        {
            try
            {
                _loading = true;
                // Get feedback for the current logged in user
                MyFeedbackItems = await ApiService.GetAsync<List<FeedbackViewModel>>($"{ApiEndpoints.Feedback}/my");
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading your feedback: {ex.Message}", Severity.Error);
            }
            finally
            {
                _loading = false;
                StateHasChanged();
            }
        }

        protected Color GetColorForFeedbackType(FeedbackType type)
        {
            return type switch
            {
                FeedbackType.Problem => Color.Error,
                FeedbackType.FeatureRequest => Color.Info,
                FeedbackType.GeneralFeedback => Color.Default,
                _ => Color.Default
            };
        }

        protected async Task OpenFeedbackDialog(FeedbackViewModel feedback)
        {
            var parameters = new DialogParameters
            {
                ["Feedback"] = feedback
            };

            var options = new DialogOptions
            { 
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseButton = true,
                CloseOnEscapeKey = false,
                NoHeader = false
            };

            var dialog = await DialogService.ShowAsync<ViewFeedbackDialog>("Feedback Details", parameters, options);
            await dialog.Result;
        }
        
        protected async Task ToggleResolvedStatus(FeedbackViewModel feedback)
        {
            // Toggle the resolved status
            feedback.IsResolved = !feedback.IsResolved;
            
            try
            {
                // Update the feedback using PUT request to the correct endpoint
                var updatedFeedback = await ApiService.UpdateAsync<FeedbackViewModel>($"{ApiEndpoints.Feedback}/{feedback.Id}", feedback);
                if (updatedFeedback != null)
                {
                    // Update the local list
                    var index = MyFeedbackItems.FindIndex(f => f.Id == updatedFeedback.Id);
                    if (index >= 0)
                    {
                        MyFeedbackItems[index] = updatedFeedback;
                    }
                    
                    Snackbar.Add($"Feedback marked as {(feedback.IsResolved ? "resolved" : "unresolved")}", Severity.Success);
                }
                else
                {
                    // Revert the change if update failed
                    feedback.IsResolved = !feedback.IsResolved;
                    Snackbar.Add("Failed to update feedback status", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                // Revert the change if update failed
                feedback.IsResolved = !feedback.IsResolved;
                Snackbar.Add($"Error updating feedback: {ex.Message}", Severity.Error);
            }
        }
    }
}