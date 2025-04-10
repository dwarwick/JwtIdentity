using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.Linq;

namespace JwtIdentity.Client.Pages.Feedback
{
    public class MyFeedbackModel : BlazorBase
    {
        [Inject] private IDialogService DialogService { get; set; }

        protected List<FeedbackViewModel> MyFeedbackItems { get; set; } = new();
        protected bool _loading = true;
        protected bool _dialogVisible = false;
        protected FeedbackViewModel _selectedFeedback = new FeedbackViewModel();
        
        protected DialogOptions dialogOptions = new() 
        { 
            MaxWidth = MaxWidth.Medium,
            FullWidth = true,
            CloseButton = true,
            CloseOnEscapeKey = false,
            NoHeader = false
        };

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

        protected void OpenFeedbackDialog(FeedbackViewModel feedback)
        {
            _selectedFeedback = feedback;
            _dialogVisible = true;
        }

        protected void CloseDialog()
        {
            _dialogVisible = false;
        }
    }
}