using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Npgsql;
using OrderService.Hubs;
using static OrderService.Controllers.OrderItemsController;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetOrderItemsController : Controller
    {
        private readonly IHubContext<OrdersHub> _ordersHubContext;
        private readonly IHubContext<TableHub> _tableHubContext;

        public GetOrderItemsController(IHubContext<OrdersHub> ordersHubContext, IHubContext<TableHub> tableHubContext)
        {
            _ordersHubContext = ordersHubContext;
            _tableHubContext = tableHubContext;
        }
        public class OrderItem
        {
            public string Orderid { get; set; }
            public string Rownum { get; set; }
            public string Id { get; set; }
            public string ItemName { get; set; }
            public double Price { get; set; }
            public string Status { get; set; }
            public string Comments { get; set; }
            public string OrderDTLSeq { get; set; }
            public string Persons { get; set; }
        }

        [HttpGet(Name = "GetOrdItems")]
        public async Task<string> GetOrderItemsAsync(int tableid, int companyid)
        {
            List<OrderItem> orderItems = new List<OrderItem>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT 
                            ROW_NUMBER() OVER (ORDER BY h.orderid, d.orderitemid) AS rownum,
                            h.orderid,
                            d.orderitemid,
                            d.orderitemname,
                            COALESCE(d.price, 0) AS price,
                            CASE 
                                WHEN d.payedflg IS NULL THEN 'pending'
                                ELSE 'completed'
                            END AS status,
                            d.orderdtlitemisseq,
                            d.orderitemdescription,
                            h.persons
                        FROM orderb_orderhdr h
                        LEFT JOIN orderb_orderdtl d 
                            ON h.orderid = d.orderid
                        WHERE h.tableid = @pi_tableid
                          AND h.companyid = @pi_companyid
                          AND h.orderid = (
                                SELECT MAX(h2.orderid)
                                FROM orderb_orderhdr h2
                                WHERE h2.tableid = @pi_tableid
                                  AND h2.statusflg = 0
                                  AND h2.companyid = @pi_companyid
                          )
                        ORDER BY h.orderid, d.orderitemid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_tableid", tableid);
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                OrderItem oi = new OrderItem
                                {
                                    Orderid = reader["orderid"]?.ToString(),
                                    Rownum = reader["rownum"]?.ToString(),
                                    Id = reader["orderitemid"]?.ToString(),
                                    ItemName = reader["orderitemname"]?.ToString(),
                                    Price = Convert.ToDouble(reader["price"]),
                                    Status = reader["status"]?.ToString(),
                                    OrderDTLSeq = reader["orderdtlitemisseq"]?.ToString(),
                                    Comments = reader["orderitemdescription"]?.ToString(),
                                    Persons = reader["persons"]?.ToString()
                                };

                                orderItems.Add(oi);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(orderItems, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }

        [HttpPost("UpdateStatusItem")]
        public async Task<IActionResult> UpdateStatusItemAsync(
           int companyID,
           string orderItemId,
           int status)
        {
            try
            {
                Console.WriteLine($"🔵 [UpdateStatusItem] Updating item {orderItemId} to status {status}");

                // ====== VALIDATE STATUS ======
                if (!IsValidStatus(status))
                {
                    return BadRequest(new { message = $"Invalid status {status}. Valid values: 1, 2, 3" });
                }

                // ====== GET CURRENT ITEM INFO ======
                var itemInfo = await GetOrderItemInfo(orderItemId, companyID);
                if (itemInfo == null)
                {
                    return NotFound(new { message = $"Order item {orderItemId} not found" });
                }

                // ====== UPDATE ITEM STATUS ======
                string updateQuery = @"
                    UPDATE orderb_orderdtl
                    SET status = @status
                    WHERE orderdtlitemisseq = @itemId
                      AND companyid = @companyid";

                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    await using (var command = new NpgsqlCommand(updateQuery, connection))
                    {
                        command.Parameters.AddWithValue("status", status);
                        command.Parameters.AddWithValue("itemId", orderItemId);
                        command.Parameters.AddWithValue("companyid", companyID);

                        int rowsAffected = await command.ExecuteNonQueryAsync();

                        if (rowsAffected > 0)
                        {
                            Console.WriteLine($"✅ [UpdateStatusItem] Item {orderItemId} status updated to {status}");

                            // ====== NOTIFY ORDERS HUB ======
                            await _ordersHubContext.Clients
                                .Group(companyID.ToString())
                                .SendAsync("ReceiveOrdersUpdate", new
                                {
                                    orderItemId,
                                    newStatus = status,
                                    orderid = itemInfo.OrderId,
                                    tableid = itemInfo.TableId
                                });

                            Console.WriteLine($"✅ [UpdateStatusItem] Notified ReceiveOrdersUpdate to company {companyID}");

                            return Ok(new
                            {
                                message = "Item status updated successfully",
                                orderItemId,
                                newStatus = status
                            });
                        }
                        else
                        {
                            return NotFound(new { message = $"Order item {orderItemId} not found" });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [UpdateStatusItem] Error: {ex.Message}");
                Console.WriteLine($"❌ [UpdateStatusItem] StackTrace: {ex.StackTrace}");
                return StatusCode(500, new { message = "Error updating order item", error = ex.Message });
            }
        }

        // ============================================================
        // UPDATE STATUS OF MULTIPLE ORDER ITEMS
        // ============================================================
        [HttpPost("UpdateStatusItems")]
        public async Task<IActionResult> UpdateStatusItemsAsync(
            int companyID,
            [FromBody] List<UpdateStatusRequest> requests)
        {
            try
            {
                Console.WriteLine($"🔵 [UpdateStatusItems] Updating {requests.Count} items");

                if (requests == null || requests.Count == 0)
                {
                    return BadRequest(new { message = "No items provided" });
                }

                var updatedItems = new List<object>();

                // ====== UPDATE EACH ITEM ======
                foreach (var request in requests)
                {
                    // Validate status
                    if (!IsValidStatus(request.Status))
                    {
                        continue; // Skip invalid items
                    }

                    // Get item info
                    var itemInfo = await GetOrderItemInfo(request.OrderItemId, companyID);
                    if (itemInfo == null)
                    {
                        continue; // Skip missing items
                    }

                    // Update status
                    string updateQuery = @"
                        UPDATE orderb_orderdtl
                        SET status = @status
                        WHERE orderdtlitemisseq = @itemId
                          AND companyid = @companyid";

                    await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                    {
                        await connection.OpenAsync();

                        await using (var command = new NpgsqlCommand(updateQuery, connection))
                        {
                            command.Parameters.AddWithValue("status", request.Status);
                            command.Parameters.AddWithValue("itemId", request.OrderItemId);
                            command.Parameters.AddWithValue("companyid", companyID);

                            int rowsAffected = await command.ExecuteNonQueryAsync();

                            if (rowsAffected > 0)
                            {
                                updatedItems.Add(new
                                {
                                    orderItemId = request.OrderItemId,
                                    status = request.Status
                                });

                                Console.WriteLine($"✅ [UpdateStatusItems] Item {request.OrderItemId} updated to status {request.Status}");
                            }
                        }
                    }
                }

                // ====== NOTIFY ORDERS HUB ======
                if (updatedItems.Count > 0)
                {
                    await _ordersHubContext.Clients
                        .Group(companyID.ToString())
                        .SendAsync("ReceiveOrdersUpdate", new
                        {
                            updatedItems,
                            totalUpdated = updatedItems.Count
                        });

                    Console.WriteLine($"✅ [UpdateStatusItems] Notified {updatedItems.Count} item updates to company {companyID}");
                }

                return Ok(new
                {
                    message = "Items updated successfully",
                    updatedCount = updatedItems.Count
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [UpdateStatusItems] Error: {ex.Message}");
                return StatusCode(500, new { message = "Error updating order items", error = ex.Message });
            }
        }

        // ============================================================
        // VALIDATE STATUS VALUE
        // ============================================================
        private bool IsValidStatus(int status)
        {
            // Valid statuses: 1 = Pending, 2 = In Progress, 3 = Delivered
            return status >= 1 && status <= 3;
        }

        // ============================================================
        // GET ORDER ITEM INFO
        // ============================================================
        private async Task<OrderItemInfo> GetOrderItemInfo(string orderItemId, int companyid)
        {
            try
            {
                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT
                            orderid,
                            ordertable as tableid,
                            status,
                            orderitemname,
                            orderitemdescription
                        FROM orderb_orderdtl
                        WHERE orderdtlitemisseq = @itemId
                          AND companyid = @companyid
                        LIMIT 1";

                    await using (var command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("itemId", orderItemId);
                        command.Parameters.AddWithValue("companyid", companyid);

                        await using (var reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                return new OrderItemInfo
                                {
                                    OrderId = reader["orderid"]?.ToString(),
                                    TableId = Convert.ToInt32(reader["tableid"]),
                                    CurrentStatus = Convert.ToInt32(reader["status"]),
                                    ItemName = reader["orderitemname"]?.ToString(),
                                    Description = reader["orderitemdescription"]?.ToString()
                                };
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ [GetOrderItemInfo] Error: {ex.Message}");
            }

            return null;
        }
        public class UpdateStatusRequest
        {
            public string OrderItemId { get; set; }
            public int Status { get; set; }
        }

        public class OrderItemInfo
        {
            public string OrderId { get; set; }
            public int TableId { get; set; }
            public int CurrentStatus { get; set; }
            public string ItemName { get; set; }
            public string Description { get; set; }
        }
    }
}