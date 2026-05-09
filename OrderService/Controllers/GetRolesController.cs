using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetRolesController : Controller
    {

        public class UserRoles
        {
            public int id { get; set; }
            public string role_name { get; set; }


        }

        [HttpGet(Name = "GetRoles")]
        public async Task<string> GetRolesAsync()
        {
            List<UserRoles> roles = new List<UserRoles>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"select id, role_name from orderb_userroles";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {  

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                UserRoles ui = new UserRoles
                                {
                                    id = Int32.Parse(reader["id"]?.ToString()),
                                    role_name = reader["role_name"]?.ToString()
                                };
                                roles.Add(ui);
                            }
                        }
                    }
                }


                return JsonConvert.SerializeObject(roles, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        } 
}
}
