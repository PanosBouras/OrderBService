using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Newtonsoft.Json;
using Npgsql;
using OrderService.Hubs;

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
        private class OrderInfo
        {
            public int TableId { get; set; }
            public int CompanyId { get; set; }
        }
        public class Item
        {
            public string OrderDTLSeq { get; set; }
            public double Price { get; set; }
        }

        private readonly IHubContext<TableHub> _tableHubContext;

        public PostPaymentRequest(
            IHubContext<TableHub> tableHubContext)
        {
            _tableHubContext = tableHubContext;
        }

        [HttpPost(Name = "PostPaymentRequest")]
        public async Task<bool> PostPaymentRequestAsync(string username,[FromBody] PaymentInfo json)
        {
            UpdateOrderDTL(json, username);
            UpdateOrderHDR(json.OrderId,username,json.Cash,json.Card);

            if (CheckForUpdateOrderHDR(json.OrderId))
            {
                var orderInfo = GetOrderInfo(json.OrderId);


                await UpdateTableStatus(orderInfo.CompanyId,orderInfo.TableId,0);

                return true;
            }

            return false;
        }

        private async Task UpdateTableStatus( int companyId,int tableId,int status)
        {
            try
            {
                await using var conn = new NpgsqlConnection(ConnectionString.Value);

                await conn.OpenAsync();

                string sql = @"UPDATE orderb_tables
                                SET status = @status
                                WHERE tableid = @tableid
                                  AND companyid = @companyid";

                await using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("status",status);
                cmd.Parameters.AddWithValue("tableid",tableId);
                cmd.Parameters.AddWithValue("companyid",companyId);

                await cmd.ExecuteNonQueryAsync();

                Console.WriteLine($"Table {tableId} updated to status {status}");

                await _tableHubContext.Clients.Group(companyId.ToString()).SendAsync("ReceiveOrderNewTable",new{tableId,newStatus = status});

                Console.WriteLine($"ReceiveOrderNewTable sent to company {companyId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"UpdateTableStatus: {ex}");
            }
        }

        private OrderInfo GetOrderInfo(string orderid)
        {
            try
            {
                using var conn = new NpgsqlConnection(ConnectionString.Value);

                conn.Open();

                string sql = @" SELECT tableid, companyid FROM orderb_orderhdr WHERE orderid = @orderid";

                using var cmd = new NpgsqlCommand(sql, conn);

                cmd.Parameters.AddWithValue("orderid",orderid);

                using var reader = cmd.ExecuteReader();

                if (reader.Read())
                {
                    return new OrderInfo
                    {
                        TableId = Convert.ToInt32(reader["tableid"]),
                        CompanyId =Convert.ToInt32(reader["companyid"])
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetOrderInfo: {ex}");
            }

            return null;
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
                                    WHERE orderdtlitemisseq = @seq;";

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

                string sql = @"UPDATE orderb_orderhdr
                                SET statusflg = 1,
                                    payeduser = @username,
                                    payeddate = NOW(),
                                    totalprice = @total,
                                    totalcashprice = @cash,
                                    totalcardprice = @card
                                WHERE orderid = @orderid;";

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
                string sql = @"SELECT CASE
                        WHEN COUNT(*) FILTER (WHERE payedflg = 1)
                             = COUNT(*) THEN 'X'
                        ELSE NULL
                    END AS paid
                    FROM orderb_orderdtl
                    WHERE orderid = @orderid;";

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