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
    [Route("[controller]")]
    [ApiController]
    public class PostPaymentRequest : ControllerBase
    {

        public class PaymentInfo
        {
            public double Card { get; set; }
            public double Cash { get; set; }
            public Item[] Items { get; set; }
            public string OrderId { get; set; }
        }

        public class Item
        {
            public string OrderDTLSeq { get; set; }
            public double Price { get; set; }
        }


        [HttpPost(Name = "PostPaymentRequest")]
        public async Task<Boolean> PostPaymentRequestAsync(String username, [FromBody] PaymentInfo Json)
        { 
                updateOrderDTL(Json,username); 
            
            if (checkForUpdateOrderHDR(Json.OrderId))
            {
                updateOrderHDR(Json.OrderId,username,Json.Cash,Json.Card);
                return true;
            }
            return false;
        }

        private void updateOrderDTL(PaymentInfo Pi,String username)
        {
            try
            {
                String insrt = @"UPDATE ORDERB_ORDERDTL SET PAYEDFLG = 1 , PAYEDUSER = :pi_username , PAYEDDATE = SYSDATE , PRICE = :pi_price WHERE ORDERDTLITEMISSEQ = :pi_orderdtlitemseq";
             for(int i = 0;i< Pi.Items.Length; i++) {
                    using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                    using (OracleCommand command = new OracleCommand(insrt, connection))
                    {
                        command.Parameters.Add("pi_username", username);
                        command.Parameters.Add("pi_price", Pi.Items[i].Price);
                        command.Parameters.Add("pi_orderdtlitemseq", Pi.Items[i].OrderDTLSeq);

                        command.Connection.Open();
                        int rows = command.ExecuteNonQuery();
                        command.Connection.Close();
                    }
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }
        private void updateOrderHDR(String orderid,String username,double cash, double card)
        {
            try
            {
                double totalprice = 0;
                totalprice = cash + card;
                String insrt = @"UPDATE ORDERB_ORDERHDR SET STATUSFLG = 1 , PAYEDUSER = :pi_username , PAYEDDATE = SYSDATE , TOTALPRICE = :pi_totalprice ,TOTALCASHPRICE = :pi_totalcash , TOTALCARDPRICE = :pi_totalcard WHERE ORDERID = :pi_orderid"; 
                    using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                    using (OracleCommand command = new OracleCommand(insrt, connection))
                    {
                        command.Parameters.Add("pi_username", username); 
                        command.Parameters.Add("pi_totalprice", totalprice);
                        command.Parameters.Add("pi_totalcash", cash); 
                        command.Parameters.Add("pi_totalcard", card);
                        command.Parameters.Add("pi_orderid", orderid);

                    command.Connection.Open();
                        int rows = command.ExecuteNonQuery();
                        command.Connection.Close();
                    } 
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

        private bool checkForUpdateOrderHDR(String orderid)
        {
            String haspaid = "";
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT 'X' AS PAID
                                            FROM DUAL
                                            WHERE (SELECT COUNT(*) 
                                                   FROM ORDERB_ORDERDTL 
                                                   WHERE ORDERID = :pi_orderid AND PAYEDFLG = 1) 
                                                  = (SELECT COUNT(*) 
                                                     FROM ORDERB_ORDERDTL 
                                                     WHERE ORDERID = :pi_orderid)";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_orderid", orderid));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                haspaid = reader["PAID"].ToString(); 
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                return false;

            }
            if (haspaid=="X")
                return true;
            return false;
        }
    }
}
