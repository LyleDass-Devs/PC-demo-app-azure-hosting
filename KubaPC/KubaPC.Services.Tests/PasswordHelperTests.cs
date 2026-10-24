using KubaPC.Services.Helpers;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class PasswordHelperTests
    {
        private readonly PasswordHelper helper = new();

        [Fact]
        public void GenerateTemporaryPassword_HasLettersAndDigitsAndExpectedLength()
        {
            string password = helper.GenerateTemporaryPassword();

            Assert.Equal(14, password.Length);
            Assert.Contains(password, c => char.IsLetter(c));
            Assert.Contains(password, c => char.IsDigit(c));
            Assert.DoesNotContain(password, c => "0O1Il".Contains(c));
        }

        [Fact]
        public void GenerateTemporaryPassword_IsDifferentEachTime()
        {
            Assert.NotEqual(helper.GenerateTemporaryPassword(), helper.GenerateTemporaryPassword());
        }

        [Fact]
        public void Verify_WrongPassword_ReturnsFalse()
        {
            string hash = helper.Hash("CorrectHorse9");

            Assert.True(helper.Verify("CorrectHorse9", hash));
            Assert.False(helper.Verify("WrongHorse9", hash));
            Assert.False(helper.Verify("CorrectHorse9", null));
        }

        [Fact]
        public void VerifyAgainstDummy_DoesNotThrow()
        {
            helper.VerifyAgainstDummy("anything");
        }
    }
}
