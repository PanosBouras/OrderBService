using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using BCrypt.Net;

namespace OrderService.Controllers
{
    public class RegisterRequest
    {
        public string Username { get; set; }
        public string Password { get; set; }
        public int UserRole { get; set; }
        public int CompanyId { get; set; }
        public int SubStore { get; set; }
        public int PositionId { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public DateTime? Birthday { get; set; }
        public int Gender { get; set; }
        public string Phone { get; set; }
    }

    [ApiController]
    [Route("[controller]")]
    public class RegisterUserController : Controller
    {
        private readonly string _connectionString = ConnectionString.Value;

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

        [HttpPost(Name = "PostRegisterUser")]
        public async Task<JsonResult> RegisterUser([FromBody] RegisterRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
                return Json(new { status = "false", message = "Username και Password είναι υποχρεωτικά." });

          //  if (request.Password.Length < 6)
           //     return Json(new { status = "false", message = "Το password πρέπει να έχει τουλάχιστον 6 χαρακτήρες." });

            //string hashedUsername = ComputeSha256Hash(request.Username);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            try
            {
                await using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync();

                // 1. Check duplicate username
                string checkQuery = @"SELECT COUNT(*) FROM ORDERB_USERS WHERE USERNAME = @username";

                await using (var checkCmd = new NpgsqlCommand(checkQuery, connection))
                {
                    checkCmd.Parameters.AddWithValue("username", request.Username);

                    int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());

                    if (count > 0)
                        return Json(new { status = "false", message = "Το username υπάρχει ήδη." });
                }

                // 2. Get new ID
                string idQuery = @"SELECT COALESCE(MAX(ID), 0) + 1 FROM ORDERB_USERS";

                int newId;
                await using (var idCmd = new NpgsqlCommand(idQuery, connection))
                {
                    newId = Convert.ToInt32(await idCmd.ExecuteScalarAsync());
                }

                // 3. Insert user
                string insertQuery = @"
                    INSERT INTO ORDERB_USERS
                    (
                        ID, USERNAME, PASSWORD, USERROLE, ACTIVE, STATUS,
                        COMPANYID, SUBSTORE, H_USERNAME, POSITIONID,
                        FIRSTNAME, LASTNAME, BIRTHDAY, GENDER, PHONE
                    )
                    VALUES
                    (
                        @id, @username, @password, @userrole, @active, @status,
                        @companyid, @substore, @h_username, @positionid,
                        @firstname, @lastname, @birthday, @gender, @phone
                    )";

                await using (var insertCmd = new NpgsqlCommand(insertQuery, connection))
                {
                    insertCmd.Parameters.AddWithValue("id", newId);
                    insertCmd.Parameters.AddWithValue("username", request.Username);
                    insertCmd.Parameters.AddWithValue("password", hashedPassword);
                    insertCmd.Parameters.AddWithValue("userrole", request.UserRole);
                    insertCmd.Parameters.AddWithValue("active", 1);
                    insertCmd.Parameters.AddWithValue("status", 1);
                    insertCmd.Parameters.AddWithValue("companyid", request.CompanyId);
                    insertCmd.Parameters.AddWithValue("substore", request.SubStore);
                    insertCmd.Parameters.AddWithValue("h_username", request.Username);
                    insertCmd.Parameters.AddWithValue("positionid", request.PositionId);
                    insertCmd.Parameters.AddWithValue("firstname", request.FirstName);
                    insertCmd.Parameters.AddWithValue("lastname", request.LastName);
                    insertCmd.Parameters.AddWithValue("birthday",
                        request.Birthday.HasValue ? request.Birthday.Value : (object)DBNull.Value);
                    insertCmd.Parameters.AddWithValue("gender", request.Gender);
                    insertCmd.Parameters.AddWithValue("phone", request.Phone ?? "");

                    await insertCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                return Json(new { status = "false", message = "Database error: " + ex.Message });
            }

            return Json(new { status = "true", message = "Ο χρήστης δημιουργήθηκε επιτυχώς." });
        }
    }
}