using System;
using System.Text;
using System.Collections.Generic;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.Models.Internal;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.JsonWebTokens;

namespace KubaPC.Services.Helpers
{
    public static class UserClaimsHelper
    {
        public static AuthenticationResponse GenerateJwtToken(TokenSourceDataModel tokenData, AppSettingsModel appSettings)
        {
            string bearer = string.Empty;
            DateTime expiration = DateTime.UtcNow.AddHours(appSettings.TokenExpireHours);

            byte[] key = Encoding.ASCII.GetBytes(appSettings.Secret);

            List<Claim> claims = new() 
            {
                new Claim(ClaimTypes.Name, tokenData.User.FullName),    
                new Claim(ClaimTypes.Email, tokenData.User.EmailAddress),    
                new Claim("UserId", tokenData.User.Id.ToString())
            };

            foreach (string role in tokenData.UserRoles)
            {
                if (!string.IsNullOrEmpty(role))
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }
            }

            if (tokenData.MustChangePassword)
            {
                claims.Add(new Claim("PasswordChangeRequired", "true"));
            }

            SecurityTokenDescriptor descriptor = new()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = expiration,
                Issuer = appSettings.Issuer,
                Audience = appSettings.Audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha512Signature)
            };

            bearer = new JsonWebTokenHandler().CreateToken(descriptor);

            return new AuthenticationResponse
            {
                Id = tokenData.User.Id,
                FullName = tokenData.User.FullName,
                EmailAddress = tokenData.User.EmailAddress,
                Roles = tokenData.UserRoles,
                Token = bearer,
                TokenExpireDate = expiration,
                MustChangePassword = tokenData.MustChangePassword
            };
        }
    }
}
