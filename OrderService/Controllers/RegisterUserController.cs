using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
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

            if (request.Password.Length < 6)
                return Json(new { status = "false", message = "Το password πρέπει να έχει τουλάχιστον 6 χαρακτήρες." });
             
            string hashedUsername = ComputeSha256Hash(request.Username); 
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password);

            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                     
                    string checkQuery = "SELECT COUNT(*) FROM ORDERB_USERS WHERE USERNAME = :pi_username";
                    using (OracleCommand checkCmd = new OracleCommand(checkQuery, connection))
                    {
                        checkCmd.Parameters.Add(new OracleParameter("pi_username", hashedUsername));
                        int count = Convert.ToInt32(checkCmd.ExecuteScalar());
                        if (count > 0)
                            return Json(new { status = "false", message = "Το username υπάρχει ήδη." });
                    }
                     
                    string idQuery = "SELECT NVL(MAX(ID), 0) + 1 FROM ORDERB_USERS";
                    int newId;
                    using (OracleCommand idCmd = new OracleCommand(idQuery, connection))
                    {
                        newId = Convert.ToInt32(idCmd.ExecuteScalar());
                    }
                     
                    string insertQuery = @"
                        INSERT INTO ORDERB_USERS 
                            (ID, USERNAME, PASSWORD, USERROLE, ACTIVE, STATUS, COMPANYID, 
                             SUBSTORE, H_USERNAME, POSITIONID, FIRSTNAME, LASTNAME, 
                             BIRTHDAY, GENDER, PHONE)
                        VALUES 
                            (:p_id, :p_username, :p_password, :p_userrole, :p_active, :p_status, :p_companyid,
                             :p_substore, :p_h_username, :p_positionid, :p_firstname, :p_lastname,
                             :p_birthday, :p_gender, :p_phone)";

                    using (OracleCommand insertCmd = new OracleCommand(insertQuery, connection))
                    {
                        insertCmd.Parameters.Add(new OracleParameter("p_id", newId));
                        insertCmd.Parameters.Add(new OracleParameter("p_username", hashedUsername));       // SHA-256
                        insertCmd.Parameters.Add(new OracleParameter("p_password", hashedPassword));       // BCrypt
                        insertCmd.Parameters.Add(new OracleParameter("p_userrole", request.UserRole));
                        insertCmd.Parameters.Add(new OracleParameter("p_active", 1));                    // active by default
                        insertCmd.Parameters.Add(new OracleParameter("p_status", 1));                    // status active
                        insertCmd.Parameters.Add(new OracleParameter("p_companyid", request.CompanyId));
                        insertCmd.Parameters.Add(new OracleParameter("p_substore", request.SubStore));
                        insertCmd.Parameters.Add(new OracleParameter("p_h_username", request.Username));    
                        insertCmd.Parameters.Add(new OracleParameter("p_positionid", request.PositionId));
                        insertCmd.Parameters.Add(new OracleParameter("p_firstname", request.FirstName));
                        insertCmd.Parameters.Add(new OracleParameter("p_lastname", request.LastName));
                        insertCmd.Parameters.Add(new OracleParameter("p_birthday", request.Birthday.HasValue ? (object)request.Birthday.Value : DBNull.Value));
                        insertCmd.Parameters.Add(new OracleParameter("p_gender", request.Gender));
                        insertCmd.Parameters.Add(new OracleParameter("p_phone", request.Phone ?? ""));

                        insertCmd.ExecuteNonQuery();
                    }
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