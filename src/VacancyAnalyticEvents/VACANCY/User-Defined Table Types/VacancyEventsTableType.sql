CREATE TYPE [VACANCY].[VacancyEventsTableType] AS TABLE
(
	[Id]				UNIQUEIDENTIFIER
,	[PublisherId]		VARCHAR(100)
,	[EventTime]			DATETIME
,	[VacancyReference]	BIGINT
,	[EventType]			VARCHAR(100)
);