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
        public async Task PostCreateOrderAsync(int tableId, [FromBody] List<Orderitems> orderJson)
        {
            String orderid = "";
            orderid = gtOrderid(tableId);

            if (String.IsNullOrEmpty(orderid))// An dld den yparxei kapoio status 0 (ara energo trapezi gia to sygkekrimeno trapezi) ftiakse nea paraggelia sto trapezi
            {
                String insrt = "INSERT INTO ORDERB_ORDERHDR(TABLEID,CREATEDATE,STATUSFLG) VALUES (:pi_tableid,sysdate,0)";
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(insrt, connection))
                {
                    command.Parameters.Add("pi_tableid", tableId);
                    command.Connection.Open();
                    command.ExecuteNonQuery();
                    command.Connection.Close();
                }
                orderid = gtOrderid(tableId);

                DeserilizeToOrderDTL(tableId, orderid, orderJson);
            }
            else//Allios sthn yparxousa paraggelia prosthese nea items
            {
                DeserilizeToOrderDTL(tableId, orderid, orderJson);

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

        private void DeserilizeToOrderDTL(int orderTableID, String orderID, List<Orderitems> items)
        { 
            foreach (var item in items)
            {

                if (item.quantity > 1)
                {
                    for (int i = 0; i < item.quantity; i++)
                    {
                        // Εκτύπωση για κάθε ποσότητα
                        Console.WriteLine($"Item {item.name.Trim()} (ID: {item.itemId}), Quantity: {item.quantity}, Comment: {item.comment}");
                        InsertOrderDTL(orderID, item.itemId, item.comment, orderTableID);
                    }
                }
                else
                {
                    // Αν η ποσότητα είναι 1, απλά εκτυπώνουμε
                    Console.WriteLine($"Item {item.name.Trim()} (ID: {item.itemId}), Quantity: {item.quantity}, Comment: {item.comment}");
                    InsertOrderDTL(orderID, item.itemId, item.comment, orderTableID);

                }
            }
        }

        private void InsertOrderDTL(String orderid, String itemid, String comments, int ordertable)
        {
            String itemname = "";
            double itemprice = 0.0;
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

            try
            {
                String insrt = @"INSERT INTO ORDERB_ORDERDTL (ORDERID,ORDERITEMID,ORDERITEMNAME,ORDERITEMDESCRIPTION,PAYEDFLG,DELETEDFLG,PRICE,ORDERTABLE)
                                                     VALUES(:pi_orderid,:pi_itemid, :pi_itemname , :pi_comments, null,0, :pi_itemprice, :pi_ordertable )";
                using (OracleConnection connection = new OracleConnection(ConnectionString.Value))
                using (OracleCommand command = new OracleCommand(insrt, connection))
                {
                    command.Parameters.Add("pi_orderid", orderid);
                    command.Parameters.Add("pi_itemid", itemid);
                    command.Parameters.Add("pi_itemname", itemname);
                    command.Parameters.Add("pi_comments", comments );
                    command.Parameters.Add("pi_itemprice", itemprice);
                    command.Parameters.Add("pi_ordertable", ordertable);
                    command.Connection.Open();
                    int rows = command.ExecuteNonQuery();
                    command.Connection.Close();
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

    }
}
