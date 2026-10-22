using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services
{
    public static class ClientRoutes
    {
        public const string HOME = "/";
        public const string ACCOUNTS = "/accounts";
        public const string OPERATORS = "/operators";
        public const string CONTRACTS = "/contracts";
        public const string CATALOGUE = "/catalogue";
        public const string PENALTY_CAPTURE = "/penalty-capture";
        public const string PENALTY_REVIEW = "/penalty-review";
        public const string FORM_F1_CAPTURE = "/form-f1-capture";
        public const string FORM_F1_REVIEW = "/form-f1-review";

        public static string Contracts(Guid operatorId) => $"/operators/{operatorId}/contracts";
        public static string PenaltyList(Guid contractId) => $"/catalogue?contractId={contractId}";
        public const string LOGIN = "/login";
        public const string FORGOT_PASSWORD = "/forgot-password";
        public const string CHANGE_PASSWORD = "/change-password";
        public const string ME = "/me";
        public const string CHANGE_PASSWORD_VOLUNTARY = "/me/password";

        public static string ComingSoon(string module) => $"/soon/{module}";
    }
}
