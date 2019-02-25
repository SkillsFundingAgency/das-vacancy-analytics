using System;
using System.IO;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics;
using Esfa.Vacancy.Analytics.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Functions
{
    public static class PublishVacancyEvent
    {
        private const string LocalSettingsFileName = "local.settings.json";
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";
        private const string EventHubName = "vacancy";
        private static VacancyEventClient _client;

        [FunctionName("PublishVacancyEvent")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = new ConfigurationBuilder()
                .SetBasePath(context.FunctionAppDirectory)
                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                .AddEnvironmentVariables()
                .Build();

            var vacancyEventHubConnString = config.GetValue<string>(VacancyEventHubConnStringKey);

            try
            {
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                var evt = JsonConvert.DeserializeObject<Esfa.VacancyAnalytics.Functions.Models.VacancyEvent>(requestBody);

                _client = new VacancyEventClient(vacancyEventHubConnString + ";EntityPath=" + EventHubName, evt.PublisherId, new LoggerFactory().CreateLogger<VacancyEventClient>());
                await SendVacancyEvent(evt);
            }
            catch (InvalidOperationException)
            {
                return new BadRequestObjectResult("Please provide a valid event type.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                return new BadRequestObjectResult("");
            }

            return new OkResult();
        }

        private static Task SendVacancyEvent(Esfa.VacancyAnalytics.Functions.Models.VacancyEvent evt)
        {
            switch (evt.EventType)
            {
                case nameof(ApprenticeshipSearchImpressionEvent):
                    return _client.PushApprenticeshipSearchEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipSavedSearchAlertImpressionEvent):
                    return _client.PushApprenticeshipSavedSearchAlertEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipBookmarkedImpressionEvent):
                    return _client.PushApprenticeshipBookmarkedEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipDetailImpressionEvent):
                    return _client.PushApprenticeshipDetailEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipApplicationCreatedEvent):
                    return _client.PushApprenticeshipApplicationCreatedEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipApplicationSubmittedEvent):
                    return _client.PushApprenticeshipApplicationSubmittedEventAsync(evt.VacancyReference);
                default:
                    throw new InvalidOperationException($"Cannot publish event {evt.EventType} for {evt.VacancyReference}");
            }
        }
    }
}
