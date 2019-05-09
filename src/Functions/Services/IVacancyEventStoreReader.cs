using System.Collections.Generic;
using System.Threading.Tasks;

namespace Esfa.VacancyAnalytics.Functions.Services
{
    public interface IVacancyEventStoreReader
    {
        Task<IEnumerable<long>> GetRecentlyAffectedVacanciesAsync(int lastNoOfHours);
    }
}