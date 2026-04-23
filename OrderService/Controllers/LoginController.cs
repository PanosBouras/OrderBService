using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
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
            string hashedPasswordFromDB = "";
             
            string hashedUsername = ComputeSha256Hash(username);

            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                     
                    string query = @"
                        SELECT u.ID, u.PASSWORD, u.H_USERNAME,
                               NVL(c.NUMOFTABLES, 0) AS NUMOFTABLES,
                               c.COMPANYID
                        FROM   ORDERB_USERS u
                               JOIN ORDERB_COMPANYINFO c ON c.COMPANYID = u.COMPANYID
                        WHERE  u.USERNAME = :pi_username
                        AND    u.ACTIVE   = 1";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_username", hashedUsername));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userid = reader["ID"].ToString();
                                hashedPasswordFromDB = reader["PASSWORD"].ToString();
                                hUsername = reader["H_USERNAME"].ToString();
                                numbersOfTables = Convert.ToInt32(reader["NUMOFTABLES"]);
                                companyID = reader["COMPANYID"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "false", message = "Database error: " + ex.Message });
            }
             
            if (!string.IsNullOrEmpty(hashedPasswordFromDB) &&
                BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDB))
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
                username = username,
                userId = userid,
                totalTables = numbersOfTables
            });
        }
    }
}