using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client; 

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostDeleteOrder : Controller
    {
        [HttpPost(Name = "PostDeleteOrder")]
        public async Task PostCreateOrderAsync(int tableid)
        {
            String delqry = "BEGIN DeleteOrder(:pi_tableid); END;";
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            using (OracleCommand command = new OracleCommand(delqry, connection))
            {
                command.Parameters.Add("pi_tableid", tableid);
                command.Connection.Open();
                command.ExecuteNonQuery();
                command.Connection.Close();
            }
        }

        }
}
