using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;
using static OrderService.Controllers.GetFoodItemsController;

namespace OrderService.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class GetCategoriesController : ControllerBase
    {
        public class Categories
        {
            public string categoryid { get; set; }
            public string categoryname { get; set; }
            public string typeid { get; set; }
            public string companyid { get; set; }
        }


        [HttpGet(Name = "GetCategories")]
        public async Task<string> GetCategories(int companyid)
        {
            List<Categories> CategoriesList = new List<Categories>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"select categoryid,categoryname,typeid from orderb_item_category where companyid = @pi_companyid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                Categories c = new Categories
                                {
                                    categoryid = reader["categoryid"]?.ToString(),
                                    categoryname = reader["categoryname"]?.ToString(),
                                    typeid = reader["typeid"]?.ToString()
                                };

                                CategoriesList.Add(c);
                            }
                        }
                    }
                }

                return JsonConvert.SerializeObject(CategoriesList, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }

    }
}
