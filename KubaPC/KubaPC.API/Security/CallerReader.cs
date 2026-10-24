using KubaPC.Models.Internal;
using System.Security.Claims;

namespace KubaPC.API.Security
{
    public static class CallerReader
    {
        public const string USER_ID_CLAIM = "UserId";

        public static Caller Read(ClaimsPrincipal user)
        {
            string? accountIdClaim = user.FindFirst(USER_ID_CLAIM)?.Value;

            if (!Guid.TryParse(accountIdClaim, out Guid accountId))
            {
                throw new InvalidOperationException("The signed-in user has no account id claim.");
            }

            IEnumerable<string> roles = user.FindAll(ClaimTypes.Role).Select(claim => claim.Value);

            return new Caller(accountId, roles);
        }
    }
}
