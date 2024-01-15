use FFCTest

sp_helptext CRM_Usp_GetLeads_Backup

EXEC CRM_Usp_GetLeads_Backup 1248,NULL,1,20,NULL,NULL,NULL,NULL,'109',NULL,NULL,NULL,NULL,NULL,NULL,'2024-01-09','2024-01-15','CreatedDate',0

ALTER PROC [dbo].[CRM_Usp_GetLeads_Backup]                      
@UserId INT             
 ,@StatusIds VARCHAR(50)            
 ,@From INT                   
 ,@To INT                   
 ,@Text VARCHAR(100)             
 ,@StateId INT                   
 ,@CityId INT               
 ,@LocalityIds VARCHAR(50)            
 ,@TagIds VARCHAR(100)                
 ,@TerritoryIds VARCHAR(100)               
 ,@LeadSourceIds VARCHAR(100)                 
 ,@LeadStageIds VARCHAR(100)                 
 ,@AssignedToUserIds VARCHAR(100)             
 ,@LeadTypesIds VARCHAR(100)                
 ,@ReferBy VARCHAR(500)            
 ,@FromDate DATETIME                         
 ,@ToDate DATETIME               
 ,@DateType VARCHAR(50)              
 ,@TotalRecords INT OUTPUT                          
AS                          
BEGIN                          
 SET NOCOUNT ON;                          
 SET TRANSACTION ISOLATION LEVEL READ UNCOMMITTED                          
                          
 DECLARE @RoleId INT = 0                          
  ,@CompanyId INT = 0                          
  ,@IsCompanyOwner BIT = 0                          
  ,@Total INT = 0;                          
                         
 IF(@ReferBy IS NOT NULL OR @ReferBy != '')                        
 BEGIN                        
                        
 SELECT                         
 @CompanyId = CompanyId                         
 FROM ReferralUsers                        
 WHERE Id IN (SELECT [Value] FROM STRING_SPLIT(@ReferBy, ','))                        
                        
 SELECT @UserId = UserId                        
 FROM Shop                        
 WHERE ShopId = @CompanyId                        
    print @UserId                   
 print @CompanyId                  
 END                        
                         
 SELECT @RoleId = RoleId                          
  ,@CompanyId = CompanyId                          
 FROM [dbo].[CRMMemberRoleMapping]                          
 WHERE UserId = @UserId                          
          
 IF(@FromDate IS NULL)                                                                
  SET @FromDate = DATEADD(D,-90, GETDATE())           
            
 IF (@RoleId = 1)                          
  SET @IsCompanyOwner = 1                          
                          
 CREATE TABLE #TempTeam (                          
  RowNumber BIGINT                          
  ,UserId INT                          
  ,UserName VARCHAR(50)                          
  ,ParentId INT                          
  ,ManagerName VARCHAR(50)                          
  ,UserLevel INT                          
  )                          
                          
 INSERT INTO #TempTeam                          
 EXEC [dbo].[CRM_Usp_GetMyTeam] @UserId                          
  ,1                          
  ,- 1                          
  ,NULL                          
  ,@Total                          
                  
 DECLARE @IsViewAllUsersData BIT = [dbo].[IsViewAllUsersData](@UserId)               
            
 SELECT @TotalRecords = COUNT(1)                          
 FROM (                          
  SELECT cl.[Id]                          
  FROM [dbo].[CRMLead] cl                          
  INNER JOIN [dbo].[CRMLeadStatus] cls ON cls.Id = cl.[StatusId]                          
  LEFT JOIN [dbo].[CRMLeadMapping] clm ON cl.Id = clm.LeadId                          
  LEFT JOIN [dbo].[User] u ON u.UserId = clm.[UserId]                          
  LEFT JOIN #TempTeam t ON t.UserId = clm.[UserId]                          
  LEFT JOIN [dbo].[Country] c ON c.Id = cl.[CountryId]                          
  LEFT JOIN [dbo].[State] s ON s.[StateId] = cl.[StateId]                          
  LEFT JOIN [dbo].[City] ci ON ci.[CityId] = cl.[CityId]                          
  LEFT JOIN [dbo].[Locality] l ON l.[LocalityId] = cl.[LocalityId]                          
  LEFT JOIN [dbo].[CRMLeadType] lt ON lt.[Id] = cl.[LeadTypeId]                          
  LEFT JOIN [dbo].[CRMLeadStage] ls ON ls.[Id] = cl.[LeadStageId]              
  LEFT JOIN [dbo].[CRMLead] parentLead ON cl.ParentLeadId = parentLead.Id                          
  LEFT JOIN [dbo].[ReferralUsers] ru ON cl.ReferredBy = ru.Id                          
  LEFT JOIN [dbo].[LeadTagMapping] ltm ON ltm.LeadId = cl.Id                                                  
  WHERE cl.CompanyId = @CompanyId                          
   AND cl.IsActive = 1             
   AND (@DateType = 'CreatedDate' AND CAST(cl.CreatedDate AS DATETIME) >= @FromDate AND CAST(cl.CreatedDate AS DATETIME) <= @ToDate) OR                      
  (@DateType = 'ModifiedDate' AND CAST(cl.ModifiedDate AS DATETIME) >= @FromDate AND CAST(cl.ModifiedDate AS DATETIME) <= @ToDate)               
   AND (                           
   @IsCompanyOwner = 1                          
    OR            
 (                          
     clm.UserId IS NOT NULL                          
     AND t.UserId IS NOT NULL                          
     )                          
  OR @IsViewAllUsersData = 1            
    OR cl.IsVisibleToAll = 1                          
    )                          
   AND (                          
    @StatusIds IS NULL                          
    OR cl.[StatusId] IN (                          
     SELECT Id                          
     FROM dbo.SplitString(@StatusIds, ',')                          
     )                          
    )                          
   AND (                          
    @Text IS NULL                          
    OR (                          
     cl.LeadName LIKE '%' + @Text + '%'                
     OR u.[Name] LIKE '%' + @Text + '%'                          
     OR l.LocalityName LIKE '%' + @Text + '%'                          
     OR ci.[Name] LIKE '%' + @Text + '%'                          
     OR cl.[PersonName] LIKE '%' + @Text + '%'                          
     OR cl.MobileNo LIKE '%' + @Text + '%'                          
     OR cl.Email LIKE '%' + @Text + '%'                          
     OR cl.RefNo LIKE '%' + @Text + '%'                          
     )                          
    )                          
   AND (                          
    @StateId IS NULL                          
    OR cl.StateId IN (@StateId)                          
    )                          
   AND (                          
    @CityId IS NULL                          
    OR cl.CityId IN (@CityId)                          
    )                          
   AND (                          
    @LocalityIds IS NULL                          
    OR cl.LocalityId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LocalityIds, ',')                          
     )                          
    )                          
   AND (                          
    @TerritoryIds IS NULL                          
    OR cl.TerritoryId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@TerritoryIds, ',')                          
     )                          
    )                          
   AND (                          
    @LeadSourceIds IS NULL                          
    OR cl.LeadSourceId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LeadSourceIds, ',')                          
     )                          
    )                          
   AND (                          
    @LeadStageIds IS NULL                          
    OR cl.LeadStageId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LeadStageIds, ',')                          
     )                          
    )                          
   AND (                          
    @AssignedToUserIds IS NULL                          
    OR clm.UserId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@AssignedToUserIds, ',')                          
     )       
    )                          
   AND (                          
    @LeadTypesIds IS NULL                          
    OR cl.LeadTypeId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LeadTypesIds, ',')                          
     )                          
    )                          
AND (                          
    @TagIds IS NULL                          
    OR EXISTS (                          
      SELECT 1                        
     FROM LeadTagMapping 
     WHERE ltm.LeadId = cl.Id
        AND ltm.TagId IN (       
       SELECT Value                        
       FROM STRING_SPLIT(@TagIds, ',')                          
       )                          
)                          
    )                          
   AND (                           
    @ReferBy IS NULL                           
    OR (                           
     cl.ReferredBy IN (                           
      SELECT Value                           
      FROM STRING_SPLIT(@ReferBy, ',')                           
      )                           
     )                           
    )                          
  ) a                          
                          
 IF (@To = - 1)                          
  SET @To = @TotalRecords;               
              
 WITH tempView                          
 AS (                          
  SELECT ROW_NUMBER() OVER (                          
    ORDER BY cl.[ModifiedDate] DESC                          
    ) AS RowNumber                          
   ,cl.[Id] AS Id                          
   ,(cl.[LeadName]) AS LeadName                          
   ,(cl.[RefNo]) [RefNo]                          
   ,(cl.[PersonName]) [PersonName]                          
   ,(cl.[PersonDesignation]) [PersonDesignation]                          
   ,(cl.[Address]) [Address]                          
   ,(cl.[StatusId]) [StatusId]                          
   ,(cls.[Status]) [Status]                          
   ,(cl.[CityId]) [CityId]                          
   ,(COALESCE(ci.Name, '-')) AS CityName                          
   ,(cl.[StateId]) [StateId]                          
   ,(COALESCE(s.Name, '-')) AS StateName                          
   ,(cl.[CountryId]) [CountryId]                          
   ,(COALESCE(c.Name, '-')) AS CountryName                          
   ,(cl.[MobileNo]) [MobileNo]                          
   ,(cl.[ContactNo]) [ContactNo]                          
   ,(cl.[Email]) [Email]                          
   ,(cl.[LocalityId]) [LocalityId]                          
   ,(COALESCE(l.[LocalityName], '-')) AS [LocalityName]         
   ,(cl.[CreatedDate]) [CreatedDate]           
   ,(cl.[ModifiedDate]) [ModifiedDate]           
   ,(COALESCE(clm.[UserId], 0)) AS [UserId]                          
   ,(COALESCE(u.Name, '-')) AS [UserName]                          
   ,(cl.[PinCode]) [PinCode]                          
   ,(cl.LeadTypeId) LeadTypeId                          
   ,(ISNULL(lt.[TypeName], '')) AS [LeadTypeName]                          
   ,(cl.LeadStageId) LeadStageId                          
   ,(ISNULL(ls.[Name], '')) AS [LeadStageName]                          
   ,(cl.ParentLeadId) ParentLeadId                          
   ,(ISNULL(parentLead.LeadName, '')) AS ParentLeadName                       
   ,COALESCE(ru.Id,0) as ReferralUserId                      
   ,(ISNULL(ru.UserName, '')) AS ReferralUserName                      
  FROM [dbo].[CRMLead] cl                          
  INNER JOIN [dbo].[CRMLeadStatus] cls ON cls.Id = cl.[StatusId]                          
  LEFT JOIN [dbo].[CRMLeadMapping] clm ON cl.Id = clm.[LeadId]                          
  LEFT JOIN #TempTeam t ON t.UserId = clm.[UserId]                          
  LEFT JOIN [dbo].[User] u ON u.UserId = clm.[UserId]                          
  LEFT JOIN [dbo].[Country] c ON c.Id = cl.[CountryId]                          
  LEFT JOIN [dbo].[State] s ON s.[StateId] = cl.[StateId]      
  LEFT JOIN [dbo].[City] ci ON ci.[CityId] = cl.[CityId]                          
  LEFT JOIN [dbo].[Locality] l ON l.[LocalityId] = cl.[LocalityId]                          
  LEFT JOIN [dbo].[CRMLeadType] lt ON lt.[Id] = cl.[LeadTypeId]                          
  LEFT JOIN [dbo].[CRMLeadStage] ls ON ls.[Id] = cl.[LeadStageId]                          
  LEFT JOIN [dbo].[CRMLead] parentLead ON cl.ParentLeadId = parentLead.Id                          
  LEFT JOIN [dbo].[ReferralUsers] ru ON cl.ReferredBy = ru.Id                          
  LEFT JOIN [dbo].[LeadTagMapping] ltm ON  cl.Id = ltm.LeadId                                                 
  WHERE cl.CompanyId = @CompanyId                          
   AND cl.IsActive = 1              
   AND (        
   (@DateType = 'CreatedDate' AND CAST(cl.CreatedDate AS DATETIME) >= @FromDate AND CAST(cl.CreatedDate AS DATETIME) <= @ToDate) OR                      
   (@DateType = 'ModifiedDate' AND CAST(cl.ModifiedDate AS DATETIME) >= @FromDate AND CAST(cl.ModifiedDate AS DATETIME) <= @ToDate)           
   )        
   AND (                     
    @IsCompanyOwner = 1                          
    OR (                          
     clm.UserId IS NOT NULL                          
     AND t.UserId IS NOT NULL                          
     )            
  OR @IsViewAllUsersData = 1            
    OR cl.IsVisibleToAll = 1                          
    )                          
   AND (                          
    @StatusIds IS NULL                          
    OR cl.[StatusId] IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@StatusIds, ',')                          
     )                          
    )                          
   AND (                          
    @Text IS NULL                          
    OR (                          
     cl.LeadName LIKE '%' + @Text + '%'                          
     OR u.Name LIKE '%' + @Text + '%'                          
     OR l.LocalityName LIKE '%' + @Text + '%'                          
     OR ci.Name LIKE '%' + @Text + '%'                          
     OR cl.[PersonName] LIKE '%' + @Text + '%'                          
     OR cl.MobileNo LIKE '%' + @Text + '%'                          
     OR cl.Email LIKE '%' + @Text + '%'                          
     OR cl.RefNo LIKE '%' + @Text + '%'                          
     )                          
    )                          
   AND (                          
    @StateId IS NULL                          
    OR cl.StateId IN (@StateId)                          
    )                          
   AND (                          
    @CityId IS NULL                          
    OR cl.CityId IN (@CityId)                          
    )                          
   AND (                          
    @LocalityIds IS NULL                          
    OR cl.LocalityId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LocalityIds, ',')                          
     )                          
    )                          
   AND (                          
    @TerritoryIds IS NULL                          
    OR cl.TerritoryId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@TerritoryIds, ',')                          
     )                          
    )                          
   AND (                          
    @LeadSourceIds IS NULL                          
    OR cl.LeadSourceId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LeadSourceIds, ',')                          
     )                          
    )                          
   AND (                          
    @LeadStageIds IS NULL                          
    OR cl.LeadStageId IN (                          
     SELECT Value                          
    FROM STRING_SPLIT(@LeadStageIds, ',')                          
     )                          
    )                        
   AND (                          
    @AssignedToUserIds IS NULL                          
    OR clm.UserId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@AssignedToUserIds, ',')                          
     )                          
    )                          
   AND (                          
    @LeadTypesIds IS NULL                          
    OR cl.LeadTypeId IN (                          
     SELECT Value                          
     FROM STRING_SPLIT(@LeadTypesIds, ',')                          
     )                          
    )                          
   AND (                  
    @TagIds IS NULL                          
    OR EXISTS (                          
     SELECT 1                        
     FROM LeadTagMapping 
     WHERE ltm.LeadId = cl.Id
        AND ltm.TagId IN (       
       SELECT Value                        
       FROM STRING_SPLIT(@TagIds, ',')                       
       )                          
     )                          
    )                          
   AND (                           
    @ReferBy IS NULL                           
    OR (                           
     cl.ReferredBy IN (                           
      SELECT Value                           
      FROM STRING_SPLIT(@ReferBy, ',')                           
      )                        
     )                           
    )                          
  )                          
 SELECT *                          
 FROM tempView                          
 WHERE (                          
   RowNumber BETWEEN @From                        
 AND @To                          
   )                          
END 
