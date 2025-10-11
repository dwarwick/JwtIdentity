using System.Threading.Tasks;

namespace JwtIdentity.Client.Shared
{
    public partial class PageReadyModel : JwtIdentity.Client.Pages.BlazorBase
    {
        protected override async Task OnAfterRenderAsync(bool firstRender)
        {
            // Signal readiness after every render so SPA navigations are picked up by tests
            try
            {
                await JSRuntime.InvokeVoidAsync("pageReady.notify");
            }
            catch { /* best-effort */ }

            await base.OnAfterRenderAsync(firstRender);
        }
    }
}
