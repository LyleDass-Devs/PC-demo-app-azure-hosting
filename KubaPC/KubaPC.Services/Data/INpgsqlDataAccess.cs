using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Data
{
    public interface INpgsqlDataAccess
    {
        //select returning multiple rows
        Task<List<T>> QueryAsync<T>(string sql, object? parameters = null);

        //select returning one row , if null = not found
        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null);

        /// Runs an INSERT or UPDATE or DELETE and returns the number of rows affected.
        Task<int> ExecuteAsync(string sql, object? parameters = null);
    }
}
