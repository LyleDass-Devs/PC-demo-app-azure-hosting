using KubaPC.SharedClient.Services;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class ReturnUrlGuardTests
    {
        [Theory]
        [InlineData("/contracts")]
        [InlineData("/form-f1-capture?trip=1")]
        public void LocalPathOrHome_LocalPaths_AreKept(string path)
        {
            Assert.Equal(path, ReturnUrlGuard.LocalPathOrHome(path));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("https://evil.example")]
        [InlineData("//evil.example")]
        [InlineData("/\\evil.example")]
        [InlineData("javascript:alert(1)")]
        [InlineData("contracts")]
        [InlineData("/\t/evil.example")]
        [InlineData("/\n/evil.example")]
        [InlineData("/ /evil.example")]
        public void LocalPathOrHome_AnythingElse_GoesHome(string? path)
        {
            Assert.Equal(ClientRoutes.HOME, ReturnUrlGuard.LocalPathOrHome(path));
        }
    }
}
