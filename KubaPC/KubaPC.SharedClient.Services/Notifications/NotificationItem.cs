namespace KubaPC.SharedClient.Services.Notifications
{
    /// <summary>Something the user chose to be told about later; one line, optionally one action.</summary>
    public sealed record NotificationAction(string Label, Func<Task> Invoke);

    public sealed record NotificationItem(
        Guid Id,
        NotificationLevel Level,
        string Title,
        string? Message,
        NotificationAction? Action,
        bool AutoDismiss);
}
