using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Responses
{
    public class CurrentUserResponse
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string EmailAddress { get; set; } = string.Empty;
        public List<string> Roles { get; set; } = new();
    }
}
