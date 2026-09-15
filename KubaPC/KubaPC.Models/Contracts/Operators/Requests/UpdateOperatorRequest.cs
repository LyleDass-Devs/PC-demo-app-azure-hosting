namespace KubaPC.Models.Contracts.Operators.Requests
{
    public class UpdateOperatorRequest
    {
        public Guid Id { get; set; }
        public string IdSociete { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
    }
}
