using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Internal
{
    public sealed record AccountOperationResult(bool IsSuccess, string? Error)
    {
        public static AccountOperationResult Success() => new(true, null);
        public static AccountOperationResult Failure(string error) => new(false, error);
    }
}
