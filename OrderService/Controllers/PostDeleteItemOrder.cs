using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.PostCreateOrder;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostDeleteItemOrder : Controller
    {
        [HttpPost(Name = "PostDeleteItemSeq")]
        public async Task PostCreateOrderAsync(int orderItemSeq)
        {
            String delqry = "DELETE ORDERB_ORDERDTL WHERE ORDERDTLITEMISSEQ = :pi_orderItemSeq";
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            using (OracleCommand command = new OracleCommand(delqry, connection))
            {
                command.Parameters.Add("pi_orderItemSeq", orderItemSeq);
                command.Connection.Open();
                command.ExecuteNonQuery();
                command.Connection.Close();
            }
        }

        }
}
