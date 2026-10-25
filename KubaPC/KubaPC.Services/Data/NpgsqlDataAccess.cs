using System;
using System.Data;
using System.Collections.Generic;
using System.Text;

using Azure.Core;
using Azure.Identity;
using Dapper;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace KubaPC.Services.Data
{
    /// <summary>
    /// Thin Dapper wrapper over a single <see cref="NpgsqlDataSource"/>. One instance per process so that
    /// there is one connection pool and, in Entra ID mode, one token refresh loop.
    /// </summary>
    public sealed class NpgsqlDataAccess : INpgsqlDataAccess, IDisposable
    {
        private const string CONNECTION_STRING_NAME = "KubaPCDatabase";

        // Scope that Azure Database for PostgreSQL accepts as a password when Entra ID authentication is on.
        private const string ENTRA_ID_TOKEN_SCOPE = "https://ossrdbms-aad.database.windows.net/.default";

        // Tokens live about an hour; refresh with a margin, and retry quickly after a failed fetch so a
        // transient identity outage does not leave the pool handing out expired passwords for long.
        private static readonly TimeSpan TOKEN_REFRESH_INTERVAL = TimeSpan.FromMinutes(50);
        private static readonly TimeSpan TOKEN_RETRY_INTERVAL = TimeSpan.FromSeconds(10);

        private readonly NpgsqlDataSource _dataSource;

        static NpgsqlDataAccess()
        {
            DefaultTypeMap.MatchNamesWithUnderscores = true;
            SqlMapper.AddTypeHandler(new DateOnlyTypeHandler());
            SqlMapper.AddTypeHandler(new NullableDateOnlyTypeHandler());
        }

        /// <summary>
        /// Builds the data source from the <c>KubaPCDatabase</c> connection string, switching on
        /// <see cref="DatabaseAuthenticationModeResolver"/> to decide whether the password comes from the
        /// connection string or from a managed identity token.
        /// </summary>
        /// <param name="configuration">The application configuration.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the connection string is missing or empty.</exception>
        public NpgsqlDataAccess(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            string? connectionString = configuration.GetConnectionString(CONNECTION_STRING_NAME);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException($"Connection string '{CONNECTION_STRING_NAME}' is missing or empty. " + "On a developer machine it belongs in User Secrets on KubaPC.API");
            }

            DatabaseAuthenticationMode mode = DatabaseAuthenticationModeResolver.Resolve(configuration);
            _dataSource = BuildDataSource(connectionString, mode);
        }

        /// <summary>
        /// Runs <paramref name="sql"/> and maps every row to <typeparamref name="T"/>.
        /// </summary>
        public async Task<List<T>> QueryAsync<T>(string sql, object? parameters = null)
        {
            await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync();

            IEnumerable<T> rows = await connection.QueryAsync<T>(sql, parameters, commandType: CommandType.Text);
            return rows.AsList();
        }

        /// <summary>
        /// Runs <paramref name="sql"/> and maps the single row to <typeparamref name="T"/>, or returns the default
        /// when there is none.
        /// </summary>
        public async Task<T?> QuerySingleOrDefaultAsync<T>(string sql, object? parameters = null)
        {
            await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync();
            return await connection.QuerySingleOrDefaultAsync<T>(sql, parameters, commandType: CommandType.Text);
        }

        /// <summary>
        /// Runs <paramref name="sql"/> and returns the number of rows affected.
        /// </summary>
        public async Task<int> ExecuteAsync(string sql, object? parameters = null)
        {
            await using NpgsqlConnection connection = await _dataSource.OpenConnectionAsync();
            return await connection.ExecuteAsync(sql, parameters, commandType: CommandType.Text);
        }

        /// <summary>
        /// Closes the connection pool and stops the token refresh loop.
        /// </summary>
        public void Dispose()
        {
            _dataSource.Dispose();
        }

        private static NpgsqlDataSource BuildDataSource(string connectionString, DatabaseAuthenticationMode mode)
        {
            NpgsqlDataSourceBuilder builder = new(connectionString);

            if (mode == DatabaseAuthenticationMode.EntraId)
            {
                // The credential is created once so its own token cache is shared by every refresh.
                DefaultAzureCredential credential = new();
                TokenRequestContext tokenRequest = new(new[] { ENTRA_ID_TOKEN_SCOPE });

                builder.UsePeriodicPasswordProvider(
                    async (_, cancellationToken) =>
                    {
                        AccessToken token = await credential.GetTokenAsync(tokenRequest, cancellationToken);
                        return token.Token;
                    },
                    TOKEN_REFRESH_INTERVAL,
                    TOKEN_RETRY_INTERVAL);
            }

            return builder.Build();
        }
    }
}
