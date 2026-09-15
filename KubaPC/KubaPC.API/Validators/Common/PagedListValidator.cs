using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Common.Requests;

namespace KubaPC.API.Validators.Common
{
    public abstract class PagedListValidator<TRequest> : Validator<TRequest> where TRequest : PagedListRequest
    {
        private const int MAXIMUM_PAGE_SIZE = 100;

        protected PagedListValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, MAXIMUM_PAGE_SIZE);
        }
    }
}
