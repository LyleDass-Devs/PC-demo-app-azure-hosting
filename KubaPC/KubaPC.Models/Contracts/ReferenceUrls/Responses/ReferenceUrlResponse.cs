namespace KubaPC.Models.Contracts.ReferenceUrls.Responses
{
    public class ReferenceUrlResponse
    {
        public string UrlKey { get; set; } = string.Empty;
        public string? UrlValue { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
