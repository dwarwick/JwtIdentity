using JwtIdentity.Common.ViewModels;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using MudBlazor;

namespace JwtIdentity.Client.Pages.Admin.Dialogs
{
    public class EditUserDialogModel : BlazorBase
    {
        [CascadingParameter] private IMudDialogInstance MudDialogInstance { get; set; } = default!;
        [Parameter] public ApplicationUserViewModel User { get; set; } = new();
        [Parameter] public List<ApplicationUserViewModel> AllUsers { get; set; } = new();

        protected MudForm _form = default!;
        protected List<string> AllRoles { get; set; } = new();
        protected string SelectedRole { get; set; } = string.Empty;
        protected IEnumerable<string> AvailableRoles => AllRoles.Where(r => !User.Roles.Contains(r));
        protected DateTime? lockoutDate;

        protected override async Task OnInitializedAsync()
        {
            var roles = await ApiService.GetAsync<List<ApplicationRoleViewModel>>($"{ApiEndpoints.Auth}/GetRolesAndPermissions");
            AllRoles = roles?.Select(r => r.Name).ToList() ?? new();
            lockoutDate = User.LockoutEnd?.DateTime;
        }

        protected bool CanSave => !string.IsNullOrWhiteSpace(User.UserName) &&
                                  User.Roles.Any() &&
                                  !AllUsers.Any(u => u.UserName.Equals(User.UserName, StringComparison.OrdinalIgnoreCase) && u.Id != User.Id);

        protected void AddRole()
        {
            if (!string.IsNullOrEmpty(SelectedRole) && !User.Roles.Contains(SelectedRole))
            {
                User.Roles.Add(SelectedRole);
                SelectedRole = string.Empty;
            }
        }

        protected void RemoveRole(string role) => User.Roles.Remove(role);

        protected async Task Save()
        {
            await _form.Validate();
            if (!CanSave || !_form.IsValid)
            {
                return;
            }

            if (User.Id == 0)
            {
                User.Password = "mypassword";
            }

            User.LockoutEnd = lockoutDate.HasValue ? new DateTimeOffset(lockoutDate.Value) : null;

            MudDialogInstance.Close(DialogResult.Ok(User));
        }

        protected void Cancel() => MudDialogInstance.Cancel();
    }
}
