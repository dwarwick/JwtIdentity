using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq;
using JwtIdentity.Client.Pages.Admin.Dialogs;

namespace JwtIdentity.Client.Pages.Admin
{
    public class ManageFeedbackModel : BlazorBase
    {
        [Inject] private IDialogService DialogService { get; set; }

        protected List<FeedbackViewModel> FeedbackItems { get; set; } = new();
        protected List<FeedbackViewModel> AllFeedbackItems { get; set; } = new();
        
        protected bool _loading = true;
        protected string _searchString = "";
        protected string _statusFilter = "all";
        protected string _typeFilter = "all";

        protected override async Task OnInitializedAsync()
        {
            await LoadFeedbackItems();
        }
        
        private async Task LoadFeedbackItems()
        {
            try
            {
                _loading = true;
                AllFeedbackItems = await ApiService.GetAsync<List<FeedbackViewModel>>(ApiEndpoints.Feedback);
                ApplyFilters();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading feedback: {ex.Message}", Severity.Error);
            }
            finally
            {
                _loading = false;
            }
        }

        protected void ApplyFilters()
        {
            var filteredList = AllFeedbackItems;
            
            // Apply status filter
            if (_statusFilter != "all")
            {
                bool isResolved = _statusFilter == "resolved";
                filteredList = filteredList.Where(f => f.IsResolved == isResolved).ToList();
            }
            
            // Apply type filter
            if (_typeFilter != "all" && Enum.TryParse<FeedbackType>(_typeFilter, out var feedbackType))
            {
                filteredList = filteredList.Where(f => f.Type == feedbackType).ToList();
            }
            
            // Apply search
            if (!string.IsNullOrEmpty(_searchString))
            {
                _searchString = _searchString.ToLower();
                filteredList = filteredList.Where(f => 
                    (f.Title?.ToLower().Contains(_searchString) ?? false) || 
                    (f.Description?.ToLower().Contains(_searchString) ?? false) ||
                    (f.Email?.ToLower().Contains(_searchString) ?? false)
                ).ToList();
            }
            
            FeedbackItems = filteredList;
            StateHasChanged();
        }

        protected void FilterByStatus(string status)
        {
            _statusFilter = status;
            ApplyFilters();
        }
        
        protected void FilterByType(string type)
        {
            _typeFilter = type;
            ApplyFilters();
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
                CloseButton = true,
                MaxWidth = MaxWidth.Medium,
                FullWidth = true,
                CloseOnEscapeKey = false
            };

            var dialog = await DialogService.ShowAsync<FeedbackDialog>("Feedback Details", parameters, options);
            var result = await dialog.Result;
            
            if (!result.Canceled)
            {
                var updatedFeedback = (FeedbackViewModel)result.Data;
                await UpdateFeedback(updatedFeedback);
            }
        }

        protected async Task ToggleResolvedStatus(FeedbackViewModel feedback)
        {
            feedback.IsResolved = !feedback.IsResolved;
            await UpdateFeedback(feedback);
        }

        protected async Task UpdateFeedback(FeedbackViewModel feedback)
        {
            try
            {
                // Use UpdateAsync instead of PostAsync
                await ApiService.UpdateAsync($"{ApiEndpoints.Feedback}/{feedback.Id}", feedback);
                Snackbar.Add("Feedback updated successfully", Severity.Success);
                await LoadFeedbackItems();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error updating feedback: {ex.Message}", Severity.Error);
            }
        }
    }
}