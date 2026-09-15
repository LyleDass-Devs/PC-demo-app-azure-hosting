namespace KubaPC.Models.Contracts.Common.Requests
{
    /// <summary>
    /// Paging, search and sort shared by every list endpoint. A list that needs an extra
    /// filter derives from this and adds only that filter.
    /// </summary>
    public class PagedListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 25;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }
}
