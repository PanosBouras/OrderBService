using Microsoft.AspNetCore.Mvc;
using Npgsql;
using Newtonsoft.Json;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetOrderItemsController : Controller
    {
        public class OrderItem
        {
            public string Orderid { get; set; }
            public string Rownum { get; set; }
            public string Id { get; set; }
            public string ItemName { get; set; }
            public double Price { get; set; }
            public string Status { get; set; }
            public string Comments { get; set; }
            public string OrderDTLSeq { get; set; }
            public string Persons { get; set; }
        }

        [HttpGet(Name = "GetOrdItems")]
        public async Task<string> GetOrderItemsAsync(int tableid, int companyid)
        {
            List<OrderItem> orderItems = new List<OrderItem>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT 
                            ROW_NUMBER() OVER (ORDER BY h.orderid, d.orderitemid) AS rownum,
                            h.orderid,
                            d.orderitemid,
                            d.orderitemname,
                            COALESCE(d.price, 0) AS price,
                            CASE 
                                WHEN d.payedflg IS NULL THEN 'pending'
                                ELSE 'completed'
                            END AS status,
                            d.orderdtlitemisseq,
                            d.orderitemdescription,
                            h.persons
                        FROM orderb_orderhdr h
                        LEFT JOIN orderb_orderdtl d 
                            ON h.orderid = d.orderid
                        WHERE h.tableid = @pi_tableid
                          AND h.companyid = @pi_companyid
                          AND h.orderid = (
                                SELECT MAX(h2.orderid)
                                FROM orderb_orderhdr h2
                                WHERE h2.tableid = @pi_tableid
                                  AND h2.statusflg = 0
                                  AND h2.companyid = @pi_companyid
                          )
                        ORDER BY h.orderid, d.orderitemid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_tableid", tableid);
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                OrderItem oi = new OrderItem
                                {
                                    Orderid = reader["orderid"]?.ToString(),
                                    Rownum = reader["rownum"]?.ToString(),
                                    Id = reader["orderitemid"]?.ToString(),
                                    ItemName = reader["orderitemname"]?.ToString(),
                                    Price = Convert.ToDouble(reader["price"]),
                                    Status = reader["status"]?.ToString(),
                                    OrderDTLSeq = reader["orderdtlitemisseq"]?.ToString(),
                                    Comments = reader["orderitemdescription"]?.ToString(),
                                    Persons = reader["persons"]?.ToString()
                                };

                                orderItems.Add(oi);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(orderItems, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }
    }
}