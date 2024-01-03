using COG.Data;
using COG.Data.Model;
using COG.Data.Model.New_Expense_Setting;
using COG.IException;
using COG.Service.Model;
using COG.Service.Model.New_Expense_Setting;
using COG.Utility;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace COG.Core.New_Expense_Setting
{
    public class WorkstationService : IWorkstationService
    {
        public async Task<ServiceResult<bool>> AddWorkstation(string corpName, int companyId, int userId, WorkstationModel workstationModel)
        {
            var serviceResult = new ServiceResult<bool>();
            try
            {
                using (var db = SqlHelper.GetDbContext(corpName))
                {
                    
                    var existingWorkstation = await db.Workstations
                        .FirstOrDefaultAsync(w => w.Name.ToLower().Equals(workstationModel.Name.ToLower()) &&
                        workstationModel.IsActive && w.CompanyId == companyId);
                    if (existingWorkstation != null)
                    {
                        serviceResult.SetFailure($"Workstation {workstationModel.Name} Already Exist");
                        return serviceResult;
                    }
                    var workstation = new Workstation()
                    {
                        CompanyId = companyId,
                        Name = workstationModel.Name,
                        CreatedBy = userId,
                        ModifiedBy = userId,
                        CreatedDate = DateTime.UtcNow,
                        ModifiedDate = DateTime.UtcNow,
                        IsActive = true
                    };
                    db.Workstations.Add(workstation);
                    serviceResult.SetSuccess(await db.SaveChangesAsync() > 1);
                }
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
            }
            return serviceResult;
        }

        public async Task<bool> DeleteWorkstation(string corpName, int userId, int workstationId)
        {
            try
            {
                using (var db = SqlHelper.GetDbContext(corpName))
                {
                    var workstation = await db.Workstations.FindAsync(workstationId);
                    workstation.IsActive = false;
                    db.Entry(workstation).State = EntityState.Modified;
                    return await db.SaveChangesAsync() > 1;
                }
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
            }
            return false;
        }

        public async Task<PagedResult<Workstation>> GetWorkstation(string corpName, int companyId, WorkstationRequestModel workstationRequestModel)
        {
            try
            {
                int totalRecords = 0;
                var companyIdParameter = DataProvider.GetIntSqlParameter("CompanyId", companyId);
                var fromParameter = DataProvider.GetIntSqlParameter("From", workstationRequestModel.From);
                var toParameter = DataProvider.GetIntSqlParameter("To", workstationRequestModel.To);
                var searchParameter = DataProvider.GetStringSqlParameter("Search", workstationRequestModel.Search);
                var isActiveParameter = DataProvider.GetBoolSqlParameter("IsActive", workstationRequestModel.IsActive);
                var totalRecordsParameter = DataProvider.GetIntSqlParameter("TotalRecords", totalRecords, true);
                using (var db = SqlHelper.GetDbContext(corpName))
                {
                    var workstation = await db.ExecuteStoredProcedureListAsync<Workstation>("USP_GetWorkstation", companyIdParameter, fromParameter, toParameter, searchParameter, isActiveParameter, totalRecordsParameter);
                    totalRecords = Convert.ToInt32(totalRecordsParameter.Value) != 0 ? Convert.ToInt32(totalRecordsParameter.Value) : totalRecords;
                    return new PagedResult<Workstation>(workstation, totalRecords);
                }
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
            }
            return new PagedResult<Workstation>();
        }

        public async Task<bool> UpdateWorkstation(string corpName, int userId, int workstationId, WorkstationModel workstationModel)
        {
            try
            {
                using (var db = SqlHelper.GetDbContext(corpName))
                {
                    var workstation = await db.Workstations.FindAsync(workstationId);
                    workstation.Name = workstationModel.Name;
                    workstation.ModifiedBy = userId;
                    workstation.ModifiedDate = DateTime.UtcNow;
                    workstation.IsActive = workstationModel.IsActive;
                    db.Entry(workstation).State = EntityState.Modified;
                    return await db.SaveChangesAsync() > 1;
                }
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
            }
            return false;
        }
        public async Task<Workstation> GetWorkStationById(string corpName, int workStationId)
        {
            using (var db = SqlHelper.GetDbContext(corpName))
            {
                return await db.Workstations.FirstOrDefaultAsync(w => w.Id == workStationId && w.IsActive == true);
            }
        }
        public int GetWorkStationIdByCompanyId(string corpName, int companyId)
        {
            using (var db = SqlHelper.GetDbContext(corpName))
            {
                return db.Workstations.FirstOrDefault(w => w.CompanyId == companyId && w.IsActive == true).Id;
            }
        }
    }
}
