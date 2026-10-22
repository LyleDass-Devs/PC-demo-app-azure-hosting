namespace KubaPC.SharedClient.Services.Notifications
{

    public sealed record NotificationAction(string Label, Func<Task> Invoke);

    public sealed record NotificationItem(
        Guid Id,
        NotificationLevel Level,
        string Title,
        string? Message,
        NotificationAction? Action,
        bool AutoDismiss);
}
