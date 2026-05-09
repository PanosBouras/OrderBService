using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using BCrypt.Net;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class LoginController : Controller
    {
        private string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));
                StringBuilder builder = new StringBuilder();
                foreach (byte b in bytes)
                    builder.Append(b.ToString("x2"));
                return builder.ToString();
            }
        }

        [HttpGet(Name = "PostLogin")]
        public async Task<JsonResult> Login([FromQuery] string username, [FromQuery] string password)
        {
            string userid = "";
            string status = "false";
            string companyID = "";
            string hUsername = "";
            int numbersOfTables = 0;
            string encryptedPasswordFromDB = "";

            try
            {
                await using var connection = new NpgsqlConnection(ConnectionString.Value);
                await connection.OpenAsync();

                string query = @"
            SELECT u.id,
                   u.password,
                   u.h_username,
                   COALESCE(c.numoftables, 0) AS numoftables,
                   c.companyid
            FROM orderb_users u
            JOIN orderb_companyinfo c ON c.companyid = u.companyid
            WHERE u.username = @username
              AND u.active = 1";

                await using var command = new NpgsqlCommand(query, connection);
                command.Parameters.AddWithValue("username", username); // UNHASHED username

                await using var reader = await command.ExecuteReaderAsync();

                if (await reader.ReadAsync())
                {
                    userid = reader["id"].ToString();
                    encryptedPasswordFromDB = reader["password"].ToString();
                    hUsername = reader["h_username"].ToString();
                    numbersOfTables = Convert.ToInt32(reader["numoftables"]);
                    companyID = reader["companyid"].ToString();
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "false", message = ex.Message });
            }

            string decryptedPassword = "";
            if (!string.IsNullOrEmpty(encryptedPasswordFromDB))
                decryptedPassword = AesCrypto.Decrypt(encryptedPasswordFromDB);

            if (password == decryptedPassword)
            {
                status = "true";
            }
            else
            {
                return Json(new { status = "false", message = "Λάθος username ή password." });
            }

            return Json(new
            {
                status,
                companyID,
                username,
                userId = userid,
                totalTables = numbersOfTables
            });
        }
    }
}