namespace KubaPC.Services.Helpers
{
    public interface IPasswordHelper
    {
        string Hash(string password);
        bool Verify(string password, string? storedHash);
        void VerifyAgainstDummy(string password);
        string GenerateTemporaryPassword();
    }
}
