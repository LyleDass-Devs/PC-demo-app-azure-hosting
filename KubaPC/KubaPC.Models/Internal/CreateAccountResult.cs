namespace KubaPC.Models.Internal
{
    public sealed record CreateAccountResult(Guid? Id, string? Error, bool IsForbidden = false, string? TemporaryPassword = null, DateTime? ExpiresAt = null)
    {
        public bool IsSuccess => Id.HasValue;

        public static CreateAccountResult Forbidden(string error) => new(null, error, true);
        public static CreateAccountResult Success(Guid id, string temporaryPassword, DateTime expiresAt) => new(id, null, false, temporaryPassword, expiresAt);
    }
}
