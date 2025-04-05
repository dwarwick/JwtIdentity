using Microsoft.AspNetCore.Components;
using MudBlazor;
using System.ComponentModel.DataAnnotations;
using JwtIdentity.Common.ViewModels;

namespace JwtIdentity.Client.Pages.Auth
{
    public class ResetPasswordModel : BlazorBase
    {
        [Parameter]
        [SupplyParameterFromQuery]
        public string Token { get; set; } = string.Empty;

        [Parameter]
        [SupplyParameterFromQuery]
        public string Email { get; set; } = string.Empty;

        protected ResetPasswordViewModel resetPasswordModel { get; set; } = new();
        protected bool isProcessing = false;

        protected override void OnInitialized()
        {
            resetPasswordModel.Email = Email;
            resetPasswordModel.Token = Token;
        }

        protected async Task HandleResetPassword()
        {
            try
            {
                if (string.IsNullOrEmpty(resetPasswordModel.Token) || string.IsNullOrEmpty(resetPasswordModel.Email))
                {
                    Snackbar.Add("Invalid reset token or email", Severity.Error);
                    return;
                }

                if (resetPasswordModel.Password != resetPasswordModel.ConfirmPassword)
                {
                    Snackbar.Add("Passwords do not match", Severity.Error);
                    return;
                }

                isProcessing = true;
                
                var response = await ApiService.PostAsync<ResetPasswordViewModel, Response<object>>($"api/auth/resetpassword", resetPasswordModel);
                
                if (response.Success)
                {
                    Snackbar.Add("Your password has been reset successfully", Severity.Success);
                    NavigationManager.NavigateTo("/password-reset-success");
                }
                else
                {
                    Snackbar.Add(response.Message ?? "Failed to reset password", Severity.Error);
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

    public class ResetPasswordViewModel
    {
        public string Email { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters long")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirm password is required")]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}