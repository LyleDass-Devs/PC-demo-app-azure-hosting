using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Requests
{
    public class UpdateAccountRequest
    {
        public Guid Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public List<string> RoleNames { get; set; } = new();
    }
}
