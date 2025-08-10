using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using JwtIdentity.Client.Pages.Admin.Dialogs;
using MudBlazor;

namespace JwtIdentity.Client.Pages.Admin
{
    public class ManageUsersModel : BlazorBase
    {
        protected List<ApplicationUserViewModel> Users { get; set; } = new();
        protected bool _loading = true;

        // Syncfusion Grid properties
        protected int FrozenColumns { get; set; } = 1;

        protected override async Task OnInitializedAsync()
        {
            await LoadUsers();
        }

        private async Task LoadUsers()
        {
            _loading = true;
            Users = await ApiService.GetAsync<List<ApplicationUserViewModel>>(ApiEndpoints.ApplicationUser);
            _loading = false;
        }

        protected async Task OpenUserDialog(ApplicationUserViewModel user)
        {
            var parameters = new DialogParameters
            {
                ["User"] = new ApplicationUserViewModel
                {
                    Id = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    PhoneNumber = user.PhoneNumber,
                    EmailConfirmed = user.EmailConfirmed,
                    PhoneNumberConfirmed = user.PhoneNumberConfirmed,
                    NormalizedEmail = user.NormalizedEmail,
                    NormalizedUserName = user.NormalizedUserName,
                    TwoFactorEnabled = user.TwoFactorEnabled,
                    LockoutEnd = user.LockoutEnd,
                    LockoutEnabled = user.LockoutEnabled,
                    AccessFailedCount = user.AccessFailedCount,
                    Theme = user.Theme,
                    CreatedDate = user.CreatedDate,
                    UpdatedDate = user.UpdatedDate,
                    Roles = new List<string>(user.Roles)
                },
                ["AllUsers"] = Users
            };

            var options = new DialogOptions { CloseButton = true, MaxWidth = MaxWidth.Medium, FullWidth = true, CloseOnEscapeKey = true };
            var dialog = await MudDialog.ShowAsync<EditUserDialog>(user.Id == 0 ? "Add User" : "Edit User", parameters, options);
            var result = await dialog.Result;
            if (!result.Canceled && result.Data is ApplicationUserViewModel model)
            {
                if (model.Id == 0)
                {
                    await ApiService.PostAsync<ApplicationUserViewModel>(ApiEndpoints.ApplicationUser, model);
                }
                else
                {
                    await ApiService.UpdateAsync($"{ApiEndpoints.ApplicationUser}/{model.Id}", model);
                }
                await LoadUsers();
            }
        }

        protected Task AddUser() => OpenUserDialog(new ApplicationUserViewModel());
    }
}
