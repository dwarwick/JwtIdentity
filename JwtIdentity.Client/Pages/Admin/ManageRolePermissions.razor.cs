using JwtIdentity.Client.Services;
using JwtIdentity.Common.Helpers;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using System.Text.Json;
using System.Threading.Tasks;

namespace JwtIdentity.Client.Pages.Admin
{
    public class ManageRolePermissionsBase : ComponentBase, IDisposable
    {
        [Inject]
        public AuthenticationStateProvider? AuthStateProvider { get; set; }

        [Inject]
        public IApiService? ApiService { get; set; }

        [Inject]
        public NavigationManager? Navigation { get; set; }

        protected List<ApplicationRoleViewModel>? applicationRoleViewModels { get; set; }

        protected ApplicationRoleViewModel? RoleViewModel { get; set; } = new();
        protected ApplicationUserViewModel? applicationUserViewModel;

        protected List<string>? AllPermissions { get; set; }

        protected List<string>? UnusedPermissions { get; set; }

        protected string? SelectedPermission { get; set; }

        protected override async Task OnInitializedAsync()
        {
            ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut += NavigateLogin;
            
            var type = typeof(Permissions);

            AllPermissions = type.GetFields().Select(q => q.Name).ToList();

            if (((CustomAuthStateProvider)AuthStateProvider!).CurrentUser != null)
            {
                applicationUserViewModel = ((CustomAuthStateProvider)AuthStateProvider!).CurrentUser;

                applicationRoleViewModels = await ApiService!.GetAsync<List<ApplicationRoleViewModel>>($"{ApiEndpoints.Auth}/GetRolesAndPermissions");


                applicationRoleViewModels = applicationRoleViewModels.Where(x => x.Name != "Administrator").ToList();

                if (applicationRoleViewModels != null && applicationRoleViewModels.Any())
                {
                    RoleViewModel = applicationRoleViewModels[0];

                    await SelectedRoleChanged();
                }

            }
        }

        protected async Task<List<string>> GetUnusedPermissions()
        {
            var authState = await AuthStateProvider!.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated == false)
            {
                await ((CustomAuthStateProvider)AuthStateProvider).LoggedOut();
                Navigation?.NavigateTo("/login");
            }       

            if (RoleViewModel != null && RoleViewModel.Claims != null && AllPermissions != null)
            {
                return AllPermissions.Where(x => !RoleViewModel.Claims.Select(x => x.ClaimValue).ToList().Contains(x)).ToList() ?? new();
            }

            return new List<string>();
        }

        protected async Task SelectedRoleChanged()
        {
            UnusedPermissions = await GetUnusedPermissions();

            if (UnusedPermissions.Any())
            {
                SelectedPermission = UnusedPermissions[0];
            }
            else
            {
                SelectedPermission = string.Empty;
            }
        }

        protected async Task AddPermission(string permission)
        {
            var authState = await AuthStateProvider!.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated == false)
            {
                await ((CustomAuthStateProvider)AuthStateProvider).LoggedOut();
                Navigation?.NavigateTo("/login");
            }            
            
            if (RoleViewModel != null && RoleViewModel.Claims != null && !string.IsNullOrEmpty(permission))
            {
                RoleClaimViewModel newPermission = new()
                {
                    ClaimType = CustomClaimTypes.Permission,
                    ClaimValue = permission,
                    RoleId = RoleViewModel.Id
                };

                RoleClaimViewModel response = await ApiService!.CreateAsync($"{ApiEndpoints.Auth}/addpermission", newPermission);


                RoleViewModel.Claims.Add(response ?? new());

                UnusedPermissions = await GetUnusedPermissions();

                StateHasChanged();

                // _ = (SnackbarService?.Add("Permission Successfully Added", MudBlazor.Severity.Success));

            }
        }

        protected async Task DeletePermission(RoleClaimViewModel permission)
        {
            var authState = await AuthStateProvider!.GetAuthenticationStateAsync();

            if (authState.User.Identity?.IsAuthenticated == false)
            {
                await ((CustomAuthStateProvider)AuthStateProvider).LoggedOut();
                Navigation?.NavigateTo("/login");
            }    

            if (RoleViewModel != null && RoleViewModel.Claims != null)
            {
                bool success = await ApiService!.DeleteAsync($"{ApiEndpoints.Auth}/deletepermission/{permission.Id}");

                if (success)
                {
                    //  _ = SnackbarService.Add("Permission Successfully Removed", MudBlazor.Severity.Success);

                    _ = RoleViewModel.Claims.Remove(permission);

                    UnusedPermissions = await GetUnusedPermissions();

                    StateHasChanged();
                }
                else
                {
                    //  _ = (SnackbarService?.Add("Error Deleting Permission", MudBlazor.Severity.Error));
                }
            }
        }

        private void NavigateLogin()
        {
            Navigation?.NavigateTo("/login");
        }

        public void Dispose()
        {
            ((CustomAuthStateProvider)AuthStateProvider!).OnLoggedOut -= NavigateLogin;
        }
    }
}
