using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using Microsoft.Azure.EventHubs;
using System;
using System.Linq;
using Newtonsoft.Json;
using System.Text;
using Esfa.Vacancy.Analytics;
using Esfa.Vacancy.Analytics.Events;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;
using Microsoft.WindowsAzure.Storage;
using System.Collections.Generic;
using System.Data.Common;
using Polly;
using Microsoft.Azure.EventHubs.Processor;

namespace Esfa.VacancyAnalytics.Functions
{
    public static class IngestVacancyEvents
    {
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";
        private const string EventHubName = "vacancy";
        private const string LocalSettingsFileName = "local.settings.json";
        private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string QueueStorageConnStringKey = "QueueStorage";
        public const string GenerateVacancyAnalyticsQueueName = "generate-vacancy-analytics-summary";
        private const string VacancyEventTableIdentifier = "[VACANCY].[Event]";
        private static string _vacancyEventStoreConnString;
        private static string _queueStorageConnString;
        private static Polly.Retry.RetryPolicy _bulkCopyRetryPolicy;

        [FunctionName("IngestVacancyEvents")]
        //public static async Task Run([EventHubTrigger(EventHubName, Connection = VacancyEventHubConnStringKey)]EventData[] eventDataSet, ILogger log, PartitionContext partitionContext, ExecutionContext context)
        public static async Task Run(EventData[] eventDataSet, ILogger log, PartitionContext partitionContext, ExecutionContext context)
        {
            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                .AddUserSecrets("")
                .AddEnvironmentVariables()
                .Build();
//var last = partitionContext.RuntimeInformation.LastEnqueuedOffset;partitionContext.
            _bulkCopyRetryPolicy = GetRetryPolicy(log);
            _vacancyEventStoreConnString = config.GetConnectionString(VacancyEventStoreConnStringKey);
            _queueStorageConnString = config.GetConnectionStringOrSetting(QueueStorageConnStringKey);
            log.LogInformation(_vacancyEventStoreConnString);

            log.LogInformation($"{DateTime.Now.ToString("HH':'mm':'ss.fff")} - No of events in batch to process: {eventDataSet.Length}");

            var eventData = BuildVacancyEventDataTable();

            foreach (var evt in eventDataSet)
            {
                try
                {
                    var actualEvt = GetVacancyEvent(evt);
                    eventData.Rows.Add(actualEvt.PublisherId, actualEvt.EventTime, actualEvt.VacancyReference, actualEvt.EventType);
                }
                catch (Exception ex)
                {
                    log.LogError(ex, $"An error occurred extracting data from the event hub message {evt.SystemProperties["SequenceNumber"]}");
                }
            }

            eventData.AcceptChanges();

            try
            {
                await SaveEventDataAsync(eventData);
            }
            catch (Exception ex)
            {
                log.LogError(ex, "an error");
            }

            var distinctVacancyReferences = GetDistinctVacancyReferencesFromTable(eventData);

            try
            {
                foreach (var item in distinctVacancyReferences)
                {
                    await PublishVacancyToQueueAsync(item);
                }
            }
            catch (Exception ex)
            {
                log.LogError(ex, $"an error occurred queuing a vacancy on to the {GenerateVacancyAnalyticsQueueName} storage queue.");
            }

            log.LogInformation($"{DateTime.Now.ToString("HH':'mm':'ss.fff")} - Finished processing batch of size: {eventDataSet.Length}.");
        }

        private static DataTable BuildVacancyEventDataTable()
        {
            var eventData = new DataTable("VacancyEvent");

            eventData.Columns.Add(nameof(VacancyEvent.PublisherId));
            eventData.Columns.Add(nameof(VacancyEvent.EventTime));
            eventData.Columns.Add(nameof(VacancyEvent.VacancyReference));
            eventData.Columns.Add(nameof(VacancyEvent.EventType));

            return eventData;
        }

        private static VacancyEvent GetVacancyEvent(EventData evt)
        {
            var body = Encoding.UTF8.GetString(evt.Body.Array);

            switch (evt.Properties["Type"].ToString())
            {
                case nameof(ApprenticeshipSearchImpressionEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipSearchImpressionEvent>(body);
                case nameof(ApprenticeshipSavedSearchAlertImpressionEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipSavedSearchAlertImpressionEvent>(body);
                case nameof(ApprenticeshipBookmarkedImpressionEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipBookmarkedImpressionEvent>(body);
                case nameof(ApprenticeshipDetailImpressionEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipDetailImpressionEvent>(body);
                case nameof(ApprenticeshipApplicationCreatedEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipApplicationCreatedEvent>(body);
                case nameof(ApprenticeshipApplicationSubmittedEvent):
                    return JsonConvert.DeserializeObject<ApprenticeshipApplicationSubmittedEvent>(body);
            }

            return null;
        }

        private static async Task SaveEventDataAsync(DataTable dt)
        {
            using (var conn = new SqlConnection(_vacancyEventStoreConnString))
            {
                await conn.OpenAsync();

                var transaction = conn.BeginTransaction();

                using (var bulkCopy = new SqlBulkCopy(conn, SqlBulkCopyOptions.KeepIdentity, transaction))
                {
                    bulkCopy.DestinationTableName = VacancyEventTableIdentifier;

                    bulkCopy.ColumnMappings.Add(nameof(VacancyEvent.PublisherId), nameof(VacancyEvent.PublisherId));
                    bulkCopy.ColumnMappings.Add(nameof(VacancyEvent.EventTime), nameof(VacancyEvent.EventTime));
                    bulkCopy.ColumnMappings.Add(nameof(VacancyEvent.VacancyReference), nameof(VacancyEvent.VacancyReference));
                    bulkCopy.ColumnMappings.Add(nameof(VacancyEvent.EventType), nameof(VacancyEvent.EventType));

                    await bulkCopy.WriteToServerAsync(dt);
                    transaction.Commit();
                }
            }
        }

        private static IEnumerable<long> GetDistinctVacancyReferencesFromTable(DataTable dt)
        {
            var columnIndex = 0;
            var filterDistinct = true;
            var distinctVacancyReferenceTable = dt.DefaultView.ToTable(filterDistinct, new[] { nameof(VacancyEvent.VacancyReference) });

            foreach (DataRow row in distinctVacancyReferenceTable.Rows)
            {
                yield return long.Parse(row[columnIndex].ToString());
            }
        }

        private static async Task PublishVacancyToQueueAsync(long vacancyReference)
        {
            var storageAccount = CloudStorageAccount.Parse(_queueStorageConnString);
            var client = storageAccount.CreateCloudQueueClient();

            var queue = client.GetQueueReference(GenerateVacancyAnalyticsQueueName);

            await queue.CreateIfNotExistsAsync();

            var message = new CloudQueueMessage(JsonConvert.SerializeObject(new { VacancyReference = vacancyReference }));

            await queue.AddMessageAsync(message);
        }

        private static Polly.Retry.RetryPolicy GetRetryPolicy(ILogger log) => Policy
                    .Handle<SqlException>()
                    .Or<DbException>()
                    .Or<InvalidOperationException>()
                    .WaitAndRetryAsync(new[]
                    {
                        TimeSpan.FromSeconds(1),
                        TimeSpan.FromSeconds(2),
                        TimeSpan.FromSeconds(4)
                    }, (exception, timeSpan, retryCount, context) =>
                    {
                        log.LogWarning($"Error executing SQL Command for method {context.OperationKey} Reason: {exception.Message}. Retrying in {timeSpan.Seconds} secs...attempt: {retryCount}");
                    });
    }
}