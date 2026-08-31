using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Internal
{
    public class LoggedInUserModel
    {
        public Guid Id { get; set; }
        public string EmailAddress { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
    }
}
