namespace KubaPC.SharedClient.Components
{
    /// <summary>Which of the two client apps is running. Shared pages use it to show only what this app can open.</summary>
    public enum KubaApp
    {
        Admin,
        Operator
    }

    /// <summary>What differs between the Admin and Operator apps on shared pages.</summary>
    public sealed record AppInfo(KubaApp App, string Title, string EntrySubtitle);
}
