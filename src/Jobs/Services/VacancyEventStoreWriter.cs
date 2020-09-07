using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;

namespace Esfa.VacancyAnalytics.Jobs.Services
{
    public class VacancyEventStoreWriter : VacancyEventStore
    {
        private readonly string _vacancyEventStoreConnString;
        private const string VacancyEventsBatchInsertSproc = "[VACANCY].[Event_INSERT_BatchEvents]";
        private readonly string _accessToken;

        public VacancyEventStoreWriter(string vacancyEventStoreConnString, string accessToken, ILogger log) : base(log)
        {
            _vacancyEventStoreConnString = vacancyEventStoreConnString;
            _accessToken = accessToken;
        }

        public async Task SaveEventDataAsync(DataTable dt)
        {
            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {
                conn.AccessToken = _accessToken;
                await conn.OpenAsync();

                using (var command = conn.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = VacancyEventsBatchInsertSproc;

                    var inputParam = command.Parameters.AddWithValue("@VE", dt);
                    inputParam.SqlDbType = SqlDbType.Structured;

                    await _retryPolicy.ExecuteAsync(async context => await command.ExecuteNonQueryAsync(), new Context(nameof(SaveEventDataAsync)));
                }
            }
        }
    }
}
