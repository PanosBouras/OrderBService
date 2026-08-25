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

            public string Value { get; set; }
            public List<RecommendationItem> Recommendations { get; set; } = new();
        }

        public class RecommendationItem
        {
            public string ItemId { get; set; }
            public string CategoryId { get; set; }
            public string ItemRecommendationsId { get; set; }
            public string RecommendationDescription { get; set; }
            public string CompanyId { get; set; }
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
            Dictionary<string, DrinkItem> drinks = new();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT 
                    i.itemid,
                    i.itemname AS name,
                    i.price,
                    i.itemcategoryid AS categoryid,
                    i.itemdescription,
                    i.value,

                    r.itemid AS rec_itemid,
                    r.categoryid AS rec_categoryid,
                    r.itemrecommendationsid,
                    r.recommendationdecription,
                    r.companyid AS rec_companyid,
                    r.price AS rec_price

                FROM orderb_item i
                LEFT JOIN orderb_recommendations r
                    ON i.itemid = r.itemid

                WHERE i.itemtypeid = 2
                AND i.companyid = @companyid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string itemId = reader["itemid"]?.ToString();

                                if (!drinks.ContainsKey(itemId))
                                {
                                    drinks[itemId] = new DrinkItem
                                    {
                                        Id = itemId,
                                        Name = reader["name"]?.ToString(),
                                        CategoryId = reader["categoryid"]?.ToString(),
                                        ItemDescription = reader["itemdescription"]?.ToString(),
                                        Price = reader["price"]?.ToString(),
                                        Value = reader["value"]?.ToString(),
                                    };
                                }

                                // recommendation
                                if (reader["itemrecommendationsid"] != DBNull.Value)
                                {
                                    drinks[itemId].Recommendations.Add(new RecommendationItem
                                    {
                                        ItemId = reader["rec_itemid"]?.ToString(),
                                        CategoryId = reader["rec_categoryid"]?.ToString(),
                                        ItemRecommendationsId = reader["itemrecommendationsid"]?.ToString(),
                                        RecommendationDescription = reader["recommendationdecription"]?.ToString(),
                                        CompanyId = reader["rec_companyid"]?.ToString(),
                                        Price = reader["rec_price"]?.ToString()
                                    });
                                }
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(drinks.Values, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return JsonConvert.SerializeObject(new
                {
                    status = "false",
                    message = ex.Message
                });
            }
        }

        [HttpGet("GetDrinkItemWithRecommendations")]
        public async Task<string> GetWithRecommendations(int companyid)
        {
            try
            {
                Dictionary<string, DrinkItem> items = new();

                await using (NpgsqlConnection connection =
                    new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                SELECT 
                    i.itemid,
                    i.itemname AS name,
                    i.price,
                    i.itemcategoryid AS categoryid,
                    i.itemdescription,
                    i.value,

                    r.itemid AS rec_itemid,
                    r.categoryid AS rec_categoryid,
                    r.itemrecommendationsid,
                    r.recommendationdecription,
                    r.companyid AS rec_companyid,
                    r.price AS rec_price

                FROM orderb_item i
                LEFT JOIN orderb_recommendations r
                    ON i.itemid = r.itemid
                WHERE i.itemtypeid = 2
                  AND i.companyid = @companyid
                ORDER BY i.itemid";

                    await using (NpgsqlCommand command =
                        new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("companyid", companyid);

                        await using (NpgsqlDataReader reader =
                            await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                string itemId = reader["itemid"]?.ToString();

                                if (!items.ContainsKey(itemId))
                                {
                                    items[itemId] = new DrinkItem
                                    {
                                        Id = itemId,
                                        Name = reader["name"]?.ToString(),
                                        CategoryId = reader["categoryid"]?.ToString(),
                                        ItemDescription = reader["itemdescription"]?.ToString(),
                                        Price = reader["price"]?.ToString(),
                                        Value = reader["value"]?.ToString(),
                                        Recommendations = new List<RecommendationItem>()
                                    };
                                }

                                if (reader["itemrecommendationsid"] != DBNull.Value)
                                {
                                    string recId = reader["itemrecommendationsid"]?.ToString();

                                    bool exists = items[itemId]
                                        .Recommendations
                                        .Any(r => r.ItemRecommendationsId == recId);

                                    if (!exists)
                                    {
                                        items[itemId].Recommendations.Add(
                                            new RecommendationItem
                                            {
                                                ItemId = reader["rec_itemid"]?.ToString(),
                                                CategoryId = reader["rec_categoryid"]?.ToString(),
                                                ItemRecommendationsId = recId,
                                                RecommendationDescription =
                                                    reader["recommendationdecription"]?.ToString(),
                                                CompanyId = reader["rec_companyid"]?.ToString(),
                                                Price = reader["rec_price"]?.ToString()
                                            }
                                        );
                                    }
                                }
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(items.Values, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);

                return JsonConvert.SerializeObject(new
                {
                    status = "false",
                    message = ex.Message
                });
            }
        }
    }
}