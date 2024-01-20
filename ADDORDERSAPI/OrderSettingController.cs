using COG.Core.CRM.Order;
using COG.Data;
using COG.Data.Model.CRM.Order;
using COG.Service.Model.CRM.Order;
using COG.WebApi.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;

namespace COG.API.Controllers.CRM.Order
{
    [HasPermission("OrderSettings")]
    [RoutePrefix("CRM")]
    public class OrderSettingController : BaseAPIController
    {
        private readonly IOrderSettingService _orderSettingService;

        public OrderSettingController(IOrderSettingService orderSettingService)
        {
            _orderSettingService = orderSettingService;
        }

        [HttpGet]
        [Route("GetOrderSettings")]
        public async Task<ServiceResult<SetOrderSettingsDto>> GetOrderSettings()
        {
            var result = await _orderSettingService.GetOrderSettings(corpName, companyId);
            return Response(result);
        }

        [HttpPost]
        [Route("SetOrderSettings")]
        public async Task<ServiceResult<SetOrderSettingsDto>> SetOrderSettings(SetOrderSettingsDto requestDto)
        {
            var result = await _orderSettingService.SetOrderSettings(corpName, companyId, userId, requestDto);
            return Response(result);
        }
    }
}
