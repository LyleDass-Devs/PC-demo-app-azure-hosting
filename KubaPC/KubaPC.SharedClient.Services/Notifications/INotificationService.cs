namespace KubaPC.SharedClient.Services.Notifications
{
    public interface INotificationService
    {
        IReadOnlyList<NotificationItem> Items { get; }
        event Action? Changed;
        void Notify(NotificationLevel level, string title, string? message = null, NotificationAction? action = null);
        void Dismiss(Guid id);
        /// <summary>Pauses the auto-dismiss timer while the person is looking at or reaching for it.</summary>
        void Hold(Guid id);
        /// <summary>Restarts the auto-dismiss timer after a hold.</summary>
        void Release(Guid id);
    }
}
