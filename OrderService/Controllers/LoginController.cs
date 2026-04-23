using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.PostPaymentRequest;
using BCrypt.Net;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]

    public class LoginController : Controller
    {
        [HttpGet(Name = "PostLogin")]
        public async Task<JsonResult> Login([FromQuery] string username, [FromQuery] string password)
        {
            string userid = "";
            string status = "false";
            string companyID = "1";
            int numbersOfTables = 0;
            string hashedPasswordFromDB = "";
            string hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                     
                    string query = @"SELECT u.ID, u.PASSWORD, NVL(c.NUMOFTABLES,0) NUMOFTABLES, c.COMPANYID 
                             FROM ORDERB_USERS u, ORDERB_COMPANYINFO c 
                             WHERE u.USERNAME = :pi_username 
                             AND c.COMPANYID = u.COMPANYID";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_username", username));

                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                userid = reader["ID"].ToString();
                                hashedPasswordFromDB = reader["PASSWORD"].ToString();
                                numbersOfTables = Int32.Parse(reader["NUMOFTABLES"].ToString());
                                companyID = reader["COMPANYID"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // log ex
            }
             
            if (!string.IsNullOrEmpty(hashedPasswordFromDB) &&
                BCrypt.Net.BCrypt.Verify(password, hashedPasswordFromDB))
            {
                status = "true";
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