using FastEndpoints;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.Roles
{
    public class GetRolesEndpoint : EndpointWithoutRequest<List<DropDownResponse>>
    {
        private readonly IRoleService roleService;

        public GetRolesEndpoint(IRoleService roleService)
        {
            this.roleService = roleService;
        }

        public override void Configure()
        {
            Get("/roles");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(CancellationToken ct) 
        {
            try
            {
                List<DropDownResponse> roles = await roleService.SelectAllActive();
                await Send.OkAsync(roles,cancellation:ct);
            }
            catch (Exception e)
            {
                Logger.LogError(e, "Get Role list failed");
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
