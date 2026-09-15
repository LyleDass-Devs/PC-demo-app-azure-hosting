namespace KubaPC.Models.Internal
{
    /// <summary>Outcome of a change that returns nothing but success or a reason.</summary>
    public sealed record OperationResult(bool IsSuccess, string? Error)
    {
        public static OperationResult Success() => new(true, null);
        public static OperationResult Failure(string error) => new(false, error);
    }

    /// <summary>Outcome of a create: the new id, or a reason it was refused.</summary>
    public sealed record CreateResult(bool IsSuccess, Guid? Id, string? Error)
    {
        public static CreateResult Success(Guid id) => new(true, id, null);
        public static CreateResult Failure(string error) => new(false, null, error);
    }
}
