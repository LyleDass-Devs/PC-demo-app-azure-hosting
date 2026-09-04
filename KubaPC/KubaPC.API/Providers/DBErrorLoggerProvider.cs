using KubaPC.API.Loggers;
using KubaPC.Services.Helpers;

namespace KubaPC.API.Providers
{
    [ProviderAlias("DatabaseErrorLogger")]
    public class DBErrorLoggerProvider :ILoggerProvider
    {
        private readonly ILoggingHelper _logger;
        public DBErrorLoggerProvider(ILoggingHelper logger)
        {
            _logger = logger;
        }

        public ILogger CreateLogger(string categroyName) => new DBErrorLogger(categroyName, _logger);
        public void Dispose() 
        {
        }

    }
}
