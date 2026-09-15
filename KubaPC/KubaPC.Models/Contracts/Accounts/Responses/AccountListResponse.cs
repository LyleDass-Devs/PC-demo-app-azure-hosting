using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Responses
{
    public class AccountListResponse
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public bool MustChangePassword { get; set; }
        public string? RoleNames { get; set; }
        public string? OperatorNames { get; set; }
    }
}
