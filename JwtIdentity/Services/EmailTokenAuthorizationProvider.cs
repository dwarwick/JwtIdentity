using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System;
using System.Threading.Tasks;

namespace JwtIdentity.Services
{
    public class EmailTokenAuthorizationProvider<TUser> : DataProtectorTokenProvider<TUser> where TUser : class
    {
        private readonly ILogger<EmailTokenAuthorizationProvider<TUser>> _logger;
        private readonly IDataProtectionProvider _dataProtectionProvider;
        
        public EmailTokenAuthorizationProvider(
            IDataProtectionProvider dataProtectionProvider,
            IOptions<DataProtectionTokenProviderOptions> options,
            ILogger<EmailTokenAuthorizationProvider<TUser>> logger)
            : base(dataProtectionProvider.CreateProtector("EmailTokenAuthorizationProvider"), options, logger)
        {
            try
            {
                _logger = logger ?? throw new ArgumentNullException(nameof(logger));
                _dataProtectionProvider = dataProtectionProvider ?? throw new ArgumentNullException(nameof(dataProtectionProvider));
                
                _logger.LogInformation("EmailTokenAuthorizationProvider initialized successfully");
            }
            catch (Exception ex)
            {
                logger?.LogError(ex, "Error initializing EmailTokenAuthorizationProvider");
                throw;
            }
        }

        public override async Task<bool> CanGenerateTwoFactorTokenAsync(UserManager<TUser> manager, TUser user)
        {
            try
            {
                if (manager == null)
                {
                    _logger.LogError("UserManager is null in CanGenerateTwoFactorTokenAsync");
                    throw new ArgumentNullException(nameof(manager));
                }

                if (user == null)
                {
                    _logger.LogError("User is null in CanGenerateTwoFactorTokenAsync");
                    throw new ArgumentNullException(nameof(user));
                }

                _logger.LogDebug("Checking if token can be generated for user");
                return await base.CanGenerateTwoFactorTokenAsync(manager, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CanGenerateTwoFactorTokenAsync for user");
                throw;
            }
        }

        public override async Task<string> GenerateAsync(string purpose, UserManager<TUser> manager, TUser user)
        {
            try
            {
                if (string.IsNullOrEmpty(purpose))
                {
                    _logger.LogError("Purpose is null or empty in GenerateAsync");
                    throw new ArgumentNullException(nameof(purpose));
                }

                if (manager == null)
                {
                    _logger.LogError("UserManager is null in GenerateAsync");
                    throw new ArgumentNullException(nameof(manager));
                }

                if (user == null)
                {
                    _logger.LogError("User is null in GenerateAsync");
                    throw new ArgumentNullException(nameof(user));
                }

                _logger.LogInformation("Generating token for user with purpose: {Purpose}", purpose);
                return await base.GenerateAsync(purpose, manager, user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user with purpose: {Purpose}", purpose);
                throw;
            }
        }

        public override async Task<bool> ValidateAsync(string purpose, string token, UserManager<TUser> manager, TUser user)
        {
            try
            {
                if (string.IsNullOrEmpty(purpose))
                {
                    _logger.LogError("Purpose is null or empty in ValidateAsync");
                    throw new ArgumentNullException(nameof(purpose));
                }

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogError("Token is null or empty in ValidateAsync");
                    throw new ArgumentNullException(nameof(token));
                }

                if (manager == null)
                {
                    _logger.LogError("UserManager is null in ValidateAsync");
                    throw new ArgumentNullException(nameof(manager));
                }

                if (user == null)
                {
                    _logger.LogError("User is null in ValidateAsync");
                    throw new ArgumentNullException(nameof(user));
                }

                _logger.LogInformation("Validating token for user with purpose: {Purpose}", purpose);
                bool result = await base.ValidateAsync(purpose, token, manager, user);
                
                if (result)
                {
                    _logger.LogInformation("Token validation successful for purpose: {Purpose}", purpose);
                }
                else
                {
                    _logger.LogWarning("Token validation failed for purpose: {Purpose}", purpose);
                }
                
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token for user with purpose: {Purpose}", purpose);
                throw;
            }
        }
    }
}
