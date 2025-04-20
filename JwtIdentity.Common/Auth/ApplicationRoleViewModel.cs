namespace JwtIdentity.Common.Auth
{
    public class ApplicationRoleViewModel
    {
        public virtual string Id { get; set; } = default!;

        /// <summary>
        /// Gets or sets the name for this role.
        /// </summary>
        public virtual string Name { get; set; }

        /// <summary>
        /// Gets or sets the normalized name for this role.
        /// </summary>
        public virtual string NormalizedName { get; set; }

        /// <summary>
        /// A random value that should change whenever a role is persisted to the store
        /// </summary>
        public virtual string ConcurrencyStamp { get; set; }

        public List<RoleClaimViewModel> Claims { get; set; }
    }
}