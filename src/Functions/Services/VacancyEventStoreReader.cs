using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.Services.AppAuthentication;
using Microsoft.Extensions.Logging;
using Polly;

namespace Esfa.VacancyAnalytics.Functions.Services
{
    public class VacancyEventStoreReader : VacancyEventStore, IVacancyEventStoreReader
    {
        private readonly string _vacancyEventStoreConnString;
        private const string RecentlyAffectedVacanciesSproc = "[VACANCY].[Event_GET_RecentlyAffectedVacancies]";
        private IHostingEnvironment _environment;
        private const string AzureResource = "https://database.windows.net/";

        public VacancyEventStoreReader(string vacancyEventStoreConnString, ILogger log, IHostingEnvironment environment) : base(log)
        {
            _vacancyEventStoreConnString = vacancyEventStoreConnString;
            _environment = environment;
        }

        public async Task<IEnumerable<long>> GetRecentlyAffectedVacanciesAsync(int lastNoOfHours)
        {
            var vacancyRefs = new List<long>();

            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {

                if (!_environment.IsDevelopment())
                {
                    AzureServiceTokenProvider azureServiceTokenProvider = new AzureServiceTokenProvider();
                    conn.AccessToken = await azureServiceTokenProvider.GetAccessTokenAsync(AzureResource);
                }

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
