namespace KubaPC.SharedClient.Services.Notifications
{
    /// <summary>
    /// Keeps the notifications currently on screen. At most three are shown; older ones
    /// drop off. A notification carrying an action never dismisses itself, because the
    /// user must be able to reach the action.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private const int MAXIMUM_VISIBLE = 3;

        private readonly List<NotificationItem> items = new();

        public IReadOnlyList<NotificationItem> Items => items;

        public event Action? Changed;

        public void Notify(NotificationLevel level, string title, string? message = null, NotificationAction? action = null)
        {
            items.Add(new NotificationItem(Guid.NewGuid(), level, title, message, action, AutoDismiss: action is null));

            while (items.Count > MAXIMUM_VISIBLE)
            {
                items.RemoveAt(0);
            }

            Changed?.Invoke();
        }

        public void Dismiss(Guid id)
        {
            int removed = items.RemoveAll(item => item.Id == id);

            if (removed > 0)
            {
                Changed?.Invoke();
            }
        }
    }
}
