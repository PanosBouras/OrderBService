using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.GetOrderItemsController;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetRecommendations : Controller
    {
        public class Recommendations {
            public String ItemID { get; set; }
            public String CategoryId { get; set; }
            public String ItemRecommendationsID { get; set; }
            public String RecommendationDecription { get; set; }
        }

        [HttpGet(Name = "GetRecommendations")]
        public async Task<string> GetRecommendationsAsync(String itemID)
        {
            string jsonString = "";

            List<Recommendations> Recommendations = new List<Recommendations>();
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT ITEMID,CATEGORYID,ITEMRECOMMENDATIONSID,RECOMMENDATIONDECRIPTION FROM ORDERB_RECOMMENDATIONS WHERE ITEMID = :pi_itemid";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_itemid", itemID));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                Recommendations re = new Recommendations
                                {
                                    ItemID = reader["ITEMID"].ToString(),
                                    CategoryId = reader["CATEGORYID"].ToString(),
                                    ItemRecommendationsID = reader["ITEMRECOMMENDATIONSID"].ToString(),
                                    RecommendationDecription = reader["RECOMMENDATIONDECRIPTION"].ToString()
                                };

                                Recommendations.Add(re);
                            }
                        }
                    }
                    jsonString = JsonConvert.SerializeObject(Recommendations, Formatting.Indented);

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
