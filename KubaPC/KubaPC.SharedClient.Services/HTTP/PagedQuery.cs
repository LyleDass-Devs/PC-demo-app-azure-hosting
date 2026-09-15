using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.SharedClient.Services.Http
{
    public static class PagedQuery
    {
        public static string From(PagedListRequest request)
        {
            List<string> parts = new()
            {
                $"page={request.Page}",
                $"pageSize={request.PageSize}",
                $"sortDescending={request.SortDescending}"
            };

            if (!string.IsNullOrWhiteSpace(request.Search)) parts.Add($"search={Uri.EscapeDataString(request.Search)}");
            if (request.IsActive.HasValue) parts.Add($"isActive={request.IsActive.Value}");
            if (!string.IsNullOrWhiteSpace(request.SortBy)) parts.Add($"sortBy={Uri.EscapeDataString(request.SortBy)}");

            return string.Join("&", parts);
        }
    }
}
