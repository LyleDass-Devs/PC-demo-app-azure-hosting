namespace KubaPC.SharedClient.Components.Layout
{

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
