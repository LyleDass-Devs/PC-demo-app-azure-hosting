using FastEndpoints;
using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Internal;
using KubaPC.Services.Services.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;

namespace KubaPC.API.Endpoints.IncomeFields
{
    public class SaveIncomeFieldsEndpoint : Endpoint<SaveIncomeFieldsRequest>
    {
        private readonly IIncomeFieldService incomeFieldService;

        public SaveIncomeFieldsEndpoint(IIncomeFieldService incomeFieldService)
        {
            this.incomeFieldService = incomeFieldService;
        }

        public override void Configure()
        {
            Post("/income-fields/save");
            AuthSchemes(JwtBearerDefaults.AuthenticationScheme);
            Policies("Admin");
        }

        public override async Task HandleAsync(SaveIncomeFieldsRequest request, CancellationToken ct)
        {
            try
            {
                Guid actor = Guid.Parse(User.FindFirst("UserId")!.Value);

                OperationResult result = await incomeFieldService.Save(request, actor);

                if (!result.IsSuccess)
                {
                    AddError(result.Error!);
                    await Send.ErrorsAsync(409, ct);
                    return;
                }

                await Send.OkAsync(new { message = "Income fields saved." }, cancellation: ct);
            }
            catch (Exception exception)
            {
                Logger.LogError(exception, "Save income fields failed for {ContractId}", request.ContractId);
                await Send.ErrorsAsync(500, ct);
            }
        }
    }
}
