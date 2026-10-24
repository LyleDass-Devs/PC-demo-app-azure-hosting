using KubaPC.Models.Internal;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class CallerTests
    {
        [Fact]
        public void IsUnscoped_SuperAdmin_ReturnsTrue()
        {
            Caller caller = new(Guid.NewGuid(), new[] { "SuperAdmin" });

            Assert.True(caller.IsUnscoped);
            Assert.True(caller.IsSuperAdmin);
            Assert.False(caller.IsAdmin);
        }

        [Fact]
        public void IsUnscoped_Admin_ReturnsFalse()
        {
            Caller caller = new(Guid.NewGuid(), new[] { "Admin", "Authoriser" });

            Assert.False(caller.IsUnscoped);
            Assert.True(caller.IsAdmin);
            Assert.True(caller.HasRole("Authoriser"));
            Assert.False(caller.HasRole("Monitor"));
        }

        [Fact]
        public void HasRole_IsCaseSensitive()
        {
            Caller caller = new(Guid.NewGuid(), new[] { "Monitor" });

            Assert.True(caller.HasRole("Monitor"));
            Assert.False(caller.HasRole("monitor"));
        }

        [Fact]
        public void Roles_DuplicatesCollapse()
        {
            Caller caller = new(Guid.NewGuid(), new[] { "Monitor", "Monitor" });

            Assert.Single(caller.Roles);
        }
    }
}
