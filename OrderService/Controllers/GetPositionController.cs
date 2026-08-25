using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetPositionController : Controller
    {

        public class UserPotitions
        {
            public int positionid { get; set; }
            public string potitionname { get; set; }


        }

        [HttpGet(Name = "GetPosition")]
        public async Task<string> GetPositionAsync()
        {
            List<UserPotitions> positions = new List<UserPotitions>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"select positionid,positionname from orderb_userposition";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                UserPotitions pos = new UserPotitions
                                {
                                    positionid = Int32.Parse(reader["positionid"]?.ToString()),
                                    potitionname = reader["positionname"]?.ToString()
                                };
                                positions.Add(pos);
                            }
                        }
                    }
                }


                return JsonConvert.SerializeObject(positions, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }
    }
}
