namespace KubaPC.SharedClient.Components
{

    public enum KubaApp
    {
        Admin,
        Operator
    }

    public sealed record AppInfo(KubaApp App, string Title, string EntrySubtitle);
}
