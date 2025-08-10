using System.Text.Json.Serialization;

namespace JwtIdentity.Common.ViewModels
{
    public class ApplicationUserViewModel
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public string Email { get; set; }
        public string PhoneNumber { get; set; }

        [JsonIgnore]
        public string PasswordHash { get; set; }

        [JsonIgnore]
        public string SecurityStamp { get; set; }

        [JsonIgnore]
        public string ConcurrencyStamp { get; set; }

        public bool EmailConfirmed { get; set; }
        public bool PhoneNumberConfirmed { get; set; }
        public string NormalizedEmail { get; set; }
        public string NormalizedUserName { get; set; }
        public bool TwoFactorEnabled { get; set; }
        public DateTimeOffset? LockoutEnd { get; set; }
        public bool LockoutEnabled { get; set; }
        public int AccessFailedCount { get; set; }

        public string Theme { get; set; }
        public string Token { get; set; }
        public string Password { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }

        public List<string> Roles { get; set; } = new();
        public List<string> Permissions { get; set; } = new();

        public string RolesDisplay => string.Join(", ", Roles);
    }
}
