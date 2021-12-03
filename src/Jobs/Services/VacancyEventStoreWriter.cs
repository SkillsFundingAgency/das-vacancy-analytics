using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;

namespace Esfa.VacancyAnalytics.Jobs.Services
{
    public class VacancyEventStoreWriter : VacancyEventStore
    {
        private readonly string _vacancyEventStoreConnString;
        private readonly bool _isDevEnvironment;
        private const string VacancyEventsBatchInsertSproc = "[VACANCY].[Event_INSERT_BatchEvents]";
        private const string AzureResource = "https://database.windows.net/";

        public VacancyEventStoreWriter(string vacancyEventStoreConnString, ILogger log, bool isDevEnvironment) : base(log)
        {
            _vacancyEventStoreConnString = vacancyEventStoreConnString;
            _isDevEnvironment = isDevEnvironment;
        }

        public async Task SaveEventDataAsync(DataTable dt)
        {
            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {
                if (!_isDevEnvironment)
                {
                    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                    conn.AccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(AzureResource);
                }

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
