using System;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Jobs.Services;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Jobs
{
	public class VacancyEventProcessRunner
	{
		private const string EventHubName = "vacancy";
		public const string HostNamePrefix = "wep";
		private readonly ILogger<VacancyEventProcessRunner> _logger;
		private readonly IEventProcessorFactory _vepFactory;
		private readonly string _vacancyEventHubConnString;
		private readonly string _queueStorageConnString;

		public VacancyEventProcessRunner(ILogger<VacancyEventProcessRunner> logger, IEventProcessorFactory vepFactory, string vacancyEventHubConnString, string queueStorageConnString)
		{
			_logger = logger;
			_vepFactory = vepFactory;
			_vacancyEventHubConnString = vacancyEventHubConnString;
			_queueStorageConnString = queueStorageConnString;
		}

		[NoAutomaticTrigger]
		public async Task RunAsync()
		{
			_logger.LogInformation($"Starting {nameof(VacancyEventProcessRunner)} job.");

			var eventProcessorHost = new EventProcessorHost(
				EventHubName,
				PartitionReceiver.DefaultConsumerGroupName,
				_vacancyEventHubConnString,
				_queueStorageConnString,
				$"{HostNamePrefix}-{DateTime.UtcNow.Second}");

			var opts = new EventProcessorOptions
			{
				PrefetchCount = 512,
				MaxBatchSize = 256
			};

			opts.SetExceptionHandler(HandleEventProcessorException);
			await eventProcessorHost.RegisterEventProcessorFactoryAsync(_vepFactory, opts);

			_logger.LogInformation($"Finished {nameof(VacancyEventProcessRunner)} job.");
		}

		private void HandleEventProcessorException(ExceptionReceivedEventArgs args)
		{
			_logger.LogError(args.Exception, $"Error occured processing vacancy events from partition: {args.PartitionId}.");
		}
	}
}