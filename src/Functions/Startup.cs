using Esfa.Vacancy.Analytics;
using Esfa.VacancyAnalytics.Functions;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

[assembly: FunctionsStartup(typeof(Startup))]

namespace Esfa.VacancyAnalytics.Functions
{
    public class Startup : FunctionsStartup
    {
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";

        public override void Configure(IFunctionsHostBuilder builder)
        {
            NLog.LogManager.Configuration = new NLog.Config.XmlLoggingConfiguration("nlog.config");

            builder.Services.AddLogging(logBuilder =>
            {
                logBuilder.AddNLog();
            });

            builder.Services.AddTransient<ILoggerProvider, NLogLoggerProvider>();

            builder.Services.AddSingleton<IVacancyEventClient>(
                x =>
                {
                    var svc = x.GetService<IConfiguration>();
                    var vacancyEventHubConnString = svc.GetConnectionStringOrSetting(VacancyEventHubConnStringKey);
                    var lf = x.GetService<ILoggerFactory>();
                    return new VacancyEventClient(string.Concat(vacancyEventHubConnString, ";EntityPath=vacancy"), "FAA", lf.CreateLogger<VacancyEventClient>());
                });
        }
    }
}