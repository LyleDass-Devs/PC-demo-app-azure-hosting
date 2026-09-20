using FluentValidation;
using KubaPC.API.Validators.Common;
using KubaPC.Models.Contracts.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Contracts.Penalties
{
    public class PenaltyListItemListValidator : PagedListValidator<PenaltyListItemListRequest>
    {
    }
}
