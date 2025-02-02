namespace JwtIdentity.Model
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string Theme { get; set; } = "light";
    }
}
