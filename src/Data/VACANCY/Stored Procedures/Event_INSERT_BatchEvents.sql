CREATE PROCEDURE [VACANCY].[Event_INSERT_BatchEvents]
	@VE [VACANCY].[VacancyEventsTableType] READONLY
AS
	SET NOCOUNT ON;

	BEGIN TRY
		BEGIN TRANSACTION

	DECLARE @Now DATETIME = GETUTCDATE()
	DECLARE @25HoursPrior DATETIME = DATEADD(HH, -25, @Now)

		;WITH cte_ExistingEvents AS
		(
			SELECT	Id
			FROM	[VACANCY].[Event]
			WHERE	VacancyReference IN (
											SELECT
											DISTINCT	VacancyReference
											FROM		@VE
										)
			AND		EventTime BETWEEN @25HoursPrior AND @Now
		)

		INSERT
		INTO		[VACANCY].[Event]
		SELECT
		DISTINCT	[Id]
		,			[PublisherId]
		,			[EventTime]
		,			[VacancyReference]
		,			[EventType]
		FROM		@VE
		WHERE		Id NOT IN (SELECT Id FROM cte_ExistingEvents)

		COMMIT TRANSACTION
	END TRY
	BEGIN CATCH
		IF (@@TRANCOUNT > 0)
			ROLLBACK TRANSACTION

		DECLARE @errMsg VARCHAR(MAX) = ERROR_MESSAGE()
		RAISERROR(@errMsg, 16, 1)
	END CATCH