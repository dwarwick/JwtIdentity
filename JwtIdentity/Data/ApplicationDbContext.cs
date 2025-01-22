namespace JwtIdentity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Seed roles
            _ = builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Id = 2, Name = "User", NormalizedName = "USER" }
            );


            // Seed users
            var hasher = new PasswordHasher<ApplicationUser>();
            _ = builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = 1,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "AdminPassword123"),
                    SecurityStamp = string.Empty
                },
                new ApplicationUser
                {
                    Id = 2,
                    UserName = "user",
                    NormalizedUserName = "USER",
                    Email = "user@example.com",
                    NormalizedEmail = "USER@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = hasher.HashPassword(null, "UserPassword123"),
                    SecurityStamp = string.Empty
                }
            );

            // Assign roles to users
            _ = builder.Entity<IdentityUserRole<int>>().HasData(
                new IdentityUserRole<int> { UserId = 1, RoleId = 1 }, // Admin
                new IdentityUserRole<int> { UserId = 2, RoleId = 2 }  // User
            );

            base.OnModelCreating(builder);
        }
    }
}
