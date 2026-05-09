using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using OrderService.Hubs;

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

        // ---------------------------
        // UPDATE STATUS
        // ---------------------------
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
                string sql = @"
                    UPDATE orderb_orderdtl
                    SET status = @status
                    WHERE orderdtlitemisseq = @id;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("status", status);
                cmd.Parameters.AddWithValue("id", orderItemId);

                int rows = await cmd.ExecuteNonQueryAsync();

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
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        // ---------------------------
        // UPDATE DETAILS
        // ---------------------------
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
                string sql = @"
                    UPDATE orderb_orderdtl d
                    SET orderitemdescription = @desc,
                        price = COALESCE(i.price, 0) + @extra,
                        modifyeduser = @user
                    FROM orderb_item i
                    WHERE d.orderdtlitemisseq = @id
                      AND i.itemid = d.orderitemid;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("desc", orderJson.comment ?? "");
                cmd.Parameters.AddWithValue("id", orderJson.orderItemId);
                cmd.Parameters.AddWithValue("extra", orderJson.extraPrice);
                cmd.Parameters.AddWithValue("user", username);

                int rows = await cmd.ExecuteNonQueryAsync();

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
            catch (Exception ex)
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