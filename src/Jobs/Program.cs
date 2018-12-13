using System;
using System.Threading.Tasks;
using Esfa.VacancyAnalytics.Jobs.Services;
using Microsoft.Azure.EventHubs;
using Microsoft.Azure.EventHubs.Processor;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Jobs
{
    public class Program
    {
        private const string VacancyEventHubConnStringKey = "VacancyEventHub";
        private const string VacancyEventStoreConnStringKey = "VacancyAnalyticEventsSqlDbConnectionString";
        private const string EventHubName = "vacancy";
        private const string HostNamePrefix = "vep";
        private const string QueueStorageConnStringKey = "AzureWebJobsStorage";
        private static ILogger _logger;

        private static readonly string EnvironmentName;
        private static readonly bool IsDevelopment;

        static Program()
        {
            EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
            IsDevelopment = EnvironmentName?.Equals("Development", StringComparison.CurrentCultureIgnoreCase) ?? false;
        }

        public static async Task Main(string[] args)
        {
            IConfigurationBuilder configBuilder = SetupConfig();

            var config = configBuilder.Build();
            LoggerFactory loggerFactory = SetupLoggerFactory();

            _logger = loggerFactory.CreateLogger("VacancyAnalyticsWebJob");

            var vacancyEventStoreConnString = config.GetConnectionString(VacancyEventStoreConnStringKey);
            var vacancyEventHubConnString = config.GetConnectionString(VacancyEventHubConnStringKey);
            var queueStorageConnString = config.GetConnectionString(QueueStorageConnStringKey);

            var eventStoreWriter = new VacancyEventStoreWriter(vacancyEventStoreConnString, loggerFactory.CreateLogger<VacancyEventStoreWriter>());

            var factory = new EventProcessorFactory(loggerFactory.CreateLogger<VacancyEventProcessor>(), eventStoreWriter);

            var eventProcessorHost = new EventProcessorHost(
                EventHubName,
                PartitionReceiver.DefaultConsumerGroupName,
                vacancyEventHubConnString,
                queueStorageConnString,
                $"{HostNamePrefix}");

            var opts = new EventProcessorOptions
            {
                PrefetchCount = 512,
                MaxBatchSize = 256
            };

            try
            {
                opts.SetExceptionHandler(HandleEventProcessorException);
                await eventProcessorHost.RegisterEventProcessorFactoryAsync(factory, opts);
                Console.WriteLine("Receiving. Press enter key to stop worker job.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
            }

            Console.ReadLine();
            await eventProcessorHost.UnregisterEventProcessorAsync();
        }

        private static void HandleEventProcessorException(ExceptionReceivedEventArgs args)
        {
            _logger.LogError(args.Exception, $"Error occured processing vacancy events from partition: {args.PartitionId}.");
        }

        private static LoggerFactory SetupLoggerFactory()
        {
            var loggerFactory = new LoggerFactory();

            loggerFactory.AddDebug()
                .AddConsole();

            loggerFactory.AddNLog(new NLogProviderOptions {CaptureMessageProperties = true, CaptureMessageTemplates = true});
            NLog.LogManager.LoadConfiguration("nlog.config");
            return loggerFactory;
        }

        private static IConfigurationBuilder SetupConfig()
        {
            var configBuilder = new ConfigurationBuilder()
                .SetBasePath(Environment.CurrentDirectory)
                .AddJsonFile("appSettings.json", optional: true)
                .AddJsonFile($"appSettings.{EnvironmentName}.json", optional: true)
                .AddEnvironmentVariables();

            if (IsDevelopment)
            {
                configBuilder.AddUserSecrets<Program>();
            }

            return configBuilder;
        }
    }
}
