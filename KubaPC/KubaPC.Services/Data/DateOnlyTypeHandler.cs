using Dapper;
using System.Data;

namespace KubaPC.Services.Data
{
    /// <summary>
    /// Lets Dapper pass and read <see cref="DateOnly"/> for Postgres date columns. Dapper knows
    /// DateTime but not DateOnly, and a date-only column must never carry a time of day.
    /// </summary>
    public sealed class DateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.ToDateTime(TimeOnly.MinValue);
        }

        public override DateOnly Parse(object value) => value switch
        {
            DateOnly date => date,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => DateOnly.Parse(value.ToString()!)
        };
    }

    /// <summary>Same as <see cref="DateOnlyTypeHandler"/> for nullable date columns.</summary>
    public sealed class NullableDateOnlyTypeHandler : SqlMapper.TypeHandler<DateOnly?>
    {
        public override void SetValue(IDbDataParameter parameter, DateOnly? value)
        {
            parameter.DbType = DbType.Date;
            parameter.Value = value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : DBNull.Value;
        }

        public override DateOnly? Parse(object value) => value switch
        {
            null or DBNull => null,
            DateOnly date => date,
            DateTime dateTime => DateOnly.FromDateTime(dateTime),
            _ => DateOnly.Parse(value.ToString()!)
        };
    }
}
