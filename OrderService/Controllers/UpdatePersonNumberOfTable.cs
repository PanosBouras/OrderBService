using System.Data;
using System.Xml.Linq;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.GetOrderItemsController;
using static OrderService.Controllers.PostCreateOrder;
using static OrderService.Controllers.PostPaymentRequest;

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
                String update = "UPDATE ORDERB_ORDERHDR SET PERSONS = :pi_personNumber WHERE COMPANYID = :pi_companyid AND TABLEID = :pi_tableid ";
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(update, connection))
                {
                    command.Parameters.Add("pi_personNumber", personNumber);
                    command.Parameters.Add("pi_companyid", companyId);
                    command.Parameters.Add("pi_tableid", tableId);
                    command.Connection.Open();
                    try
                    {
                       command.ExecuteNonQuery();
                    }
                    catch(Exception ex2)
                    {
                        return Json(new
                        {
                            success = "ERROR",
                            message = ex2.Message,
                            stack = ex2.StackTrace
                        });
                    }
                    command.Connection.Close();
                }
            }
            catch (Exception ex)
            {
               
            }
            return Json(new
            {
                success = "OK"
            });
        }

    }
}
