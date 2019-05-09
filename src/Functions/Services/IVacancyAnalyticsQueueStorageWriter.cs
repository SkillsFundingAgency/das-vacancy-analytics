using System.Threading.Tasks;

namespace Esfa.VacancyAnalytics.Functions.Services
{
    internal interface IVacancyAnalyticsQueueStorageWriter
    {
        Task QueueVacancyAsync(long vacancyReference);
    }
}