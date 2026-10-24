using KubaPC.Models.Contracts.IncomeFields.Requests;
using KubaPC.Models.Contracts.IncomeFields.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;

namespace KubaPC.Services.Services.Logic
{
    public class IncomeFieldService : BaseService, IIncomeFieldService
    {
        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        private readonly IOperatorAccessService access;

        public IncomeFieldService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<IncomeFieldsResponse?> SelectByContract(Guid contractId, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, contractId);

            if (!allowed.IsSuccess)
            {
                return null;
            }

            const string sql = @"SELECT id, contract_id, income_private_hires_enabled, income_dedicated_contracts_enabled, modified_at
                                 FROM income_field_configurations
                                 WHERE contract_id = @ContractId AND is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<IncomeFieldsResponse>(sql, new { ContractId = contractId });
        }

        public async Task<OperationResult> Save(SaveIncomeFieldsRequest request, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, request.ContractId);

            if (!allowed.IsSuccess)
            {
                return allowed;
            }

            Guid actor = caller.AccountId;
            const string contractSql = @"SELECT count(*) FROM contracts WHERE id = @ContractId AND is_active";
            long contractCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(contractSql, new { request.ContractId });

            if (contractCount == 0)
            {
                return OperationResult.Failure("The contract is not active.");
            }

            const string sql = ELEVATE + @"
                WITH updated AS (
                    UPDATE income_field_configurations
                    SET income_private_hires_enabled = @IncomePrivateHiresEnabled,
                        income_dedicated_contracts_enabled = @IncomeDedicatedContractsEnabled,
                        modified_at = now(), modified_by = @Actor
                    WHERE contract_id = @ContractId AND is_active
                    RETURNING id
                )
                INSERT INTO income_field_configurations
                    (contract_id, income_private_hires_enabled, income_dedicated_contracts_enabled, created_by)
                SELECT @ContractId, @IncomePrivateHiresEnabled, @IncomeDedicatedContractsEnabled, @Actor
                WHERE NOT EXISTS (SELECT 1 FROM updated)" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.ContractId,
                request.IncomePrivateHiresEnabled,
                request.IncomeDedicatedContractsEnabled,
                Actor = actor
            });

            return OperationResult.Success();
        }
    }
}
