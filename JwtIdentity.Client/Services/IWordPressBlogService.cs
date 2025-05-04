namespace JwtIdentity.Client.Services
{
    public interface IWordPressBlogService
    {
        Task<WordPressPostResponse> GetAllPostsAsync();
    }
}
