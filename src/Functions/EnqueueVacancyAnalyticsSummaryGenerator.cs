using System;
using System.Linq;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Functions
{
    public static class EnqueueVacancyAnalyticsSummaryGenerator
    {
        private const string LocalSettingsFileName = "local.settings.json";
        private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string QueueStorageConnStringKey = "QueueStorage";

        private static IConfigurationRoot _config;
        private static object _syncRoot = new object();

        [FunctionName("EnqueueVacancyAnalyticsSummaryGenerator")]
        public static async Task Run([TimerTrigger("0 1 * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function executed at: {DateTime.UtcNow}");

            var config = GetConfiguration(context.FunctionAppDirectory);

            var reader = new VacancyEventStoreReader(config.GetConnectionString(VacancyEventStoreConnStringKey), log);

            var queue = new QueueStorageWriter(config.GetConnectionString(QueueStorageConnStringKey));

            var vacancyRefs = await reader.GetRecentlyAffectedVacanciesAsync(lastNoOfHours: 1);
            await Task.WhenAll(vacancyRefs.Select(vr => queue.QueueVacancyAsync(vr)));

            log.LogInformation($"Finished C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function finished at: {DateTime.UtcNow}");
        }

        private static IConfigurationRoot GetConfiguration(string functionAppDirectory)
        {
            if (_config != null)
            {
                return _config;
            }

            lock (_syncRoot)
            {
                if (_config == null)
                {
                    _config = new ConfigurationBuilder()
                                .SetBasePath(functionAppDirectory)
                                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();
                }
            }

            return _config;
        }
    }
}
