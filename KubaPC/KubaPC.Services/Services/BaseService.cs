using System.Reflection;
using System.Text.RegularExpressions;

using Dapper;
using KubaPC.Services.Data;

namespace KubaPC.Services.Services
{
    
    public abstract class BaseService
    {
        // Audit columns the database or the update logic own - excluded from generated SQL by default.
        private static readonly HashSet<string> DEFAULT_COLUMNS = new()
        {
            "IsActive",
            "CreatedAt",
            "CreatedBy",
            "ModifiedAt",
            "ModifiedBy"
        };

        protected readonly INpgsqlDataAccess _dataAccess;

        protected BaseService(INpgsqlDataAccess dataAccess)
        {
            _dataAccess = dataAccess;
        }

        protected static DynamicParameters CreateDynamicParameters<T>(T model, bool includeId = false, bool includeAuditColumns = false)
        {
            DynamicParameters parameters = new();

            foreach (PropertyInfo property in ListMappedProperties(model, includeId, includeAuditColumns))
            {
                parameters.Add(property.Name, property.GetValue(model));
            }

            return parameters;
        }

        // tableName must always be a hardcoded literal, never input.
        protected static string CreateSqlInsert<T>(T model, string tableName, bool returningId = false, bool includeId = false, bool includeAuditColumns = false)
        {
            List<string> names = ListMappedProperties(model, includeId, includeAuditColumns)
                .Select(p => p.Name)
                .ToList();

            string columns = string.Join(", ", names.Select(ToSnakeCase));
            string values = string.Join(", ", names.Select(n => $"@{n}"));

            string sql = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

            return returningId ? sql + " RETURNING id" : sql;
        }

        protected static string CreateSqlUpdate<T>(T model, string tableName)
        {
            List<string> names = ListMappedProperties(model, includeId: false, includeAuditColumns: false)
                .Select(p => p.Name)
                .ToList();

            List<string> assignments = names.Select(n => $"{ToSnakeCase(n)} = @{n}").ToList();
            assignments.Add("modified_at = now()");
            assignments.Add("modified_by = @ModifiedBy");

            return $"UPDATE {tableName} SET {string.Join(", ", assignments)} WHERE id = @Id";
        }

        protected static string CreateSqlSoftDelete(string tableName)
        {
            return $"UPDATE {tableName} SET is_active = FALSE, modified_at = now(), modified_by = @ModifiedBy WHERE id = @Id";
        }

        // Not callable now: kubapc_app holds no DELETE right on any table. Kept so the option exists.
        protected static string CreateSqlHardDelete(string tableName)
        {
            return $"DELETE FROM {tableName} WHERE id = @Id";
        }

        private static List<PropertyInfo> ListMappedProperties<T>(T model, bool includeId, bool includeAuditColumns)
        {
            if (model is null)
            {
                throw new ArgumentNullException(nameof(model));
            }

            return model.GetType()
                .GetProperties()
                .Where(p => (includeId || p.Name != "Id")
                         && (includeAuditColumns || !DEFAULT_COLUMNS.Contains(p.Name)))
                .ToList();
        }

        
        private static string ToSnakeCase(string propertyName)
        {
            return Regex.Replace(propertyName, "(?<=[a-z0-9])[A-Z]", "_$0").ToLowerInvariant();
        }
    }
}
