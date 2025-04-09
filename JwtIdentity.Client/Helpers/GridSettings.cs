using Syncfusion.Blazor.Grids;

namespace JwtIdentity.Client.Helpers
{
    internal static class GridSettings
    {
        internal static GridTextWrapSettings TextWrapSettings { get; set; } = new()
        {
            WrapMode = WrapMode.Content
        };

        internal static GridLine GridLines { get; set; } = GridLine.Both;
    }
}
