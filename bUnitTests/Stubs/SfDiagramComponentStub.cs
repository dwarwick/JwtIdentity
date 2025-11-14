using Syncfusion.Blazor.Diagram;

namespace JwtIdentity.Client.Tests.Stubs
{
    // This stub inherits from SfDiagramComponent so:
    // - @ref="diagram" still works (type is assignable)
    // - All the same parameters still exist
    // - But we skip the JS-heavy lifecycle
    public class SfDiagramComponentStub : SfDiagramComponent
    {
        // This is where the real component calls DomUtil.MeasureBounds etc.
        // We override it and *do not* call base to avoid JS interop.
        protected override Task OnAfterRenderAsync(bool firstRender)
        {
            return Task.CompletedTask;
        }
    }
}
