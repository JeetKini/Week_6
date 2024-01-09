ALTER PROC [dbo].[USP_GetOpportunities]                         
@UserId INT            
,@FromDate DATE           
,@ToDate DATE             
,@TagIds VARCHAR(100)                                                
,@AssignedToUserIds VARCHAR(100)                                                   
,@LeadIds VARCHAR(100)                      
,@Text VARCHAR(100)        
--,@DateType VARCHAR(100)        
--,@ManagerIds VARCHAR(100)                                   
AS                                                      
BEGIN                  
        
                        
 IF (@TagIds = '')                        
  SET @TagIds = NULL                        
 IF (@LeadIds = '')                        
  SET @LeadIds = NULL                        
 --IF (@ManagerIds = '')                        
 -- SET @ManagerIds = NULL                        
 IF (@AssignedToUserIds = '')                        
  SET @AssignedToUserIds = NULL                        
                                                      
 --CREATE TABLE #TempTeam                                                            
 --(                                                            
 -- RowNumber INT                                                            
 -- ,UserId INT                                                            
 -- ,UserName VARCHAR(50)                                                            
 -- ,ParentId INT                                                            
 -- ,ManagerName VARCHAR(50)                                                            
 -- ,UserLevel INT                                                            
 --)                                                            
                                                      
 IF(@FromDate IS NULL)                                                  
  SET @FromDate = DATEADD(D,-90, GETDATE())                                                  
                                                      
 DECLARE @totalUsers INT = 0                                                      
                                              
  --INSERT INTO #TempTeam                                                                                         
  -- SELECT                                             
  -- ROW_NUMBER() OVER(ORDER BY UserLevel ASC)                                            
  -- ,*                                             
  -- FROM fn_GetMyTeam(@UserId)                              
                                    
 SELECT                             
  DISTINCT o.Id                            
  ,o.LeadId                            
  ,lead.LeadName                              
  ,lead.MobileNo AS MobileNo                              
  ,o.OpportunityName                                                      
  ,o.OpportunityStatusId, os.Status                                                      
  ,o.Value                                                      
  ,o.AssignedToUserId                                                    
  ,team.UserName AS AssignedToUserName                                                    
  ,o.CreatedBy                                                    
  ,CAST(FORMAT(o.CloseDate, 'MM-dd-yyyy') AS VARCHAR(10)) AS CloseDate                                                      
  ,CAST(FORMAT(o.CreateDate, 'MM-dd-yyyy hh:mm:ss tt') AS VARCHAR(30)) AS CreateDate                                                    
  ,u.Name AS CreatedByUser                                   
  ,o.Notes                        
  ,o.Quotation                        
  ,o.WonOpportunity                
  ,o.OpportunityNumber        
          
 FROM                             
  Opportunity o                                                       
  INNER JOIN CRMLead lead ON lead.Id = o.LeadId                                                      
  INNER JOIN OpportunityStatusMaster os ON os.Id = o.OpportunityStatusId                                                      
  INNER JOIN fn_GetMyTeam(@UserId) team ON team.UserId = o.AssignedToUserId                                                      
  INNER JOIN [User] u ON u.UserId = o.CreatedBy                                                    
  LEFT JOIN OpportunityTagMapping otm ON otm.OpportunityId = o.Id                                          
WHERE                        
  o.IsActive = 1       
  AND (cast(o.CreateDate as date) >= @FromDate AND (@ToDate IS NULL OR cast(o.CreateDate as date) <= @ToDate))      
  AND( @TagIds IS NULL OR otm.TagId IN (SELECT * FROM [SplitString](@TagIds,',')))                       
  AND (@LeadIds IS NULL OR o.LeadId IN (SELECT Id FROM dbo.SplitString(@LeadIds,',')))
  AND (@AssignedToUserIds IS NULL OR o.AssignedToUserId IN (SELECT * FROM [SplitString](@AssignedToUserIds,',')))
 ORDER BY o.id        
         
 --WHERE                             
 -- o.IsActive = 1         
 -- AND case         
 -- when @FilterType=o.CreateDate        
 -- then (cast(o.CreateDate as date) = @FromDate AND (@ToDate IS NULL OR cast(o.CreateDate as date) <= @ToDate))         
 -- when         
 -- AND( @TagIds IS NULL OR otm.TagId IN (SELECT * FROM [SplitString](@TagIds,',')))                         
 -- AND (@LeadIds IS NULL OR o.LeadId IN (SELECT Id FROM dbo.SplitString(@LeadIds,',')))                        
 --ORDER BY o.id         
         
        
                                                   
END 