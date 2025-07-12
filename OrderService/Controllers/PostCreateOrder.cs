using System.Data;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Oracle.ManagedDataAccess.Client;
using static OrderService.Controllers.GetOrderItemsController;

namespace OrderService.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class PostCreateOrder : Controller
    {


        public class Orderitems
        {
            public string itemId { get; set; }
            public string name { get; set; }
            public int quantity { get; set; }
            public string comment { get; set; }
        }



        [HttpPost(Name = "PostCreateOrder")]
        public async Task PostCreateOrderAsync(int tableId,String username, String userid, [FromBody] List<Orderitems> orderJson)
        {
            String orderid = "";
            orderid =  gtOrderid(tableId);

            if (String.IsNullOrEmpty(orderid))// An dld den yparxei kapoio status 0 (ara energo trapezi gia to sygkekrimeno trapezi) ftiakse nea paraggelia sto trapezi
            {
                String insrt = "INSERT INTO ORDERB_ORDERHDR(ORDERID,TABLEID,CREATEDATE,STATUSFLG,CREATEUSER) VALUES ((SELECT TO_CHAR(SYSDATE,'ddMMyyHHmiss') FROM DUAL),:pi_tableid,sysdate,0,:pi_username)";
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(insrt, connection))
                {
                    command.Parameters.Add("pi_tableid", tableId);
                    command.Parameters.Add("pi_username", username);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
                orderid = gtOrderid(tableId);

                DeserilizeToOrderDTL(tableId, orderid, orderJson, username, userid);
            }
            else//Allios sthn yparxousa paraggelia prosthese nea items
            {
                DeserilizeToOrderDTL(tableId, orderid, orderJson, username, userid);

            }
        }

        private String gtOrderid(int tableId)
        {
            String orderid = "";
            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
            {
                try
                {
                    connection.Open();
                    string query = @"SELECT ORDERID FROM ORDERB_ORDERHDR WHERE TABLEID = :pi_tableid AND STATUSFLG =0";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_tableid", tableId));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                orderid = reader["ORDERID"].ToString();
                            }
                        }
                    }
                }
                catch (Exception ex)
                {

                }
            }
            return orderid;
        }


        private void DeserilizeToOrderDTL(int orderTableID, String orderID, List<Orderitems> items, String username,String userid)
        { 
            foreach (var item in items)
            {

                if (item.quantity > 1)
                {
                    for (int i = 0; i < item.quantity; i++)
                    {
                        // Εκτύπωση για κάθε ποσότητα
                        Console.WriteLine($"Item {item.name.Trim()} (ID: {item.itemId}), Quantity: {item.quantity}, Comment: {item.comment}");
                        InsertOrderDTL(orderID, item.itemId, item.comment, orderTableID, username, userid);
                    }
                }
                else
                {
                    // Αν η ποσότητα είναι 1, απλά εκτυπώνουμε
                    Console.WriteLine($"Item {item.name.Trim()} (ID: {item.itemId}), Quantity: {item.quantity}, Comment: {item.comment}");
                    InsertOrderDTL(orderID, item.itemId, item.comment, orderTableID, username, userid);

                }
            }
        }

        private async void InsertOrderDTL(String orderid, String itemid, String comments, int ordertable,String username, String userid)
        {
            String itemname = "";
            double itemprice = 0.0;
            String orderItemDTL = "";
            String companyID = "";
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT COMPANYID FROM ORDERB_USERS WHERE ID = :pi_userid";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_userid", userid));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                companyID = reader["COMPANYID"].ToString();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }
            try
            {
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                {
                    connection.Open();
                    string query = @"SELECT ITEMNAME ,PRICE FROM ORDERB_ITEM WHERE ITEMID = :pi_itemid";

                    using (OracleCommand command = new OracleCommand(query, connection))
                    {
                        command.Parameters.Add(new OracleParameter("pi_itemid", itemid));
                        using (OracleDataReader reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                itemname = reader["ITEMNAME"].ToString();
                                itemprice = Double.Parse(reader["PRICE"].ToString());
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {

            }

            /*            try
                        {
                            using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                            {
                                connection.Open();
                                string query = @"SELECT CONCAT(:pi_companyID,CONCAT(ORDERTABLE,CONCAT(ORDERID,COUNT(ORDERID)))) AS ORDERDTLITEMISSEQ  FROM ORDERB_ORDERDTL WHERE ORDERID = :pi_orderid AND ORDERITEMID = :pi_orderitemid GROUP BY ORDERID,ORDERTABLE";

                                using (OracleCommand command = new OracleCommand(query, connection))
                                {
                                    command.Parameters.Add(new OracleParameter("pi_companyID", companyID));
                                    command.Parameters.Add(new OracleParameter("pi_orderid", orderid));
                                    command.Parameters.Add(new OracleParameter("pi_orderitemid", itemid));
                                    using (OracleDataReader reader = command.ExecuteReader())
                                    {
                                        while (reader.Read())
                                        {
                                            orderItemDTL = reader["ORDERDTLITEMISSEQ"].ToString(); 
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {

                        }*/
            //Thread.Sleep(1000);


            DateTime now = DateTime.Now;

            // Εξαγάγουμε τα δευτερόλεπτα και τα χιλιοστά του δευτερολέπτου
            int seconds = now.Second;
            int milliseconds = now.Millisecond;

            // Συνδυάζουμε τα δύο για να πάρουμε μια μοναδική τιμή
            string randomUniqNumber = (seconds.ToString() + milliseconds.ToString()).Substring(0, 5);

            try
            {
                String insrt = @"INSERT INTO ORDERB_ORDERDTL (ORDERID,ORDERITEMID,ORDERITEMNAME,ORDERITEMDESCRIPTION,PAYEDFLG,DELETEDFLG,PRICE,ORDERTABLE,CREATEDUSER,STATUS,ORDERDTLITEMISSEQ,CREATEDATE)
                                                     VALUES(:pi_orderid,:pi_itemid, :pi_itemname , :pi_comments, null,0, :pi_itemprice, :pi_ordertable,:pi_username,1,:pi_orderItemDTL,sysdate )";
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(insrt, connection))
                {
                    command.Parameters.Add("pi_orderid", orderid);
                    command.Parameters.Add("pi_itemid", itemid);
                    command.Parameters.Add("pi_itemname", itemname);
                    command.Parameters.Add("pi_comments", comments );
                    command.Parameters.Add("pi_itemprice", itemprice);
                    command.Parameters.Add("pi_ordertable", ordertable);
                    command.Parameters.Add("pi_username", username);
                    command.Parameters.Add("pi_orderItemDTL", orderid+ randomUniqNumber);
                    command.Connection.Open();
                    int rows = command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

    }
}
