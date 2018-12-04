using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics.Events;
using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Functions
{
    internal class VacancyEventProcessor : IEventProcessor
	{
		private readonly ILogger _log;
		private readonly VacancyEventStoreWriter _writer;
		private readonly QueueStorageWriter _qsWriter;
		private Stopwatch _checkpointStopwatch;

		public VacancyEventProcessor(VacancyEventStoreWriter eventStoreWriter, QueueStorageWriter qsWriter, ILogger log)
		{
			_log = log;
			_writer = eventStoreWriter;
			_qsWriter = qsWriter;
		}

		public async Task CloseAsync(PartitionContext context, CloseReason reason)
		{
			_log.LogInformation($"{nameof(VacancyEventProcessor)} shutting down. Partition '{context.PartitionId}', Reason: '{reason}'.");

			//if (reason == CloseReason.Shutdown)
			//{
				//await context.CheckpointAsync();
			//}

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

			foreach (var evt in eventDataSet)
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

			var distinctVacancyReferences = GetDistinctVacancyReferencesFromTable(eventDataTable);

			try
			{
				foreach (var item in distinctVacancyReferences)
				{
					await _qsWriter.QueueVacancyAsync(item);
				}
			}
			catch (Exception ex)
			{
				_log.LogError(ex, $"An error occurred queuing a vacancy on to the storage queue.");
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
			}

			return null;
		}

		private IEnumerable<long> GetDistinctVacancyReferencesFromTable(DataTable dt)
		{
			var columnIndex = 0;
			var filterDistinct = true;
			var distinctVacancyReferenceTable = dt.DefaultView.ToTable(filterDistinct, new[] { nameof(VacancyEvent.VacancyReference) });

			foreach (DataRow row in distinctVacancyReferenceTable.Rows)
			{
				yield return long.Parse(row[columnIndex].ToString());
			}
		}
	}
}