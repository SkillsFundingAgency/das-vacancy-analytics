using System;
using System.Linq;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Functions
{
    public class EnqueueVacancyAnalyticsSummaryGenerator
    {
        private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string QueueStorageConnStringKey = "QueueStorage";
        private readonly ILogger<EnqueueVacancyAnalyticsSummaryGenerator> _log;
        private readonly IConfiguration _config;

        public EnqueueVacancyAnalyticsSummaryGenerator(ILogger<EnqueueVacancyAnalyticsSummaryGenerator> log, IConfiguration config)
        {
            _log = log;
            _config = config;
        }

        [FunctionName("EnqueueVacancyAnalyticsSummaryGenerator")]
        public async Task Run([TimerTrigger("0 1 * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            log.LogInformation($"C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function executed at: {DateTime.UtcNow}");

            var reader = new VacancyEventStoreReader(_config.GetConnectionString(VacancyEventStoreConnStringKey), log);

            var queue = new VacancyAnalyticsQueueStorageWriter(_config.GetConnectionString(QueueStorageConnStringKey));

            var vacancyRefs = await reader.GetRecentlyAffectedVacanciesAsync(lastNoOfHours: 1);
            await Task.WhenAll(vacancyRefs.Select(vr => queue.QueueVacancyAsync(vr)));

            log.LogInformation($"Finished C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function completed at: {DateTime.UtcNow}");
        }
    }
}
