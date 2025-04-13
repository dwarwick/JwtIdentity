using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Options;

namespace JwtIdentity.Services
{
    public class EmailTokenAuthorizationProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        public EmailTokenAuthorizationProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<DataProtectionTokenProviderOptions> options,
            ILogger<EmailTokenAuthorizationProvider<TUser>> logger)
            : base(dataProtectionProvider.CreateProtector("EmailTokenAuthorizationProvider"), options, logger)
        {
        }
    }
}
