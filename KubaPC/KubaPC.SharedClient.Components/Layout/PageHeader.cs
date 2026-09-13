namespace KubaPC.SharedClient.Components.Layout
{
    /// <summary>The title the top bar shows. Pages set it; the bar listens.</summary>
    public class PageHeader
    {
        public string Title { get; private set; } = string.Empty;

        public event Action? Changed;

        public void Set(string title)
        {
            Title = title;
            Changed?.Invoke();
        }
    }
}
