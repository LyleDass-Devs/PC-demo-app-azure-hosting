using KubaPC.Models.Internal;
using KubaPC.Services.Helpers;

namespace KubaPC.API.Loggers
{
    public class DBErrorLogger :ILogger
    {
        private const LogLevel MINIMUM_LEVEL = LogLevel.Error;
        private readonly string _categoryName;
        private readonly ILoggingHelper _loggingHelper;

        public DBErrorLogger(string categoryName, ILoggingHelper loggingHelper)
        {
            _categoryName = categoryName;
            _loggingHelper = loggingHelper;
        }

        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => logLevel >= MINIMUM_LEVEL;

        public void Log<TState>(LogLevel level, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            if (!IsEnabled(level))
            {
                return;
            }

            ErrorLogModel entry = new ()
            {
                LogLevel = level.ToString(),
                EventName = string.IsNullOrWhiteSpace(eventId.Name) ? _categoryName : eventId.Name,
                Message = formatter(state, exception),
                ExceptionMessage = exception?.Message,
                InnerExceptionMessage = exception?.InnerException?.Message,
                ExceptionStackTrace = exception?.StackTrace,
                ExceptionSource = exception?.Source
            };

            _ = Task.Run(() => _loggingHelper.LogAsync(entry));
        }



    }
}
