using System;
using System.Threading.Tasks;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Esfa.VacancyAnalytics.Functions.Services;

namespace Esfa.VacancyAnalytics.Functions
{
    public static class VacancyEventProcessorRunner
    {
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";
        private const string EventHubName = "vacancy";
        private const string StorageContainerName = "vacancy-event-processor";
        private const string LocalSettingsFileName = "local.settings.json";
        private const string QueueStorageConnStringKey = "QueueStorage";
        private static ILogger _log;

        [FunctionName("VacancyEventProcessorRunner")]
        public static async Task Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            _log = log;
            _log.LogInformation($"C# Timer trigger function executed at: {DateTime.UtcNow}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var vacancyEventHubConnString = config.GetConnectionStringOrSetting(VacancyEventHubConnStringKey);
            var queueStorageConnString = config.GetConnectionString(QueueStorageConnStringKey);

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                vacancyEventHubConnString,
                queueStorageConnString,
                StorageContainerName);

            var opts = new EventProcessorOptions
            {
                PrefetchCount = 512,
                MaxBatchSize = 256
            };

            opts.SetExceptionHandler(HandleEventProcessorException);
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(config, _log), opts);
        }

        private static void HandleEventProcessorException(ExceptionReceivedEventArgs args)
        {
            _log.LogError(args.Exception, $"Error occured processing vacancy events from partition: {args.PartitionId}.");
        }
    }
}