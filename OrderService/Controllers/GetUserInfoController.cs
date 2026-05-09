using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class GetUserInfoController : Controller
    {
        public class UserInfo
        {
            public string username { get; set; }
            public string password { get; set; }
            public string id { get; set; }
            public int? userrole { get; set; }
            public int?  active { get; set; }
            public int? status { get; set; }
            public int? companyid { get; set; }
            public int? substore { get; set; }
            public int? positionid { get; set; }
            public string firstname { get; set; }
            public string lastname { get; set; }
            public string birthday { get; set; }
            public int? gender { get; set; }
            public string phone { get; set; }


        }

        [HttpGet(Name = "GetUserInfo")]
        public async Task<string> GetUserInfoAsync(int companyid, int userid)
        {
            List<UserInfo> user = new List<UserInfo>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"select id,
                                        username,
                                        password,
                                        userrole,
                                        active,
                                        status,
                                        companyid,
                                        substore,
                                        positionid,
                                        firstname,
                                        lastname,
                                        birthday,
                                        gender,
                                        phone from orderb_users where id = @pi_id and companyid = @pi_companyid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_id", userid);
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                UserInfo ui = new UserInfo
                                {
                                    username = reader["username"]?.ToString(),
                                    password = reader["password"]?.ToString(),
                                    id = reader["id"]?.ToString(),
                                    userrole = GetNullableInt(reader["userrole"]),
                                    active = GetNullableInt(reader["active"]),
                                    status = GetNullableInt(reader["status"]),
                                    companyid = GetNullableInt(reader["companyid"]),
                                    substore = GetNullableInt(reader["substore"]),
                                    positionid = GetNullableInt(reader["positionid"]),
                                    firstname = reader["firstname"]?.ToString(),
                                    lastname = reader["lastname"]?.ToString(),
                                    birthday = reader["birthday"]?.ToString(),
                                    gender = GetNullableInt(reader["gender"]),
                                    phone = reader["phone"]?.ToString()
                                };
                                if (!String.IsNullOrEmpty(ui.password))
                                {
                                    ui.password= AesCrypto.Decrypt(ui.password);
                                }
                                user.Add(ui);
                            }
                        }
                    }
                }
               

                return JsonConvert.SerializeObject(user, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }


        [HttpGet("all")]
        public async Task<string> GetAllUsersInfoAsync(int companyid)
        {
            List<UserInfo> user = new List<UserInfo>();

            try
            {
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"select id,
                                        username,
                                        password,
                                        userrole,
                                        active,
                                        status,
                                        companyid,
                                        substore,
                                        positionid,
                                        firstname,
                                        lastname,
                                        birthday,
                                        gender,
                                        phone from orderb_users where companyid = @pi_companyid";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("pi_companyid", companyid);

                        await using (NpgsqlDataReader reader = await command.ExecuteReaderAsync())
                        {
                            while (await reader.ReadAsync())
                            {
                                UserInfo ui = new UserInfo
                                {
                                    username = reader["username"]?.ToString(),
                                    password = reader["password"]?.ToString(),
                                    id = reader["id"]?.ToString(),
                                    userrole = GetNullableInt(reader["userrole"]),
                                    active = GetNullableInt(reader["active"]),
                                    status = GetNullableInt(reader["status"]),
                                    companyid = GetNullableInt(reader["companyid"]),
                                    substore = GetNullableInt(reader["substore"]),
                                    positionid = GetNullableInt(reader["positionid"]),
                                    firstname = reader["firstname"]?.ToString(),
                                    lastname = reader["lastname"]?.ToString(),
                                    birthday = reader["birthday"]?.ToString(),
                                    gender = GetNullableInt(reader["gender"]),
                                    phone = reader["phone"]?.ToString()
                                };
                                if (!String.IsNullOrEmpty(ui.password))
                                {
                                    ui.password = AesCrypto.Decrypt(ui.password);
                                }
                                user.Add(ui);
                            }
                        }
                    }
                }


                return JsonConvert.SerializeObject(user, Formatting.Indented);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }

        public static int? GetNullableInt(object value)
        {
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }
    }
}
