namespace JwtIdentity.Models
{
    public class ApplicationRole : IdentityRole<int>
    {
        public ICollection<RoleClaim>? Claims { get; set; }
    }
}
