using Dapper;
using KubaPC.Models.Internal;
using System.Data;

namespace KubaPC.Services.Security
{
    public static class OperatorScope
    {
        public const string ACCESS_DENIED = "You do not have access to that operator's data.";

        public static string ForOperatorColumn(string operatorColumn) =>
            $@" AND (@IsUnscoped OR EXISTS (SELECT 1 FROM account_operators scope
                                             WHERE scope.account_id = @Actor AND scope.is_active
                                               AND scope.operator_id = {operatorColumn}))";

        public static string ForContractColumn(string contractColumn) =>
            $@" AND (@IsUnscoped OR EXISTS (SELECT 1 FROM account_operators scope
                                             JOIN contracts scope_contract ON scope_contract.operator_id = scope.operator_id
                                             WHERE scope.account_id = @Actor AND scope.is_active
                                               AND scope_contract.id = {contractColumn}))";

        public static void AddParameters(DynamicParameters parameters, Caller caller)
        {
            parameters.Add("Actor", caller.AccountId, DbType.Guid);
            parameters.Add("IsUnscoped", caller.IsUnscoped, DbType.Boolean);
        }
    }
}
