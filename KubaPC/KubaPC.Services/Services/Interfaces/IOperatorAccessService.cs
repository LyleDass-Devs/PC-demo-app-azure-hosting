using KubaPC.Models.Internal;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IOperatorAccessService
    {
        Task<OperationResult> CheckOperator(Caller caller, Guid operatorId);
        Task<OperationResult> CheckContract(Caller caller, Guid contractId);
        Task<OperationResult> CheckRoute(Caller caller, Guid routeId);
        Task<OperationResult> CheckTrip(Caller caller, Guid tripId);
        Task<OperationResult> CheckContractRate(Caller caller, Guid rateId);
        Task<OperationResult> CheckPenaltyListItem(Caller caller, Guid itemId);
        Task<OperationResult> CheckPenaltyCapture(Caller caller, Guid captureId);
        Task<OperationResult> CheckFormF1Record(Caller caller, Guid recordId);
    }
}
