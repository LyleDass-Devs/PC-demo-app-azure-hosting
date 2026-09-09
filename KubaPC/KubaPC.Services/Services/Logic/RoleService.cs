using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;

namespace KubaPC.Services.Services.Logic
{
    public class RoleService : BaseService, IRoleService
    {
        public RoleService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<List<DropDownResponse>> SelectAllActive()
        {
            const string sql = @"SELECT id, role_name AS value
                                 FROM roles
                                 WHERE is_active
                                 ORDER BY role_name";

            return await _dataAccess.QueryAsync<DropDownResponse>(sql, new { });
        }
    }
}