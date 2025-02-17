namespace JwtIdentity.Models
{
    public class BaseModel
    {
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ApplicationUser CreatedBy { get; set; }
    }
}
