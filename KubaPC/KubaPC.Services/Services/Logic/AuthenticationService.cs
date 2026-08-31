using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Helpers;
using KubaPC.Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Logic
{
    public class AuthenticationService : IAuthenticationService
    {
        protected readonly IAccountService accountService;
        protected readonly IPasswordHelper passwordHelper;
        protected readonly AppSettingsModel appSettings;

        public AuthenticationService(IAccountService accountService, IPasswordHelper passwordHelper, AppSettingsModel appSettings)
        { 
            this.accountService = accountService;
            this.passwordHelper = passwordHelper;
            this.appSettings = appSettings;
        }

        public async Task<AuthenticationResponse?> Authenticate(AuthenticationRequest request)
        {
            AccountCredentialsModel? account = await accountService.SelectActiveByEmail(request.EmailAddress);

            if (account is null || !passwordHelper.Verify(request.Password, account.PasswordHash))
            {
                return null;
            }

            List<string> roles = await accountService.SelectRoleNames(account.Id);

            await accountService.UpdateLastLogin(account.Id);

            TokenSourceDataModel tokenData = new TokenSourceDataModel()
            {
                User = new LoggedInUserModel
                {
                    Id = account.Id,
                    EmailAddress = account.EmailAddress,
                    FullName = account.FullName
                },

                UserRoles = roles
            };

            return UserClaimsHelper.GenerateJwtToken(tokenData, appSettings);
        }
    }
}
