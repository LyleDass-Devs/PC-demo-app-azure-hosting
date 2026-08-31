using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.Authentication.Requests;

namespace KubaPC.API.Validators.Authentication
{
    public class LoginValidator : Validator<AuthenticationRequest>
    {
        public LoginValidator()
        {
            RuleFor(x => x.EmailAddress).NotEmpty().WithMessage("Email address is required.").EmailAddress();
            RuleFor(x => x.Password).NotEmpty().WithMessage("Password is required.");
        }
    }
}
