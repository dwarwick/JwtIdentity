namespace JwtIdentity.Client.Helpers
{
    public class Utility : IUtility
    {
        public Utility(NavigationManager navigationManager)
        {
            NavigationManager = navigationManager;
        }

        public NavigationManager NavigationManager { get; }

        public string Domain
        {
            get
            {
                if (NavigationManager != null && NavigationManager.Uri != null)
                {
                    return new Uri(NavigationManager.Uri).Host;
                }
                else
                {
                    return string.Empty;
                }
            }
        }
    }
}
