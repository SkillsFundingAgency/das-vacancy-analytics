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

        private static IConfigurationRoot _config;
        private static object _syncRoot = new object();

        [FunctionName("PublishVacancyEvent")]
        public static async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var config = GetConfiguration(context.FunctionAppDirectory);

            var vacancyEventHubConnString = config.GetValue<string>(VacancyEventHubConnStringKey);

            try
            {
                string requestBody = string.Empty;

                using (var sr = new StreamReader(req.Body))
                {
                    requestBody = await sr.ReadToEndAsync();
                }

                var evt = JsonConvert.DeserializeObject<Esfa.VacancyAnalytics.Functions.Models.VacancyEvent>(requestBody);

                var client = new VacancyEventClient(vacancyEventHubConnString + ";EntityPath=" + EventHubName, evt.PublisherId, new LoggerFactory().CreateLogger<VacancyEventClient>());
                await SendVacancyEvent(client, evt);
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

        private static IConfigurationRoot GetConfiguration(string functionAppDirectory)
        {
            if (_config != null)
            {
                return _config;
            }
            
            lock(_syncRoot)
            {
                if (_config == null)
                {
                    _config = new ConfigurationBuilder()
                                .SetBasePath(functionAppDirectory)
                                .AddJsonFile(LocalSettingsFileName, optional: true, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();
                }
            }

            return _config;
        }

        private static Task SendVacancyEvent(VacancyEventClient client, Esfa.VacancyAnalytics.Functions.Models.VacancyEvent evt)
        {
            switch (evt.EventType)
            {
                case nameof(ApprenticeshipSearchImpressionEvent):
                    return client.PushApprenticeshipSearchEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipSavedSearchAlertImpressionEvent):
                    return client.PushApprenticeshipSavedSearchAlertEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipBookmarkedImpressionEvent):
                    return client.PushApprenticeshipBookmarkedEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipDetailImpressionEvent):
                    return client.PushApprenticeshipDetailEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipApplicationCreatedEvent):
                    return client.PushApprenticeshipApplicationCreatedEventAsync(evt.VacancyReference);
                case nameof(ApprenticeshipApplicationSubmittedEvent):
                    return client.PushApprenticeshipApplicationSubmittedEventAsync(evt.VacancyReference);
                default:
                    throw new InvalidOperationException($"Cannot publish event {evt.EventType} for {evt.VacancyReference}");
            }
        }
    }
}
