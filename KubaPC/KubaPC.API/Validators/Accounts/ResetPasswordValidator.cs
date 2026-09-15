using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;

namespace KubaPC.API.Validators.Accounts
{
    public class ResetPasswordValidator : Validator<ResetPasswordRequest>
    {
        private const int MINIMUM_PASSWORD_LENGTH = 8;

        public ResetPasswordValidator()
        {
            RuleFor(x => x.Id).NotEmpty();
            RuleFor(x => x.TemporaryPassword).NotEmpty().MinimumLength(MINIMUM_PASSWORD_LENGTH)
                .WithMessage($"Password must be at least {MINIMUM_PASSWORD_LENGTH} characters.");
        }
    }
}
