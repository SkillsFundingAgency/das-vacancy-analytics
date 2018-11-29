CREATE PROCEDURE [VACANCY].[GetEventsForConsumer]
	@VacancyReference BIGINT
AS
	SELECT	VacancyReference
	,		PublisherId
	,		EventType
	,		SUM(COUNT(EventType)) OVER(PARTITION BY VacancyReference, PublisherId) AS NoOfOccurancesPerPublisher
	,		SUM(COUNT(EventType)) OVER(PARTITION BY VacancyReference) AS NoOfOccurances
	,		Id
	FROM	[VACANCY].[Event]
	GROUP
	BY		VacancyReference
	,		EventType
	,		PublisherId
	,		Id
	ORDER
	BY		VacancyReference
	,		EventType
	,		PublisherId
	,		Id