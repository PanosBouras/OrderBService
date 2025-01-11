using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetTablesController : Controller
    {

            [HttpGet(Name = "PostTables")]
            public async Task<int> GetTables()
            {
            int tables = 0;
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    String Snumtables = "";
                    string query = "SELECT NVL(NUMOFTABLES,0) NUMOFTABLES FROM ORDERB_COMPANYINFO";
                    using (OracleCommand command = new OracleCommand(query, connection))
                    { 
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Snumtables = reader["NUMOFTABLES"].ToString();

                            }
                        }
                    }
                    Int32.TryParse(Snumtables, out   tables);
                    return tables;
                }
                catch (Exception ex)
                { 

                }
            }
            return tables;
               
            }
        }
    
}
