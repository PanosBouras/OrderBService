using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Oracle.ManagedDataAccess.Client;
using OrderService.Hubs;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class OrderItemsController : ControllerBase
    {
        private readonly IHubContext<OrdersHub> _hubContext;

        public OrderItemsController(IHubContext<OrdersHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public class OrderItemInfo
        {
            public string comment { get; set; }
            public float extraPrice { get; set; }
            public string orderItemId { get; set; }
            public string[] selectedRecommendations { get; set; }
            public string username { get; set; }
        }
         
        [HttpPost("UpdateStatusItem")]
        public async Task<IActionResult> UpdateStatus(
            [FromQuery] string orderItemId,
            [FromQuery] string companyID,
            [FromQuery] int status)
        {
            if (string.IsNullOrEmpty(orderItemId))
                return BadRequest("orderItemId is required");

            try
            {
                string updateSql =
                    "UPDATE ORDERB_ORDERDTL " +
                    "SET STATUS = :pi_status " +
                    "WHERE ORDERDTLITEMISSEQ = :pi_orderItemId";

                using var connection = new OracleConnection(ConnectionString.Value);
                using var command = new OracleCommand(updateSql, connection);

                command.Parameters.Add("pi_status", OracleDbType.Int32).Value = status;
                command.Parameters.Add("pi_orderItemId", OracleDbType.Varchar2).Value = orderItemId;

                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    await _hubContext.Clients
                        .Group(companyID)
                        .SendAsync("ReceiveOrdersUpdate", new
                        {
                            orderItemId,
                            status
                        });
                }
                return Ok(new
                {
                    success = true,
                    rowsAffected = rows
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
         
        [HttpPost("UpdateOrderItem")]
        public async Task<IActionResult> UpdateDetails(
            [FromQuery] string companyID,
            [FromQuery] int tableId,
            [FromQuery] string username,
            [FromBody] OrderItemInfo orderJson)
        {
            if (orderJson == null)
                return BadRequest("Body is required");

            try
            {
                string updateSql =
                    "UPDATE ORDERB_ORDERDTL " +
                    "SET ORDERITEMDESCRIPTION = :pi_description, " +
                    "PRICE = (SELECT PRICE FROM ORDERB_ITEM WHERE ITEMID = " +
                    "(SELECT ORDERITEMID FROM ORDERB_ORDERDTL WHERE ORDERDTLITEMISSEQ = :pi_orderItemId)) + :pi_price, " +
                    "MODIFYEDUSER = :pi_username " +
                    "WHERE ORDERDTLITEMISSEQ = :pi_orderItemId";

                using var connection = new OracleConnection(ConnectionString.Value);
                using var command = new OracleCommand(updateSql, connection);

                command.Parameters.Add("pi_description", OracleDbType.Varchar2).Value = orderJson.comment;
                command.Parameters.Add("pi_orderItemId", OracleDbType.Varchar2).Value = orderJson.orderItemId;
                command.Parameters.Add("pi_price", OracleDbType.Decimal).Value = orderJson.extraPrice;
                command.Parameters.Add("pi_username", OracleDbType.Varchar2).Value = username;

                await connection.OpenAsync();
                int rows = await command.ExecuteNonQueryAsync();
                if (rows > 0)
                {
                    await _hubContext.Clients.All
                        .SendAsync("ReceiveOrdersUpdate", orderJson);
                }
                return Ok(new
                {
                    success = true,
                    rowsAffected = rows
                });
            }
            catch (System.Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }
    }
}
