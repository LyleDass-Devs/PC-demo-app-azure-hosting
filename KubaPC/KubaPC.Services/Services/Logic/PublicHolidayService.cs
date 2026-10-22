using Dapper;
using KubaPC.Models.Contracts.Common.Responses;
using KubaPC.Models.Contracts.PublicHolidays.Requests;
using KubaPC.Models.Contracts.PublicHolidays.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;
using System.Data;

namespace KubaPC.Services.Services.Logic
{
    public class PublicHolidayService : BaseService, IPublicHolidayService
    {
        private static readonly Dictionary<string, string> SORTABLE_COLUMNS = new(StringComparer.OrdinalIgnoreCase)
        {
            ["holidayDate"] = "h.holiday_date",
            ["holidayName"] = "h.holiday_name"
        };

        public PublicHolidayService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<PagedResponse<PublicHolidayResponse>> SelectPaged(PublicHolidayListRequest request)
        {
            string sortColumn = SORTABLE_COLUMNS.TryGetValue(request.SortBy ?? string.Empty, out string? mapped) ? mapped : "h.holiday_date";
            string sortDirection = request.SortDescending ? "DESC" : "ASC";

            string sql = $@"SELECT h.id, h.holiday_date, h.holiday_name, h.is_active,
                                   count(*) OVER() AS total_count
                            FROM public_holidays h
                            WHERE (@Year IS NULL OR extract(year FROM h.holiday_date) = @Year)
                              AND (@Search IS NULL OR h.holiday_name ILIKE @SearchPattern)
                              AND (@IsActive IS NULL OR h.is_active = @IsActive)
                            ORDER BY {sortColumn} {sortDirection}
                            LIMIT @PageSize OFFSET @Offset";

            DynamicParameters parameters = new();
            parameters.Add("Year", request.Year, DbType.Int32);
            parameters.Add("Search", request.Search, DbType.String);
            parameters.Add("SearchPattern", $"%{request.Search}%", DbType.String);
            parameters.Add("IsActive", request.IsActive, DbType.Boolean);
            parameters.Add("PageSize", request.PageSize, DbType.Int32);
            parameters.Add("Offset", (request.Page - 1) * request.PageSize, DbType.Int32);

            List<PublicHolidayRowModel> rows = await _dataAccess.QueryAsync<PublicHolidayRowModel>(sql, parameters);

            long itemCount = rows.Count > 0 ? rows[0].TotalCount : 0;

            return new PagedResponse<PublicHolidayResponse>
            {
                Page = request.Page,
                PageSize = request.PageSize,
                ItemCount = itemCount,
                PageCount = (int)Math.Ceiling(itemCount / (double)request.PageSize),
                Data = rows.Select(row => new PublicHolidayResponse
                {
                    Id = row.Id,
                    HolidayDate = row.HolidayDate,
                    HolidayName = row.HolidayName,
                    IsActive = row.IsActive
                }).ToList()
            };
        }

        public async Task<List<int>> SelectYears()
        {
            const string sql = @"SELECT DISTINCT extract(year FROM holiday_date)::int AS year
                                 FROM public_holidays
                                 WHERE is_active
                                 ORDER BY year DESC";

            return await _dataAccess.QueryAsync<int>(sql, new { });
        }

        public async Task<CreateResult> Create(CreatePublicHolidayRequest request, Guid actor)
        {
            if (await IsDateTaken(request.HolidayDate, null))
            {
                return CreateResult.Failure($"{request.HolidayDate:d MMMM yyyy} is already on the calendar.");
            }

            const string sql = @"INSERT INTO public_holidays (holiday_date, holiday_name, created_by)
                                 VALUES (@HolidayDate, @HolidayName, @Actor)
                                 RETURNING id";

            Guid id = await _dataAccess.QuerySingleOrDefaultAsync<Guid>(sql, new
            {
                request.HolidayDate,
                HolidayName = request.HolidayName.Trim(),
                Actor = actor
            });

            return CreateResult.Success(id);
        }

        public async Task<OperationResult> Update(UpdatePublicHolidayRequest request, Guid actor)
        {
            if (await IsDateTaken(request.HolidayDate, request.Id))
            {
                return OperationResult.Failure($"{request.HolidayDate:d MMMM yyyy} is already on the calendar.");
            }

            const string sql = @"UPDATE public_holidays
                                 SET holiday_date = @HolidayDate, holiday_name = @HolidayName,
                                     modified_at = now(), modified_by = @Actor
                                 WHERE id = @Id AND is_active";

            int updated = await _dataAccess.ExecuteAsync(sql, new
            {
                request.Id,
                request.HolidayDate,
                HolidayName = request.HolidayName.Trim(),
                Actor = actor
            });

            return updated == 0
                ? OperationResult.Failure("No active public holiday was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> Deactivate(Guid holidayId, Guid actor)
        {
            const string sql = @"UPDATE public_holidays
                                 SET is_active = FALSE, modified_at = now(), modified_by = @Actor
                                 WHERE id = @HolidayId AND is_active";

            int updated = await _dataAccess.ExecuteAsync(sql, new { HolidayId = holidayId, Actor = actor });

            return updated == 0
                ? OperationResult.Failure("No active public holiday was found with that id.")
                : OperationResult.Success();
        }

        public async Task<OperationResult> Reactivate(Guid holidayId, Guid actor)
        {
            const string dateSql = @"SELECT holiday_date FROM public_holidays WHERE id = @HolidayId AND NOT is_active";
            DateOnly? holidayDate = await _dataAccess.QuerySingleOrDefaultAsync<DateOnly?>(dateSql, new { HolidayId = holidayId });

            if (holidayDate is null)
            {
                return OperationResult.Failure("No deactivated public holiday was found with that id.");
            }

            if (await IsDateTaken(holidayDate.Value, holidayId))
            {
                return OperationResult.Failure($"Another holiday now uses {holidayDate.Value:d MMMM yyyy}.");
            }

            const string sql = @"UPDATE public_holidays
                                 SET is_active = TRUE, modified_at = now(), modified_by = @Actor
                                 WHERE id = @HolidayId AND NOT is_active";

            await _dataAccess.ExecuteAsync(sql, new { HolidayId = holidayId, Actor = actor });

            return OperationResult.Success();
        }

        private async Task<bool> IsDateTaken(DateOnly holidayDate, Guid? exceptId)
        {
            const string sql = @"SELECT count(*)
                                 FROM public_holidays
                                 WHERE holiday_date = @HolidayDate AND is_active
                                   AND (@ExceptId IS NULL OR id <> @ExceptId)";

            DynamicParameters parameters = new();
            parameters.Add("HolidayDate", holidayDate, DbType.Date);
            parameters.Add("ExceptId", exceptId, DbType.Guid);

            return await _dataAccess.QuerySingleOrDefaultAsync<long>(sql, parameters) > 0;
        }

        private sealed class PublicHolidayRowModel
        {
            public Guid Id { get; set; }
            public DateOnly HolidayDate { get; set; }
            public string HolidayName { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public long TotalCount { get; set; }
        }
    }
}
