using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Npgsql;
using OrderService.Hubs;
using System.ComponentModel.Design;

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
            public string ORDERDTLITEMISSEQ { get; set; }
            public string ORDERITEMDESCRIPTION { get; set; }
            public string TYPEID { get; set; }
            public string ITEM_TYPE { get; set; }
            public string CATEGORYID { get; set; }
            public string CATEGORY_NAME { get; set; }
            public string ITEMID { get; set; }
            public string ITEMNAME { get; set; }
            public string ORDERTABLE { get; set; }
            public string STATUS { get; set; }
        }

        [HttpGet(Name = "GetShowOrdersController")]
        public async Task<string> GetShowOrdersControllerAsync(string companyID, int rows)
        {
            List<Orders> ordersList = new List<Orders>();
            int count = 0;

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT
                            od.orderdtlitemisseq,
                            od.orderitemdescription,
                            it.typeid,
                            it.typename AS item_type,
                            ic.categoryid,
                            ic.categoryname AS category_name,
                            i.itemid,
                            i.itemname,
                            od.ordertable,
                            od.status
                        FROM orderb_orderdtl od
                        JOIN orderb_item i
                            ON i.itemid = od.orderitemid
                        JOIN orderb_item_category ic
                            ON ic.categoryid = i.itemcategoryid
                           AND ic.typeid = i.itemtypeid
                        JOIN orderb_items_types it
                            ON it.typeid = i.itemtypeid
                        WHERE od.payedflg IS NULL
                          AND od.deletedflg = 0
                          AND od.status != 3
                          AND od.createdate BETWEEN NOW() - INTERVAL '1 day' AND NOW()
                          AND od.companyid = @pi_companyid
                        GROUP BY
                            od.orderdtlitemisseq,
                            od.orderitemdescription,
                            it.typeid,
                            it.typename,
                            ic.categoryid,
                            ic.categoryname,
                            i.itemid,
                            i.itemname,
                            od.ordertable,
                            od.status
                        ORDER BY
                            it.typeid,
                            ic.categoryid,
                            i.itemname";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_companyid", int.Parse(companyID));

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                count++;

                                Orders re = new Orders
                                {
                                    ORDERDTLITEMISSEQ = reader["orderdtlitemisseq"]?.ToString(),
                                    ORDERITEMDESCRIPTION = reader["orderitemdescription"]?.ToString(),
                                    TYPEID = reader["typeid"]?.ToString(),
                                    ITEM_TYPE = reader["item_type"]?.ToString(),
                                    CATEGORYID = reader["categoryid"]?.ToString(),
                                    CATEGORY_NAME = reader["category_name"]?.ToString(),
                                    ITEMID = reader["itemid"]?.ToString(),
                                    ITEMNAME = reader["itemname"]?.ToString(),
                                    ORDERTABLE = reader["ordertable"]?.ToString(),
                                    STATUS = reader["status"]?.ToString()
                                };

                                ordersList.Add(re);
                            }
                        }
                    }
                }

                if (rows == -1)
                {
                    return JsonConvert.SerializeObject(ordersList);
                }
                else
                {
                    if (count != rows)
                        return JsonConvert.SerializeObject(ordersList);
                    else
                        return JsonConvert.SerializeObject("-1");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }
    }
}