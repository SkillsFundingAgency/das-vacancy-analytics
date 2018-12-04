using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;

namespace Esfa.VacancyAnalytics.Functions.Services
{
	public class VacancyEventStoreWriter
	{
		private readonly string _vacancyEventStoreConnString;
		private readonly ILogger _log;
        private readonly RetryPolicy _retryPolicy;
        private const string VacancyEventsBatchInsertSproc = "[VACANCY].[Event_INSERT_BatchEvents]";

		public VacancyEventStoreWriter(string vacancyEventStoreConnString, ILogger log)
		{
			_vacancyEventStoreConnString = vacancyEventStoreConnString;
			_log = log;
			_retryPolicy = GetRetryPolicy(_log);
		}

		public async Task SaveEventDataAsync(DataTable dt)
		{
			using (var conn = new SqlConnection(_vacancyEventStoreConnString))
			{
				await conn.OpenAsync();

				var command = conn.CreateCommand();

				command.CommandType = CommandType.StoredProcedure;
				command.CommandText = VacancyEventsBatchInsertSproc;

				var inputParam = command.Parameters.AddWithValue("@VE", dt);
				inputParam.SqlDbType = SqlDbType.Structured;

				await _retryPolicy.ExecuteAsync(async context => await command.ExecuteNonQueryAsync(), new Context(nameof(SaveEventDataAsync)));
			}
		}

		private Polly.Retry.RetryPolicy GetRetryPolicy(ILogger log) => Policy
					.Handle<SqlException>()
					.Or<DbException>()
					.WaitAndRetryAsync(new[]
					{
						TimeSpan.FromSeconds(1),
						TimeSpan.FromSeconds(2),
						TimeSpan.FromSeconds(4)
					}, (exception, timeSpan, retryCount, context) =>
					{
						log.LogWarning($"Error executing SQL Command for method {context.OperationKey} Reason: {exception.Message}. Retrying in {timeSpan.Seconds} secs...attempt: {retryCount}");
					});
	}
}