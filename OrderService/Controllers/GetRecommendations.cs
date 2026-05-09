using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetRecommendations : Controller
    {
        public class Recommendations
        {
            public string ItemID { get; set; }
            public string CategoryId { get; set; }
            public string ItemRecommendationsID { get; set; }
            public string RecommendationDecription { get; set; }
            public string RecommendationPrice { get; set; }
        }

        [HttpGet(Name = "GetRecommendations")]
        public async Task<string> GetRecommendationsAsync(string itemID)
        {
            List<Recommendations> recommendationsList = new List<Recommendations>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT 
                            itemid,
                            categoryid,
                            itemrecommendationsid,
                            recommendationdecription,
                            price
                        FROM orderb_recommendations
                        WHERE itemid = @pi_itemid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_itemid", itemID);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Recommendations re = new Recommendations
                                {
                                    ItemID = reader["itemid"]?.ToString(),
                                    CategoryId = reader["categoryid"]?.ToString(),
                                    ItemRecommendationsID = reader["itemrecommendationsid"]?.ToString(),
                                    RecommendationDecription = reader["recommendationdecription"]?.ToString(),
                                    RecommendationPrice = reader["price"]?.ToString()
                                };

                                recommendationsList.Add(re);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(recommendationsList, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }
    }
}