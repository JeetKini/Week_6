using COG.Core.CRM.CompanySetting;
using COG.Core.Security;
using COG.Core.Tags;
using COG.Data;
using COG.Data.Model;
using COG.Data.Model.Opportunities;
using COG.IException;
using COG.Service.Model;
using COG.Service.Model.CRM.Opportunity;
using COG.Service.Model.CRM.Report.ResponseModel;
using COG.Service.Model.CRM.Tag;
using COG.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;

namespace COG.Core.CRM.Opportunities
{
    public class OpportunityService : IOpportunityService
    {
        ILeadService _leadService;
        IOpportunityTagMappingService _opportunityTagMappingService;
        IOpportunityProductMappingService _opportunityProductMappingService;
        ITagService _tagService;
        ILeadTransactionService _leadTransactionService;
        IUserService _userService;
        ICompanyAppSettingService _companyAppSettingService;
        private ICRMSettingService _crmSettingService;
        private IPermissionService _permissionService;
        private IOpportunityReasonMappingService _opportunityReasonMappingService;
        private IOpportunityAttachmentMappingService _opportunityAttachmentMappingService;

        private static Dictionary<string, string> appSettings = IAppSettings.App_Settings.GetAppSettings();
        static string multimediaContentPath = appSettings.ContainsKey("SITE_URL") ? appSettings["SITE_URL"] : "";

        public OpportunityService(ILeadService leadService, IOpportunityTagMappingService opportunityTagMappingService
                                    , IOpportunityProductMappingService opportunityProductMappingService
                                    , ITagService tagService
                                    , ILeadTransactionService leadTransactionService
                                    , IUserService userService
                                    , ICompanyAppSettingService companyAppSettingService
                                    , ICRMSettingService crmSettingService
                                    , IPermissionService permissionService
                                    , IOpportunityReasonMappingService opportunityReasonMappingService
                                    , IOpportunityAttachmentMappingService opportunityAttachmentMappingService)
        {
            _leadService = leadService;
            _opportunityTagMappingService = opportunityTagMappingService;
            _opportunityProductMappingService = opportunityProductMappingService;
            _tagService = tagService;
            _leadTransactionService = leadTransactionService;
            _userService = userService;
            _companyAppSettingService = companyAppSettingService;
            _crmSettingService = crmSettingService;
            _permissionService = permissionService;
            _opportunityReasonMappingService = opportunityReasonMappingService;
            _opportunityAttachmentMappingService = opportunityAttachmentMappingService;
        }

        public ServiceResult AddOpportunity(string corpName, int companyId, int userId, AddOpportunityDto opportunityDto)
        {
            using (var db = SqlHelper.GetDbContext(corpName))
            {
                var result = new ServiceResult();
                try
                {
                    var totalRecords = (from o in db.Opportunities
                                        join c in db.CRMLeads on o.LeadId equals c.Id
                                        where c.CompanyId == companyId && o.OpportunityNumber != null 
                                        select o).Count()+1;
                    var opportunity = new Opportunity();
                    opportunity.OpportunityName = opportunityDto.OpportunityName.Trim();
                    opportunity.LeadId = opportunityDto.LeadId;
                    opportunity.CloseDate = opportunityDto.CloseDate;
                    opportunity.Value = opportunityDto.Value;
                    opportunity.AssignedToUserId = opportunityDto.AssignedToUserId;
                    opportunity.OpportunityStatusId = opportunityDto.OpportunityStatusId;
                    opportunity.Tags = string.IsNullOrEmpty(opportunityDto.Tags) ? string.Empty : opportunityDto.Tags.Trim();
                    opportunity.Notes = string.IsNullOrEmpty(opportunityDto.Notes) ? string.Empty : opportunityDto.Notes.Trim();
                    opportunity.OpportunityNumber = string.Format("{0:00}", totalRecords);
                    opportunity.CreatedBy = userId;
                    opportunity.ModifiedBy = userId;
                    opportunity.CreateDate = DateTime.UtcNow;
                    opportunity.ModifiedDate = DateTime.UtcNow;
                    opportunity.IsActive = true;

                    //ADDING ATTACHMENTS
                    if (opportunity.OpportunityStatusId == 3)
                    {
                        if (!string.IsNullOrEmpty(opportunityDto.MultimediaFileName))
                            opportunity.Quotation = _opportunityAttachmentMappingService.SaveAttachments(corpName, companyId, opportunityDto);
                    }

                    if (opportunity.OpportunityStatusId == 4)
                    {
                        if (!string.IsNullOrEmpty(opportunityDto.MultimediaFileName))
                            opportunity.WonOpportunity = _opportunityAttachmentMappingService.SaveAttachments(corpName, companyId, opportunityDto);
                    }

                    db.Opportunities.Add(opportunity);
                    db.SaveChanges();

                    Task.Run(() => _leadService.UpdateLeadStageOnAddingOpportunity(opportunityDto.LeadId, opportunity.OpportunityStatusId, userId));
                    Task.Run(() => AddLeadTransaction(corpName, companyId, opportunity));

                    if (opportunityDto.OpportunityProduct != null)
                        Task.Run(() => AddProduct(companyId, userId, opportunity.Id, opportunityDto.OpportunityProduct));

                    if (opportunityDto.OpportunityTags != null)
                        Task.Run(() => AddTag(companyId, userId, opportunity.Id, opportunityDto.OpportunityTags));

                    //Adding Reason
                    if (opportunity.OpportunityStatusId == 5)
                    {
                        //Call ReasonMappingService
                        Task.Run(() => _opportunityReasonMappingService.OpportunityReasonMapping(corpName, companyId, userId, opportunity.Id, opportunityDto.OpportunityReasons));

                    }

                    result.SetSuccess();
                }
                catch (Exception ex)
                {
                    ExceptionLogging.LogExceptionToDB(ex);
                    result.SetFailure(ex.Message);
                }

                return result;
            }
        }

        private void AddLeadTransaction(string corpName, int companyId, Opportunity opportunity)
        {
            var lrmObj = new UpdateLeadStatusModel
            {
                LeadId = opportunity.LeadId,
                UserId = opportunity.CreatedBy,
            };

            var assignedToUserName = _userService.GetUserById(corpName, opportunity.AssignedToUserId).Name;

            var crmSetting = _crmSettingService.GetCRMSetting(opportunity.CreatedBy);
            var currencySymybol = _companyAppSettingService.GetCompanyAppSetting(companyId, "CurrencySymbol").Value;

            string opportunityValueText = $@"value : {opportunity.Value} ";

            if (crmSetting.TargetFieldId == 1)  // Price
                opportunityValueText += currencySymybol;
            else
                opportunityValueText += crmSetting.DisplayUnitSymbol;


            lrmObj.Note = $"Opportunity : {opportunity.OpportunityName}"
                         + $" with {opportunityValueText}."
                         + $" Closing date {opportunity.CloseDate.ToString("dd-MMM-yyyy")}."
                         + $" This is assigned to {assignedToUserName}.";

            _leadTransactionService.CommentOnLeadTransactions(lrmObj);
        }

        public void AddProduct(int companyId, int userId, int opportunityId, List<ProductMappingDto> productMappingList)
        {
            foreach (var product in productMappingList)
            {
                _opportunityProductMappingService.AddOpportunityProductMapping(opportunityId, product.Id);
            }
        }

        // This is for adding new tags and mapping tags to opportunity
        public void AddTag(int companyId, int userId, int opportunityId, List<TagMappingDto> opportunityTagMapList)
        {
            foreach (var newTag in opportunityTagMapList)
            {
                // Add new tag
                var tag = _tagService.Add(companyId, userId, newTag.TagName);

                // Map tag to opportunity
                _opportunityTagMappingService.AddOpportunityTagMapping(opportunityId, tag.Id);
            }
        }

        public OpportunityDto GetOpportunity(string corpName, int companyId, int opportunityId)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var opportunityIdParameter = DataProvider.GetIntSqlParameter("OpportunityId", opportunityId);

                var opportunity = db.ExecuteStoredProcedureList<OpportunityDto>
                            ("USP_GetOpportunity", opportunityIdParameter).FirstOrDefault();

                var tags = _opportunityTagMappingService.GetOpportunityTags(opportunityId);

                var products = _opportunityProductMappingService.GetOpportunityProduct(opportunityId);

                var reasons = _opportunityReasonMappingService.GetOpportunityReasons(corpName, companyId, opportunityId);

                if (opportunity != null)
                {
                    opportunity.Tags = tags;
                    opportunity.OpportunityProduct = products;
                    opportunity.OpportunityReason = reasons;
                }

                return opportunity;
            }
        }

        public List<OpportunityListModel> GetOpportunities(int userId, GetOpportunitiesRequestDto reportDto)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {

                var userTimeZone = (new UserService()).GetUserTimeZone(userId);

                userId = reportDto.SalesUserId > 0 ? reportDto.SalesUserId : userId;
                //reportDto.FromDate = reportDto.FromDate.HasValue ? reportDto.FromDate : DateTime.UtcNow.Date.AddDays(-90);
                //reportDto.ToDate = reportDto.ToDate.HasValue ? reportDto.ToDate : DateTime.UtcNow.Date.AddDays(30);

                reportDto.ToDate = reportDto.ToDate == null ? DateTime.Now.AddDays(1).Date : UtilityHelper.ConvertToUTC((reportDto.ToDate.Value), userTimeZone);
                reportDto.FromDate = reportDto.FromDate == null ? reportDto.ToDate.Value.AddDays(-90) : UtilityHelper.ConvertToUTC((reportDto.FromDate.Value), userTimeZone);

                if (string.IsNullOrEmpty(reportDto.AssignedToUserIds))
                    reportDto.AssignedToUserIds = null;

                if (string.IsNullOrEmpty(reportDto.LeadIds))
                    reportDto.LeadIds = null;

                var userIdParameter = DataProvider.GetIntSqlParameter("UserId", userId);
                var fromDateParameter = DataProvider.GetDateSqlParameter("FromDate", reportDto.FromDate);
                var toDateParameter = DataProvider.GetDateSqlParameter("ToDate", reportDto.ToDate);
                var tagIdsParameter = DataProvider.GetStringSqlParameter("TagIds", reportDto.TagIds);
                var assignedToUserIdsParameter = DataProvider.GetStringSqlParameter("AssignedToUserIds", reportDto.AssignedToUserIds);
                var leadIdsParameter = DataProvider.GetStringSqlParameter("LeadIds", reportDto.LeadIds);
                var searchParameter = DataProvider.GetStringSqlParameter("Text", reportDto.Text);
                var dateTypeParameter = DataProvider.GetStringSqlParameter("DateType", reportDto.DateType);
                var opportunities = db.ExecuteStoredProcedureList<OpportunityListModel>
                    ("USP_GetOpportunities_Backup", userIdParameter, fromDateParameter, toDateParameter, tagIdsParameter
                    , assignedToUserIdsParameter, leadIdsParameter, searchParameter, dateTypeParameter);


                //ADDING ATTACHMENTS LINKS IN GET METHOD
                foreach (var item in opportunities)
                {
                    item.Quotation = (item.Quotation != null ? (multimediaContentPath + item.Quotation) : "");  
                    
                    item.WonOpportunity = (item.WonOpportunity != null ? (multimediaContentPath + item.WonOpportunity) : "");
                }

                return opportunities;
            }
        }

        public ServiceResult UpdateOpportunity(string corpName, int companyId, int userId, AddOpportunityDto reqObj)
        {
            var result = new ServiceResult();
            try
            {
                Opportunity opportunity;

                var isAccessAllUserData = _permissionService.Authorize(PermissionProvider.AllUsersData);

                using (var db = SqlHelper.GetDbContext(corpName))
                {
                    opportunity = db.Opportunities
                       .Where(x => x.Id == reqObj.Id &&
                       ((x.AssignedToUserId == userId || x.CreatedBy == userId)
                            || (isAccessAllUserData))
                       ).FirstOrDefault();
                }

                var opportunityStatusId = opportunity.OpportunityStatusId;

                if (opportunity != null)
                {
                    using (var db = SqlHelper.GetDbContext(corpName))
                    {
                        opportunity.OpportunityName = string.IsNullOrEmpty(reqObj.OpportunityName) ? opportunity.OpportunityName : reqObj.OpportunityName.Trim();
                        opportunity.OpportunityStatusId = reqObj.OpportunityStatusId;
                        opportunity.LeadId = reqObj.LeadId;
                        opportunity.AssignedToUserId = reqObj.AssignedToUserId == 0 ? opportunity.AssignedToUserId : reqObj.AssignedToUserId;
                        opportunity.Value = reqObj.Value;
                        opportunity.CloseDate = reqObj.CloseDate;
                        opportunity.Tags = string.IsNullOrEmpty(reqObj.Tags) ? opportunity.Tags : reqObj.Tags.Trim();
                        opportunity.ModifiedDate = DateTime.UtcNow;
                        opportunity.ModifiedBy = userId;
                        opportunity.Notes = string.IsNullOrEmpty(reqObj.Notes) ? opportunity.Notes : reqObj.Notes;

                        //ADDING ATTACHMENTS
                        if (opportunity.OpportunityStatusId == 3)
                        {
                            if (!string.IsNullOrEmpty(reqObj.MultimediaFileName))
                                opportunity.Quotation = _opportunityAttachmentMappingService.SaveAttachments(corpName, companyId, reqObj);
                            if (reqObj.IsFileAttached == false)
                            {
                                opportunity.Quotation = "";
                            }
                        }

                        if (opportunity.OpportunityStatusId == 4)
                        {
                            if (!string.IsNullOrEmpty(reqObj.MultimediaFileName))
                                opportunity.WonOpportunity = _opportunityAttachmentMappingService.SaveAttachments(corpName, companyId, reqObj);
                            if (reqObj.IsFileAttached == false)
                            {
                                opportunity.WonOpportunity = "";
                            }
                        }

                        if (reqObj.IsRemovedAttachment == false)
                        {
                            opportunity.Quotation = "";
                            opportunity.WonOpportunity = "";
                        }

                        db.Entry(opportunity).State = EntityState.Modified;
                        db.SaveChanges();


                        //UpdateLeadStageOnUpdatingOpportunities

                        Task.Run(() => UpdateOpportunityLeadTransaction(corpName, userId, opportunity, opportunityStatusId));

                        if (reqObj.OpportunityStatusId == 4)
                        {
                            var lead = db.CRMLeads.AsNoTracking().Where(x => x.Id == reqObj.LeadId).FirstOrDefault();

                            var previousLeadStage = lead.LeadStageId;

                            if (lead.LeadStageId == 2)
                            {
                                lead.LeadStageId = 3;
                                lead.ModifiedDate = DateTime.UtcNow;
                                db.Entry(lead).State = EntityState.Modified;
                                db.SaveChanges();

                                Task.Run(() => UpdateLeadStageTransaction(corpName, companyId, userId, reqObj.LeadId, previousLeadStage));
                            }
                        }
                    }

                    if (reqObj.OpportunityTags != null)
                    {
                        // Remove tag mapping
                        foreach (var tag in reqObj.OpportunityTags.Where(x => x.IsActive == false))
                        {
                            _opportunityTagMappingService.DeleteOpportunityTagMapping(opportunity.Id, tag.TagId);
                        }

                        // Add tag mapping
                        foreach (var tag in reqObj.OpportunityTags.Where(x => x.IsActive == true))
                        {
                            int tagId = tag.TagId;
                            if (tag.TagId == -1)
                            {
                                var newTag = _tagService.Add(companyId, userId, tag.TagName);
                                tagId = newTag.Id;
                            }


                            _opportunityTagMappingService.AddOpportunityTagMapping(opportunity.Id, tagId);
                        }

                    }
                    _opportunityProductMappingService.DeleteOpportunityProductMappingAll(opportunity.Id);
                    if (reqObj.OpportunityProduct != null)
                    {

                        // Add product mapping
                        foreach (var product in reqObj.OpportunityProduct)
                        {
                            _opportunityProductMappingService.AddOpportunityProductMapping(opportunity.Id, product.Id);
                        }

                    }

                    if (reqObj.OpportunityStatusId == 5)
                    {
                        //Add Reason Mapping
                        Task.Run(() => _opportunityReasonMappingService.OpportunityReasonMapping(corpName, companyId, userId, reqObj.Id, reqObj.OpportunityReasons));

                    }

                    result.SetSuccess();
                }
                else
                    result.SetAccessDenied();
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
                result.SetFailure(ex.Message);
            }

            return result;
        }

        private void UpdateOpportunityLeadTransaction(string corpName, int userId, Opportunity opportunity, int opportunityStatusId)
        {
            using (var db = SqlHelper.GetDbContext(corpName))
            {
                var lrmObj = new UpdateLeadStatusModel
                {
                    LeadId = opportunity.LeadId,
                    UserId = userId,
                };

                var opportunityStatus = db.OpportunityStatusMasters.AsNoTracking().Where(x => x.Id == opportunity.OpportunityStatusId)
                                       .Select(x => x.Status).FirstOrDefault();

                var previousOpportunityStatus = db.OpportunityStatusMasters.AsNoTracking().Where(x => x.Id == opportunityStatusId)
                                      .Select(x => x.Status).FirstOrDefault();

                lrmObj.Note = $"Opportunity has been updated: {previousOpportunityStatus} to {opportunityStatus}.";

                _leadTransactionService.CommentOnLeadTransactions(lrmObj);
            }
        }

        public void UpdateLeadStageTransaction(string corpName, int companyId, int userId, int leadId, int previousLeadStageId)
        {
            using (var db = SqlHelper.GetDbContext(corpName))
            {
                var lrmObj = new UpdateLeadStatusModel
                {
                    LeadId = leadId,
                    UserId = userId,
                };
                var leadStage = db.CRMLeadStage.AsNoTracking().Where(x => x.Id == 3 && x.IsDefault == true).Select(x => x.Name).FirstOrDefault();

                var previousLeadStage = db.CRMLeadStage.AsNoTracking().Where(x => x.Id == previousLeadStageId && x.IsDefault == true).Select(x => x.Name).FirstOrDefault();

                lrmObj.Note = $"Customer stage has been updated: {previousLeadStage} to {leadStage}.";

                _leadTransactionService.CommentOnLeadTransactions(lrmObj);
            }
        }

        public ServiceResult DeleteOpportunity(int userId, int opportunityId)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var result = new ServiceResult();

                var opportunity = db.Opportunities.Where(x => x.Id == opportunityId).FirstOrDefault();

                if (opportunity != null)
                {
                    if (opportunity.CreatedBy == userId)
                    {
                        opportunity.IsActive = false;
                        opportunity.ModifiedDate = DateTime.UtcNow;
                        opportunity.ModifiedBy = userId;

                        db.Entry(opportunity).State = EntityState.Modified;
                        db.SaveChanges();

                        result.SetSuccess();
                    }
                    else
                    {
                        result.SetAccessDenied();
                    }
                }

                return result;
            }
        }

        public ServiceResult<List<OpportunityStatusMaster>> GetOpportunityStatusMaster()
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var result = new ServiceResult<List<OpportunityStatusMaster>>();
                var oppMasters = db.OpportunityStatusMasters.Where(x => x.IsActive == true).ToList();

                result.SetSuccess(oppMasters);
                return result;
            }
        }

        public List<ReportCommonModel> GetOpportunitiesReportByDate(int userId, GetSalesReport salesReport)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {

                var report = new List<ReportCommonModel>();
                try
                {
                    var callLogUserId = salesReport.SalesUserId > 0 ? salesReport.SalesUserId : userId;

                    var userIdParameter = DataProvider.GetIntSqlParameter("UserId", callLogUserId);
                    var fromDateParameter = DataProvider.GetDateSqlParameter("FromDate", salesReport.FromDate);
                    var toDateParameter = DataProvider.GetDateSqlParameter("ToDate", salesReport.ToDate);

                    report = db.ExecuteStoredProcedureList<ReportCommonModel>("CRM_USP_GetOpportunitiesReportByDate",
                        userIdParameter, fromDateParameter, toDateParameter).ToList();
                }
                catch (Exception ex)
                {
                    ExceptionLogging.LogExceptionToDB(ex);
                }

                return report;
            }
        }

        public List<ReportByTeamModel> GetOpportunityReportByTeam(int userId, GetOpportunitiesRequestDto salesReport)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var reports = new List<ReportByTeamModel>();
                try
                {
                    salesReport.ToDate = salesReport.ToDate ?? DateTime.Now.AddDays(1).Date;
                    salesReport.FromDate = salesReport.FromDate ?? salesReport.ToDate.Value.AddDays(-6);

                    salesReport.SalesUserId = salesReport.SalesUserId == 0 ? userId : salesReport.SalesUserId;

                    var userIdParameter = DataProvider.GetIntSqlParameter("UserId", salesReport.SalesUserId);
                    var userLevelParameter = DataProvider.GetIntSqlParameter("UserLevel", 1);
                    var fromDateParameter = DataProvider.GetDateSqlParameter("FromDate", salesReport.FromDate);
                    var toDateParameter = DataProvider.GetDateSqlParameter("ToDate", salesReport.ToDate);

                    reports = db.ExecuteStoredProcedureList<ReportByTeamModel>("CRM_USP_OpportunityReportByTeam",
                        userIdParameter, userLevelParameter, fromDateParameter, toDateParameter).ToList();
                }
                catch (Exception ex)
                {
                    ExceptionLogging.LogExceptionToDB(ex);
                }

                return reports;
            }
        }

        public List<OpportunityReportSummaryByStageDto> GetOpportunityReportSummaryByStatus(int userId, GetOpportunitiesRequestDto salesReport)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var reports = new List<OpportunityReportSummaryByStageDto>();
                try
                {
                    salesReport.ToDate = salesReport.ToDate ?? DateTime.Now.AddDays(1).Date;
                    salesReport.FromDate = salesReport.FromDate ?? salesReport.ToDate.Value.AddDays(-90);

                    salesReport.SalesUserId = salesReport.SalesUserId == 0 ? userId : salesReport.SalesUserId;

                    var userIdParameter = DataProvider.GetIntSqlParameter("UserId", salesReport.SalesUserId);
                    var fromDateParameter = DataProvider.GetDateSqlParameter("FromDate", salesReport.FromDate);
                    var toDateParameter = DataProvider.GetDateSqlParameter("ToDate", salesReport.ToDate);

                    reports = db.ExecuteStoredProcedureList<OpportunityReportSummaryByStageDto>("CRM_USP_GetOpportunityReportSummaryByStage",
                        userIdParameter, fromDateParameter, toDateParameter).ToList();
                }
                catch (Exception ex)
                {
                    ExceptionLogging.LogExceptionToDB(ex);
                }

                return reports;
            }
        }

        public List<OpportunityReportSummaryForAllUsersDto> GetOpportunityReportSummaryForAllUsers(int userId, GetReportCommonDto salesReport)
        {
            using (ChatOnGoEntities db = new ChatOnGoEntities())
            {
                var reports = new List<OpportunityReportSummaryForAllUsersDto>();
                try
                {
                    salesReport.ToDate = salesReport.ToDate ?? DateTime.Now.AddDays(1).Date;
                    salesReport.FromDate = salesReport.FromDate ?? salesReport.ToDate.Value.AddDays(-90);

                    salesReport.SalesUserId = salesReport.SalesUserId == 0 ? userId : salesReport.SalesUserId;

                    var userIdParameter = DataProvider.GetIntSqlParameter("UserId", salesReport.SalesUserId);
                    var fromDateParameter = DataProvider.GetDateSqlParameter("FromDate", salesReport.FromDate);
                    var toDateParameter = DataProvider.GetDateSqlParameter("ToDate", salesReport.ToDate);

                    reports = db.ExecuteStoredProcedureList<OpportunityReportSummaryForAllUsersDto>("CRM_USP_GetOpportunityReportSummaryForAllUsers",
                        userIdParameter, fromDateParameter, toDateParameter).ToList();
                }
                catch (Exception ex)
                {
                    ExceptionLogging.LogExceptionToDB(ex);
                }

                return reports;
            }
        }
    }
}
