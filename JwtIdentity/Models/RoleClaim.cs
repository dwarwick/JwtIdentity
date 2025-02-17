namespace JwtIdentity.Models
{
    public class RoleClaim : IdentityRoleClaim<int>
    {
        public ApplicationRole Role { get; set; }
    }
}