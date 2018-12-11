using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NLog.Extensions.Logging;

namespace Esfa.VacancyAnalytics.Jobs
{
	class Program
	{
		private static readonly string EnvironmentName;
		private static readonly bool IsDevelopment;

		static Program()
		{
			EnvironmentName = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");
			IsDevelopment = EnvironmentName?.Equals("Development", StringComparison.CurrentCultureIgnoreCase) ?? false;
		}

		/* public static async Task Main(string[] args)
		{
			ILogger logger = null;

			try
			{
				var host = CreateHostBuilder().Build();
				using (host)
				{
					//logger = ((ILoggerFactory)host.Services.GetService(typeof(ILoggerFactory)))
						//.CreateLogger(nameof(Program));
					//host.Services.
					await host.RunAsync();
				}
			}
			catch (Exception ex)
			{
				logger?.LogCritical(ex, "The Job has met with a horrible end!!");
				throw;
			}
			finally
			{
				NLog.LogManager.Shutdown();
			}
		} */

		private static IHostBuilder CreateHostBuilder()
		{
			return new HostBuilder()
					.UseEnvironment(EnvironmentName)
					.ConfigureWebJobs(b =>
					{
						b.AddAzureStorageCoreServices()
							.AddAzureStorage()
							.AddTimers();  // is this needed???
					})
					.ConfigureAppConfiguration(b =>
					{
						b.AddJsonFile("appSettings.json", optional: false)
							.AddJsonFile($"appSettings.{EnvironmentName}.json", true)
							.AddEnvironmentVariables();

						if (IsDevelopment)
						{
							b.AddUserSecrets<Program>();
						}
					})
					.ConfigureLogging((context, b) =>
					{
						b.SetMinimumLevel(LogLevel.Trace);
						b.AddDebug();
						b.AddConsole();
						b.AddNLog();

						// If this key exists in any config, use it to enable App Insights
						string appInsightsKey = context.Configuration["APPINSIGHTS_INSTRUMENTATIONKEY"];
						if (!string.IsNullOrEmpty(appInsightsKey))
						{
							b.AddApplicationInsights(o => o.InstrumentationKey = appInsightsKey);
						}
					})
					.ConfigureServices((context, services) =>
					{
						//services.AddSingleton<IQueueProcessorFactory, CustomQueueProcessorFactory>();
						/* services.Configure<QueuesOptions>(options =>
						{
							//maximum number of queue messages that are picked up simultaneously to be executed in parallel (default is 16)
							options.BatchSize = 1;
							//Maximum number of retries before a queue message is sent to a poison queue (default is 5)
							options.MaxDequeueCount = 5;
							//maximum wait time before polling again when a queue is empty (default is 1 minute).
							options.MaxPollingInterval = TimeSpan.FromSeconds(10);
						}); */

						services.ConfigureJobServices(context.Configuration);
					})
					.UseConsoleLifetime();
		}
	}
}
