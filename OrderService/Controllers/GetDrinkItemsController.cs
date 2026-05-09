using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;
using System.ComponentModel.Design;
using static OrderService.Controllers.GetDrinkItemsController;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetDrinkItemsController : Controller
    {
        public class DrinkItem
        {
            public string Id { get; set; }
            public string Name { get; set; }
            public string CategoryId { get; set; }
            public string SortOrder { get; set; }
            public string ItemDescription { get; set; }
            public string Price { get; set; }
        }

        [HttpGet(Name = "GetDrinkItems")]
        public async Task<string> GetDrinkItems()
        {
            List<DrinkItem> orderFoodItems = new List<DrinkItem>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                        SELECT 
                            NULL AS itemid,
                            categoryname AS name,
                            NULL AS price,
                            categoryid,
                            0 AS sort_order,
                            NULL AS itemdescription
                        FROM orderb_item_category
                        WHERE typeid = 2

                        UNION ALL

                        SELECT 
                            itemid,
                            ('   ' || itemname) AS name,
                            price,
                            itemcategoryid AS categoryid,
                            1 AS sort_order,
                            itemdescription AS itemdescription
                        FROM orderb_item
                        WHERE itemtypeid = 2
                          AND activeflg = 1

                        ORDER BY categoryid, sort_order, name";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DrinkItem oi = new DrinkItem
                                {
                                    Id = reader["itemid"]?.ToString(),
                                    Name = reader["name"]?.ToString(),
                                    CategoryId = reader["categoryid"]?.ToString(),
                                    SortOrder = reader["sort_order"]?.ToString(),
                                    ItemDescription = reader["itemdescription"]?.ToString(),
                                    Price = reader["price"]?.ToString()
                                };

                                orderFoodItems.Add(oi);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(orderFoodItems, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }

        [HttpGet("GetAll")]
        public async Task<string> GetAllDrinkItems(int companyid)
        {
            List<DrinkItem> orderFoodItems = new List<DrinkItem>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT 
                            itemid,
                            itemname AS name,
                            price,
                            itemcategoryid AS categoryid,
                            itemdescription AS itemdescription
                        FROM orderb_item
                        WHERE itemtypeid = 2
                        and  companyid = @companyid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                DrinkItem oi = new DrinkItem
                                {
                                    Id = reader["itemid"]?.ToString(),
                                    Name = reader["name"]?.ToString(),
                                    CategoryId = reader["categoryid"]?.ToString(),
                                    ItemDescription = reader["itemdescription"]?.ToString(),
                                    Price = reader["price"]?.ToString()
                                };

                                orderFoodItems.Add(oi);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(orderFoodItems, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }
    }
}