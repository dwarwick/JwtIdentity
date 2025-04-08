using Microsoft.AspNetCore.Components;

namespace JwtIdentity.Client.Pages.Documentation
{
    public class DocumentationModel : BlazorBase
    {
        // This class inherits from BlazorBase which provides all the necessary
        // services like navigation, authentication, snackbar, etc.
        
        protected override async Task OnInitializedAsync()
        {
            await base.OnInitializedAsync();
            // Any initialization code can go here if needed
        }
    }
}