namespace KubaPC.Services.Security
{
    public static class BankAccountMask
    {
        private const int VISIBLE_DIGITS = 4;
        private const char MASK = '•';

        public static string? Apply(string? accountNumber)
        {
            if (string.IsNullOrWhiteSpace(accountNumber))
            {
                return accountNumber;
            }

            string trimmed = accountNumber.Trim();

            if (trimmed.Length <= VISIBLE_DIGITS)
            {
                return new string(MASK, trimmed.Length);
            }

            return new string(MASK, trimmed.Length - VISIBLE_DIGITS) + trimmed[^VISIBLE_DIGITS..];
        }
    }
}
