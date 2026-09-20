using KubaPC.Models.Contracts.ReferenceUrls.Requests;
using KubaPC.Models.Contracts.ReferenceUrls.Responses;
using KubaPC.Models.Internal;
using KubaPC.Services.Data;
using KubaPC.Services.Services.Interfaces;

namespace KubaPC.Services.Services.Logic
{
    public class ReferenceUrlService : BaseService, IReferenceUrlService
    {
        private const string ELEVATE = "BEGIN; SET LOCAL ROLE kubapc_administrator; ";
        private const string COMMIT = "; COMMIT;";

        public ReferenceUrlService(INpgsqlDataAccess dataAccess) : base(dataAccess)
        {
        }

        public async Task<List<ReferenceUrlResponse>> SelectAll()
        {
            // Every key is listed, with its address when one has been saved, so the screen always shows both rows.
            const string sql = @"SELECT k.key_value AS url_key, r.url_value, r.modified_at
                                 FROM url_keys k
                                 LEFT JOIN reference_urls r ON r.url_key = k.key_value AND r.is_active
                                 ORDER BY k.display_order";

            return await _dataAccess.QueryAsync<ReferenceUrlResponse>(sql, new { });
        }

        public async Task<OperationResult> Save(SaveReferenceUrlRequest request, Guid actor)
        {
            const string keySql = @"SELECT count(*) FROM url_keys WHERE key_value = @UrlKey";
            long keyCount = await _dataAccess.QuerySingleOrDefaultAsync<long>(keySql, new { request.UrlKey });

            if (keyCount == 0)
            {
                return OperationResult.Failure("That is not one of the reference links Form D prints.");
            }

            const string sql = ELEVATE + @"
                WITH updated AS (
                    UPDATE reference_urls
                    SET url_value = @UrlValue, modified_at = now(), modified_by = @Actor
                    WHERE url_key = @UrlKey AND is_active
                    RETURNING id
                )
                INSERT INTO reference_urls (url_key, url_value, created_by)
                SELECT @UrlKey, @UrlValue, @Actor
                WHERE NOT EXISTS (SELECT 1 FROM updated)" + COMMIT;

            await _dataAccess.ExecuteAsync(sql, new
            {
                request.UrlKey,
                UrlValue = request.UrlValue.Trim(),
                Actor = actor
            });

            return OperationResult.Success();
        }
    }
}
