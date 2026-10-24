using KubaPC.Models.Contracts.Escalations.Requests;
using KubaPC.Models.Contracts.Escalations.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Logic
{
    public class EscalationParameterService : BaseService, IEscalationParameterService
    {
        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        private readonly IOperatorAccessService access;

        public EscalationParameterService(INpgsqlDataAccess dataAccess, IOperatorAccessService access) : base(dataAccess)
        {
            this.access = access;
        }

        public async Task<EscalationParametersResponse?> SelectByContract(Guid contractId, Caller caller)
        {
            OperationResult allowed = await access.CheckContract(caller, contractId);

            if (!allowed.IsSuccess)
            {
                return null;
            }

            const string sql = @"SELECT id, contract_id, labour_weight, fuel_weight, cpi_weight,
                                        base_month, base_year, labour_base_value, fuel_base_value, cpi_base_value, modified_at
                                 FROM escalation_parameters
                                 WHERE contract_id = @ContractId AND is_active";

            return await _dataAccess.QuerySingleOrDefaultAsync<EscalationParametersResponse>(sql, new { ContractId = contractId });
        }

        public async Task<OperationResult> Save(SaveEscalationParametersRequest request, Caller caller)
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
                    UPDATE escalation_parameters
                    SET labour_weight = @LabourWeight, fuel_weight = @FuelWeight, cpi_weight = @CpiWeight,
                        base_month = @BaseMonth, base_year = @BaseYear,
                        labour_base_value = @LabourBaseValue, fuel_base_value = @FuelBaseValue, cpi_base_value = @CpiBaseValue,
                        modified_at = now(), modified_by = @Actor
                    WHERE contract_id = @ContractId AND is_active
                    RETURNING id
                )
                INSERT INTO escalation_parameters
                    (contract_id, labour_weight, fuel_weight, cpi_weight, base_month, base_year,
                     labour_base_value, fuel_base_value, cpi_base_value, created_by)
                SELECT @ContractId, @LabourWeight, @FuelWeight, @CpiWeight, @BaseMonth, @BaseYear,
                       @LabourBaseValue, @FuelBaseValue, @CpiBaseValue, @Actor
                WHERE NOT EXISTS (SELECT 1 FROM updated)" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.ContractId,
                request.LabourWeight,
                request.FuelWeight,
                request.CpiWeight,
                request.BaseMonth,
                request.BaseYear,
                request.LabourBaseValue,
                request.FuelBaseValue,
                request.CpiBaseValue,
                Actor = actor
            });

            return OperationResult.Success();
        }
    }
}
