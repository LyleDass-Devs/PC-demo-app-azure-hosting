namespace KubaPC.Models.Internal
{
    public sealed record AccountOperationResult(bool IsSuccess, string? Error, bool IsForbidden = false)
    {
        public static AccountOperationResult Success() => new(true, null);
        public static AccountOperationResult Failure(string error) => new(false, error);
        public static AccountOperationResult Forbidden(string error) => new(false, error, true);
    }
}
