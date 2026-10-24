using KubaPC.Models.Internal;
using KubaPC.Services.Security;
using System.Security.Claims;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class SessionValidatorTests
    {
        private static SessionStateModel ActiveState(int version = 3) => new()
        {
            IsActive = true,
            TokenVersion = version,
            MustChangePassword = false,
            RoleNames = new List<string> { "Monitor" }
        };

        [Fact]
        public void IsCurrent_MatchingVersionOnActiveAccount_ReturnsTrue()
        {
            Assert.True(SessionValidator.IsCurrent(ActiveState(), "3"));
        }

        [Fact]
        public void IsCurrent_OlderVersion_ReturnsFalse()
        {
            Assert.False(SessionValidator.IsCurrent(ActiveState(4), "3"));
        }

        [Fact]
        public void IsCurrent_InactiveAccount_ReturnsFalse()
        {
            SessionStateModel state = ActiveState();
            state.IsActive = false;

            Assert.False(SessionValidator.IsCurrent(state, "3"));
        }

        [Fact]
        public void IsCurrent_MissingAccountOrClaim_ReturnsFalse()
        {
            Assert.False(SessionValidator.IsCurrent(null, "3"));
            Assert.False(SessionValidator.IsCurrent(ActiveState(), null));
            Assert.False(SessionValidator.IsCurrent(ActiveState(), "three"));
        }

        [Fact]
        public void Refresh_ReplacesRolesAndPasswordFlagFromTheDatabase()
        {
            ClaimsIdentity identity = new(new[]
            {
                new Claim(ClaimTypes.Role, "SuperAdmin"),
                new Claim(ClaimTypes.Role, "Monitor"),
                new Claim("UserId", Guid.NewGuid().ToString())
            });
            SessionStateModel state = new()
            {
                IsActive = true,
                TokenVersion = 1,
                MustChangePassword = true,
                RoleNames = new List<string> { "Authoriser" }
            };

            SessionValidator.Refresh(identity, state);

            Assert.Equal(new[] { "Authoriser" }, identity.FindAll(ClaimTypes.Role).Select(c => c.Value).ToArray());
            Assert.True(identity.HasClaim(SessionValidator.PASSWORD_CHANGE_CLAIM, "true"));
            Assert.NotNull(identity.FindFirst("UserId"));
        }

        [Fact]
        public void Refresh_ClearsPasswordFlagWhenNoLongerRequired()
        {
            ClaimsIdentity identity = new(new[] { new Claim(SessionValidator.PASSWORD_CHANGE_CLAIM, "true") });

            SessionValidator.Refresh(identity, ActiveState());

            Assert.Null(identity.FindFirst(SessionValidator.PASSWORD_CHANGE_CLAIM));
        }
    }
}
