--sp_helptext GetAllLeadFrequencyVisitReport


 -- drop table #TempTeam  drop table #LeadReport
 --exec CRM_Usp_GetAllLeadFrequencyVisitReport 1248,'1257',1,10,'2024-01-10 12:00:00AM','2024-01-10 11:59:59PM','1248',0
CREATE PROC CRM_Usp_GetAllLeadFrequencyVisitReport  

   @UserId INT ,  
   @UserIds VARCHAR(200) ,  
   @From INT ,  
   @To INT  ,  
   @FromDate DATETIME,  
   @ToDate DATETIME ,
   @ManagerIds VARCHAR(200),
   @TotalRecords INT OUTPUT  
AS  
BEGIN  
  
    SET NOCOUNT ON;  
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;  
  
    DECLARE @CompanyId INT = 0,  
            @Total INT = 0;  
  
  
 IF (@UserIds ='')  
 SET @UserIds = NULL
 
 IF (@ManagerIds='')
 SET @ManagerIds=NULL
  
 IF (@FromDate ='' OR @FromDate = NULL)  
    SET @FromDate = DATEADD(D, -30, GETDATE())
	
	IF (@ToDate = '' OR @ToDate IS NULL)
    SET @ToDate = GETDATE()
		     
  
    SELECT @CompanyId = CompanyId FROM [dbo].[CRMMemberRoleMapping] WHERE UserId = @UserId;  
  
    CREATE TABLE #TempTeam  
    (  
        RowNumber BIGINT,  
        UserId INT,  
        UserName VARCHAR(50),  
        ParentId INT,  
        ManagerName VARCHAR(50),  
        UserLevel INT  
    );  
  
    INSERT INTO #TempTeam  
    EXEC [dbo].[CRM_Usp_GetMyTeam] @UserId, 1, -1, NULL, @Total;  
  
    CREATE TABLE #LeadReport  
    (  
        RowNumber INT IDENTITY(1, 1),  
        LeadId INT,  
        LeadName VARCHAR(200),  
        LeadTotalRosterPlans INT,  
        TotalVisits INT,  
        LeadRosterVisitsPercentage DECIMAL(5, 2),  
        VisitingDates VARCHAR(MAX),  
        PersonNames VARCHAR(500)  
    );  
  
    INSERT INTO #LeadReport  
    SELECT  
        Id,  
        LeadName,  
        LeadTotalRosterPlans,  
        LeadVisited,  
        CASE WHEN LeadVisited = 0 THEN 0 ELSE (CONVERT(DECIMAL(5, 2), LeadVisited) * 100) / LeadTotalRosterPlans END AS LeadRosterVisitsPercentage,  
        [VisitingDates],  
        [PersonNames]  
    FROM  
    (  
        SELECT  
            cl.Id,  
            cl.LeadName,  
            SUM(CASE WHEN clr.Id IS NOT NULL THEN 1 ELSE 0 END) AS LeadTotalRosterPlans,  
            SUM(CASE WHEN clr.RosterStatusId = 2 THEN 1 ELSE 0 END) AS LeadVisited,  
            ISNULL(STUFF((SELECT ', ' + CAST(FORMAT([VisitingDate], 'MM/dd/yyyy HH:mm:SS') AS VARCHAR(10)) [text()]  
                          FROM [dbo].[User] u  
                          INNER JOIN [dbo].[CRMLeadRoster] t ON u.UserId = t.UserId  
						  INNER JOIN CRMTeam ct on ct.ParentId =u.UserId
      --  INNER JOIN [fn_GetMyTeam](@UserId) m  ON t.UserId = m.ParentId  
                          WHERE t.RosterStatusId = 2 AND t.LeadId = cl.Id  
                         AND (@FromDate IS NULL OR t.RosterPlaningDate BETWEEN @FromDate AND @ToDate)
AND (
    (@UserIds IS NULL OR u.UserId IN (SELECT value FROM STRING_SPLIT(@UserIds, ',')))
    or
    (@ManagerIds IS NULL OR ct.ParentId IN (SELECT value FROM STRING_SPLIT(@ManagerIds, ',')))
)

                          ORDER BY [VisitingDate] DESC  
                          FOR XML PATH(''), TYPE)  
                          .value('.', 'NVARCHAR(MAX)'), 1, 2, ' '), '-') [VisitingDates],  
            ISNULL(STUFF((SELECT DISTINCT ', ' + CAST(Name AS VARCHAR(50)) [text()]  
                          FROM [dbo].[User] u  
                          INNER JOIN [dbo].[CRMLeadRoster] t ON u.UserId = t.UserId  
						   INNER JOIN CRMTeam ct on ct.ParentId =u.UserId
         --  INNER JOIN [fn_GetMyTeam](@UserId) m  ON t.UserId = m.ParentId  
                          WHERE t.RosterStatusId = 2 AND t.LeadId = cl.Id 
						 AND (@FromDate IS NULL OR t.RosterPlaningDate BETWEEN @FromDate AND @ToDate)
                         AND (
                              (@UserIds IS NULL OR u.UserId IN (SELECT value FROM STRING_SPLIT(@UserIds, ',')))
                               OR
                              (@ManagerIds IS NULL OR ct.ParentId IN (SELECT value FROM STRING_SPLIT(@ManagerIds, ',')))
                             )

						  FOR XML PATH(''), TYPE)  
                          .value('.', 'NVARCHAR(MAX)'), 1, 2, ' '), '-') [PersonNames]  
        FROM  [dbo].[CRMLead] cl  
        LEFT JOIN [dbo].[CRMLeadRoster] clr ON clr.LeadId = cl.Id  
        WHERE cl.CompanyId = @CompanyId  
        GROUP BY cl.Id,  
                 cl.LeadName  
    ) AS LeadFrequencyVisit  
    ORDER BY LeadVisited DESC;  
  
    SELECT @TotalRecords = COUNT(1) FROM #LeadReport;  
  
    IF (@To = -1)  
        SET @To = @TotalRecords;  
  
   UPDATE #LeadReport  
  SET TotalVisits = CASE   
                    WHEN VisitingDates = '-' or VisitingDates is null THEN 0  
                    ELSE LEN(VisitingDates) - LEN(REPLACE(VisitingDates, ',', '')) + 1  
                  END;  
  
    
   SELECT *   
FROM #LeadReport   
WHERE RowNumber BETWEEN @From AND @To   
    AND VisitingDates IS NOT NULL   
    AND VisitingDates <> '-'   
ORDER BY TotalVisits DESC;  
  
END;