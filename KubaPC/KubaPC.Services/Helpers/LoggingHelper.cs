using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services;

namespace KubaPC.Services.Helpers
{

    public class LoggingHelper : BaseService, ILoggingHelper
    {
        public LoggingHelper(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
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

                Console.Error.WriteLine($"error_logs write failed: {writeFailure.Message}");
            }
        }
    }
}
