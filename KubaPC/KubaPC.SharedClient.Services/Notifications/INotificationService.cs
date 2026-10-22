namespace KubaPC.SharedClient.Services.Notifications
{
    public interface INotificationService
    {
        IReadOnlyList<NotificationItem> Items { get; }
        event Action? Changed;
        void Notify(NotificationLevel level, string title, string? message = null, NotificationAction? action = null);
        void Dismiss(Guid id);

        void Hold(Guid id);

        void Release(Guid id);
    }
}
