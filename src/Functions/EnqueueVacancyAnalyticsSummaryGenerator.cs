using System;
using System.Collections.Generic;
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
            [Queue(GlobalConstants.GenerateVacancyAnalyticsQueueName), StorageAccount("QueueStorage")] IAsyncCollector<string> vacancyReferenceStorageQueue)
        {
            log.LogInformation($"C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function executed at: {DateTime.UtcNow}");

            var reader = new VacancyEventStoreReader(_config.GetConnectionString(VacancyEventStoreConnStringKey), log, _hostingEnvironment);

            var vacancyRefs = (await reader.GetRecentlyAffectedVacanciesAsync(lastNoOfHours: 1)).ToList();

            var enqueueTasks = new List<Task>();
            var sendCounter = 0;
            foreach (var vacancyRef in vacancyRefs)
            {
                enqueueTasks.Add(vacancyReferenceStorageQueue.AddAsync(JsonConvert.SerializeObject(new { VacancyReference = vacancyRef })));
                sendCounter++;

                if (sendCounter % 1000 == 0)
                {
                    await Task.WhenAll(enqueueTasks);
                    log.LogInformation($"Queued {sendCounter} of {vacancyRefs.Count} messages.");
                    enqueueTasks.Clear();
                    await Task.Delay(250);
                }
            }

            // await final tasks not % 1000
            await Task.WhenAll(enqueueTasks);

            log.LogInformation($"Finished C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function completed at: {DateTime.UtcNow}");
        }
    }
}