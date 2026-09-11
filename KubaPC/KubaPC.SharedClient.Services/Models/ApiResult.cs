namespace KubaPC.SharedClient.Services.Models
{
    public class ApiResult
    {
        public bool IsSuccess { get; init; }

        public int StatusCode { get; init; }

        public string? ErrorCode { get; init; }

        public string? ErrorMessage { get; init; }

        public IReadOnlyDictionary<string, string[]> ValidationErrors { get; init; }
            = new Dictionary<string, string[]>();

        public bool HasValidationErrors => ValidationErrors.Count > 0;
    }

    public class ApiResult<T> : ApiResult
    {
        public T? Data { get; init; }
    }
}