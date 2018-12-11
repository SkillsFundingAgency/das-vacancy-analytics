using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Jobs.Services
{
    internal class EventProcessorFactory : IEventProcessorFactory
	{
		private readonly VacancyEventStoreWriter _eventStoreWriter;
		private readonly ILogger<VacancyEventProcessor> _vepLogger;

		public EventProcessorFactory(ILogger<VacancyEventProcessor> vepLogger, VacancyEventStoreWriter eventStoreWriter)
		{
			_eventStoreWriter = eventStoreWriter;
			_vepLogger = vepLogger;
		}

		public IEventProcessor CreateEventProcessor(PartitionContext context)
		{
			var vep = new VacancyEventProcessor(_eventStoreWriter, _vepLogger);
			return vep;
		}
	}
}