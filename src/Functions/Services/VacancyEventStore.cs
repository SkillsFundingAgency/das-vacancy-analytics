using System;
using System.Data.Common;
using System.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Esfa.VacancyAnalytics.Functions.Services
{
    public abstract class VacancyEventStore
    {
        protected readonly ILogger _log;
        protected readonly AsyncRetryPolicy _retryPolicy;

        public VacancyEventStore(ILogger log)
        {
            _log = log;
            _retryPolicy = GetRetryPolicy(_log);
        }

        private Polly.Retry.AsyncRetryPolicy GetRetryPolicy(ILogger log) => Policy
            .Handle<SqlException>()
            .Or<DbException>()
            .WaitAndRetryAsync(new[]
            {
                TimeSpan.FromSeconds(1),
                TimeSpan.FromSeconds(2),
                TimeSpan.FromSeconds(4)
            }, (exception, timeSpan, retryCount, context) => { log.LogWarning($"Error executing SQL Command for method {context.OperationKey} Reason: {exception.Message}. Retrying in {timeSpan.Seconds} secs...attempt: {retryCount}"); });
    }
}
