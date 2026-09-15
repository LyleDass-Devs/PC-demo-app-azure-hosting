using FluentValidation;
using KubaPC.API.Validators.Common;
using KubaPC.Models.Contracts.Contracts.Requests;

namespace KubaPC.API.Validators.Contracts
{
    public class ContractListValidator : PagedListValidator<ContractListRequest>
    {
        public ContractListValidator()
        {
            RuleFor(x => x.OperatorId).NotEmpty();
        }
    }
}
