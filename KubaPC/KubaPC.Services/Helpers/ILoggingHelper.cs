using KubaPC.Models.Internal;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Helpers
{
    public interface ILoggingHelper
    {
        Task LogAsync(Exception e, string requestPath = "");
        Task LogAsync(ErrorLogModel entry);
    }
}
