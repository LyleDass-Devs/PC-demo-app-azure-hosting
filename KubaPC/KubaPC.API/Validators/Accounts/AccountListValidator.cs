using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class AccountListValidator : Validator<AccountListRequest>
    {
        private const int MAXIMUM_PAGE_SIZE = 100;

        public AccountListValidator()
        {
            RuleFor(x => x.Page).GreaterThanOrEqualTo(1);
            RuleFor(x => x.PageSize).InclusiveBetween(1, MAXIMUM_PAGE_SIZE);
        }
    }
}
