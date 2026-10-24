using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Accounts.Requests;
using System.Diagnostics.Metrics;

namespace KubaPC.API.Validators.Accounts
{
    public class ChangePasswordValidator :Validator<ChangePasswordRequest>
    {
        private const int MINIMUM_PASSWORD_LENGTH = 8;
        private const int MAXIMUM_PASSWORD_LENGTH = 72;

        public ChangePasswordValidator()
        {
            RuleFor(x => x.OldPassword).NotEmpty().MaximumLength(MAXIMUM_PASSWORD_LENGTH);
            RuleFor(x => x.NewPassword)
                    .NotEmpty()
                    .MinimumLength(MINIMUM_PASSWORD_LENGTH)
                    .MaximumLength(MAXIMUM_PASSWORD_LENGTH)
                    .Matches("[A-Za-z]").WithMessage("The password must contain at least one letter.")
                    .Matches("[0-9]").WithMessage("The password must contain at least one digit.")
                    .NotEqual(x => x.OldPassword)
                    .WithMessage(("The new password must differ from the old one."));

        }

    }
}
