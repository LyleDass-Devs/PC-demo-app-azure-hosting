using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Requests
{
    public class ChangePasswordRequest
    {
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
