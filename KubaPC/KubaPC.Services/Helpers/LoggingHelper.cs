using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services;

namespace KubaPC.Services.Helpers
{
    /// Writes exceptions to the error_logs table. Never throws: a logger
    /// that can take the application down is worse than a lost log line.

    public class LoggingHelper : BaseService, ILoggingHelper
    {
        public LoggingHelper(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public Task LogAsync(Exception exception, string requestPath = "")
        {
            ErrorLogModel entry = new()
            {
                LogLevel = "Error",
                EventName = exception.TargetSite?.DeclaringType?.FullName,
                Message = exception.Message,
                ExceptionMessage = exception.Message,
                InnerExceptionMessage = exception.InnerException?.Message,
                ExceptionStackTrace = exception.StackTrace,
                ExceptionSource = exception.Source,
                RequestPath = requestPath
            };

            return LogAsync(entry);
        }

        public async Task LogAsync(ErrorLogModel entry)
        {
            try
            {
                string sql = CreateSqlInsert(entry, "error_logs");
                await _dataAccess.ExecuteAsync(sql, CreateDynamicParameters(entry));
            }
            catch (Exception writeFailure)
            {
                // Deliberate swallow: the database is down or the insert failed.
                // Rethrowing would replace the real error with a logging error,
                Console.Error.WriteLine($"error_logs write failed: {writeFailure.Message}");
            }
        }
    }
}