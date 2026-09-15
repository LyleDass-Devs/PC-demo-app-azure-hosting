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

        /// <summary>Contracts always live under their operator.</summary>
        public static string Contracts(Guid operatorId) => $"/operators/{operatorId}/contracts";
        public static string PenaltyList(Guid contractId) => $"/contracts/{contractId}/penalties";
        public const string LOGIN = "/login";
        public const string FORGOT_PASSWORD = "/forgot-password";
        public const string CHANGE_PASSWORD = "/change-password";
        public const string ME = "/me";
        public const string CHANGE_PASSWORD_VOLUNTARY = "/me/password";

        /// <summary>A module that has a tile but no screen yet.</summary>
        public static string ComingSoon(string module) => $"/soon/{module}";
    }
}
