using KubaPC.Models.Contracts.Common.Responses;

namespace KubaPC.Models.Contracts.FormF1.Responses
{
    public class FormF1LookupsResponse
    {
        public List<string> RecordTypes { get; set; } = new();
        public List<DropDownResponse> DnoReasons { get; set; } = new();
    }
}
