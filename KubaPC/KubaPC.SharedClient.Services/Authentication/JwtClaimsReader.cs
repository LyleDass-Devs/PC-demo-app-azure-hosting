using System.Security.Claims;
using System.Text.Json;

namespace KubaPC.SharedClient.Services.Authentication
{
    /// <summary>
    /// Reads the claims out of a JWT so the browser knows who is signed in and
    /// what they may see.
    ///
    /// This deliberately does not verify the token's signature. The browser holds
    /// no signing key and could not be trusted with one; the API verifies every
    /// token on every request, which is the check that actually protects data.
    /// What happens here only decides what the interface offers to show.
    /// </summary>
    public static class JwtClaimsReader
    {
        private const string EXPIRY_CLAIM = "exp";

        /// <summary>Claims carried by the token, or an empty list if it cannot be read.</summary>
        public static List<Claim> ReadClaims(string token)
        {
            Dictionary<string, JsonElement>? payload = ReadPayload(token);

            if (payload is null)
            {
                return new List<Claim>();
            }

            List<Claim> claims = new();

            foreach (KeyValuePair<string, JsonElement> entry in payload)
            {
                // A claim type can repeat: a user with two roles arrives as an array.
                if (entry.Value.ValueKind == JsonValueKind.Array)
                {
                    foreach (JsonElement item in entry.Value.EnumerateArray())
                    {
                        claims.Add(new Claim(entry.Key, item.ToString()));
                    }
                }
                else
                {
                    claims.Add(new Claim(entry.Key, entry.Value.ToString()));
                }
            }

            return claims;
        }

        /// <summary>True when the token has no usable expiry, or that expiry has passed.</summary>
        public static bool IsExpired(string token)
        {
            Dictionary<string, JsonElement>? payload = ReadPayload(token);

            // TryGetInt64 throws rather than returning false when the element is
            // not a number at all, so the kind is checked before it is called.
            if (payload is null
                || !payload.TryGetValue(EXPIRY_CLAIM, out JsonElement expiry)
                || expiry.ValueKind != JsonValueKind.Number
                || !expiry.TryGetInt64(out long expirySeconds))
            {
                // Unreadable is treated as expired: refusing a token we cannot
                // understand is safer than trusting it.
                return true;
            }

            return DateTimeOffset.FromUnixTimeSeconds(expirySeconds) <= DateTimeOffset.UtcNow;
        }

        private static Dictionary<string, JsonElement>? ReadPayload(string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            string[] segments = token.Split('.');

            if (segments.Length != 3)
            {
                return null;
            }

            try
            {
                byte[] payloadBytes = DecodeBase64Url(segments[1]);
                return JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(payloadBytes);
            }
            catch (Exception)
            {
                // A stored value that is not a token at all: treat as no token.
                return null;
            }
        }

        private static byte[] DecodeBase64Url(string segment)
        {
            // JWTs use base64url: '-' and '_' replace '+' and '/', and the trailing
            // '=' padding is stripped. Convert.FromBase64String needs both back.
            string base64 = segment.Replace('-', '+').Replace('_', '/');

            switch (base64.Length % 4)
            {
                case 2: base64 += "=="; break;
                case 3: base64 += "="; break;
            }

            return Convert.FromBase64String(base64);
        }
    }
}
