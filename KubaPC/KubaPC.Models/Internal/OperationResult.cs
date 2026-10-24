namespace KubaPC.Models.Internal
{
    public sealed record OperationResult(bool IsSuccess, string? Error, bool IsForbidden = false)
    {
        public static OperationResult Success() => new(true, null);
        public static OperationResult Failure(string error) => new(false, error);
        public static OperationResult Forbidden(string error) => new(false, error, true);
    }

    public sealed record CreateResult(bool IsSuccess, Guid? Id, string? Error, bool IsForbidden = false)
    {
        public static CreateResult Success(Guid id) => new(true, id, null);
        public static CreateResult Failure(string error) => new(false, null, error);
        public static CreateResult Forbidden(string error) => new(false, null, error, true);
        public static CreateResult From(OperationResult result) => new(result.IsSuccess, null, result.Error, result.IsForbidden);
    }
}
