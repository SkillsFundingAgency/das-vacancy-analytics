using System;
using System.IO;
using System.Threading.Tasks;
using Esfa.Vacancy.Analytics;
using Esfa.Vacancy.Analytics.Events;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Esfa.VacancyAnalytics.Functions
{
    public class PublishVacancyEvent
    {
        private readonly IVacancyEventClient _client;
        private readonly ILogger<PublishVacancyEvent> _log;

        public PublishVacancyEvent(ILogger<PublishVacancyEvent> log, IVacancyEventClient client)
        {
            _log = log;
            _client = client;
        }

        [FunctionName("PublishVacancyEvent")]
        public async Task<IActionResult> Run([HttpTrigger(AuthorizationLevel.Function, "post", Route = "events")]
            HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string requestBody = string.Empty;

                using (var sr = new StreamReader(req.Body))
                {
                    requestBody = await sr.ReadToEndAsync();
                }

                var evt = JsonConvert.DeserializeObject<Models.VacancyEvent>(requestBody);

                await SendVacancyEvent(evt);
            }
            catch (InvalidOperationException)
            {
                return new BadRequestObjectResult("Please provide a valid vacancy event type.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw new Exception("Could not publish vacancy event to EventHub.");
            }

            return new OkResult();
        }

        private Task SendVacancyEvent(Esfa.VacancyAnalytics.Functions.Models.VacancyEvent evt)
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
