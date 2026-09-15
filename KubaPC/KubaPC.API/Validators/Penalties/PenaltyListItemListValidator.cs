using FluentValidation;
using KubaPC.API.Validators.Common;
using KubaPC.Models.Contracts.Penalties.Requests;

namespace KubaPC.API.Validators.Penalties
{
    public class PenaltyListItemListValidator : PagedListValidator<PenaltyListItemListRequest>
    {
    }
}
