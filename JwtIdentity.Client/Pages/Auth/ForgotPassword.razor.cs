using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Client.Pages.Auth
{
    public class ForgotPasswordModel : BlazorBase
    {
        protected ForgotPasswordViewModel forgotPasswordModel { get; set; } = new();
        protected bool isProcessing = false;

        protected async Task HandleForgotPassword()
        {
            try
            {
                isProcessing = true;
                
                var response = await ApiService.PostAsync<ForgotPasswordViewModel, Response<object>>($"api/auth/forgotpassword", forgotPasswordModel);
                
                if (response.Success)
                {
                    Snackbar.Add("Password reset link has been sent to your email", Severity.Success);
                    NavigationManager.NavigateTo($"/password-reset-sent?email={forgotPasswordModel.Email}");
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to send password reset email", Severity.Error);
                }
            }
            catch (Exception ex)
            {
                Snackbar.Add($"An error occurred: {ex.Message}", Severity.Error);
            }
            finally
            {
                isProcessing = false;
                StateHasChanged();
            }
        }
    }

    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email is required")]
        [EmailAddress(ErrorMessage = "Invalid email address")]
        public string Email { get; set; } = string.Empty;
    }
}