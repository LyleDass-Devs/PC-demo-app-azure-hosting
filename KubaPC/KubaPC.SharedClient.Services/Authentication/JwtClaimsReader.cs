using System.Security.Claims;
using System.Text.Json;

namespace KubaPC.SharedClient.Services.Authentication
{

    public static class JwtClaimsReader
    {
        private const string EXPIRY_CLAIM = "exp";

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

        public static bool IsExpired(string token)
        {
            Dictionary<string, JsonElement>? payload = ReadPayload(token);

            if (payload is null
                || !payload.TryGetValue(EXPIRY_CLAIM, out JsonElement expiry)
                || expiry.ValueKind != JsonValueKind.Number
                || !expiry.TryGetInt64(out long expirySeconds))
            {

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

                return null;
            }
        }

        private static byte[] DecodeBase64Url(string segment)
        {

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
