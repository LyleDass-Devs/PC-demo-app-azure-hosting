using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Requests
{
  
    public class ResetPasswordRequest
    {
        public Guid Id { get; set; }
        public string TemporaryPassword { get; set; } = string.Empty;
    }
}
