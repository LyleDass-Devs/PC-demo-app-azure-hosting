namespace KubaPC.Models.Internal
{
    public sealed record OperationResult(bool IsSuccess, string? Error)
    {
        public static OperationResult Success() => new(true, null);
        public static OperationResult Failure(string error) => new(false, error);
    }

    public sealed record CreateResult(bool IsSuccess, Guid? Id, string? Error)
    {
        public static CreateResult Success(Guid id) => new(true, id, null);
        public static CreateResult Failure(string error) => new(false, null, error);
    }
}
