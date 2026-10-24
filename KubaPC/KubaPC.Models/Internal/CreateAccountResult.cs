namespace KubaPC.Models.Internal
{
    public sealed record CreateAccountResult(Guid? Id, string? Error, bool IsForbidden = false)
    {
        public bool IsSuccess => Id.HasValue;

        public static CreateAccountResult Forbidden(string error) => new(null, error, true);
    }
}
