using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Internal
{
    public class ErrorLogModel
    {
        public Guid Id { get; set; }
        public string LogLevel { get; set; } = string.Empty;
        public string? EventName { get; set; }
        public string? Message { get; set; }
        public string? ExceptionMessage { get; set; }
        public string? InnerExceptionMessage { get; set; }
        public string? ExceptionStackTrace { get; set; }
        public string? ExceptionSource { get; set; }
        public string? RequestPath { get; set; }
    }
}
