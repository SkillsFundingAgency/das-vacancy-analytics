using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Queue;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Functions.Services
{
	internal sealed class QueueStorageWriter
	{
		private readonly string _connectionString;
		private const string GenerateVacancyAnalyticsQueueName = "generate-vacancy-analytics-summary";

		public QueueStorageWriter(string queueStorageConnString)
		{
			_connectionString = queueStorageConnString;
		}

		public async Task QueueVacancyAsync(long vacancyReference)
		{
			var storageAccount = CloudStorageAccount.Parse(_connectionString);
			var client = storageAccount.CreateCloudQueueClient();
			
			var queue = client.GetQueueReference(GenerateVacancyAnalyticsQueueName);
			await queue.CreateIfNotExistsAsync();

			var message = new CloudQueueMessage(JsonConvert.SerializeObject(new { VacancyReference = vacancyReference }));

			await queue.AddMessageAsync(message);
		}
	}
}