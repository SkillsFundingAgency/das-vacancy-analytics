CREATE PROCEDURE [VACANCY].[Event_GET_RecentlyAffectedVacancies]
	@LastNoOfHours INT = 1
AS
	SELECT
	DISTINCT	VacancyReference
	FROM 		[VACANCY].[Event]
	WHERE 		EventTime > DATEADD(HOUR, -@LastNoOfHours, GETUTCDATE())