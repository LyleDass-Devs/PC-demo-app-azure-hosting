using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Data
{
    public interface INpgsqlDataAccess
    {

        Task<List<T>> QueryAsync<T>(string sql, object? parameters = null);

        Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null);

        Task<int> ExecuteAsync(string sql, object? parameters = null);
    }
}
