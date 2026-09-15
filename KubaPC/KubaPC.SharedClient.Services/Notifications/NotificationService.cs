namespace KubaPC.SharedClient.Services.Notifications
{
    /// <summary>
    /// Keeps the notifications currently on screen. At most three are shown; older ones
    /// drop off. Every notification dismisses itself after <see cref="LIFETIME"/>; the
    /// timer pauses while the pointer or keyboard focus is on it, so an action stays reachable.
    /// </summary>
    public class NotificationService : INotificationService
    {
        private const int MAXIMUM_VISIBLE = 3;
        public static readonly TimeSpan LIFETIME = TimeSpan.FromSeconds(5);

        private readonly List<NotificationItem> items = new();
        private readonly Dictionary<Guid, CancellationTokenSource> timers = new();

        public IReadOnlyList<NotificationItem> Items => items;

        public event Action? Changed;

        public void Notify(NotificationLevel level, string title, string? message = null, NotificationAction? action = null)
        {
            var item = new NotificationItem(Guid.NewGuid(), level, title, message, action, AutoDismiss: true);
            items.Add(item);

            while (items.Count > MAXIMUM_VISIBLE)
            {
                Dismiss(items[0].Id);
            }

            StartTimer(item.Id);
            Changed?.Invoke();
        }

        public void Dismiss(Guid id)
        {
            StopTimer(id);
            int removed = items.RemoveAll(item => item.Id == id);

            if (removed > 0)
            {
                Changed?.Invoke();
            }
        }

        public void Hold(Guid id) => StopTimer(id);

        public void Release(Guid id)
        {
            if (items.Any(item => item.Id == id))
            {
                StartTimer(id);
            }
        }

        private void StartTimer(Guid id)
        {
            StopTimer(id);
            var source = new CancellationTokenSource();
            timers[id] = source;
            _ = DismissAfterLifetime(id, source.Token);
        }

        private void StopTimer(Guid id)
        {
            if (timers.Remove(id, out CancellationTokenSource? source))
            {
                source.Cancel();
                source.Dispose();
            }
        }

        private async Task DismissAfterLifetime(Guid id, CancellationToken token)
        {
            try
            {
                await Task.Delay(LIFETIME, token);
            }
            catch (TaskCanceledException)
            {
                // Held by hover or focus, or dismissed by hand: the timer simply stops.
                return;
            }

            Dismiss(id);
        }
    }
}
