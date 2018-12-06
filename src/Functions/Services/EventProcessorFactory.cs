using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Functions.Services
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
			var eventStoreWriter = new VacancyEventStoreWriter(_config.GetConnectionString(VacancyEventStoreConnStringKey), _log);
			var vep = new VacancyEventProcessor(eventStoreWriter,_log);
			return vep;
		}
	}
}