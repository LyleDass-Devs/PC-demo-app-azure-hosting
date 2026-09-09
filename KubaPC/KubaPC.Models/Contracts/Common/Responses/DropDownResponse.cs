using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Common.Responses
{
    public class DropDownResponse
    {
        public Guid Id { get; set; }
        public string Value { get; set; } = string.Empty;
    }
}
