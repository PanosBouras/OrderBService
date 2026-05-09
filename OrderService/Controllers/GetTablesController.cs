using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetTablesController : Controller
    {
        [HttpGet(Name = "PostTables")]
        public async Task<int> GetTables()
        {
            int tables = 0;

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"SELECT COALESCE(numoftables, 0) AS numoftables
                        FROM orderb_companyinfo
                        LIMIT 1";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            if (await reader.ReadAsync())
                            {
                                int.TryParse(reader["numoftables"]?.ToString(), out tables);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return tables;
        }
    }
}