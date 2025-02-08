namespace JwtIdentity.Data
{
    public class ApplicationDbContext 
    : IdentityDbContext<
        ApplicationUser,                // TUser
        ApplicationRole,                // TRole
        int,                            // TKey
        IdentityUserClaim<int>,         // TUserClaim
        IdentityUserRole<int>,          // TUserRole
        IdentityUserLogin<int>,         // TUserLogin
        RoleClaim,                      // TRoleClaim (YOUR custom class)
        IdentityUserToken<int>          // TUserToken
    >
{
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
        {
        }

        public DbSet<ApplicationUser> ApplicationUsers { get; set; }
        public DbSet<ApplicationRole> ApplicationRoles { get; set; }

        // You can add a DbSet if you like:
        public DbSet<RoleClaim> RoleClaims { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // create relationship between roleclaims and roles
            builder.Entity<RoleClaim>()
            .HasOne(rc => rc.Role)
            .WithMany(r => r.Claims)
            .HasForeignKey(rc => rc.RoleId)
            .IsRequired();

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

            var type = typeof(Permissions);
            List<string>? AllPermissions;

            AllPermissions = type.GetFields().Select(q => q.Name).ToList();

            // Seed all permissions for Admin role
            _ = builder.Entity<RoleClaim>().HasData(
                AllPermissions.Select(q => new RoleClaim
                {
                    Id = AllPermissions.IndexOf(q) + 1,
                    RoleId = 1,
                    ClaimType = "permission",
                    ClaimValue = q
                }).ToArray()
            );            
        }
    }
}
