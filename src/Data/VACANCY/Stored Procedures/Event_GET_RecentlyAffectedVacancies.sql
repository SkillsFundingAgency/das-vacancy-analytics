CREATE PROCEDURE [VACANCY].[Event_GET_RecentlyAffectedVacancies]
	@LastNoOfHours INT = 1
AS
	DECLARE @CompareTime DATETIME = DATEADD(HOUR, -@LastNoOfHours, GETUTCDATE())

	SELECT
	DISTINCT	VacancyReference
	FROM		[VACANCY].[Event]
	WHERE		EventTime > @CompareTime