using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Internal
{
    public sealed record CreateAccountResult(Guid? Id, string? Error)
    {
        public bool IsSuccess => Id.HasValue;
    }
}
