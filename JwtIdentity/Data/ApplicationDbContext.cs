namespace JwtIdentity.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, ApplicationRole, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            // Seed roles
            _ = builder.Entity<ApplicationRole>().HasData(
                new ApplicationRole { Id = 1, Name = "Admin", NormalizedName = "ADMIN" },
                new ApplicationRole { Id = 2, Name = "User", NormalizedName = "USER" }
            );


            // Seed users
            _ = new PasswordHasher<ApplicationUser>();

            _ = builder.Entity<ApplicationUser>().HasData(
                new ApplicationUser
                {
                    Id = 1,
                    UserName = "admin",
                    NormalizedUserName = "ADMIN",
                    Email = "admin@example.com",
                    NormalizedEmail = "ADMIN@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAIAAYagAAAAENYeHtZjzzSCzot7QF9qVAC25mKeyiv6v/kdBakqJiTW7Jt5TCt/9tSVdTSABsJGtQ==",
                    ConcurrencyStamp = "975c6e69-81c0-463e-bc0f-212c970f34d4",
                    SecurityStamp = string.Empty,
                    Theme = "dark"
                },
                new ApplicationUser
                {
                    Id = 2,
                    UserName = "user",
                    NormalizedUserName = "USER",
                    Email = "user@example.com",
                    NormalizedEmail = "USER@EXAMPLE.COM",
                    EmailConfirmed = true,
                    PasswordHash = "AQAAAAIAAYagAAAAEDaaeD+y1I6b06Mfnm/tKqk8uIC+IIyCC5XMjODRg0PAJuxDcmPh6iihBkSLhMoyJQ==",
                    ConcurrencyStamp = "be6fc596-979b-42b1-906e-d6d5a59d6fce",
                    SecurityStamp = string.Empty,
                    Theme = "light"
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
