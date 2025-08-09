using Microsoft.AspNetCore.Components.Web;

namespace JwtIdentity.Client.Pages.Common
{
    public partial class SpinnerButtonModel : ComponentBase
    {
        [Parameter] public bool IsBusy { get; set; }
        [Parameter] public string Text { get; set; }
        [Parameter] public MudBlazor.ButtonType ButtonType { get; set; } = MudBlazor.ButtonType.Submit;
        [Parameter] public MudBlazor.Color Color { get; set; } = MudBlazor.Color.Primary;
        [Parameter] public bool Disabled { get; set; }
        [Parameter] public EventCallback<MouseEventArgs> OnClick { get; set; }
    }
}
