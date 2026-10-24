namespace KubaPC.Models.Internal
{
    public sealed record ResetPasswordResult(bool IsSuccess, string? Error, string? TemporaryPassword, DateTime? ExpiresAt, bool IsForbidden = false)
    {
        public static ResetPasswordResult Success(string temporaryPassword, DateTime expiresAt) => new(true, null, temporaryPassword, expiresAt);
        public static ResetPasswordResult Failure(string error) => new(false, error, null, null);
        public static ResetPasswordResult From(AccountOperationResult result) => new(result.IsSuccess, result.Error, null, null, result.IsForbidden);
    }
}
