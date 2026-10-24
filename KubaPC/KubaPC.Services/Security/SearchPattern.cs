namespace KubaPC.Services.Security
{
    public static class SearchPattern
    {
        public static string Build(string? search)
        {
            if (string.IsNullOrWhiteSpace(search))
            {
                return "%";
            }

            string escaped = search.Trim()
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");

            return $"%{escaped}%";
        }
    }
}
