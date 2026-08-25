using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using Oracle.ManagedDataAccess.Client;
using OrderService.Hubs;
using System.Collections;
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
        private readonly IHubContext<TableHub> _tableHubContext;

        public PostDeleteOrder(IHubContext<OrdersHub> hubContext, IHubContext<TableHub> tableHubContext)
        {
            _hubContext = hubContext;
            _tableHubContext = tableHubContext;
        }

        [HttpPost(Name = "PostDeleteOrder")]
        public async Task PostDeleteItemOrderAsync(String companyID, String tableid, String username)
        {
            try
            {
                string delqry = "CALL public.deleteorder(@pi_companyid, @pi_tableid, @pi_username);";

                await using (NpgsqlConnection connection =
                    new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    await using (NpgsqlCommand command =
                        new NpgsqlCommand(delqry, connection))
                    {
                       //   command.Parameters.AddWithValue("pi_companyid", companyID);
                      //    command.Parameters.AddWithValue("pi_tableid", tableid);
                     //     command.Parameters.AddWithValue("pi_username", username);
                        command.Parameters.AddWithValue("pi_companyid",NpgsqlTypes.NpgsqlDbType.Integer,Convert.ToInt32(companyID));
                        command.Parameters.AddWithValue("pi_tableid",NpgsqlTypes.NpgsqlDbType.Integer,Convert.ToInt32(tableid));
                        command.Parameters.AddWithValue("pi_username",NpgsqlTypes.NpgsqlDbType.Text,username);

                        int rows = await command.ExecuteNonQueryAsync();

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
}