namespace KubaPC.Models.Contracts.Operators.Requests
{
    /// <summary>A new bus operator. IdSociete is the company code the SAEIV feed uses for it.</summary>
    public class CreateOperatorRequest
    {
        public string IdSociete { get; set; } = string.Empty;
        public string OperatorName { get; set; } = string.Empty;
    }
}
