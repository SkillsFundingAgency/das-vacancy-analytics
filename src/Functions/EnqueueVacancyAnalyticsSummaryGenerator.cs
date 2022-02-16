using System;
using System.Linq;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Functions.Models;
using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Functions
{
    public class EnqueueVacancyAnalyticsSummaryGenerator
    {
        private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string QueueStorageConnStringKey = "QueueStorage";
        private readonly ILogger<EnqueueVacancyAnalyticsSummaryGenerator> _log;
        private readonly IConfiguration _config;
        private readonly IHostingEnvironment _hostingEnvironment;

        public EnqueueVacancyAnalyticsSummaryGenerator(ILogger<EnqueueVacancyAnalyticsSummaryGenerator> log, IConfiguration config, IHostingEnvironment hostingEnvironment)
        {
            _log = log;
            _config = config;
            _hostingEnvironment = hostingEnvironment;
        }

        [FunctionName("EnqueueVacancyAnalyticsSummaryGenerator")]
        public async Task Run([TimerTrigger("0 1 * * * *")] TimerInfo myTimer, ILogger log, ExecutionContext context,
            [Queue(GlobalConstants.GenerateVacancyAnalyticsQueueName), StorageAccount("QueueStorage")] ICollector<string> VacancyReferenceStorageQueue)
        {
            log.LogInformation($"C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function executed at: {DateTime.UtcNow}");

            var reader = new VacancyEventStoreReader(_config.GetConnectionString(VacancyEventStoreConnStringKey), log, _hostingEnvironment);

            var vacancyRefs = await reader.GetRecentlyAffectedVacanciesAsync(lastNoOfHours: 1);

            vacancyRefs.ToList().ForEach(vacancyReference => VacancyReferenceStorageQueue.Add(JsonConvert.SerializeObject(new { VacancyReference = vacancyReference })));

            log.LogInformation($"Finished C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function completed at: {DateTime.UtcNow}");
        }
    }
}