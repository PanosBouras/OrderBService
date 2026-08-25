using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using OrderService.Hubs;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostDeleteItemOrder : ControllerBase
    {
        private readonly IHubContext<OrdersHub> _hubContext;
        private readonly IHubContext<TableHub> _tableHubContext;

        public PostDeleteItemOrder(IHubContext<OrdersHub> hubContext, IHubContext<TableHub> tableHubContext)
        {
            _hubContext = hubContext;
            _tableHubContext = tableHubContext;
        }

        [HttpPost]
        public async Task<IActionResult> PostDeleteItemOrderAsync(string companyID, String tableid,string orderItemSeq,string username)
        {
            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString.Value);
                await connection.OpenAsync();

                int companyIdInt = int.Parse(companyID);

                string orderId = null;

                // 1. GET orderid
                const string getOrderSql = @"
                    SELECT orderid
                    FROM orderb_orderdtl
                    WHERE orderdtlitemisseq = @seq
                      AND ordertable = @tableid
                      AND companyid = @companyID
                    LIMIT 1;
                ";

                await using (var cmd = new NpgsqlCommand(getOrderSql, connection))
                {
                    cmd.Parameters.Add("seq", NpgsqlTypes.NpgsqlDbType.Varchar).Value = orderItemSeq;
                    cmd.Parameters.Add("tableid", NpgsqlTypes.NpgsqlDbType.Varchar).Value = tableid;
                    cmd.Parameters.Add("companyID", NpgsqlTypes.NpgsqlDbType.Integer).Value = companyIdInt;

                    var result = await cmd.ExecuteScalarAsync();
                    orderId = result?.ToString();
                }

                // 2. DELETE item
                const string deleteSql = @"
                    DELETE FROM orderb_orderdtl
                    WHERE orderdtlitemisseq = @seq AND ordertable = @tableid
                      AND companyid = @companyID;
                ";

                int rows;

                await using (var cmd = new NpgsqlCommand(deleteSql, connection))
                {
                    cmd.Parameters.Add("seq", NpgsqlTypes.NpgsqlDbType.Varchar).Value = orderItemSeq;
                    cmd.Parameters.Add("tableid", NpgsqlTypes.NpgsqlDbType.Varchar).Value = tableid;
                    cmd.Parameters.Add("companyID", NpgsqlTypes.NpgsqlDbType.Integer).Value = companyIdInt;

                    rows = await cmd.ExecuteNonQueryAsync();
                }

                if (rows > 0)
                {
                    await _hubContext.Clients
                        .Group(companyID)
                        .SendAsync("ReceiveOrdersDeleteItem", new
                        {
                            orderItemSeq,
                            orderId
                        });
                }

                // 3. CHECK IF ORDER IS EMPTY
                if (!string.IsNullOrEmpty(orderId))
                {
                    const string checkSql = @"
                        SELECT COUNT(*)
                        FROM orderb_orderdtl
                        WHERE orderid = @orderId
                        AND ordertable = @tableid
                      AND companyid = @companyID ;
                    ";

                    int count;

                    await using (var cmd = new NpgsqlCommand(checkSql, connection))
                    {
                        cmd.Parameters.Add("orderId", NpgsqlTypes.NpgsqlDbType.Varchar).Value = orderId;
                        cmd.Parameters.Add("tableid", NpgsqlTypes.NpgsqlDbType.Varchar).Value = tableid;
                        cmd.Parameters.Add("companyID", NpgsqlTypes.NpgsqlDbType.Integer).Value = companyIdInt;
                        count = Convert.ToInt32(await cmd.ExecuteScalarAsync());
                    }

                    if (count == 0)
                    {
                        try
                        {
                            string delqry = "CALL public.deleteorder(@pi_companyid, @pi_tableid, @pi_username);";

                            await using (NpgsqlConnection connection1 =
                                new NpgsqlConnection(ConnectionString.Value))
                            {
                                await connection1.OpenAsync();

                                await using (NpgsqlCommand command =
                                    new NpgsqlCommand(delqry, connection1))
                                { 
                                    command.Parameters.AddWithValue("pi_companyid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(companyID));
                                    command.Parameters.AddWithValue("pi_tableid", NpgsqlTypes.NpgsqlDbType.Integer, Convert.ToInt32(tableid));
                                    command.Parameters.AddWithValue("pi_username", NpgsqlTypes.NpgsqlDbType.Text, username);

                                    int rows2 = await command.ExecuteNonQueryAsync();

                                    await _hubContext.Clients
                                        .Group(companyID.ToString())
                                        .SendAsync("ReceiveOrdersDeleteOrder",
                                            "Deleted order table:" + tableid);

                                    await _tableHubContext.Clients
                                        .Group(companyID.ToString())
                                        .SendAsync("TableStatusChanged",
                                            tableid.ToString(), 0);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            string sss = ex.Message;
                        }
                    }
                }

                return Ok(true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(500, ex.Message);
            }
        }
    }
}