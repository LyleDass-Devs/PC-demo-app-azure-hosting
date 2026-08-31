using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Authentication.Requests
{
    public class AuthenticationRequest
    {
        public string EmailAddress { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
