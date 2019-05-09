namespace Esfa.VacancyAnalytics.Functions.Models
{
    internal struct VacancyEvent
    {
        public long VacancyReference { get; set; }
        public string EventType { get; set; }
        public string PublisherId { get; set; }
        public string Data { get; set; }
    }
}
