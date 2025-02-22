namespace JwtIdentity.Common.Auth
{
    public class RegisterViewModel
    {
        [Required]  
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string ConfirmPassword { get; set; }
        public string Response { get; set; }
    }
}
