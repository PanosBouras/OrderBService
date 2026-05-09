using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UpdatePersonNumberOfTable : Controller
    {
        [HttpPost(Name = "UpdatePersonNumberOfTable")]
        public async Task<JsonResult> UpdatePersonNumberOfTableAsync(int tableId, int companyId, int personNumber)
        {
            try
            {
                string sql = @"
                    UPDATE orderb_orderhdr
                    SET persons = @persons
                    WHERE companyid = @companyid
                      AND tableid = @tableid;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                await conn.OpenAsync();

                using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("persons", personNumber);
                cmd.Parameters.AddWithValue("companyid", companyId);
                cmd.Parameters.AddWithValue("tableid", tableId);

                await cmd.ExecuteNonQueryAsync();

                return Json(new
                {
                    success = "OK"
                });
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = "ERROR",
                    message = ex.Message,
                    stack = ex.StackTrace
                });
            }
        }
    }
}