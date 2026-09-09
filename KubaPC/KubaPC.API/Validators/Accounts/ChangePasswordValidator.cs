using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;
using System.Diagnostics.Metrics;

namespace KubaPC.API.Validators.Accounts
{
    public class ChangePasswordValidator :Validator<ChangePasswordRequest>
    {
        private const int MINIMUM_PASSWORD_LENGTH = 8;

        public ChangePasswordValidator()
        {
            RuleFor(x => x.OldPassword).NotEmpty();
            RuleFor(x => x.NewPassword)
                    .NotEmpty()
                    .MinimumLength(MINIMUM_PASSWORD_LENGTH)
                    .Matches("[A-Za-a]").WithMessage("The password must contain at least one letter.")
                    .Matches("[0-9]").WithMessage("The password must contain at least one digit.")
                    .NotEqual(x => x.OldPassword)
                    .WithMessage(("The new password must differ from the old one."));

        }

    }
}
