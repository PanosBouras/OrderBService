using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Oracle.ManagedDataAccess.Client;
using OrderService.Hubs;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Xml.Linq;
using static OrderService.Controllers.GetShowOrdersController;
using static OrderService.Controllers.PostCreateOrder;
using static OrderService.Controllers.PostPaymentRequest;

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
        public async Task PostDeleteItemOrderAsync(String companyID,String orderItemSeq,String username)
        {
            String delqry = "DELETE ORDERB_ORDERDTL WHERE ORDERDTLITEMISSEQ = :pi_orderItemSeq";
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(delqry, connection))
                {
                    command.Parameters.Add("pi_orderItemSeq", orderItemSeq);
                    command.Connection.Open();
                    int rows = command.ExecuteNonQuery();
                    if (rows > 0)
                    {
                        await _hubContext.Clients
                            .Group(companyID)
                            .SendAsync("ReceiveOrdersDeleteItem", "Deleted item:"+ orderItemSeq);
                    }
                    command.Connection.Close();
                }
            }
            catch (Exception ex)
            {

            }

            String updatehdr = "UPDATE ORDERB_ORDERHDR SET MODIFYEDUSER = :pi_user WHERE  ORDERID = ( SELECT MAX(ORDERID) FROM ORDERB_ORDERDTL WHERE ORDERDTLITEMISSEQ = :pi_orderItemSeq)";
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(delqry, connection))
                {
                    command.Parameters.Add("pi_user", username);
                    command.Parameters.Add("pi_orderItemSeq", orderItemSeq);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
            catch (Exception ex)
            {

            }
        }

        }
}
