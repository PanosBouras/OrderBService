using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Oracle.ManagedDataAccess.Client;
using OrderService.Hubs;
using System.Collections.Generic;
using System.ComponentModel.Design;
using static OrderService.Controllers.PostCreateOrder;

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
        public async Task PostDeleteItemOrderAsync(String companyID, String tableid,String username)
        {
            String delqry = "BEGIN DeleteOrder(:pi_tableid,:pi_username); END;";
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            using (OracleCommand command = new OracleCommand(delqry, connection))
            {
                command.Parameters.Add("pi_tableid", tableid);
                command.Parameters.Add("pi_username", username);
                command.Connection.Open();
                int rows = command.ExecuteNonQuery();
                if (rows > 0)
                {
                    await _hubContext.Clients
                        .Group(companyID)
                        .SendAsync("ReceiveOrdersDeleteOrder", "Deleted order table:" + tableid);
                }
                command.Connection.Close();
            }
        }

        }
}
