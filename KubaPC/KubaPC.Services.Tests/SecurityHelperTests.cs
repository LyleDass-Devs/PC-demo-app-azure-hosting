using KubaPC.Services.Security;
using Xunit;

namespace KubaPC.Services.Tests
{
    public class SecurityHelperTests
    {
        [Fact]
        public void SearchPattern_EscapesWildcardsAndBackslashes()
        {
            Assert.Equal("%50\\%\\_off\\\\%", SearchPattern.Build("50%_off\\"));
        }

        [Fact]
        public void SearchPattern_BlankSearchMatchesEverything()
        {
            Assert.Equal("%", SearchPattern.Build(null));
            Assert.Equal("%", SearchPattern.Build("   "));
        }

        [Fact]
        public void SearchPattern_TrimsAndWraps()
        {
            Assert.Equal("%ZNT 1129%", SearchPattern.Build("  ZNT 1129 "));
        }

        [Fact]
        public void BankAccountMask_ShowsLastFourOnly()
        {
            Assert.Equal("••••••8432", BankAccountMask.Apply("1234568432"));
        }

        [Fact]
        public void BankAccountMask_ShortNumbersAreFullyHidden()
        {
            Assert.Equal("•••", BankAccountMask.Apply("123"));
        }

        [Fact]
        public void BankAccountMask_BlankStaysBlank()
        {
            Assert.Null(BankAccountMask.Apply(null));
            Assert.Equal("", BankAccountMask.Apply(""));
        }
    }
}
