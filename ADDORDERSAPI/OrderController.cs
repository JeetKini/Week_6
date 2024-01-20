using COG.Core.CRM.Order;
using COG.Data;
using COG.Data.Model;
using COG.IException;
using COG.Service.Model.CRM.Order;
using COG.Utility;
using COG.Utility.DTO;
using COG.WebApi.Common;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Description;

namespace COG.API.Controllers.CRM
{
    [HasPermission("OrdersView")]
    [RoutePrefix("CRM")]
    public class OrderController : BaseAPIController
    {
        private readonly IOrderService _orderService;
        private readonly IOrderChangeHistoryService _historyService;

        public OrderController(IOrderService orderService, IOrderChangeHistoryService historyService)
        {
            _orderService = orderService;
            _historyService = historyService;
        }

        [HasPermission("OrderManagement")]
        [HttpPost]
        [Route("EditOrder")]
        [ResponseType(typeof(ResponseInfo))]
        public async Task<ServiceResult<OrderMaster>> EditOrder(EditOrderMasterDto orderDetail)
        {
            var result = await _orderService.EditOrder(corpName, companyId, userId, orderDetail);
            return result;
        }

        /// <summary>
        /// Update Order Status
        /// </summary>
        /// <param name="id"></param>
        /// <param name="statusId"></param>
        /// <returns></returns>
        [HasPermission("OrderStatusEdit")]
        [HttpPost]
        [Route("Order/{id}/Status")]
        public ServiceResult UpdateOrderStatus(int id, UpdateOrderStatusDto req)
        {
            var result = _orderService.UpdateOrderStatus(companyId, userId, req);
            return result;
        }


        [HasPermission("OrderManagement")]
        [HttpPost]
        [Route("Order/{id}/Discount")]
        public ServiceResult UpdateOrderDiscount(int id, UpdateOrderDiscountDto req)
        {
            var result = _orderService.UpdateOrderDiscount(companyId, userId, req);
            return result;
        }

        [HasPermission("OrderStatusEdit")]
        [HttpPost]
        [Route("Order/{id}/Detail/Status")]
        public ServiceResult UpdateOrderDetailStatus(int id, UpdateOrderDetailStatusDto req)
        {
            var result = _orderService.UpdateOrderDetailStatus(companyId, userId, req);
            return result;
        }
       
        [HasPermission("OrdersAdd")]
        [HttpPost]
        [Route("Order")]
        public ServiceResult AddOrder(OrderModel orderDetail)
        {
            ServiceResult result = new ServiceResult();
            if (ModelState.IsValid)
            {
                orderDetail.UserId = userId;
                var isSuccess = _orderService.AddOrder(corpName, companyId, userId, orderDetail);

                if (isSuccess.Item1 == true && !string.IsNullOrEmpty(isSuccess.Item2))
                {
                    result.SetSuccess();
                }
                else if (isSuccess.Item1 == false && !string.IsNullOrEmpty(isSuccess.Item2))
                {
                    result.SetAlreadyExist(isSuccess.Item2);
                }
                else
                {
                    result.SetFailure("There was an error while placing the order");
                }
            }
            else
            {
                return Error();
            }
            return result;
        }

        [HttpPost]
        [Route("OrderNotificationSetting")]
        public async Task<ServiceResult> OrderNotificationSetting(OrderNotificationSettingsModel orderNotificationSettingsModel)
        {
            ServiceResult result = new ServiceResult();
            if (ModelState.IsValid)
            {
                var data = await _orderService.OrderNotificationSetting(corpName, companyId, orderNotificationSettingsModel);
                result.SetSuccess();
            }
            else
            {
                return Error();
            }

            return result;
        }

        /// <summary>
        /// This is obsolete, please use GET Orders API
        /// </summary>
        /// <param name="order"></param>
        /// <returns></returns>
        [Obsolete]
        [HttpPost]
        [Route("MyOrder")]
        [ResponseType(typeof(ResponseInfo))]
        public IHttpActionResult GetMyOrder(OrderRequestDto order)
        {
            ResponseInfo info = new ResponseInfo();

            var myOrders = _orderService.GetMyOrder(userId, order);

            if (myOrders != null)
            {
                info.SetSuccess(myOrders);
            }

            return Ok(info);
        }

        [HttpGet]
        [Route("Orders")]
        public async Task<ServiceResult<PagedResult<MyOrderModel>>> GetOrders([FromUri]OrderRequestDto order)
        {
            if (order == null)
                order = new OrderRequestDto();

            var orders = await _orderService.GetOrders(corpName, companyId, userId, order);
            return Response(orders);
        }

        [HttpGet]
        [Route("Orders/Export")]
        public async Task<HttpResponseMessage> GetAllOrdersDetailsForDownload([FromUri]OrderRequestDto order)
        {
            if (order == null)
                order = new OrderRequestDto();

            var orders = await _orderService.GetOrders(corpName, companyId, userId, order);
            var orderDetails = await _orderService.GetAllOrdersDetails(userId, order);

            var ordersDt = GetOrdersDataTable(orders.Items.ToList());

            var heading = "";
            if(order.FromDate != null && order.ToDate != null)
            {
                heading = $" - From {order.FromDate} - To {order.ToDate}";
            }

            var sheets = new List<MultipleExcelSheetsDto>
            {
                new MultipleExcelSheetsDto
                {
                    dt = ordersDt,
                    ReportHeading = "Orders" + heading,
                    WorkSheetName = "Orders"
                },
                new MultipleExcelSheetsDto
                {
                    dt = orderDetails.ToDataTable(),
                    ReportHeading = "Order Details" + heading,
                    WorkSheetName = "Order Details"
                }
            };

            var dataBytes = MultimediaHelper.GetBytesForExportToExcel_MultipleSheets(sheets);

            HttpResponseMessage httpResponseMessage = UtilityHelper.GetHTTPResponseForExportToExcel(Request, dataBytes, "Visit Report");

            return httpResponseMessage;
        }

        private DataTable GetOrdersDataTable(List<MyOrderModel> ordersList)
        {
            var orderDt = new DataTable();
            try
            {
                orderDt = ordersList.ToDataTable();
                var orderModel = new MyOrderModel();
                orderDt.Columns.Remove(nameof(orderModel.Id));
                orderDt.Columns.Remove(nameof(orderModel.UserId));
                orderDt.Columns.Remove(nameof(orderModel.LeadId));
                orderDt.Columns.Remove(nameof(orderModel.ParentLeadId));
                orderDt.Columns.Remove(nameof(orderModel.OrderStatusId));
                orderDt.Columns.Remove(nameof(orderModel.IsEditable));
            }
            catch (Exception ex)
            {
                ExceptionLogging.LogExceptionToDB(ex);
            }
            return orderDt;
        }

        [HttpGet]
        [Route("OrderMasterDetail")]
        [ResponseType(typeof(ResponseInfo))]
        public IHttpActionResult GetOrderMasterDetail(int orderId)
        {
            ResponseInfo info = new ResponseInfo();

            if (orderId > 0)
            {
                var orderMasterDetail = _orderService.GetOrderMasterDetail(userId, orderId);
                if (orderMasterDetail != null)
                    info.SetSuccess(orderMasterDetail);
            }

            return Ok(info);
        }


        [HttpGet]
        [Route("OrderDetail")]
        [ResponseType(typeof(ResponseInfo))]
        public IHttpActionResult GetOrderDetail(int orderId)
        {
            ResponseInfo info = new ResponseInfo();

            var orderDetail = _orderService.GetOrderDetail(userId, orderId);
            if (orderDetail.Count > 0)
                info.SetSuccess(orderDetail);

            return Ok(info);
        }


        [HttpPost]
        [Route("EditOrderDetail")]
        [ResponseType(typeof(ResponseInfo))]
        public IHttpActionResult EditOrderDetail(List<EditOrderDetailModel> OrderDetails)
        {
            ResponseInfo info = new ResponseInfo();
            var result = _orderService.EditOrderDetail(userId, OrderDetails);
            if (result)
                info.SetSuccess("");
            else
                info.SetFailure("");

            return Ok(info);
        }

        [HttpGet]
        [Route("Order/{id}/History")]
        public ServiceResult<List<OrderChangeHistoryDto>> GetOrderChangeHistory(int id)
        {
            List<OrderChangeHistoryDto> history = _historyService.GetOrderHistory(companyId, userId, id);
            return Response(history);
        }

        [HttpGet]
        [Route("order/{id}/GetPdf")]
        public async Task<ServiceResult> GetPdf(int id)
        {
            var result = new ServiceResult();
            result = await _orderService.GeneratePDF(corpName, companyId, userId, id);
            return result;
        }

        [HasPermission("OrderManagement")]
        [HttpPost]
        [Route("Order/{id}/Detail/Quantity")]
        public ServiceResult UpdateOrderDetailQuantity(int id, UpdateOrderDetailQuantityDto req)
        {
            var result = _orderService.UpdateOrderDetailQuantity(companyId, userId, req);
            return result;
        }

        [HasPermission("OrderManagement")]
        [HttpPost]
        [Route("Order/{id}/Detail/Discount")]
        public ServiceResult UpdateOrderDetailDiscount(int id, UpdateOrderDetailDiscountDto req)
        {
            var result = _orderService.UpdateOrderDetailDiscount(companyId, userId, req);
            return result;
        }

        [HasPermission("OrderManagement")]
        [HttpPost]
        [Route("Order/{id}/Detail")]
        public async Task<ServiceResult> UpdateOrderDetails(int id, UpdateOrderDetailsDto req)
        {
            var result = await _orderService.UpdateOrderDetails(corpName, companyId, userId, req);
            return result;
        }

        [HasPermission("OrdersAdd")]
        [HttpPost]
        [Route("Order/AddProductsUpdateOrder")]
        public ServiceResult AddProductsUpdateOrder(OrderModel orderDetail)
        {
            ServiceResult result = new ServiceResult();
            if (ModelState.IsValid)
            {
                orderDetail.UserId = userId;
                var isSuccess = _orderService.UpdateOrderProduct(corpName, companyId, userId, orderDetail);

                if (isSuccess)
                    result.SetSuccess();
                else
                    result.SetFailure("There was an error while updating the order");
            }
            else
            {
                return Error();
            }

            return result;
        }
    }
}
