using System.Collections.Generic;

namespace Esfa.VacancyAnalytics.Functions.Models
{
    public class BatchEventsRequest
    {
        public string EventType { get; set; }
        public IEnumerable<string> VacancyRefs { get; set; }
    }
}