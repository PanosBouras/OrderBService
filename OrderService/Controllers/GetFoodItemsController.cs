using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.GetFoodItemsController;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetFoodItemsController : Controller
    {

        public class FoodItem
        {
            public string Id { get; set; }
            public String Name { get; set; }
            public String CategoryId { get; set; }
            public String SortOrder { get; set; }
            public String ItemDescription { get; set; }
            public String Price { get; set; }
        }


        [HttpGet(Name = "GetFoodItems")]
        public async Task<string> GetFoodItems()
        {
            string jsonString = "";

            List<FoodItem> orderFoodItems = new List<FoodItem>();
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT 
                                    null as ITEMID,
                                        CAST(CATEGORYNAME AS VARCHAR2(4000)) AS NAME, 
                                        null as PRICE,
                                        CAST(CATEGORYID AS NUMBER) AS CATEGORYID, 
                                        0 AS SORT_ORDER, 
                                        CAST(NULL AS VARCHAR2(4000)) AS ITEMDESCRIPTION
                                    FROM ORDERB_ITEM_CATEGORY
                                    WHERE TYPEID = 1

                                    UNION ALL

                                    SELECT 
                                    ITEMID,
                                        CAST('   ' || ITEMNAME AS VARCHAR2(4000)) AS NAME, 
                                        PRICE,
                                        CAST(ITEMCATEGORYID AS NUMBER) AS CATEGORYID, 
                                        1 AS SORT_ORDER, 
                                        CAST(ITEMDESCRIPTION AS VARCHAR2(4000)) AS ITEMDESCRIPTION
                                    FROM ORDERB_ITEM
                                    WHERE ITEMTYPEID = 1
                                    AND ACTIVEFLG=1
                                    ORDER BY CATEGORYID, SORT_ORDER, NAME";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                FoodItem Oi = new FoodItem
                                {
                                    Id = reader["ITEMID"].ToString(),
                                    Name = reader["NAME"].ToString(),
                                    CategoryId = reader["CATEGORYID"].ToString(),
                                    SortOrder = reader["SORT_ORDER"].ToString(),
                                    ItemDescription = reader["ITEMDESCRIPTION"].ToString(),
                                    Price = reader["PRICE"].ToString()
                                };

                                orderFoodItems.Add(Oi);
                            }
                        }
                    }
                    jsonString = JsonConvert.SerializeObject(orderFoodItems, Formatting.Indented);

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
