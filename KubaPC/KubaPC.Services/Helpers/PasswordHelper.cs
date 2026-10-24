using System.Security.Cryptography;

namespace KubaPC.Services.Helpers
{
    public class PasswordHelper : IPasswordHelper
    {
        private const int WORK_FACTOR = 12;
        private const int TEMPORARY_PASSWORD_LENGTH = 14;
        private const string LETTERS = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz";
        private const string DIGITS = "23456789";

        private static readonly Lazy<string> DUMMY_HASH = new(() => BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString("N"), WORK_FACTOR));

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WORK_FACTOR);
        }

        public bool Verify(string password, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }

        public void VerifyAgainstDummy(string password)
        {
            BCrypt.Net.BCrypt.Verify(password, DUMMY_HASH.Value);
        }

        public string GenerateTemporaryPassword()
        {
            char[] characters = new char[TEMPORARY_PASSWORD_LENGTH];

            for (int index = 0; index < TEMPORARY_PASSWORD_LENGTH - 2; index++)
            {
                characters[index] = LETTERS[RandomNumberGenerator.GetInt32(LETTERS.Length)];
            }

            characters[TEMPORARY_PASSWORD_LENGTH - 2] = DIGITS[RandomNumberGenerator.GetInt32(DIGITS.Length)];
            characters[TEMPORARY_PASSWORD_LENGTH - 1] = DIGITS[RandomNumberGenerator.GetInt32(DIGITS.Length)];

            RandomNumberGenerator.Shuffle(characters.AsSpan());

            return new string(characters);
        }
    }
}
