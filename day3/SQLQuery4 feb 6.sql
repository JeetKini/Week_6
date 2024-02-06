--sp_helptext CRM_Usp_GetLeadFrequencyVisitReport

--exec CRM_Usp_GetLeadFrequencyVisitReport 1248,null,1,10,null,null,null,0

--CREATE PROCEDURE [dbo].[CRM_Usp_GetLeadFrequencyVisitReport]      
 --drop table #TempTeam drop table #LeadReport    
    declare
   @UserId INT=1248,    
   @UserIds VARCHAR(200)=null,    
   @From INT = 1,    
   @To INT = 10,    
   @FromDate DATE=null,    
   @ToDate DATE=null,    
   @Text VARCHAR(200)=null,    
   @TotalRecords INT=0 --OUTPUT            
 -- AS            
  BEGIN            
            
    SET NOCOUNT ON;            
    SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED;            
            
    DECLARE @CompanyId INT = 0,            
      @Total INT = 0;            
        
         
        
 IF(@FromDate IS NULL)        
 BEGIN        
  SET @FromDate =  DATEADD(DD,-30,GETUTCDATE())        
  SET @ToDate = GETDATE()        
 END        
        
            
    SELECT @CompanyId = CompanyId FROM [dbo].[CRMMemberRoleMapping] WHERE UserId = @UserId            
                  
    CREATE TABLE #TempTeam            
    (            
      RowNumber BIGINT            
     ,UserId INT            
     ,UserName VARCHAR(50)            
     ,ParentId INT            
     ,ManagerName VARCHAR(50)                
     ,UserLevel INT                
    )            
            
    INSERT INTO #TempTeam            
    EXEC [dbo].[CRM_Usp_GetMyTeam] @UserId, 1, -1, NULL, @Total            
            
    CREATE TABLE #LeadReport            
     (            
      RowNumber INT IDENTITY(1,1),            
      LeadId INT,            
      LeadName VARCHAR(200),            
      LeadTotalRosterPlans INT,            
      TotalVisits INT,            
      LeadRosterVisitsPercentage DECIMAL(5,2),            
      VisitingDates VARCHAR(MAX),            
      PersonNames VARCHAR(500)            
     )            
            
            
     INSERT INTO #LeadReport            
     SELECT        
   Id            
      ,LeadName            
      ,LeadTotalRosterPlans            
      ,LeadVisited            
      ,CASE WHEN LeadVisited = 0             
         THEN 0             
         ELSE (CONVERT(DECIMAL(5,2),LeadVisited)*100)/LeadTotalRosterPlans             
         END AS LeadRosterVisitsPercentage            
      ,[VisitingDates]            
      ,[PersonNames]            
     FROM             
     (SELECT             
        cl.Id            
       ,cl.LeadName            
       ,SUM(CASE WHEN clr.Id IS NOT NULL THEN 1 ELSE 0 END) AS LeadTotalRosterPlans            
       ,SUM(CASE WHEN clr.RosterStatusId = 2 THEN 1 ELSE 0 END) AS LeadVisited            
       ,ISNULL (STUFF((SELECT  ', ' + CAST(FORMAT([VisitingDate], 'MM/dd/yyyy') AS VARCHAR(10)) [text()]            
          FROM [dbo].[User] u            
          INNER JOIN [dbo].[CRMLeadRoster] t ON u.UserId = t.UserId            
          WHERE t.RosterStatusId = 2 AND t.LeadId = cl.Id   
          ORDER BY [VisitingDate] DESC  
          FOR XML PATH(''), TYPE)            
         .value('.','NVARCHAR(MAX)'),1,2,' '),'-') [VisitingDates]            
       ,ISNULL (STUFF((SELECT DISTINCT ', ' + CAST(Name AS VARCHAR(50)) [text()]            
          FROM [dbo].[User] u            
          INNER JOIN [dbo].[CRMLeadRoster] t ON u.UserId = t.UserId            
          WHERE t.RosterStatusId = 2 AND t.LeadId = cl.Id            
          FOR XML PATH(''), TYPE)            
         .value('.','NVARCHAR(MAX)'),1,2,' '),'-') [PersonNames]            
      FROM  [dbo].[CRMLead] cl            
      LEFT JOIN [dbo].[CRMLeadRoster] clr ON clr.LeadId = cl.Id            
               AND (@FromDate IS NULL OR (clr.RosterPlaningDate BETWEEN @FromDate AND @ToDate))            
                 
      WHERE cl.CompanyId = @CompanyId             
       AND (@Text IS NULL OR cl.LeadName LIKE '%' + @Text + '%')            
      GROUP BY cl.Id,            
         cl.LeadName) AS LeadFrequencyVisit            
     Order by LeadVisited DESC
	
            
            
     SELECT @TotalRecords = COUNT(1) FROM #LeadReport            
                     
     IF(@To = -1)            
      SET @To = @TotalRecords            
        
 UPDATE #LeadReport    
    SET TotalVisits = LEN(VisitingDates) - LEN(REPLACE(VisitingDates, ',', '')) + 1;    
                
     SELECT * FROM #LeadReport WHERE RowNumber BETWEEN @FROM AND @To 
	 order by TotalVisits desc
            
  END 