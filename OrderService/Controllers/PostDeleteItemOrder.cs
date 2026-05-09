using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using OrderService.Hubs;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostDeleteItemOrder : Controller
    {
        private readonly IHubContext<OrdersHub> _hubContext;

        public PostDeleteItemOrder(IHubContext<OrdersHub> hubContext)
        {
            _hubContext = hubContext;
        }

        [HttpPost(Name = "PostDeleteItemSeq")]
        public async Task PostDeleteItemOrderAsync(string companyID, string orderItemSeq, string username)
        {
            try
            {
                await using (var connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    // 1) DELETE
                    string delqry = @"
                        DELETE FROM orderb_orderdtl
                        WHERE orderdtlitemisseq = @seq";

                    int rows = 0;

                    await using (var command = new NpgsqlCommand(delqry, connection))
                    {
                        command.Parameters.AddWithValue("seq", orderItemSeq);
                        rows = await command.ExecuteNonQueryAsync();
                    }

                    if (rows > 0)
                    {
                        await _hubContext.Clients
                            .Group(companyID)
                            .SendAsync("ReceiveOrdersDeleteItem", "Deleted item:" + orderItemSeq);
                    }

                    // 2) UPDATE HEADER (FIXED VERSION)
                    string updatehdr = @"
                        UPDATE orderb_orderhdr
                        SET modifyeduser = @user
                        WHERE orderid = (
                            SELECT orderid
                            FROM orderb_orderdtl
                            WHERE orderdtlitemisseq = @seq
                            LIMIT 1
                        )";

                    await using (var command2 = new NpgsqlCommand(updatehdr, connection))
                    {
                        command2.Parameters.AddWithValue("user", username);
                        command2.Parameters.AddWithValue("seq", orderItemSeq);

                        await command2.ExecuteNonQueryAsync();
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