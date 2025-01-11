using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.GetOrderItemsController;
using System.Text.Json;
using Newtonsoft.Json;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetOrderItemsController : Controller
    {
        public class OrderItem
        {
            public String Orderid { get; set; }
            public String Rownum { get; set; }
            public String Id { get; set; }
            public String ItemName { get; set; }
            public Double Price { get; set; }
            public String Status { get; set; }

            public String Comments { get; set; }
            public String OrderDTLSeq { get; set; }
        }


        [HttpGet(Name = "GetOrdItems")]
        public async Task<string> GetOrderItemsAsync(int tableid)
        {
            string jsonString = "";

            List<OrderItem> orderItems = new List<OrderItem>();
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT 
                                    ROWNUM,
                                    h.ORDERID,
                                    d.ORDERITEMID,
                                    d.ORDERITEMNAME,
                                    NVL(d.PRICE,0) PRICE,
                                    DECODE(d.PAYEDFLG, NULL, 'pending', 'completed') AS STATUS,
                                    d.ORDERDTLITEMISSEQ,
                                    d.ORDERITEMDESCRIPTION
                                FROM 
                                    ORDERB_ORDERHDR h 
                                    LEFT JOIN ORDERB_ORDERDTL d ON h.ORDERID = d.ORDERID  
                                WHERE 
                                    h.TABLEID = :pi_tableid
                                    AND h.ORDERID = (SELECT MAX(h2.ORDERID) FROM ORDERB_ORDERHDR h2 WHERE h2.TABLEID = :pi_tableid  AND h2.STATUSFLG =0)
  
                                ORDER BY 
                                    h.ORDERID, d.ORDERITEMID
                                ";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_tableid", tableid));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                OrderItem Oi = new OrderItem
                                {
                                    Orderid = reader["ORDERID"].ToString(),
                                    Rownum = reader["ROWNUM"].ToString(),
                                    Id = reader["ORDERITEMID"].ToString(),
                                    ItemName = reader["ORDERITEMNAME"].ToString(),
                                    Price = Double.Parse(reader["PRICE"].ToString()),
                                    Status = reader["STATUS"].ToString(),
                                    OrderDTLSeq = reader["ORDERDTLITEMISSEQ"].ToString(),
                                    Comments= reader["ORDERITEMDESCRIPTION"].ToString()
                                };

                                orderItems.Add(Oi);
                            }
                        }
                    }
                    jsonString = JsonConvert.SerializeObject(orderItems, Formatting.Indented);

                }
                catch (Exception ex)
                {
                    // Handle exceptions (log them, rethrow them, etc.)
                    Console.WriteLine(ex.Message);
                    // You may want to return an error message or handle it differently
                }
            }

            return jsonString;
        }
    }
}
