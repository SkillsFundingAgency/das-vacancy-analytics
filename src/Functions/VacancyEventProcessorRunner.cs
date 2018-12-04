using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Text;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics.Events;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;
using Polly;

namespace Esfa.VacancyAnalytics.Functions
{
    public static class VacancyEventProcessorRunner
    {
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";
        private const string EventHubName = "vacancy";
        private const string StorageContainerName = "vacancy-event-processor";
        //private const string StorageAccountName = "Storage account name";
        //private const string StorageAccountKey = "Storage account key";

        private const string LocalSettingsFileName = "local.settings.json";
        //private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string QueueStorageConnStringKey = "QueueStorage";
        public const string GenerateVacancyAnalyticsQueueName = "generate-vacancy-analytics-summary";
        //private const string VacancyEventTableIdentifier = "[VACANCY].[Event]";
        //private static string _vacancyEventStoreConnString;
        private static string _queueStorageConnString;
        //private static Polly.Retry.RetryPolicy _bulkCopyRetryPolicy;
        private static ILogger _log;


        [FunctionName("VacancyEventProcessorRunner")]
        public static async Task Run([TimerTrigger("0 */2 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
        {
            _log = log;
            _log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var vacancyEventHubConnString = config.GetConnectionStringOrSetting(VacancyEventHubConnStringKey);
            _queueStorageConnString = config.GetConnectionString(QueueStorageConnStringKey);

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                vacancyEventHubConnString,
                _queueStorageConnString,
                StorageContainerName);

            // Registers the Event Processor Host and starts receiving messages
            var opts = new EventProcessorOptions
            {
                PrefetchCount = 512,
                MaxBatchSize = 256
            };

            opts.SetExceptionHandler(HandleEventProcessorException);
//eventProcessorHost.PartitionManagerOptions.RenewInterval
            await eventProcessorHost.RegisterEventProcessorFactoryAsync(new EventProcessorFactory(config, _log), opts);
            //await eventProcessorHost.RegisterEventProcessorAsync<SimpleEventProcessor>(opts);
            //await eventProcessorHost.UnregisterEventProcessorAsync();
        }

        private static void HandleEventProcessorException(ExceptionReceivedEventArgs args)
        {
            _log.LogError(args.Exception, $"Error occured processing vacancy events from partition: {args.PartitionId}.");
        }
    }
}