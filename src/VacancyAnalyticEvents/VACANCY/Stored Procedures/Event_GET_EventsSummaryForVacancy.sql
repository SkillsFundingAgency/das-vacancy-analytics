CREATE PROCEDURE [VACANCY].[Event_GET_EventsSummaryForVacancy]
	@VacancyReference BIGINT
AS
	SET NOCOUNT ON;

	SELECT	EventType
	INTO	#VacancyEvents
	FROM	[VACANCY].[Event]
	WHERE	VacancyReference = @VacancyReference

	SELECT	@VacancyReference AS VacancyReference
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent') AS NoOfApprenticeshipSearches
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent') AS NoOfApprenticeshipSavedSearchAlerts
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent') AS NoOfApprenticeshipSaved
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent') AS NoOfApprenticeshipDetailsViews
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent') AS NoOfApprenticeshipApplicationsCreated
	,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent') AS NoOfApprenticeshipApplicationsSubmitted