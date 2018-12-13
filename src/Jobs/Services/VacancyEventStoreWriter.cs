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

        public VacancyEventStoreWriter(string vacancyEventStoreConnString, ILogger log) : base(log)
        {
            _vacancyEventStoreConnString = vacancyEventStoreConnString;
        }

        public async Task SaveEventDataAsync(DataTable dt)
        {
            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {
                await conn.OpenAsync();

                var command = conn.CreateCommand();

                command.CommandType = CommandType.StoredProcedure;
                command.CommandText = VacancyEventsBatchInsertSproc;

                var inputParam = command.Parameters.AddWithValue("@VE", dt);
                inputParam.SqlDbType = SqlDbType.Structured;

                await _retryPolicy.ExecuteAsync(async context => await command.ExecuteNonQueryAsync(), new Context(nameof(SaveEventDataAsync)));
            }
        }
    }
}
