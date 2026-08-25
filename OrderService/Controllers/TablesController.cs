using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using OrderService.Hubs;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class TablesController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly IHubContext<TableHub> _hub;

        public TablesController(IConfiguration configuration, IHubContext<TableHub> hub)
        {
            _connectionString = ConnectionString.Value;
            _hub = hub;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<TableDto>>> GetAllTables()
        {
            var tables = new List<TableDto>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string query = "SELECT tableid, status FROM orderb_tables ORDER BY tableid";

            using var command = new NpgsqlCommand(query, connection);
            using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                tables.Add(new TableDto
                {
                    Tableid = (decimal)reader["tableid"],
                    Status = reader["status"] == DBNull.Value ? 0 : (decimal)reader["status"]
                });
            }

            return Ok(tables);
        }

        // ===========================
        // UPDATE SINGLE TABLE
        // ===========================
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateTableStatus(decimal id, [FromBody] UpdateTableStatusRequest request)
        {
            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            string sql = "UPDATE orderb_tables SET status = @status WHERE tableid = @id";

            using var cmd = new NpgsqlCommand(sql, connection);
            cmd.Parameters.AddWithValue("@status", request.Status);
            cmd.Parameters.AddWithValue("@id", id);

            await cmd.ExecuteNonQueryAsync();
             
            await _hub.Clients
                .Group(request.CompanyId.ToString())
                .SendAsync("TableStatusChanged", id.ToString(), request.Status);

            return Ok();
        }

        // ===========================
        // BATCH UPDATE
        // ===========================
        [HttpPut("batch-status")]
        public async Task<IActionResult> UpdateMultipleTablesStatus([FromBody] BatchUpdateRequest request)
        {
            var updates = new Dictionary<string, int>();

            using var connection = new NpgsqlConnection(_connectionString);
            await connection.OpenAsync();

            foreach (var item in request.Items)
            {
                string sql = "UPDATE orderb_tables SET status = @status WHERE tableid = @id";

                using var cmd = new NpgsqlCommand(sql, connection);
                cmd.Parameters.AddWithValue("@status", item.Status);
                cmd.Parameters.AddWithValue("@id", item.TableId);

                await cmd.ExecuteNonQueryAsync();

                updates[item.TableId.ToString()] = item.Status;
            }
             
            await _hub.Clients
                .Group(request.CompanyId.ToString())
                .SendAsync("TablesStatusUpdated", updates);

            return Ok();
        }
    }

    public class TableDto
    {
        public decimal Tableid { get; set; }
        public decimal Status { get; set; }
    }

    public class UpdateTableStatusRequest
    {
        public decimal Status { get; set; }
        public int CompanyId { get; set; }
    }

    public class BatchUpdateRequest
    {
        public int CompanyId { get; set; }
        public List<TableUpdateItem> Items { get; set; }
    }

    public class TableUpdateItem
    {
        public decimal TableId { get; set; }
        public int Status { get; set; }
    }
}