using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Npgsql;

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
        public async Task<bool> PostPaymentRequestAsync(string username, [FromBody] PaymentInfo json)
        {
            UpdateOrderDTL(json, username);

            if (CheckForUpdateOrderHDR(json.OrderId))
            {
                UpdateOrderHDR(json.OrderId, username, json.Cash, json.Card);
                return true;
            }

            return false;
        }

        private void UpdateOrderDTL(PaymentInfo pi, string username)
        {
            try
            {
                string sql = @"UPDATE orderb_orderdtl
                    SET payedflg = 1,
                        payeduser = @username,
                        payeddate = NOW(),
                        price = @price
                    WHERE orderdtlitemisseq = @seq;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                conn.Open();

                foreach (var item in pi.Items)
                {
                    using var cmd = new NpgsqlCommand(sql, conn);
                    cmd.Parameters.AddWithValue("username", username);
                    cmd.Parameters.AddWithValue("price", item.Price);
                    cmd.Parameters.AddWithValue("seq", item.OrderDTLSeq);

                    cmd.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private void UpdateOrderHDR(string orderid, string username, double cash, double card)
        {
            try
            {
                double total = cash + card;

                string sql = @"
                    UPDATE orderb_orderhdr
                    SET statusflg = 1,
                        payeduser = @username,
                        payeddate = NOW(),
                        totalprice = @total,
                        totalcashprice = @cash,
                        totalcardprice = @card
                    WHERE orderid = @orderid;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                conn.Open();

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("username", username);
                cmd.Parameters.AddWithValue("total", total);
                cmd.Parameters.AddWithValue("cash", cash);
                cmd.Parameters.AddWithValue("card", card);
                cmd.Parameters.AddWithValue("orderid", orderid);

                cmd.ExecuteNonQuery();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private bool CheckForUpdateOrderHDR(string orderid)
        {
            try
            {
                string sql = @"
                    SELECT CASE
                        WHEN COUNT(*) FILTER (WHERE payedflg = 1)
                             = COUNT(*) THEN 'X'
                        ELSE NULL
                    END AS paid
                    FROM orderb_orderdtl
                    WHERE orderid = @orderid;
                ";

                using var conn = new NpgsqlConnection(ConnectionString.Value);
                conn.Open();

                using var cmd = new NpgsqlCommand(sql, conn);
                cmd.Parameters.AddWithValue("orderid", orderid);

                var result = cmd.ExecuteScalar()?.ToString();

                return result == "X";
            }
            catch
            {
                return false;
            }
        }
    }
}