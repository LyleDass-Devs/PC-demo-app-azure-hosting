using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services
{
    public static class ClientRoutes
    {
        public const string HOME = "/";
        public const string ACCOUNTS = "/accounts";
        public const string LOGIN = "/login";
        public const string CHANGE_PASSWORD = "/change-password";
        public const string ME = "/me";
        public const string CHANGE_PASSWORD_VOLUNTARY = "/me/password";

        /// <summary>A module that has a tile but no screen yet.</summary>
        public static string ComingSoon(string module) => $"/soon/{module}";
    }
}
