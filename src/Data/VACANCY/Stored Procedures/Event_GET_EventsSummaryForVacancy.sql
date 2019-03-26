CREATE PROCEDURE [VACANCY].[Event_GET_EventsSummaryForVacancy]
    @VacancyReference BIGINT
AS
    SET NOCOUNT ON;

    SELECT	EventType
    ,       EventTime
    INTO	#VacancyEvents
    FROM	[VACANCY].[Event]
    WHERE	VacancyReference = @VacancyReference

    DECLARE @Now DATETIME = GETUTCDATE()

    DECLARE @SevenDaysAgo DATETIME = DATEADD(D, -7, @Now)
    DECLARE @SixDaysAgo DATETIME = DATEADD(D, -6, @Now)
    DECLARE @FiveDaysAgo DATETIME = DATEADD(D, -5, @Now)
    DECLARE @FourDaysAgo DATETIME = DATEADD(D, -4, @Now)
    DECLARE @ThreeDaysAgo DATETIME = DATEADD(D, -3, @Now)
    DECLARE @TwoDaysAgo DATETIME = DATEADD(D, -2, @Now)
    DECLARE @OneDayAgo DATETIME = DATEADD(D, -1, @Now)

    SELECT	@VacancyReference AS VacancyReference
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent') AS NoOfApprenticeshipSearches
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipSearchesSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipSearchesSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipSearchesFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipSearchesFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipSearchesThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipSearchesTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSearchImpressionEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipSearchesOneDayAgo
    --
    --
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent') AS NoOfApprenticeshipSavedSearchAlerts
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipSavedSearchAlertsSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipSavedSearchAlertsSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipSavedSearchAlertsFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipSavedSearchAlertsFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipSavedSearchAlertsThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipSavedSearchAlertsTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipSavedSearchAlertImpressionEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipSavedSearchAlertsOneDayAgo
    --
    --
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent') AS NoOfApprenticeshipSaved
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipSavedSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipSavedSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipSavedFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipSavedFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipSavedThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipSavedTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipBookmarkedImpressionEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipSavedOneDayAgo
    --
    --
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent') AS NoOfApprenticeshipDetailsViews
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipDetailsViewsSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipDetailsViewsSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipDetailsViewsFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipDetailsViewsFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipDetailsViewsThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipDetailsViewsTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipDetailImpressionEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipDetailsViewsOneDayAgo
    --
    --
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent') AS NoOfApprenticeshipApplicationsCreated
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipApplicationsCreatedSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipApplicationsCreatedSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipApplicationsCreatedFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipApplicationsCreatedFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipApplicationsCreatedThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipApplicationsCreatedTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationCreatedEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipApplicationsCreatedOneDayAgo
    --
    --
    ,		(SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent') AS NoOfApprenticeshipApplicationsSubmitted
    --      Breakdown over last 7 day period
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @SevenDaysAgo AND @SixDaysAgo) AS NoOfApprenticeshipApplicationsSubmittedSevenDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @SixDaysAgo AND @FiveDaysAgo) AS NoOfApprenticeshipApplicationsSubmittedSixDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @FiveDaysAgo AND @FourDaysAgo) AS NoOfApprenticeshipApplicationsSubmittedFiveDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @FourDaysAgo AND @ThreeDaysAgo) AS NoOfApprenticeshipApplicationsSubmittedFourDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @ThreeDaysAgo AND @TwoDaysAgo) AS NoOfApprenticeshipApplicationsSubmittedThreeDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @TwoDaysAgo AND @OneDayAgo) AS NoOfApprenticeshipApplicationsSubmittedTwoDaysAgo
    ,       (SELECT COUNT(1) FROM #VacancyEvents WHERE EventType = 'ApprenticeshipApplicationSubmittedEvent' AND EventTime BETWEEN @OneDayAgo AND @Now) AS NoOfApprenticeshipApplicationsSubmittedOneDayAgo