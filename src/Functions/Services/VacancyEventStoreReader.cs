using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;

namespace Esfa.VacancyAnalytics.Functions.Services
{
    public class VacancyEventStoreReader : VacancyEventStore, IVacancyEventStoreReader
    {
        private readonly string _vacancyEventStoreConnString;
        private const string RecentlyAffectedVacanciesSproc = "[VACANCY].[Event_GET_RecentlyAffectedVacancies]";

        public VacancyEventStoreReader(string vacancyEventStoreConnString, ILogger log) : base(log)
        {
            _vacancyEventStoreConnString = vacancyEventStoreConnString;
        }

        public async Task<IEnumerable<long>> GetRecentlyAffectedVacanciesAsync(int lastNoOfHours)
        {
            var vacancyRefs = new List<long>();

            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {
                AzureServiceTokenProvider tokenProvider = new AzureServiceTokenProvider();
                string accessToken = await tokenProvider.GetAccessTokenAsync("https://database.windows.net/");
                conn.AccessToken = accessToken;
                await conn.OpenAsync();

                using (var command = conn.CreateCommand())
                {
                    command.CommandType = CommandType.StoredProcedure;
                    command.CommandText = RecentlyAffectedVacanciesSproc;

                    var inputParam = command.Parameters.AddWithValue("@LastNoOfHours", lastNoOfHours);

                    using (var reader = await _retryPolicy.ExecuteAsync(async context => await command.ExecuteReaderAsync(), new Context(nameof(GetRecentlyAffectedVacanciesAsync))))
                    {
                        while (await reader.ReadAsync())
                        {
                            vacancyRefs.Add(reader.GetInt64(0));
                        }
                    }
                }
            }

            return vacancyRefs;
        }
    }
}
