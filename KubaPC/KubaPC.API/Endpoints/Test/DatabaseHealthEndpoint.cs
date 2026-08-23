using FastEndpoints;
using KubaPC.Services.Data;
using Npgsql.Internal;
using System.Net;


namespace KubaPC.API.Endpoints.Test
{
    public sealed record DatabaseHealthResponse(long RoleCount);
    public class DatabaseHealthEndpoint : EndpointWithoutRequest<DatabaseHealthResponse>
    {
        private readonly INpgsqlDataAccess _dataAccess;
        public DatabaseHealthEndpoint(INpgsqlDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        public override void Configure()
        {
            Get("/health/database");
            AllowAnonymous();
        }

        public override async Task HandleAsync(CancellationToken ct)
        {
            try
            {
                long roleCount = await _dataAccess.QuerySingleOrDefaultAsync<long>("Select count(*) From roles where is_active");
                Response = new DatabaseHealthResponse(roleCount);
            }
            catch (Exception e)
            {
                throw;
            }
        }
    }
}
