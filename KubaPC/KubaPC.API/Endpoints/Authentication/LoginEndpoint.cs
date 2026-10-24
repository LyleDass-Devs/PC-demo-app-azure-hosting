using FastEndpoints;
using KubaPC.Models.Contracts.Authentication.Requests;
using KubaPC.Models.Contracts.Authentication.Responses;
using KubaPC.Services.Services.Interfaces;
using System.Net;

namespace KubaPC.API.Endpoints.Authentication
{
    public class LoginEndpoint : Endpoint<AuthenticationRequest, AuthenticationResponse>
    {
        private readonly IAuthenticationService authenticationService;

        public LoginEndpoint(IAuthenticationService authenticationService)
        {
            this.authenticationService = authenticationService;
        }

        public override void Configure()
        {
            Post("/authentication/authenticate");
            AllowAnonymous();
        }

        public override async Task HandleAsync(AuthenticationRequest request, CancellationToken ct)
        {
            try
            {
                AuthenticationResponse? response = await authenticationService.Authenticate(request);

                if (response is null)
                {
                    await Send.UnauthorizedAsync(cancellation: ct);
                    return;
                }

                await Send.OkAsync(response, cancellation: ct);

            }
            catch (Exception e)
            {
                Logger.LogError(e, "Login failed for {EmailAddress}", request.EmailAddress);
                await Send.ErrorsAsync((int)HttpStatusCode.InternalServerError, cancellation: ct);
            }
        }
    }
}
