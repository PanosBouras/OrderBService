using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.PostPaymentRequest;

namespace OrderService.Controllers
{


    public class Credentials
    {
        public string Username { get; set; }
        public decimal Password { get; set; }

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
        public async Task<string> Login([FromQuery] string username, [FromQuery] string password)
        {

            string key = "orderB";
            Credentials credentials = new Credentials();
            // Hash the password using SHA-256
            string hashedUsername = credentials.ComputeSha256Hash(username);
            string hashedPassword = credentials.ComputeSha256Hash(password);
            string userid = "";
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT ID FROM ORDERB_USERS WHERE H_USERNAME = :pi_username AND PASSWORD = :pi_password";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_username", hashedUsername));
                        command.Parameters.Add(new OracleParameter("pi_password", hashedPassword));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                userid = reader["ID"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

/*
            if (username == "panos" && password == "123")
            {
                return "true";
            }*/
            return userid;
        }

    }
}                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           