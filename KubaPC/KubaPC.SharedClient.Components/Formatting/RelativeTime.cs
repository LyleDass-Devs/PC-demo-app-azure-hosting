namespace KubaPC.SharedClient.Components.Formatting
{
    /// <summary>"2 min ago", "Yesterday", "Never": how the interface says when something last happened.</summary>
    public static class RelativeTime
    {
        public static string Describe(DateTime? at)
        {
            if (at is null)
            {
                return "Never";
            }

            TimeSpan age = DateTime.UtcNow - DateTime.SpecifyKind(at.Value, DateTimeKind.Utc);

            if (age.TotalMinutes < 1) return "Just now";
            if (age.TotalMinutes < 60) return $"{(int)age.TotalMinutes} min ago";
            if (age.TotalHours < 24) return $"{(int)age.TotalHours} h ago";
            if (age.TotalDays < 2) return "Yesterday";
            if (age.TotalDays < 7) return $"{(int)age.TotalDays} days ago";

            return at.Value.ToLocalTime().ToString("d MMM yyyy");
        }
    }
}
