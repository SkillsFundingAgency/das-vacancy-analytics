CREATE TABLE [VACANCY].[Event] (
    [Id]               UNIQUEIDENTIFIER DEFAULT (newsequentialid()) NOT NULL,
    [PublisherId]      VARCHAR (50)     NULL,
    [EventTime]        DATETIME         NOT NULL,
    [VacancyReference] BIGINT           NULL,
    [EventType]        VARCHAR (100)    NULL,
    PRIMARY KEY CLUSTERED ([Id] ASC)
);

