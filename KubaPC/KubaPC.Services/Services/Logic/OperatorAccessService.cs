using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Security;
using KubaPC.Services.Services.Interfaces;

namespace KubaPC.Services.Services.Logic
{
    public class OperatorAccessService : BaseService, IOperatorAccessService
    {
        private const string GRANTED_OPERATORS = @"SELECT scope.operator_id
                                                    FROM account_operators scope
                                                    WHERE scope.account_id = @Actor AND scope.is_active";

        public OperatorAccessService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public Task<OperationResult> CheckOperator(Caller caller, Guid operatorId) =>
            Check(caller, @"SELECT count(*) FROM operators o
                            WHERE o.id = @Id AND o.id IN (" + GRANTED_OPERATORS + ")", operatorId);

        public Task<OperationResult> CheckContract(Caller caller, Guid contractId) =>
            Check(caller, @"SELECT count(*) FROM contracts c
                            WHERE c.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", contractId);

        public Task<OperationResult> CheckRoute(Caller caller, Guid routeId) =>
            Check(caller, @"SELECT count(*) FROM routes r
                            JOIN contracts c ON c.id = r.contract_id
                            WHERE r.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", routeId);

        public Task<OperationResult> CheckTrip(Caller caller, Guid tripId) =>
            Check(caller, @"SELECT count(*) FROM trips t
                            JOIN routes r ON r.id = t.route_id
                            JOIN contracts c ON c.id = r.contract_id
                            WHERE t.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", tripId);

        public Task<OperationResult> CheckContractRate(Caller caller, Guid rateId) =>
            Check(caller, @"SELECT count(*) FROM contract_rates cr
                            JOIN contracts c ON c.id = cr.contract_id
                            WHERE cr.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", rateId);

        public Task<OperationResult> CheckPenaltyListItem(Caller caller, Guid itemId) =>
            Check(caller, @"SELECT count(*) FROM penalty_list_items p
                            JOIN contracts c ON c.id = p.contract_id
                            WHERE p.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", itemId);

        public Task<OperationResult> CheckPenaltyCapture(Caller caller, Guid captureId) =>
            Check(caller, @"SELECT count(*) FROM penalty_captures pc
                            JOIN routes r ON r.id = pc.route_id
                            JOIN contracts c ON c.id = r.contract_id
                            WHERE pc.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", captureId);

        public Task<OperationResult> CheckFormF1Record(Caller caller, Guid recordId) =>
            Check(caller, @"SELECT count(*) FROM form_f1_records f
                            JOIN routes r ON r.id = f.route_id
                            JOIN contracts c ON c.id = r.contract_id
                            WHERE f.id = @Id AND c.operator_id IN (" + GRANTED_OPERATORS + ")", recordId);

        private async Task<OperationResult> Check(Caller caller, string sql, Guid id)
        {
            if (caller.IsUnscoped)
            {
                return OperationResult.Success();
            }

            long count = await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, new { Id = id, Actor = caller.AccountId });

            return count > 0 ? OperationResult.Success() : OperationResult.Forbidden(OperatorScope.ACCESS_DENIED);
        }
    }
}
