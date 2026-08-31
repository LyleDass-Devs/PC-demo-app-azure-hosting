using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Authentication.Responses
{
    public class AuthenticationResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
        public string Token { get; set; } = string.Empty;
        public DateTime TokenExpireDate { get; set; }
    }
}
