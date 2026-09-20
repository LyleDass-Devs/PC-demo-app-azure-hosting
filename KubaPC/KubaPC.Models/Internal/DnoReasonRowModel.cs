namespace KubaPC.Models.Internal
{
    public class DnoReasonRowModel
    {
        public Guid Id { get; set; }
        public string ReasonValue { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public int UseCount { get; set; }
        public long TotalCount { get; set; }
    }
}
