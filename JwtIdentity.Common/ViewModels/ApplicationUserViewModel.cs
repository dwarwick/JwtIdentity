namespace JwtIdentity.Common.ViewModels
{
    public class ApplicationUserViewModel
    {
        // insert all properties from IdentityUser<TKey> here
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; }
        public string ConcurrencyStamp { get; set; }
        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public string NormalizedEmail { get; set; }
        public string NormalizedUserName { get; set; }
        public string TwoFactorEnabled { get; set; }
        public string LockoutEnd { get; set; }
        public string LockoutEnabled { get; set; }
        public string AccessFailedCount { get; set; }

        public string Theme { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }

        public List<string> Roles { get; set; } = new List<string>();
        public List<string> Permissions { get; set; } = new List<string>();
    }
}
