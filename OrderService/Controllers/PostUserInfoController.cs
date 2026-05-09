using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;
using System;
using System.Globalization;
using System.Reflection;
using static OrderService.Controllers.GetRolesController;
using static OrderService.Controllers.PostCreateOrder;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostUserInfoController : Controller
    {
        public class UserInfo
        {
            public string username { get; set; }
            public string password { get; set; }
            public string id { get; set; }
            public int? userrole { get; set; }
            public int? active { get; set; }
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

        [HttpPost("{companyid}")]
        public async Task<string> PostCreateUserInfoAsync([FromBody] UserInfo userJson)
        { 

            try
            {
                    int insert = 0;
                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                                    INSERT INTO public.orderb_users 
                                    (username, ""password"", userrole, active, status, companyid, substore, positionid, firstname, lastname, birthday, gender, phone)
                                    VALUES
                                    (@username, @password, @userrole, @active, @status, @companyid, @substore, @positionid, @firstname, @lastname, @birthday, @gender, @phone)
                                    ";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        UserInfo ui = new UserInfo();
                        userJson.password = AesCrypto.Encrypt(userJson.password);

                        command.Parameters.AddWithValue("@username", userJson.username ?? "");
                        command.Parameters.AddWithValue("@password", userJson.password ?? "");
                        command.Parameters.AddWithValue("@userrole", userJson.userrole);
                        command.Parameters.AddWithValue("@active", userJson.active);
                        command.Parameters.AddWithValue("@status", userJson.status);
                        command.Parameters.AddWithValue("@companyid", userJson.companyid);
                        command.Parameters.AddWithValue("@substore", userJson.substore);
                        command.Parameters.AddWithValue("@positionid", userJson.positionid);
                        command.Parameters.AddWithValue("@firstname", userJson.firstname ?? "");
                        command.Parameters.AddWithValue("@lastname", userJson.lastname ?? "");
                        command.Parameters.AddWithValue("@birthday", string.IsNullOrEmpty(userJson.birthday) ? (object)DBNull.Value : DateTime.ParseExact(userJson.birthday, "dd/MM/yyyy", CultureInfo.InvariantCulture));
                        command.Parameters.AddWithValue("@gender", userJson.gender);
                        command.Parameters.AddWithValue("@phone", userJson.phone ?? "");
                        insert = await command.ExecuteNonQueryAsync();
                    }
                }
                string status = "0";
                status = insert == 1 ? "true" : "false"; 
                return status;
/*
                return JsonConvert.SerializeObject(user, Formatting.Indented);*/
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }


        [HttpPut("{companyid}/{userid}")]
        public async Task<string> UpdateUser(int companyid, int userid, [FromBody] UserInfo userJson)
        {
            try
            {
                int update = 0;

                await using (NpgsqlConnection connection = new NpgsqlConnection(ConnectionString.Value))
                {
                    await connection.OpenAsync();

                    string query = @"
                UPDATE public.orderb_users
                SET 
                    username = @username,
                    ""password"" = @password,
                    userrole = @userrole,
                    active = @active,
                    status = @status,
                    companyid = @companyid,
                    substore = @substore,
                    positionid = @positionid,
                    firstname = @firstname,
                    lastname = @lastname,
                    birthday = @birthday,
                    gender = @gender,
                    phone = @phone
                WHERE id = @id
            ";

                    await using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        userJson.password = string.IsNullOrEmpty(userJson.password)
                            ? userJson.password
                            : AesCrypto.Encrypt(userJson.password);

                        command.Parameters.AddWithValue("@id", Convert.ToInt32(userJson.id));
                        command.Parameters.AddWithValue("@username", userJson.username ?? "");
                        command.Parameters.AddWithValue("@password", userJson.password ?? "");
                        command.Parameters.AddWithValue("@userrole", userJson.userrole);
                        command.Parameters.AddWithValue("@active", userJson.active);
                        command.Parameters.AddWithValue("@status", userJson.status);
                        command.Parameters.AddWithValue("@companyid", userJson.companyid);
                        command.Parameters.AddWithValue("@substore", userJson.substore);
                        command.Parameters.AddWithValue("@positionid", userJson.positionid);
                        command.Parameters.AddWithValue("@firstname", userJson.firstname ?? "");
                        command.Parameters.AddWithValue("@lastname", userJson.lastname ?? "");
                        DateTime birthday = DateTime.Parse(userJson.birthday, CultureInfo.InvariantCulture);
                        command.Parameters.AddWithValue("@birthday", string.IsNullOrEmpty(userJson.birthday) ? (object)DBNull.Value : DateTime.ParseExact(userJson.birthday, "dd/MM/yyyy", CultureInfo.InvariantCulture));

                        command.Parameters.AddWithValue("@gender", userJson.gender);
                        command.Parameters.AddWithValue("@phone", userJson.phone ?? "");

                        update = await command.ExecuteNonQueryAsync();
                    }
                }

                return update == 1 ? "true" : "false";
            }
            catch (Exception ex)
            {
                return JsonConvert.SerializeObject(new { status = "false", message = ex.Message });
            }
        }

        public static int? GetNullableInt(object value)
        {
            return value == DBNull.Value ? null : Convert.ToInt32(value);
        }
    }
}
