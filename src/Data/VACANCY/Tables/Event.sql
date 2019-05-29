CREATE TABLE [VACANCY].[Event]
(
	[Id]				UNIQUEIDENTIFIER	NOT NULL,
	[PublisherId]		VARCHAR(100)		NOT NULL,
	[EventTime]			DATETIME			NOT NULL,
	[VacancyReference]	BIGINT				NOT NULL,
	[EventType]			VARCHAR(100)		NOT NULL,
	PRIMARY KEY CLUSTERED ([Id] ASC)
);
GO

CREATE INDEX [IX_VacancyReference] ON [VACANCY].[Event] ([VacancyReference])
GO