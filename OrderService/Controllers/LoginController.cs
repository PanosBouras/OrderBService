using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.PostPaymentRequest;
using BCrypt.Net;

namespace OrderService.Controllers
{


    public class Credentials
    {
        public string Username { get; set; }
        public string Password { get; set; }

        public string ComputeSha256Hash(string rawData)
        {
            using (SHA256 sha256Hash = SHA256.Create())
            {
                // Compute the hash
                byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(rawData));

                // Convert byte array to hex string
                StringBuilder builder = new StringBuilder();
                foreach (byte byteValue in bytes)
                {
                    builder.Append(byteValue.ToString("x2"));
                }

                return builder.ToString();
            }
        }
    }


    [ApiController]
    [Route("[controller]")]

    public class LoginController : Controller
    {
        [HttpGet(Name = "PostLogin")]
        public async Task<JsonResult> Login([FromQuery] string username, [FromQuery] string password)
        {

            string key = "orderB";
            Credentials credentials = new Credentials();
            // Hash the password using SHA-256
            //string hashedUsername = credentials.ComputeSha256Hash(username);

            string hashedUsername = BCrypt.Net.BCrypt.HashPassword(username);
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            bool isValidUsername = BCrypt.Net.BCrypt.Verify(username, hashedUsername);
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, hashedPassword);
            string userid = "";
            String status = "false";
            String companyID = "1";
            int numbersOfTables = 0;
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT u.ID, NVL(c.NUMOFTABLES,0) NUMOFTABLES, c.COMPANYID FROM ORDERB_USERS u , ORDERB_COMPANYINFO c WHERE u.USERNAME = :pi_username AND u.PASSWORD = :pi_password AND c.COMPANYID = u.COMPANYID";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_username", username));
                        command.Parameters.Add(new OracleParameter("pi_password", hashedPassword));
                        // command.Parameters.Add(new OracleParameter("pi_password", hashedPassword));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userid = reader["ID"].ToString();
                                numbersOfTables = Int32.Parse(reader["NUMOFTABLES"].ToString());
                                companyID = reader["COMPANYID"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            if (isValidUsername && isValidPassword)
            {
                status = "true";
            }
/*
            if (username == "panos" && password == "123")
            {
                status = "true";
            }*/
            return Json(new
                {
                    status = status,
                    companyID = companyID,
                    username = username,
                    userId = userid,
                    TotalTables = numbersOfTables,
                    debug_username = username,
                    debug_password = password,
                    hashedUsername,
                    hashedPassword
                });

        }

    }
}