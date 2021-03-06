using System;
using System.Data;
using Esfa.Vacancy.Analytics.Events;

namespace Esfa.VacancyAnalytics.Jobs.Services
{
    public static class VacancyEventDataTableBuilder
    {
        public static DataTable Build()
        {
            var eventData = new DataTable("VacancyEvent");

            eventData.Columns.Add(nameof(VacancyEvent.Id), typeof(Guid));
            eventData.Columns.Add(nameof(VacancyEvent.PublisherId));
            eventData.Columns.Add(nameof(VacancyEvent.EventTime), typeof(DateTime));
            eventData.Columns.Add(nameof(VacancyEvent.VacancyReference));
            eventData.Columns.Add(nameof(VacancyEvent.EventType));

            return eventData;
        }
    }
}
