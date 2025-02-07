namespace JwtIdentity.Model
{
    public class ApplicationRole : IdentityRole<int>
    {
        public ICollection<RoleClaim>? Claims { get; set; }
    }
}
