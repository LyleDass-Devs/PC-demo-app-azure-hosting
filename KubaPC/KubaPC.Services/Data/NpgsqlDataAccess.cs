using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace KubaPC.Services.Data
{
    public sealed class NpgsqlDataAccess : INpgsqlDataAccess
    {
        private const string CONNECTION_STRING_NAME = "KubaPCDatabase";
        private readonly string _connectionString;

        static NpgsqlDataAccess()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
        }

        public NpgsqlDataAccess(IConfiguration configuration)
        {
            string? connectionString = configuration.GetConnectionString(CONNECTION_STRING_NAME);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{CONNECTION_STRING_NAME}' is missing or empty. " + "On a developer machine it belongs in User Secrets on KubaPC.API");
            }

            _connectionString = connectionString;
        }

        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);

            IEnumerable<T> rows = await connection.QueryAsync<T>(sql, parameters, commandType: CommandType.Text);
            return rows.AsList();
        }

        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, commandType: CommandType.Text);
        }

        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            await using var connection = new NpgsqlConnection(_connectionString);
            return await connection.ExecuteAsync(sql, parameters, commandType: CommandType.Text);
        }
    }
}
