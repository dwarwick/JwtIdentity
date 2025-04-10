using JwtIdentity.Client.Services;
using JwtIdentity.Common.ViewModels;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace JwtIdentity.Client.Pages.Admin.Settings
{
    public class SettingsManagementModel : BlazorBase
    {
        protected IApiService ApiService => base.ApiService;
        protected ISnackbar Snackbar => base.Snackbar;
        
        [Inject]
        protected IDialogService DialogService { get; set; }

        protected List<SettingViewModel> Settings { get; set; } = new();
        protected List<string> Categories { get; set; } = new();
        protected string SelectedCategory { get; set; } = null;
        protected bool IsLoading { get; set; } = true;
        protected string SearchString { get; set; } = "";
        protected SettingViewModel SettingForDialog { get; set; } = new();
        protected bool DialogVisible { get; set; } = false;
        protected bool IsNewSetting { get; set; } = false;

        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            await LoadCategoriesAsync();
            await LoadSettingsAsync();
        }

        protected async Task LoadCategoriesAsync()
        {
            try
            {
                Categories = await ApiService.GetAsync<List<string>>("api/settings/categories");
                if (Categories == null)
                {
                    Categories = new List<string>();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading categories: {ex.Message}", Severity.Error);
                Categories = new List<string>();
            }
        }

        protected async Task LoadSettingsAsync()
        {
            IsLoading = true;
            try
            {
                string url = "api/settings";
                if (!string.IsNullOrEmpty(SelectedCategory))
                {
                    url += $"?category={Uri.EscapeDataString(SelectedCategory)}";
                }

                Settings = await ApiService.GetAsync<List<SettingViewModel>>(url);
                if (Settings == null)
                {
                    Settings = new List<SettingViewModel>();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error loading settings: {ex.Message}", Severity.Error);
                Settings = new List<SettingViewModel>();
            }
            finally
            {
                IsLoading = false;
            }
        }

        protected async Task OnCategoryChanged(string category)
        {
            SelectedCategory = category;
            await LoadSettingsAsync();
        }

        protected bool FilterFunc(SettingViewModel setting)
        {
            if (string.IsNullOrWhiteSpace(SearchString))
                return true;

            return setting.Key?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) == true ||
                   setting.Value?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) == true || 
                   setting.Description?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) == true ||
                   setting.Category?.Contains(SearchString, StringComparison.OrdinalIgnoreCase) == true;
        }

        protected async Task OpenDialogAsync(SettingViewModel setting = null)
        {
            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium };
            var parameters = new DialogParameters();
            var title = "Create New Setting";
            
            if (setting == null)
            {
                // Creating new setting
                setting = new SettingViewModel
                {
                    Category = SelectedCategory ?? "General",
                    IsEditable = true,
                    DataType = "String"
                };
            }
            else
            {
                // Editing existing setting
                title = "Edit Setting";
            }
            
            parameters.Add("Setting", setting);
            parameters.Add("IsNew", setting.Id == 0);
            
            var dialog = await DialogService.ShowAsync<SettingDialog>(title, parameters, options);
            var result = await dialog.Result;
            
            if (!result.Canceled)
            {
                await LoadSettingsAsync();
                await LoadCategoriesAsync();
            }
        }

        protected void CloseDialog()
        {
            DialogVisible = false;
        }

        protected async Task SaveSettingAsync()
        {
            try
            {
                if (IsNewSetting)
                {
                    await ApiService.PostAsync<SettingViewModel>("api/settings", SettingForDialog);
                    Snackbar.Add($"Setting '{SettingForDialog.Key}' created successfully", Severity.Success);
                }
                else
                {
                    await ApiService.UpdateAsync<SettingViewModel>($"api/settings/{SettingForDialog.Key}", SettingForDialog);
                    Snackbar.Add($"Setting '{SettingForDialog.Key}' updated successfully", Severity.Success);
                }

                DialogVisible = false;
                await LoadSettingsAsync();
                
                // Reload categories in case we added a new one
                if (IsNewSetting)
                {
                    await LoadCategoriesAsync();
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error saving setting: {ex.Message}", Severity.Error);
            }
        }

        protected async Task DeleteSettingAsync(SettingViewModel setting)
        {
            bool? result = await DialogService.ShowMessageBox(
                "Confirm Delete",
                $"Are you sure you want to delete the setting '{setting.Key}'?",
                yesText: "Delete",
                cancelText: "Cancel");

            if (result == true)
            {
                try
                {
                    await ApiService.DeleteAsync($"api/settings/{setting.Key}");
                    Snackbar.Add($"Setting '{setting.Key}' deleted successfully", Severity.Success);
                    await LoadSettingsAsync();
                }
                catch (Exception ex)
                {
                    Snackbar.Add($"Error deleting setting: {ex.Message}", Severity.Error);
                }
            }
        }

        protected async Task CreateTestSettingAsync()
        {
            try
            {
                // Create a test setting
                var testSetting = new SettingViewModel
                {
                    Key = "Test.Setting." + DateTime.Now.Ticks,
                    Value = "This is a test setting value",
                    DataType = "String",
                    Description = "A test setting to verify the settings system is working",
                    Category = "Test",
                    IsEditable = true
                };
                
                await ApiService.PostAsync<SettingViewModel>("api/settings", testSetting);
                Snackbar.Add("Test setting created successfully", Severity.Success);
                
                // Reload data
                await LoadCategoriesAsync();
                await LoadSettingsAsync();
            }
            catch (Exception ex)
            {
                Snackbar.Add($"Error creating test setting: {ex.Message}", Severity.Error);
            }
        }
    }
}