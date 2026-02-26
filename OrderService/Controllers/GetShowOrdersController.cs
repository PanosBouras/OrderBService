using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using Microsoft.AspNetCore.SignalR;
using OrderService.Hubs;


namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetShowOrdersController : Controller
    {
        public readonly IHubContext<OrdersHub> _hubContext;
        public GetShowOrdersController(IHubContext<OrdersHub> hubContext)
        {
            _hubContext = hubContext;
        }
        public class Orders
        {
            public String ORDERDTLITEMISSEQ { get; set; }
            public String ORDERITEMDESCRIPTION { get; set; }
            public String TYPEID { get; set; }
            public String ITEM_TYPE { get; set; }
            public String CATEGORYID { get; set; }
            public String CATEGORY_NAME { get; set; }
            public String ITEMID { get; set; }
            public String ITEMNAME { get; set; }
            public String ORDERTABLE { get; set; }
            public String STATUS { get; set; }
        }

        [HttpGet(Name = "GetShowOrdersController")]
        public async Task<string> GetShowOrdersControllerAsync(String companyID, int rows)
        {
            string jsonString = "";
            int count = 0;
            List<Orders> Recommendations = new List<Orders>();
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT
                                        ORDERDTLITEMISSEQ,
                                        ORDERITEMDESCRIPTION,
                                            it.TYPEID,
                                            it.TYPENAME                 AS ITEM_TYPE,
                                            ic.CATEGORYID,
                                            ic.CATEGORYNAME             AS CATEGORY_NAME,
                                            i.ITEMID,
                                            i.ITEMNAME,
                                            od.ORDERTABLE,
                                            od.STATUS
                                        FROM ORDERB_ORDERDTL od
                                        JOIN ORDERB_ITEM i
                                            ON i.ITEMID = od.ORDERITEMID
                                        JOIN ORDERB_ITEM_CATEGORY ic
                                            ON ic.CATEGORYID = i.ITEMCATEGORYID
                                           AND ic.TYPEID = i.ITEMTYPEID
                                        JOIN ORDERB_ITEMS_TYPES it
                                            ON it.TYPEID = i.ITEMTYPEID
                                        WHERE 
                                            od.PAYEDFLG IS NULL
                                            AND od.DELETEDFLG = 0
                                            AND od.STATUS != 3
                                            AND od.CREATEDATE BETWEEN SYSDATE - 1 AND SYSDATE
                                            AND od.COMPANYID = :pi_companyid
                                        GROUP BY
                                        ORDERDTLITEMISSEQ,
                                        ORDERITEMDESCRIPTION,
                                            it.TYPEID,
                                            it.TYPENAME,
                                            ic.CATEGORYID,
                                            ic.CATEGORYNAME,
                                            i.ITEMID,
                                            i.ITEMNAME,
                                            od.ORDERTABLE,
                                            od.STATUS
                                        ORDER BY
                                            it.TYPEID,
                                            ic.CATEGORYID,
                                            i.ITEMNAME";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_companyid", companyID));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                count++;
                                Orders re = new Orders
                                {
                                    ORDERDTLITEMISSEQ = reader["ORDERDTLITEMISSEQ"].ToString(),
                                    ORDERITEMDESCRIPTION = reader["ORDERITEMDESCRIPTION"].ToString(),
                                    TYPEID = reader["TYPEID"].ToString(),
                                    ITEM_TYPE = reader["ITEM_TYPE"].ToString(),
                                    CATEGORYID = reader["CATEGORYID"].ToString(),
                                    CATEGORY_NAME = reader["CATEGORY_NAME"].ToString(),
                                    ITEMID = reader["ITEMID"].ToString(),
                                    ITEMNAME = reader["ITEMNAME"].ToString(),
                                    ORDERTABLE = reader["ORDERTABLE"].ToString(),
                                    STATUS = reader["STATUS"].ToString()
                                };

                                Recommendations.Add(re);
                            }
                        }
                    }
                    if (rows == -1)
                    {
                        jsonString = JsonConvert.SerializeObject(Recommendations);
                    }
                    else
                    {
                        if (count != rows)
                            jsonString = JsonConvert.SerializeObject(Recommendations);
                        else
                            jsonString = JsonConvert.SerializeObject("-1");
                    }


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
