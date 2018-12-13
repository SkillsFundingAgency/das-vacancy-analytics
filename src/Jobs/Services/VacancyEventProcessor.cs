using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics.Events;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Jobs.Services
{
    internal class VacancyEventProcessor : IEventProcessor
    {
        private readonly ILogger _log;
        private readonly VacancyEventStoreWriter _writer;
        private Stopwatch _checkpointStopwatch;
        private const string TimeTakenDisplayFormat = @"hh\:mm\:ss";

        public VacancyEventProcessor(VacancyEventStoreWriter eventStoreWriter, ILogger log)
        {
            _log = log;
            _writer = eventStoreWriter;
        }

        public async Task CloseAsync(PartitionContext context, CloseReason reason)
        {
            _checkpointStopwatch.Stop();
            _log.LogInformation($@"{nameof(VacancyEventProcessor)} shutting down. Partition '{context.PartitionId}', Reason: '{reason}'. 
			Alive for {_checkpointStopwatch.Elapsed.ToString(TimeTakenDisplayFormat)}.");
            await Task.CompletedTask;
        }

        public async Task OpenAsync(PartitionContext context)
        {
            _log.LogInformation($"{nameof(VacancyEventProcessor)} initialized. Partition: '{context.PartitionId}'");
            _checkpointStopwatch = Stopwatch.StartNew();
            await Task.CompletedTask;
        }

        public Task ProcessErrorAsync(PartitionContext context, Exception error)
        {
            _log.LogError(error, $"Error on Partition: {context.PartitionId}, Error: {error.Message}");
            return Task.CompletedTask;
        }

        public async Task ProcessEventsAsync(PartitionContext context, IEnumerable<EventData> eventDataSet)
        {
            _log.LogInformation($"Processing partition {context.PartitionId} event batch size {eventDataSet.Count()}.");

            var eventDataTable = VacancyEventDataTableBuilder.Build();

            foreach (var evt in eventDataSet.Distinct())
            {
                try
                {
                    var actualEvt = GetVacancyEvent(evt);
                    eventDataTable.Rows.Add(actualEvt.Id, actualEvt.PublisherId, actualEvt.EventTime, actualEvt.VacancyReference, actualEvt.EventType);
                }
                catch (Exception ex)
                {
                    _log.LogError(ex, $"An error occurred extracting data from the event hub message {evt.SystemProperties["SequenceNumber"]}");
                    throw;
                }
            }

            eventDataTable.AcceptChanges();

            try
            {
                await _writer.SaveEventDataAsync(eventDataTable);
                await context.CheckpointAsync();
                _log.LogInformation($"Finished processing partition {context.PartitionId} event batch size {eventDataSet.Count()}.");
            }
            catch (Exception ex)
            {
                _log.LogError(ex, "An error has occured with uploading events to the event store.");
                throw;
            }
        }

        private VacancyEvent GetVacancyEvent(EventData evt)
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
                default:
                    return null;
            }
        }
    }
}
