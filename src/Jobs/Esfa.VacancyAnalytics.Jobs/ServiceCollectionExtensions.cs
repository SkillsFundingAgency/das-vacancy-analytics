using Esfa.VacancyAnalytics.Jobs.Services;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Jobs
{
	internal static class ServiceCollectionExtensions
	{
		private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
		private const string VacancyEventHubConnStringKey = "VacancyEventHub";
		private const string QueueStorageConnStringKey = "QueueStorage";

		public static void ConfigureJobServices(this IServiceCollection services, IConfiguration config)
		{
			var vacancyEventStoreConnString = config.GetConnectionString(VacancyEventStoreConnStringKey);
			var vacancyEventHubConnString = config.GetConnectionString(VacancyEventHubConnStringKey);
			var queueStorageConnString = config.GetConnectionString(QueueStorageConnStringKey);

			services.AddSingleton<VacancyEventStoreWriter>(x => new VacancyEventStoreWriter(vacancyEventStoreConnString, x.GetService<ILogger<VacancyEventStoreWriter>>()));

			services.AddScoped<EventProcessorFactory>(x => new EventProcessorFactory(x.GetService<ILogger<VacancyEventProcessor>>(), x.GetService<VacancyEventStoreWriter>()));

			services.AddScoped<IEventProcessorFactory, EventProcessorFactory>();

			services.AddScoped(x => 
								new VacancyEventProcessRunner(x.GetService<ILogger<VacancyEventProcessRunner>>(), 
																x.GetService<IEventProcessorFactory>(),
																vacancyEventHubConnString,
																queueStorageConnString));
		}
	}
}