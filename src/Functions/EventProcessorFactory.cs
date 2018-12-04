using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Functions
{
	internal class EventProcessorFactory : IEventProcessorFactory
	{
		private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
		private const string QueueStorageConnStringKey = "QueueStorage";
		private readonly IConfigurationRoot _config;
		private readonly ILogger _log;

		public EventProcessorFactory(IConfigurationRoot config, ILogger log)
		{
			_config = config;
			_log = log;
		}

		public IEventProcessor CreateEventProcessor(PartitionContext context)
		{
			return new VacancyEventProcessor(
				new VacancyEventStoreWriter(_config.GetConnectionString(VacancyEventStoreConnStringKey), _log),
				new QueueStorageWriter(_config.GetConnectionString(QueueStorageConnStringKey)),
				_log);
		}
	}
}