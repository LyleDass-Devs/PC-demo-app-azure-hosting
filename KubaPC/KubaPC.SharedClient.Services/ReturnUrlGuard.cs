namespace KubaPC.SharedClient.Services
{
    public static class ReturnUrlGuard
    {
        public static string LocalPathOrHome(string? returnUrl)
        {
            if (string.IsNullOrWhiteSpace(returnUrl)
                || returnUrl.Any(character => char.IsControl(character) || char.IsWhiteSpace(character))
                || !returnUrl.StartsWith('/')
                || returnUrl.StartsWith("//")
                || returnUrl.Contains('\\')
                || returnUrl.Contains(':'))
            {
                return ClientRoutes.HOME;
            }

            return returnUrl;
        }
    }
}
