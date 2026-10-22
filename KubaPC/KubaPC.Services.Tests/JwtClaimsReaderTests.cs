using System.Security.Claims;
using System.Text;
using System.Text.Json;

using KubaPC.SharedClient.Services.Authentication;

namespace KubaPC.Services.Tests
{
    public class JwtClaimsReaderTests
    {

        private static string BuildToken(object payload)
        {
            static string Encode(string value) =>
                Convert.ToBase64String(Encoding.UTF8.GetBytes(value))
                    .TrimEnd('=')
                    .Replace('+', '-')
                    .Replace('/', '_');

            string header = Encode("{\"alg\":\"HS512\",\"typ\":\"JWT\"}");
            string body = Encode(JsonSerializer.Serialize(payload));

            return $"{header}.{body}.not-a-real-signature";
        }

        private static long UnixSeconds(TimeSpan fromNow) =>
            DateTimeOffset.UtcNow.Add(fromNow).ToUnixTimeSeconds();

        [Fact]
        public void ReadClaims_ValidToken_ReturnsEveryClaim()
        {
            string token = BuildToken(new { UserId = "abc-123", unique_name = "Lyle Dass" });

            List<Claim> claims = JwtClaimsReader.ReadClaims(token);

            Assert.Equal("abc-123", claims.Single(claim => claim.Type == "UserId").Value);
            Assert.Equal("Lyle Dass", claims.Single(claim => claim.Type == "unique_name").Value);
        }

        [Fact]
        public void ReadClaims_TokenWithTwoRoles_ReturnsBothRoles()
        {

            string token = BuildToken(new { role = new[] { "Monitor", "DOT" } });

            List<Claim> claims = JwtClaimsReader.ReadClaims(token);

            List<string> roles = claims.Where(claim => claim.Type == "role").Select(claim => claim.Value).ToList();

            Assert.Equal(2, roles.Count);
            Assert.Contains("Monitor", roles);
            Assert.Contains("DOT", roles);
        }

        [Fact]
        public void ReadClaims_SingleRole_ReturnsThatRole()
        {
            string token = BuildToken(new { role = "SuperAdmin" });

            List<Claim> claims = JwtClaimsReader.ReadClaims(token);

            Assert.Equal("SuperAdmin", claims.Single(claim => claim.Type == "role").Value);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("not-a-token")]
        [InlineData("only.two")]
        [InlineData("three.but-not-base64!!.parts")]
        public void ReadClaims_UnusableToken_ReturnsEmpty(string token)
        {
            Assert.Empty(JwtClaimsReader.ReadClaims(token));
        }

        [Fact]
        public void IsExpired_FutureExpiry_ReturnsFalse()
        {
            string token = BuildToken(new { exp = UnixSeconds(TimeSpan.FromHours(1)) });

            Assert.False(JwtClaimsReader.IsExpired(token));
        }

        [Fact]
        public void IsExpired_PastExpiry_ReturnsTrue()
        {
            string token = BuildToken(new { exp = UnixSeconds(TimeSpan.FromHours(-1)) });

            Assert.True(JwtClaimsReader.IsExpired(token));
        }

        [Fact]
        public void IsExpired_NoExpiryClaim_ReturnsTrue()
        {

            string token = BuildToken(new { UserId = "abc-123" });

            Assert.True(JwtClaimsReader.IsExpired(token));
        }

        [Fact]
        public void IsExpired_ExpiryIsNotANumber_ReturnsTrue()
        {
            string token = BuildToken(new { exp = "tomorrow" });

            Assert.True(JwtClaimsReader.IsExpired(token));
        }

        [Theory]
        [InlineData("")]
        [InlineData("not-a-token")]
        public void IsExpired_UnusableToken_ReturnsTrue(string token)
        {
            Assert.True(JwtClaimsReader.IsExpired(token));
        }
    }
}
