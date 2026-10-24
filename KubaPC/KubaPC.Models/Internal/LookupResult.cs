namespace KubaPC.Models.Internal
{
    public sealed record LookupResult<T>(bool IsSuccess, T? Data, string? Error, bool IsForbidden = false)
    {
        public static LookupResult<T> Success(T data) => new(true, data, null);
        public static LookupResult<T> Failure(string error) => new(false, default, error);
        public static LookupResult<T> From(OperationResult result) => new(result.IsSuccess, default, result.Error, result.IsForbidden);
    }
}
