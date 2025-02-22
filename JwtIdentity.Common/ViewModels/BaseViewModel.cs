namespace JwtIdentity.Common.ViewModels
{
    public class BaseViewModel
    {
        public DateTime CreatedDate { get; set; }
        public DateTime UpdatedDate { get; set; }
        public ApplicationUserViewModel CreatedBy { get; set; }
        public int CreatedById { get; set; }
    }
}
