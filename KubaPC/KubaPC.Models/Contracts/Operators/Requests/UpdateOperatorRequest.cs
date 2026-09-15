namespace KubaPC.Models.Contracts.Operators.Requests
{
    /// <summary>Renames an operator or corrects its company code.</summary>
    public class UpdateOperatorRequest
    {
        public Guid Id { get; set; }
        public string IdSociete { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
    }
}
