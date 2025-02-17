namespace JwtIdentity.Models
{
    public class ApplicationUser : IdentityUser<int>
    {
        public string Theme { get; set; } = "light";
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
    }
}
