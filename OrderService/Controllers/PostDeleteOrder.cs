using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using OrderService.Hubs;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostDeleteOrder : Controller
    {
        private readonly IHubContext<OrdersHub> _hubContext;

        public PostDeleteOrder(IHubContext<OrdersHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost(Name = "PostDeleteOrder")]
        public async Task PostDeleteOrderAsync(string companyID, string tableid, string username)
        {
            try
            {
                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    // call PostgreSQL procedure
                    string callProc = "CALL deleteorder(@tableid, @username);";

                    await using (var command = new NpgsqlCommand(callProc, connection))
                    {
                        command.Parameters.AddWithValue("tableid", int.Parse(tableid));
                        command.Parameters.AddWithValue("username", username);

                        int rows = await command.ExecuteNonQueryAsync();

                        // NOTE: PostgreSQL CALL returns -1 usually (no affected rows)
                        if (rows >= -1)
                        {
                            await _hubContext.Clients
                                .Group(companyID)
                                .SendAsync("ReceiveOrdersDeleteOrder",
                                    "Deleted order table:" + tableid);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}