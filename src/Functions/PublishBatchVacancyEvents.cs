using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Esfa.Vacancy.Analytics;
using Esfa.Vacancy.Analytics.Events;
using System.Collections.Generic;
using System.Linq;
using Esfa.VacancyAnalytics.Functions.Models;

namespace Esfa.VacancyAnalytics.Functions
{
    public class PublishBatchVacancyEvents
    {
        private readonly IVacancyEventClient _client;
        private readonly ILogger<PublishBatchVacancyEvents> _log;

        public PublishBatchVacancyEvents(ILogger<PublishBatchVacancyEvents> log, IVacancyEventClient client)
        {
            _log = log;
            _client = client;
        }

        [FunctionName("PublishBatchVacancyEvents")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = "batchEvents")] HttpRequest req, ILogger log, ExecutionContext context)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            try
            {
                string requestBody = string.Empty;

                using (var sr = new StreamReader(req.Body))
                {
                    requestBody = await sr.ReadToEndAsync();
                }

                // consider max size of request that can be sent to the function, bear in mind the firewall issues as well

                var batchReq = JsonConvert.DeserializeObject<BatchEventsRequest>(requestBody);

                if (batchReq.VacancyRefs.Any(vr => long.TryParse(vr, out var value) == false))
                {
                    return new BadRequestObjectResult("Please provide valid vacancy reference numbers.");
                }

                var batch = TransformToBatchVacancyEvents(batchReq.EventType, batchReq.VacancyRefs);
                await _client.PublishBatchEventsAsync(batch);
            }
            catch (InvalidOperationException)
            {
                return new BadRequestObjectResult("Please provide a valid vacancy event type.");
            }
            catch (Exception ex)
            {
                log.LogError(ex, ex.Message);
                throw new Exception("Could not publish vacancy events to EventHub.");
            }

            return new OkResult();
        }

        private IEnumerable<VacancyEvent> TransformToBatchVacancyEvents(string eventType, IEnumerable<string> vacancyRefs)
        {
            foreach (var vacancyRef in vacancyRefs)
            {
                switch (eventType)
                {
                    case nameof(ApprenticeshipSearchImpressionEvent):
                        yield return new ApprenticeshipSearchImpressionEvent(long.Parse(vacancyRef));
                        break;
                    case nameof(ApprenticeshipSavedSearchAlertImpressionEvent):
                        yield return new ApprenticeshipSavedSearchAlertImpressionEvent(long.Parse(vacancyRef));
                        break;
                    default:
                        throw new InvalidOperationException($"Cannot publish event {eventType} for {vacancyRef}");
                }
            }
        }
    }
}
