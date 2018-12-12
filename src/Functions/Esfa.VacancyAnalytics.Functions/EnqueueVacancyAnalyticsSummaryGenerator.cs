using System;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Functions.Services;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Functions
{
	public static class EnqueueVacancyAnalyticsSummaryGenerator
	{
		private const string LocalSettingsFileName = "local.settings.json";
		private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
		private const string QueueStorageConnStringKey = "QueueStorage";

		[FunctionName("EnqueueVacancyAnalyticsSummaryGenerator")]
		public static async Task Run([TimerTrigger("0 1 * * * *")]TimerInfo myTimer, ILogger log, ExecutionContext context)
		{
			log.LogInformation($"C# Timer trigger {nameof(EnqueueVacancyAnalyticsSummaryGenerator)} function executed at: {DateTime.Now}");

			var config = new ConfigurationBuilder()
				.SetBasePath(context.FunctionAppDirectory)
				.AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
				.AddEnvironmentVariables()
				.Build();

			var reader = new VacancyEventStoreReader(config.GetConnectionString(VacancyEventStoreConnStringKey), log);

			var queue = new QueueStorageWriter(config.GetConnectionString(QueueStorageConnStringKey));

			var vacancyRefs = await reader.GetRecentlyAffectedVacanciesAsync(lastNoOfHours: 1);

			foreach (var vacancyRef in vacancyRefs)
			{
				await queue.QueueVacancyAsync(vacancyRef);
			}
		}
	}
}