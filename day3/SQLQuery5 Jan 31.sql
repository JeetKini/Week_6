SP_HELPTEXT USP_DF_GetDynamicFormAnswer

  
  EXEC USP_DF_GetDynamicFormAnswer NULL,NULL,NULL,121  
    
/****** Object:  StoredProcedure [dbo].[USP_DF_GetDynamicFormAnswer] - RJ  Script Date: 10/07/2017 03:07:57 PM ******/    
CREATE PROC [dbo].[USP_DF_GetDynamicFormAnswer]    
@FormId INT    
,@LeadTransactionId INT    
,@UserId INT    
,@FormFilledId INT    
AS    
    
 IF(@LeadTransactionId = 0)    
  SET @LeadTransactionId = NULL    
 ELSE    
  SELECT TOP 1 @FormFilledId = FormFilledId FROM DynamicFormData WHERE LeadTransactionId = @LeadTransactionId    
    
 IF(@FormFilledId = 0)    
  SET @FormFilledId = NULL    
    
 SELECT TOP 1 @FormId = FormId FROM DynamicFormFilledBy WHERE Id = @FormFilledId    
    
    
--PRINT '@FormId ' + CAST(ISNULL(@FormId,'11111') AS VARCHAR(100))    
--PRINT '@LeadTransactionId ' + CAST(ISNULL(@LeadTransactionId,'22222') AS VARCHAR(100))    
--PRINT '@FormFilledId ' + CAST(@FormFilledId AS VARCHAR(100))    
    
 SELECT  f.Id    
   ,ModuleType    
   ,f.Title AS FormTitle    
   ,StartDate    
   ,ExpiryDate    
   ,IsOneTime    
   ,IsGeoTag    
   ,f.IsActive    
   ,s.Id AS SectionId    
   ,s.SectionSequence    
   ,s.Title AS SectionTitle    
   ,c.Id AS ControlId    
   ,c.[ControlSequence]    
   ,c.Label    
   ,c.Type AS ControlType    
   ,c.Placeholder    
   ,c.IsMandatory    
   ,c.[IsRangeBound]    
   ,c.StartRange    
   ,c.EndRange    
   ,o.Id AS OptionId    
   ,o.OptionText    
   ,d.OptionId AS SelectedOptionId    
   ,ISNULL(d.Text,'') AS SelectedText    
 FROM DynamicForms f    
 INNER JOIN DynamicFormsSections s ON f.Id = s.FormId AND s.IsActive = 1    
 INNER JOIN DynamicFormsSectionsControls c ON c.SectionId = s.Id AND c.IsActive = 1    
 LEFT JOIN DynamicFormsSectionsControlsOptions o ON o.ControlId = c.Id AND o.IsActive = 1    
 LEFT JOIN DynamicFormData d ON d.FormId = f.Id AND d.FormControlId = c.Id     
     AND(d.OptionId IS NULL OR d.OptionId = o.Id)    
      AND d.UserId = @UserId    
      AND FormFilledId = @FormFilledId    
 WHERE  f.Id = @FormId    
 AND c.Type  NOT IN ('FFC_MasterLeadId', 'FFC_ProductId','FFC_MasterUserId')  