using FastEndpoints;
using FluentValidation;
using KubaPC.Models.Contracts.ReferenceUrls.Requests;

namespace KubaPC.API.Validators.ReferenceUrls
{
    public class SaveReferenceUrlValidator : Validator<SaveReferenceUrlRequest>
    {
        public SaveReferenceUrlValidator()
        {
            RuleFor(x => x.UrlKey).NotEmpty().MaximumLength(50);
            RuleFor(x => x.UrlValue).NotEmpty().WithMessage("Enter the address.").MaximumLength(2000)
                .Must(BeHttp).WithMessage("The address must start with https:// or http://.");
        }

        private static bool BeHttp(string value) =>
            Uri.TryCreate(value?.Trim(), UriKind.Absolute, out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeHttp);
    }
}
