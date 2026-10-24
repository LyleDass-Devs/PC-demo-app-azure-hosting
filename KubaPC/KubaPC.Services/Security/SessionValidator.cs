using KubaPC.Models.Internal;
using System.Security.Claims;

namespace KubaPC.Services.Security
{
    public static class SessionValidator
    {
        public const string TOKEN_VERSION_CLAIM = "TokenVersion";
        public const string PASSWORD_CHANGE_CLAIM = "PasswordChangeRequired";
        public const string SESSION_ENDED = "The session is no longer valid. Please sign in again.";

        public static bool IsCurrent(SessionStateModel? state, string? tokenVersionClaim)
        {
            if (state is null || !state.IsActive)
            {
                return false;
            }

            return int.TryParse(tokenVersionClaim, out int tokenVersion) && tokenVersion == state.TokenVersion;
        }

        public static void Refresh(ClaimsIdentity identity, SessionStateModel state)
        {
            foreach (Claim stale in identity.FindAll(ClaimTypes.Role).Concat(identity.FindAll(PASSWORD_CHANGE_CLAIM)).ToList())
            {
                identity.RemoveClaim(stale);
            }

            foreach (string role in state.RoleNames.Where(role => !string.IsNullOrWhiteSpace(role)))
            {
                identity.AddClaim(new Claim(ClaimTypes.Role, role));
            }

            if (state.MustChangePassword)
            {
                identity.AddClaim(new Claim(PASSWORD_CHANGE_CLAIM, "true"));
            }
        }
    }
}
