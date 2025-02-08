namespace JwtIdentity.Model
{
    public class RoleClaim : IdentityRoleClaim<int>
    {
        public ApplicationRole Role { get; set; }
    }
}