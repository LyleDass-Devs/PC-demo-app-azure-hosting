using KubaPC.Services.Data;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class DatabaseAuthenticationModeResolverTests
    {
        [Fact]
        public void Resolve_FlagTrue_ReturnsEntraId()
        {
            IConfiguration configuration = BuildConfiguration("true");

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);

            Assert.Equal(DatabaseAuthenticationMode.EntraId, mode);
        }

        [Fact]
        public void Resolve_FlagUpperCaseTrue_ReturnsEntraId()
        {
            IConfiguration configuration = BuildConfiguration("TRUE");

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);

            Assert.Equal(DatabaseAuthenticationMode.EntraId, mode);
        }

        [Fact]
        public void Resolve_FlagFalse_ReturnsPassword()
        {
            IConfiguration configuration = BuildConfiguration("false");

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);

            Assert.Equal(DatabaseAuthenticationMode.Password, mode);
        }

        [Fact]
        public void Resolve_FlagMissing_ReturnsPassword()
        {
            IConfiguration configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>())
                .Build();

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);

            Assert.Equal(DatabaseAuthenticationMode.Password, mode);
        }

        [Fact]
        public void Resolve_FlagEmpty_ReturnsPassword()
        {
            IConfiguration configuration = BuildConfiguration(string.Empty);

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);

            Assert.Equal(DatabaseAuthenticationMode.Password, mode);
        }

        [Fact]
        public void Resolve_NullConfiguration_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DatabaseAuthenticationModeResolver.Resolve(null!));
        }

        private static IConfiguration BuildConfiguration(string flagValue)
        {
            return new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    [DatabaseAuthenticationModeResolver.CONFIGURATION_KEY] = flagValue
                })
                .Build();
        }
    }
}
